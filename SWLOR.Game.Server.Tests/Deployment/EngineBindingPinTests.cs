using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Deployment;

/// <summary>
/// Pins the engine bindings to the versions the Nwn.Hosting package is built against. Under hot reload the host
/// and every game build share one copy of NWN.Core and NWN.Native, so a bump must be deliberate: change the
/// constants here together with the project files and lock files, after checking the hosting package.
/// </summary>
[TestFixture]
public class EngineBindingPinTests
{
    private const string CoreVersion = "8193.37.7";
    private const string NativeVersion = "8193.37.8";

    [Test]
    public void ServerProjectReferencesThePinnedBindings()
    {
        var text = ReadProject("SWLOR.Game.Server", "SWLOR.Game.Server.csproj");

        ReferencedVersions(text, "NWN.Core").Should().Equal(CoreVersion);
        ReferencedVersions(text, "NWN.Native").Should().Equal(NativeVersion);
    }

    [Test]
    public void ApiProjectReferencesThePinnedCoreBinding()
    {
        var text = ReadProject("SWLOR.NWN.API", "SWLOR.NWN.API.csproj");

        ReferencedVersions(text, "NWN.Core").Should().Equal(CoreVersion);
        ReferencedVersions(text, "NWN.Native").Should().BeEmpty("the API project reaches NWN.Native only through the server project");
    }

    private static List<string> ReferencedVersions(string projectText, string package) =>
        Regex.Matches(projectText, $@"<PackageReference\s+Include=""{Regex.Escape(package)}""\s+Version=""([^""]+)""")
            .Select(m => m.Groups[1].Value).ToList();

    private static string ReadProject(string folder, string file) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), folder, file));

    private static string FindRepositoryRoot()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder != null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "SWLOR.Game.Server.sln")))
                return folder.FullName;
        }

        throw new InvalidOperationException($"Could not find SWLOR.Game.Server.sln above {AppContext.BaseDirectory}.");
    }
}
