using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Core
{
    /// <summary>The real <see cref="IGenerationBoot"/>: the server's registrations and <see cref="ServerManager"/>'s bootstrapper.</summary>
    public sealed class HostedServerBoot : IGenerationBoot
    {
        /// <summary>The NWNX signal that ends the module load.</summary>
        public const string ModuleLoadFinishSignal = "ON_MODULE_LOAD_FINISH";

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

        /// <summary>The server's handling of the module-load signal, as <see cref="HostedSignalHandler"/> runs it for generation 1.</summary>
        public void ResumeAfterModuleLoad()
        {
            NativeInteropManager.ProcessSignal(ModuleLoadFinishSignal);
        }

        /// <inheritdoc cref="ServerBootstrapper.ShutdownHosted"/>
        public void Retire()
        {
            ServerManager.Bootstrapper.ShutdownHosted();
        }
    }
}
