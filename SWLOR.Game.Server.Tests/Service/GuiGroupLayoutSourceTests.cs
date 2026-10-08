using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

[TestFixture]
public class GuiGroupLayoutSourceTests
{
    // A group layout sent on its own to a window that has already drawn stays blank in the
    // real client, and a delayed re-apply blanks content that was showing (Character Sheet,
    // Oct 2026). Group layouts must reach the client only inside the composed root layout.
    [Test]
    public void GroupLayoutsAreOnlySentInsideTheComposedRootLayout()
    {
        var serverRoot = Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server");
        var callers = Directory
            .EnumerateFiles(serverRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                           !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => File.ReadAllText(path).Contains("NuiSetGroupLayout("))
            .Select(path => Path.GetFileName(path))
            .ToList();

        callers.Should().BeEquivalentTo(new[] { "GuiViewModelBase.cs" });

        var source = File.ReadAllText(Path.Combine(serverRoot, "Service", "GuiService", "GuiViewModelBase.cs"));
        var calls = Regex.Matches(source, @"NuiSetGroupLayout\((?<args>[^;]*)\);");

        calls.Should().ContainSingle();
        calls[0].Groups["args"].Value.Should().Contain("WindowElementId");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("repository root should be discoverable from the test directory");
        return directory!;
    }
}
