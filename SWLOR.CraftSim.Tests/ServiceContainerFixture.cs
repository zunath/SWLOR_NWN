using NUnit.Framework;
using SWLOR.Game.Server.Composition;

namespace SWLOR.CraftSim.Tests
{
    /// <summary>
    /// Builds the server's service container once for the whole assembly (a SetUpFixture covers only its own namespace, so it sits in the root one). The simulator reads the game's own rules through
    /// the static facades (Guild, Craft...), which forward to the container the game server builds at boot.
    /// </summary>
    [SetUpFixture]
    public sealed class ServiceContainerFixture
    {
        [OneTimeSetUp]
        public void BuildServiceContainer()
        {
            if (!CompositionRoot.IsBuilt)
                CompositionRoot.Build(CompositionRoot.ServerHost);
        }
    }
}
