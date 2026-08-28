using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    public interface ICombatEnemyMechanic
    {
        void Initialize(CombatEnemyMechanicContext context);
        void OnPhaseEnter(CombatPhaseDefinition phase, int phaseIndex);
        void OnPhaseExit(CombatPhaseDefinition phase, int phaseIndex);
        void SetPaused(bool paused);
        void Shutdown();
    }

    public readonly struct CombatEnemyMechanicContext
    {
        public CombatEnemyMechanicContext(CombatBoardView board, int sessionVersion)
        {
            Board = board;
            SessionVersion = sessionVersion;
        }
        public CombatBoardView Board { get; }
        public int SessionVersion { get; }
    }

    [DisallowMultipleComponent]
    public sealed class CombatEnemyActor : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform projectileOrigin;
        [SerializeField] private Transform[] projectileAnchors;
        [SerializeField] private Transform vfxAnchor;
        [SerializeField] private Transform damageAnchor;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Graphic[] graphics;
        [SerializeField] private Animator animator;
        [Tooltip("Every assigned component must implement ICombatEnemyMechanic.")]
        [SerializeField] private MonoBehaviour[] mechanicModules;

        [Header("Idle Visual Float")]
        [SerializeField] private bool idleFloatEnabled = true;
        [Tooltip("Vertical amplitude in the visual parent's local units. 1.5 is a subtle UI drift.")]
        [SerializeField, Range(0f, 6f)] private float idleFloatAmplitude = 1.5f;
        [SerializeField, Min(1f)] private float idleFloatPeriod = 3.6f;

        private Transform floatingVisual;
        private float visualRestY;
        private float floatElapsed;
        private bool floatPaused;
        private bool initialized;
        private bool shutdown;

        public Transform VisualRoot => visualRoot;
        public Transform ProjectileOrigin => projectileOrigin != null ? projectileOrigin : transform;
        public Vector3 ProjectileOriginPosition => ProjectileOrigin.position;
        public Transform VfxAnchor => vfxAnchor != null ? vfxAnchor : transform;
        public Transform DamageAnchor => damageAnchor != null ? damageAnchor : VfxAnchor;
        public Renderer[] Renderers => renderers;
        public Graphic[] Graphics => graphics;
        public Animator Animator => animator;

        public void Initialize(CombatEnemyMechanicContext context)
        {
            if (initialized && !shutdown)
                return;
            ResetIdleFloat();
            initialized = true;
            shutdown = false;
            floatPaused = false;
            ForEachMechanic(mechanic => mechanic.Initialize(context));
        }

        public void EnterPhase(CombatPhaseDefinition phase, int phaseIndex)
        {
            if (!shutdown)
            {
                floatPaused = false;
                ForEachMechanic(mechanic => mechanic.OnPhaseEnter(phase, phaseIndex));
            }
        }

        public void ExitPhase(CombatPhaseDefinition phase, int phaseIndex)
        {
            if (!shutdown)
            {
                floatPaused = true;
                ForEachMechanic(mechanic => mechanic.OnPhaseExit(phase, phaseIndex));
            }
        }

        public void SetPaused(bool paused)
        {
            if (!shutdown)
            {
                floatPaused = paused;
                ForEachMechanic(mechanic => mechanic.SetPaused(paused));
            }
        }

        public void Shutdown()
        {
            if (shutdown)
                return;
            shutdown = true;
            ResetIdleFloat();
            ForEachMechanic(mechanic => mechanic.Shutdown());
        }

        private void LateUpdate() => TickIdleFloat(Time.deltaTime);

        private void TickIdleFloat(float deltaTime)
        {
            if (!initialized || shutdown || !isActiveAndEnabled || !idleFloatEnabled ||
                visualRoot == null || visualRoot == transform)
            {
                ResetIdleFloat();
                return;
            }

            if (floatingVisual != visualRoot)
            {
                ResetIdleFloat();
                floatingVisual = visualRoot;
                visualRestY = visualRoot.localPosition.y;
            }
            float period = Mathf.Max(1f, idleFloatPeriod);
            if (!floatPaused)
                floatElapsed = Mathf.Repeat(floatElapsed + Mathf.Max(0f, deltaTime), period);

            // Hit feedback owns X; intro owns scale and victory owns alpha.
            // Reapply only Y after those effects, without accumulating offsets.
            Vector3 position = floatingVisual.localPosition;
            position.y = visualRestY + Mathf.Sin(floatElapsed * (Mathf.PI * 2f / period)) *
                Mathf.Max(0f, idleFloatAmplitude);
            floatingVisual.localPosition = position;
        }

        private void ResetIdleFloat()
        {
            if (floatingVisual != null)
            {
                Vector3 position = floatingVisual.localPosition;
                position.y = visualRestY;
                floatingVisual.localPosition = position;
            }
            floatingVisual = null;
            floatElapsed = 0f;
        }

        private void OnDisable() => ResetIdleFloat();
        private void OnDestroy() => Shutdown();

        private void ForEachMechanic(System.Action<ICombatEnemyMechanic> action)
        {
            if (mechanicModules == null)
                return;
            for (int i = 0; i < mechanicModules.Length; i++)
            {
                MonoBehaviour module = mechanicModules[i];
                if (module is ICombatEnemyMechanic mechanic)
                    action(mechanic);
            }
        }

        private void OnValidate()
        {
            if (mechanicModules == null)
                return;
            for (int i = 0; i < mechanicModules.Length; i++)
            {
                if (mechanicModules[i] != null && !(mechanicModules[i] is ICombatEnemyMechanic))
                    Debug.LogError($"[CombatEnemyActor] '{mechanicModules[i].name}' does not implement ICombatEnemyMechanic.", this);
            }
        }
    }
}
