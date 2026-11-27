using NUnit.Framework;
using SWLOR.Game.Server.IntegrationTests.Support;

namespace SWLOR.Game.Server.IntegrationTests;

/// <summary>Connects to the throwaway Redis and loads the real database once for the whole assembly.</summary>
[SetUpFixture]
public sealed class IntegrationAssemblySetup
{
    [OneTimeSetUp]
    public void ConnectAndLoad() => IntegrationEnvironment.Start();

    [OneTimeTearDown]
    public void Disconnect() => IntegrationEnvironment.Stop();
}
