using Microsoft.Extensions.DependencyInjection;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>The real <see cref="IGenerationBoot"/>: the server's container, <see cref="ServerManager"/>'s bootstrapper.</summary>
    internal sealed class ServerGenerationBoot : IGenerationBoot
    {
        /// <summary>
        /// Builds a container from the server's registrations with the server's options (<c>ValidateOnBuild</c>,
        /// <c>ValidateScopes</c>, every singleton created) and disposes it. Constructors may not call the engine
        /// (<c>DependencyInjection.md</c>), so this is safe off the engine thread; the server's own container is not touched.
        /// </summary>
        public void Validate()
        {
            var services = new ServiceCollection();
            CompositionRoot.RegisterServerServices(services);
            using (ContainerHost.Create(services))
            {
            }
        }

        public void Activate(HostedBoot boot)
        {
            ServerManager.Bootstrapper.BootstrapHosted(boot);
        }

        public void Retire()
        {
            ServerManager.Bootstrapper.ShutdownHosted();
        }
    }
}
