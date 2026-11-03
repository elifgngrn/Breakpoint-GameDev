using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adds a relaxed pose and a small walk cycle to the supplied unrigged OBJ mesh.
/// Meshes are cloned at runtime and the imported asset is never modified.
/// </summary>
public sealed class ProceduralHumanoidAnimator : MonoBehaviour
{
    private sealed class MeshPose
    {
        public Mesh mesh;
        public Vector3[] source;
        public Vector3[] posed;
    }

    [SerializeField, Min(0f)] private float walkSpeedForFullCycle = 3.2f;
    private readonly List<MeshPose> meshes = new List<MeshPose>();
    private PlayerController player;
    private float wakeStartTime = -10f;
    private bool warnedUnreadableMesh;

    public bool IsWakingUp => Time.time - wakeStartTime < 1.25f;

    private void Awake()
    {
        player = GetComponentInParent<PlayerController>();
        // Opening beat: begin low at the desk and ease into the relaxed standing pose.
        wakeStartTime = Time.time;
        foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            try
            {
                Mesh instance = Instantiate(filter.sharedMesh);
                instance.name = filter.sharedMesh.name + " (Runtime Pose)";
                filter.sharedMesh = instance;
                meshes.Add(new MeshPose
                {
                    mesh = instance,
                    source = instance.vertices,
                    posed = new Vector3[instance.vertexCount]
                });
            }
            catch (UnityException)
            {
                warnedUnreadableMesh = true;
            }
        }

        if (warnedUnreadableMesh)
            Debug.LogWarning("ProceduralHumanoidAnimator: OBJ mesh is not readable. Enable Read/Write on its import settings.", this);
    }

    public void PlayWakeUp()
    {
        wakeStartTime = Time.time;
    }

    private void LateUpdate()
    {
        if (meshes.Count == 0) return;

        float speed = player != null ? player.CurrentPlanarSpeed : 0f;
        float gait = Mathf.Clamp01(speed / walkSpeedForFullCycle);
        float cycleRate = Mathf.Lerp(1.7f, 8.5f, gait);
        float phase = Time.time * cycleRate;
        float stride = Mathf.Sin(phase) * gait;
        float idleBreath = Mathf.Sin(Time.time * 1.35f) * (1f - gait);
        float wakeBlend = IsWakingUp ? Mathf.SmoothStep(0f, 1f, (Time.time - wakeStartTime) / 1.25f) : 1f;

        foreach (MeshPose binding in meshes)
        {
            Vector3[] source = binding.source;
            Vector3[] posed = binding.posed;
            for (int i = 0; i < source.Length; i++)
                posed[i] = PoseVertex(source[i], stride, idleBreath, gait, wakeBlend);

            binding.mesh.vertices = posed;
            binding.mesh.RecalculateBounds();
            binding.mesh.RecalculateNormals();
        }
    }

    private static Vector3 PoseVertex(Vector3 source, float stride, float idleBreath, float gait, float wakeBlend)
    {
        Vector3 point = source;
        point.y += Mathf.Sin(Time.time * 2f) * (0.012f + 0.022f * gait);

        float crouch = 1f - wakeBlend;
        if (point.y > 0.9f)
        {
            float torsoWeight = Mathf.InverseLerp(0.9f, 1.18f, point.y);
            point.z += torsoWeight * (0.045f * crouch + 0.022f * gait * Mathf.Cos(Time.time * 3f));
            point.y -= torsoWeight * 0.12f * crouch;
        }

        float absX = Mathf.Abs(point.x);
        if (point.y > 1.05f && absX > 0.2f)
        {
            float armWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, 0.42f, absX));
            float side = Mathf.Sign(point.x);
            Vector3 shoulder = new Vector3(side * 0.24f, 1.42f, 0f);
            float swing = Mathf.Sin(Time.time * 5f + side * Mathf.PI) * 0.08f * gait;
            float relax = Mathf.Lerp(0f, 54f, wakeBlend);
            float angle = (side < 0f ? relax : -relax) + swing * Mathf.Rad2Deg + idleBreath * 2.5f;
            Vector3 rotated = shoulder + Quaternion.AngleAxis(angle, Vector3.forward) * (point - shoulder);
            rotated.z += swing * side * 0.16f;
            point = Vector3.Lerp(point, rotated, armWeight);
        }

        if (point.y < 1.02f && absX < 0.31f)
        {
            float side = Mathf.Sign(point.x == 0f ? 1f : point.x);
            Vector3 hip = new Vector3(side * 0.145f, 0.98f, 0f);
            float legWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.025f, 0.095f, absX));
            float swingAngle = -stride * side * 23f * wakeBlend;
            Vector3 rotated = hip + Quaternion.AngleAxis(swingAngle, Vector3.right) * (point - hip);

            if (point.y < 0.55f)
            {
                float kneeWeight = Mathf.InverseLerp(0.55f, 0.12f, point.y);
                float kneeBend = Mathf.Max(0f, stride * side) * 18f * wakeBlend;
                Vector3 knee = new Vector3(side * 0.15f, 0.53f, 0f);
                Vector3 shin = knee + Quaternion.AngleAxis(kneeBend, Vector3.right) * (rotated - knee);
                rotated = Vector3.Lerp(rotated, shin, kneeWeight);
            }

            float wakeKnee = Mathf.InverseLerp(0.55f, 0.05f, point.y) * crouch * 0.18f;
            rotated.z += wakeKnee;
            point = Vector3.Lerp(point, rotated, legWeight);
        }

        return point;
    }
}
