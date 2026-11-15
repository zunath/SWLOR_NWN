namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// A clean-up a service runs when the server shuts down or a build is retired. <see cref="ServerTeardown"/> takes every
    /// step the container registers and runs them in registration order, so the platform names no service above it (supply
    /// rule 2 of MaintainabilityRefactorDomainOrder.md).
    /// </summary>
    public interface IShutdownStep
    {
        /// <summary>Stops what the service runs and releases what it holds. A step that throws does not stop the later ones.</summary>
        void StopOnShutdown();
    }
}
