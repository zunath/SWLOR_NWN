using NUnit.Framework;
using SWLOR.EngineSeamGenerator;

namespace SWLOR.NWN.API.Tests.Support;

/// <summary>Finds the repository root (the folder with SWLOR.Game.Server.sln) from the test output folder.</summary>
internal static class RepositoryRoot
{
    public static string Path { get; } = RepositoryLocator.Find(TestContext.CurrentContext.TestDirectory)
                                         ?? throw new InvalidOperationException("SWLOR.Game.Server.sln was not found above the test output folder.");
}
