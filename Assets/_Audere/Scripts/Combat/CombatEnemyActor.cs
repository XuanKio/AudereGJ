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
            initialized = true;
            shutdown = false;
            ForEachMechanic(mechanic => mechanic.Initialize(context));
        }

        public void EnterPhase(CombatPhaseDefinition phase, int phaseIndex)
        {
            if (!shutdown)
                ForEachMechanic(mechanic => mechanic.OnPhaseEnter(phase, phaseIndex));
        }

        public void ExitPhase(CombatPhaseDefinition phase, int phaseIndex)
        {
            if (!shutdown)
                ForEachMechanic(mechanic => mechanic.OnPhaseExit(phase, phaseIndex));
        }

        public void SetPaused(bool paused)
        {
            if (!shutdown)
                ForEachMechanic(mechanic => mechanic.SetPaused(paused));
        }

        public void Shutdown()
        {
            if (shutdown)
                return;
            shutdown = true;
            ForEachMechanic(mechanic => mechanic.Shutdown());
        }

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
