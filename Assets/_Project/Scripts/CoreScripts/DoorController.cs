using System.Collections;
using UnityEngine;
using Breakpoint.Core;

namespace Core
{
    /// <summary>
    /// Kapıyı yalnızca diyalog kararıyla açar. Oyuncu açılmış kapının trigger alanından
    /// geçtiğinde kaçış tamamlanır; kapıyı açmak tek başına sahne/oyun geçişi başlatmaz.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class DoorController : MonoBehaviour
    {
        [SerializeField] private AudioClip unlockSound;
        [SerializeField, Min(0.1f)] private float openDuration = 0.7f;
        [SerializeField, Min(0.1f)] private float openDistance = 3.2f;
        [SerializeField] private Color exitGlowColor = new Color(1f, 0.68f, 0.38f);
        [SerializeField, Min(0f)] private float exitGlowIntensity = 3f;
        [SerializeField, Min(0.1f)] private float exitGlowRange = 8f;
        [SerializeField, Min(0.1f)] private float exitGlowFadeDuration = 1.1f;

        private AudioSource audioSource;
        private Collider doorCollider;
        private Light exitGlow;
        private PlayerController playerController;
        private Bounds exitBounds;
        private CharacterController playerCollider;
        private bool isUnlocked;
        private bool escapeRaised;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            doorCollider = GetComponent<Collider>();

            if (doorCollider == null)
            {
                Debug.LogError("DoorController: Kapı Collider bileşeni bulunamadı.", this);
                enabled = false;
                return;
            }

            // Kapı kilitliyken fiziksel engel, açılınca geçiş trigger'ı olarak çalışır.
            doorCollider.isTrigger = false;
            exitBounds = doorCollider.bounds;
            CreateExitGlow();
        }

        private void Update()
        {
            if (!isUnlocked || escapeRaised) return;

            if (playerController == null)
                playerController = FindAnyObjectByType<PlayerController>();

            if (playerCollider == null && playerController != null)
                playerCollider = playerController.GetComponent<CharacterController>();

            if (playerCollider != null && exitBounds.Intersects(playerCollider.bounds))
            {
                escapeRaised = true;
                Debug.Log("Oyuncu çıkış eşiğini geçti; kaçış tamamlandı.", this);
                GameEvents.RaiseEscapeCompleted();
            }
        }

        public void UnlockAndOpenDoor()
        {
            if (!enabled || doorCollider == null || isUnlocked)
                return;

            isUnlocked = true;
            // Kapı görseli hareket eder; çıkış bölgesini başlangıç konumunda sakla.
            // Böylece kapı animasyonu trigger'ı da kaydırıp kaçışı bozmaz.
            doorCollider.enabled = false;

            DoorSceneDressing dressing = FindAnyObjectByType<DoorSceneDressing>();
            if (dressing != null)
                dressing.ShowExitRoute(exitBounds.center);

            if (audioSource != null && unlockSound != null)
                audioSource.PlayOneShot(unlockSound);

            StartCoroutine(SlideDoorOpen());
            StartCoroutine(BringUpExitGlow());
            EmitExitMotes();
            Debug.Log("Kapı açıldı. Kaçışı tamamlamak için çıkıştan geçin.", this);
        }

        private void CreateExitGlow()
        {
            GameObject glowObject = new GameObject("ExitWarmGlow");
            glowObject.transform.position = exitBounds.center + Vector3.forward * 0.55f;

            exitGlow = glowObject.AddComponent<Light>();
            exitGlow.type = LightType.Point;
            exitGlow.color = exitGlowColor;
            exitGlow.intensity = 0f;
            exitGlow.range = exitGlowRange;
            exitGlow.shadows = LightShadows.None;
        }

        private IEnumerator BringUpExitGlow()
        {
            float elapsed = 0f;
            while (elapsed < exitGlowFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / exitGlowFadeDuration);
                float eased = t * t * (3f - 2f * t);
                exitGlow.intensity = Mathf.Lerp(0f, exitGlowIntensity, eased);
                exitGlow.range = Mathf.Lerp(exitGlowRange * 0.65f, exitGlowRange, eased);
                yield return null;
            }

            exitGlow.intensity = exitGlowIntensity;
            exitGlow.range = exitGlowRange;

            // Kısa sıcak ışık vurgusu, açılan çıkışı görsel olarak öne çıkarır.
            const float pulseDuration = 1.2f;
            elapsed = 0f;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float pulse = Mathf.Sin(elapsed / pulseDuration * Mathf.PI * 2f);
                exitGlow.intensity = exitGlowIntensity * (0.9f + pulse * 0.1f);
                yield return null;
            }

            exitGlow.intensity = exitGlowIntensity;
        }

        private void EmitExitMotes()
        {
            GameObject motesObject = new GameObject("ExitLightMotes");
            motesObject.SetActive(false);
            motesObject.transform.position = exitBounds.center + Vector3.forward * 0.45f;

            ParticleSystem motes = motesObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = motes.main;
            main.duration = 0.8f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.42f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.055f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.76f, 0.48f, 0.75f));
            main.gravityModifier = -0.025f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = motes.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)22) });

            ParticleSystem.ShapeModule shape = motes.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(2.1f, 3.1f, 0.08f);

            ParticleSystem.VelocityOverLifetimeModule velocity = motes.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.18f, 0.18f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.08f, 0.28f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.38f, -0.12f);

            ParticleSystem.ColorOverLifetimeModule fade = motes.colorOverLifetime;
            fade.enabled = true;
            Gradient alphaFade = new Gradient();
            alphaFade.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.78f, 0.52f), 0f), new GradientColorKey(new Color(1f, 0.9f, 0.72f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.15f), new GradientAlphaKey(0f, 1f) });
            fade.color = new ParticleSystem.MinMaxGradient(alphaFade);

            ParticleSystemRenderer renderer = motes.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            motesObject.SetActive(true);
            motes.Play();
            Destroy(motesObject, 3f);
        }

        private IEnumerator SlideDoorOpen()
        {
            Vector3 start = transform.localPosition;
            Vector3 end = start + Vector3.right * openDistance;
            float elapsed = 0f;

            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / openDuration);
                float eased = t * t * (3f - 2f * t);
                transform.localPosition = Vector3.LerpUnclamped(start, end, eased);
                yield return null;
            }

            transform.localPosition = end;
        }

    }
}
