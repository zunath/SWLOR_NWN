using System.Collections.Generic;
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

        /// <summary>The signal table both entries share (<see cref="NativeInteropManager.ProcessSignal"/>).</summary>
        public void ProcessSignal(string signal)
        {
            NativeInteropManager.ProcessSignal(signal);
        }

        /// <inheritdoc cref="ServerBootstrapper.RebuildAfterReload"/>
        public IReadOnlyList<ReloadStepResult> RebuildAfterReload()
        {
            return ServerManager.Bootstrapper.RebuildAfterReload();
        }

        /// <inheritdoc cref="ServerBootstrapper.CarryForReload"/>
        public IReadOnlyList<ReloadStepResult> CarryForReload()
        {
            return ServerManager.Bootstrapper.CarryForReload();
        }

        /// <inheritdoc cref="ServerBootstrapper.QuiescePlayers"/>
        public IReadOnlyList<ReloadStepResult> QuiescePlayers()
        {
            return ServerManager.Bootstrapper.QuiescePlayers();
        }

        /// <inheritdoc cref="ServerBootstrapper.RestorePlayers"/>
        public IReadOnlyList<ReloadStepResult> RestorePlayers()
        {
            return ServerManager.Bootstrapper.RestorePlayers();
        }

        /// <inheritdoc cref="ServerBootstrapper.DescribeCaches"/>
        public IReadOnlyList<ReloadDigestLine> DescribeCaches()
        {
            return ServerManager.Bootstrapper.DescribeCaches();
        }

        /// <inheritdoc cref="ServerBootstrapper.DescribeAdoptedState"/>
        public IReadOnlyList<ReloadDigestLine> DescribeAdoptedState()
        {
            return ServerManager.Bootstrapper.DescribeAdoptedState();
        }

        /// <summary>Schedules <paramref name="action"/> on the server's scheduler, which runs at the start of the next frame.</summary>
        public void RunNextFrame(Action action)
        {
            Scheduler.Schedule(action, TimeSpan.Zero);
        }

        /// <inheritdoc cref="ServerBootstrapper.ShutdownHosted"/>
        public void Retire()
        {
            ServerManager.Bootstrapper.ShutdownHosted();
        }
    }
}
