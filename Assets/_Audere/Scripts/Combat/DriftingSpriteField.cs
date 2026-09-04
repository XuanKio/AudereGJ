using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    // An enemy-owned presentation module. It never claims input or modifies shared materials.
    public sealed class DriftingSpriteField : MonoBehaviour, ICombatEnemyMechanic
    {
        [SerializeField] private RawImage image;
        [SerializeField] private Material fieldMaterial;
        [SerializeField] private float[] phaseOpacity = { .55f, .18f };
        private Material runtimeMaterial;
        private float elapsed, opacity, targetOpacity;
        private bool paused, running;
        public bool IsPresenting => running && image != null && image.enabled;
        public float MotionTime => elapsed;
        public void Initialize(CombatEnemyMechanicContext context)
        {
            Shutdown();
            if (image == null || fieldMaterial == null) return;
            runtimeMaterial = new Material(fieldMaterial);
            image.material = runtimeMaterial; image.enabled = true;
            opacity = 0f; elapsed = 0f; paused = false; running = true;
            Apply();
        }
        public void OnPhaseEnter(CombatPhaseDefinition phase, int phaseIndex)
        {
            paused = false;
            targetOpacity = phaseOpacity != null && phaseOpacity.Length > 0
                ? Mathf.Clamp01(phaseOpacity[Mathf.Clamp(phaseIndex, 0, phaseOpacity.Length - 1)]) : .4f;
        }
        public void OnPhaseExit(CombatPhaseDefinition phase, int phaseIndex) => paused = true;
        public void SetPaused(bool value) => paused = value;
        private void Update()
        {
            if (!running || paused) return;
            elapsed += Time.deltaTime;
            opacity = Mathf.MoveTowards(opacity, targetOpacity, Time.deltaTime * .45f);
            Apply();
        }
        private void Apply()
        {
            if (runtimeMaterial == null || image == null) return;
            runtimeMaterial.SetFloat("_MotionTime", elapsed);
            runtimeMaterial.SetFloat("_Opacity", opacity);
            Rect r = image.rectTransform.rect;
            runtimeMaterial.SetFloat("_Aspect", r.width / Mathf.Max(1f, r.height));
        }
        public void Shutdown()
        {
            running = false; paused = false; elapsed = opacity = targetOpacity = 0f;
            if (image != null) { image.enabled = false; image.material = fieldMaterial; }
            if (runtimeMaterial != null)
            {
                if (Application.isPlaying) Destroy(runtimeMaterial); else DestroyImmediate(runtimeMaterial);
                runtimeMaterial = null;
            }
        }
        private void OnDisable() => Shutdown();
        private void OnDestroy() => Shutdown();
    }
}
