using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The client never retires a fire-and-forget visual effect that has no model, so applying a sound-only effect
/// leaves it on its target for good. Ship combat applied one per shot and hit, which piled thousands onto the
/// player's ship in a capital ship fight and dropped the frame rate until relog.
/// </summary>
public class ShipSoundTests
{
    /// <summary>
    /// Stock screen shakes (VFX_FNF_SCREEN_SHAKE, _BUMP, _SHAKE2). The engine shakes the camera for these rows itself,
    /// and a capital ship fight's worth of them did not build up client cost (see the pull request).
    /// </summary>
    private static readonly int[] EngineScreenShakes = { 286, 287, 356 };

    private static readonly string[] ModelColumns =
    {
        "Imp_HeadCon_Node", "Imp_Impact_Node", "Imp_Root_S_Node", "Imp_Root_M_Node", "Imp_Root_L_Node", "Imp_Root_H_Node",
        "ProgFX_Impact", "ProgFX_Duration", "ProgFX_Cessation",
        "Ces_HeadCon_Node", "Ces_Impact_Node", "Ces_Root_S_Node", "Ces_Root_M_Node", "Ces_Root_L_Node", "Ces_Root_H_Node",
        "ShakeType"
    };

    [Test]
    public void ServerCode_NeverAppliesSoundOnlyVisualEffects()
    {
        var root = FindRepositoryRoot();
        var soundOnly = SoundOnlyEffectRows(root);
        soundOnly.Should().Contain(new[] { 827, 833, 834, 835, 836, 837, 838 },
            "the ship combat sound effects are the rows that leaked");

        var names = Enum.GetValues<VisualEffect>()
            .Where(effect => soundOnly.Contains((int)effect))
            .Select(effect => effect.ToString())
            .ToArray();
        names.Should().NotBeEmpty();

        var reference = new Regex($@"VisualEffect\.({string.Join("|", names.Select(Regex.Escape))})\b");
        var serverRoot = Path.Combine(root, "SWLOR.Game.Server");
        var offenders = Directory.EnumerateFiles(serverRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(serverRoot, path))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => (path, line, number: index + 1)))
            .Where(entry => reference.IsMatch(entry.line))
            .Select(entry => $"{Path.GetRelativePath(root, entry.path)}:{entry.number}: {entry.line.Trim()}")
            .ToList();

        offenders.Should().BeEmpty("play these sounds with ShipSound (NWNX PlaySound) instead of a model-less visual effect");
    }

    [Test]
    public void EveryShipSound_HasItsSoundFile()
    {
        var root = FindRepositoryRoot();
        var sounds = typeof(ShipSound)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToList();

        sounds.Should().HaveCount(7);
        foreach (var sound in sounds)
        {
            File.Exists(Path.Combine(root, "SWLOR_Haks", "sw_sound", $"{sound}.wav"))
                .Should().BeTrue($"{sound}.wav must ship in the sound hak");
        }
    }

    private static HashSet<int> SoundOnlyEffectRows(string root)
    {
        var table = File.ReadAllLines(Path.Combine(root, "SWLOR_Haks", "sw_2da", "visualeffects.2da"));
        var header = Regex.Split(table.First(line => line.TrimStart().StartsWith("Label ")).Trim(), @"\s+");
        var column = header.Select((name, index) => (name, index)).ToDictionary(pair => pair.name, pair => pair.index + 1);
        var rows = new HashSet<int>();

        foreach (var line in table.Where(line => Regex.IsMatch(line, @"^\s*\d+\s")))
        {
            var fields = Regex.Matches(line, "\"[^\"]*\"|\\S+").Select(match => match.Value).ToArray();
            string Field(string name) => column[name] < fields.Length ? fields[column[name]] : "****";
            bool Empty(string name) => Field(name).StartsWith("***");

            var id = int.Parse(fields[0]);
            if (Field("Type_FD") == "F" && ModelColumns.All(Empty) && !Empty("SoundImpact") && !EngineScreenShakes.Contains(id))
                rows.Add(id);
        }

        return rows;
    }

    private static bool IsBuildOutput(string serverRoot, string path)
    {
        var relative = Path.GetRelativePath(serverRoot, path).Replace('\\', '/');
        return relative.StartsWith("bin/") || relative.StartsWith("obj/");
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate SWLOR_NWN repository root.");
    }
}
