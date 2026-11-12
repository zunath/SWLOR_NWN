using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Core
{
    /// <summary>The real <see cref="IGenerationBoot"/>: the server's registrations and <see cref="ServerManager"/>'s bootstrapper.</summary>
    public sealed class HostedServerBoot : IGenerationBoot
    {
        /// <inheritdoc cref="CompositionRoot.ValidateServerServices"/>
        public void Validate()
        {
            CompositionRoot.ValidateServerServices();
        }

        /// <inheritdoc cref="ServerBootstrapper.BootstrapHosted"/>
        public void Activate(HostedBoot boot)
        {
            ServerManager.Bootstrapper.BootstrapHosted(boot);
        }

        /// <inheritdoc cref="ServerBootstrapper.ShutdownHosted"/>
        public void Retire()
        {
            ServerManager.Bootstrapper.ShutdownHosted();
        }
    }
}
