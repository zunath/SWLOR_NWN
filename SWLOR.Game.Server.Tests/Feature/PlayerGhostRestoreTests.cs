using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class PlayerGhostRestoreTests
{
    [Test]
    public void EveryRevive_RestoresTheLoginGhost()
    {
        var server = Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server");
        var revivingFiles = Directory.EnumerateFiles(server, "*.cs", SearchOption.AllDirectories)
            .Select(path => (Path: path, Source: File.ReadAllText(path)))
            .Where(file => file.Source.Contains("EffectResurrection()"))
            .ToList();

        revivingFiles.Should().NotBeEmpty();
        foreach (var (path, source) in revivingFiles)
        {
            Regex.Matches(source, @"RestoreGhostAfterRevive\(").Count.Should().Be(
                Regex.Matches(source, @"EffectResurrection\(\)").Count,
                $"death strips the login ghost, so every revive in {Path.GetFileName(path)} must restore it");
        }
    }

    [Test]
    public void LeavingSpaceMode_RestoresTheLoginGhost()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), "SWLOR.Game.Server", "Feature", "PlayerTemporaryEffects.cs"));

        source.Should().MatchRegex(@"\[NWNEventHandler\(ScriptName\.OnSpaceExit\)\]\s+public static void RestoreGhostOnSpaceExit\(\)",
            "entering space mode strips every effect, including the login ghost");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }
}
