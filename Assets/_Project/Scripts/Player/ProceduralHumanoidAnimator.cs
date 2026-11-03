using System.Collections.Generic;
using UnityEngine;

/// <summary>Lightweight pose for the project's unrigged student OBJ mesh.</summary>
public sealed class ProceduralHumanoidAnimator : MonoBehaviour
{
    private sealed class MeshPose
    {
        public Mesh mesh;
        public Vector3[] source;
        public Vector3[] posed;
    }

    [SerializeField, Min(0.1f)] private float walkSpeedForFullCycle = 3.2f;
    private readonly List<MeshPose> meshes = new List<MeshPose>();
    private PlayerController player;
    private float wakeStartTime = -10f;
    private bool isSeated = true;

    public bool IsWakingUp => Time.time - wakeStartTime < 1.25f;

    private void Awake()
    {
        player = GetComponentInParent<PlayerController>();
        foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            try
            {
                Mesh instance = Instantiate(filter.sharedMesh);
                instance.name = filter.sharedMesh.name + " (Runtime Pose)";
                filter.sharedMesh = instance;
                meshes.Add(new MeshPose { mesh = instance, source = instance.vertices, posed = new Vector3[instance.vertexCount] });
            }
            catch (UnityException)
            {
                Debug.LogWarning("Student OBJ is not readable; enable Read/Write in its import settings.", this);
            }
        }
    }

    public void PlayWakeUp()
    {
        isSeated = false;
        wakeStartTime = Time.time;
    }

    private void LateUpdate()
    {
        if (meshes.Count == 0) return;
        float speed = player != null ? player.CurrentPlanarSpeed : 0f;
        float gait = Mathf.Clamp01(speed / walkSpeedForFullCycle);
        float blend = isSeated ? 0f : (IsWakingUp ? Mathf.SmoothStep(0f, 1f, (Time.time - wakeStartTime) / 1.25f) : 1f);
        float stride = Mathf.Sin(Time.time * Mathf.Lerp(1.8f, 8.5f, gait)) * gait;
        bool airborne = player != null && player.IsAirborne;
        float verticalVelocity = player != null ? player.VerticalVelocity : 0f;

        foreach (MeshPose binding in meshes)
        {
            for (int i = 0; i < binding.source.Length; i++)
            {
                Vector3 standing = StandPose(binding.source[i], stride, blend, airborne, verticalVelocity);
                // Procedural stride rotations can pull vertices below the character's
                // local floor plane. Keep grounded soles at the body base on landings.
                if (!isSeated && !airborne && standing.y < 0.018f)
                    standing.y = 0.018f;
                binding.posed[i] = isSeated ? SitPose(binding.source[i]) : standing;
            }
            binding.mesh.vertices = binding.posed;
            binding.mesh.RecalculateBounds();
            binding.mesh.RecalculateNormals();
        }
    }

    // The source OBJ is a single T-pose mesh. Rotate the arms down around the
    // shoulder axis and use a small hip/knee bend for locomotion.
    private static Vector3 StandPose(Vector3 p, float stride, float blend, bool airborne, float verticalVelocity)
    {
        float side = Mathf.Sign(p.x == 0f ? 1f : p.x);
        if (p.y > 1.05f && Mathf.Abs(p.x) > 0.2f)
        {
            Vector3 shoulder = new Vector3(side * 0.24f, 1.43f, 0f);
            float airArmAngle = verticalVelocity > 0f ? 54f : 72f;
            float restingArmAngle = airborne ? airArmAngle : 78f;
            float angle = (side < 0f ? 1f : -1f) * restingArmAngle * blend;
            angle += Mathf.Sin(Time.time * 5f + side * Mathf.PI) * 7f * Mathf.Clamp01(Mathf.Abs(stride));
            p = shoulder + Quaternion.AngleAxis(angle, Vector3.forward) * (p - shoulder);
        }
        if (p.y < 1.02f && Mathf.Abs(p.x) < 0.34f)
        {
            Vector3 hip = new Vector3(side * 0.15f, 0.98f, 0f);
            float jumpTuck = airborne ? 13f : 0f;
            float angle = (-stride * side * 18f + jumpTuck) * blend;
            p = hip + Quaternion.AngleAxis(angle, Vector3.right) * (p - hip);
            if (p.y < 0.52f)
            {
                Vector3 knee = new Vector3(side * 0.15f, 0.52f, 0f);
                p = knee + Quaternion.AngleAxis(Mathf.Max(0f, stride * side) * 12f * blend, Vector3.right) * (p - knee);
            }
        }
        return p;
    }

    // Seat: thighs extend forward from the hips, knees stay above the chair
    // edge, and the shins hang down. This avoids folding legs into the mesh.
    private static Vector3 SitPose(Vector3 source)
    {
        Vector3 p = source;
        float side = Mathf.Sign(source.x == 0f ? 1f : source.x);
        float ax = Mathf.Abs(source.x);
        if (source.y >= 0.48f && source.y <= 1.02f && ax < 0.34f)
        {
            float thigh = Mathf.InverseLerp(0.48f, 1.02f, source.y);
            p.y = Mathf.Lerp(0.5f, 0.98f, thigh);
            p.z += Mathf.Lerp(0.46f, 0f, thigh);
        }
        else if (source.y < 0.48f && ax < 0.34f)
        {
            p.z += 0.46f;
            if (source.y < 0.13f) p.z += Mathf.InverseLerp(0.13f, 0f, source.y) * 0.12f;
        }

        if (source.y > 1.05f && ax > 0.2f)
        {
            Vector3 shoulder = new Vector3(side * 0.24f, 1.43f, 0f);
            p = shoulder + Quaternion.AngleAxis((side < 0f ? 1f : -1f) * 82f, Vector3.forward) * (source - shoulder);
            // Bring the forearms slightly forward so the hands rest over the lap.
            p.z += Mathf.Clamp01((1.5f - source.y) / 0.45f) * 0.15f;
        }
        return p;
    }
}
