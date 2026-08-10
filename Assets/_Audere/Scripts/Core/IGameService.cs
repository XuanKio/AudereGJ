namespace Audere.Core
{
    /// <summary>
    /// Contract for a global, long-lived game service. The <see cref="Bootstrapper"/>
    /// discovers every IGameService under its services root and calls
    /// <see cref="Initialize"/> once, in a deterministic order, before the first scene load.
    /// </summary>
    public interface IGameService
    {
        /// <summary>Called exactly once by the Bootstrapper during startup.</summary>
        void Initialize();
    }
}
