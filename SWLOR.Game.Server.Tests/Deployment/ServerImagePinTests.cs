using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Deployment;

/// <summary>
/// Guards the pinned .NET runtime in the server image: Nwn.Hosting depends on the exact runtime patch, so the
/// 10.0 packages must carry an explicit version and every file naming the image must use the same tag.
/// </summary>
[TestFixture]
public class ServerImagePinTests
{
    private static readonly Regex PackagePattern = new(
        @"^\s*(dotnet-(?:runtime|apphost-pack|hostfxr|runtime-deps)-10\.0)(=\S+)?\s*\\?\s*$",
        RegexOptions.Multiline);

    [Test]
    public void DotnetTenPackagesAreInstalledAtAnExactVersion()
    {
        var dockerfile = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server", "Docker", "Dockerfile"));
        var matches = PackagePattern.Matches(dockerfile).Cast<Match>().ToList();

        matches.Select(m => m.Groups[1].Value).Should().BeEquivalentTo(
            "dotnet-runtime-10.0", "dotnet-apphost-pack-10.0", "dotnet-hostfxr-10.0", "dotnet-runtime-deps-10.0");
        matches.Where(m => !Regex.IsMatch(m.Groups[2].Value, @"^=\d+\.\d+\.\d+-\d+$"))
            .Select(m => m.Groups[1].Value).Should().BeEmpty("every .NET 10 package needs an exact =version pin");
    }

    [Test]
    public void ServerImageTagMatchesBothComposeFiles()
    {
        var root = FindRepositoryRoot();
        var tag = File.ReadAllText(Path.Combine(root, "scripts", "deployment", "server-image.txt")).Trim();
        tag.Should().StartWith("zunath/nwn-dotnet:");

        foreach (var name in new[] { "docker-compose.yml", "docker-compose.enginetests.yml" })
        {
            var text = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Docker", name));
            var tags = Regex.Matches(text, @"image:\s*(zunath/nwn-dotnet:\S+)").Select(m => m.Groups[1].Value).ToList();
            tags.Should().NotBeEmpty(name);
            tags.Should().OnlyContain(t => t == tag, $"{name} must use the tag in server-image.txt");
        }
    }

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
