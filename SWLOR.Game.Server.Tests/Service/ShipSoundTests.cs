using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Space;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The client never retires a fire-and-forget visual effect that has no model and no camera shake, so applying one
/// leaves it on the client for good. Ship combat applied one per shot and hit (and the stock screen shakes became
/// that kind when camera shakes were removed), which piled thousands up in a capital ship fight and dropped the
/// frame rate until relog. These rows must play their sound directly instead.
/// </summary>
public class ShipSoundTests
{
    private static readonly string[] ModelAndShakeColumns =
    {
        "Imp_HeadCon_Node", "Imp_Impact_Node", "Imp_Root_S_Node", "Imp_Root_M_Node", "Imp_Root_L_Node", "Imp_Root_H_Node",
        "ProgFX_Impact", "ProgFX_Duration", "ProgFX_Cessation",
        "Ces_HeadCon_Node", "Ces_Impact_Node", "Ces_Root_S_Node", "Ces_Root_M_Node", "Ces_Root_L_Node", "Ces_Root_H_Node",
        "ShakeType", "ShakeDuration"
    };

    [Test]
    public void SoundOnlyRows_IncludeTheShipSoundsAndScreenShakes()
    {
        var soundOnly = SoundOnlyEffectRows(FindRepositoryRoot());

        soundOnly.Keys.Should().Contain(new[] { 286, 287, 356, 827, 833, 834, 835, 836, 837, 838 });
    }

    [Test]
    public void ServerCode_NeverAppliesSoundOnlyEffectsDirectly()
    {
        var root = FindRepositoryRoot();
        var names = SoundOnlyEffectNames(root).Keys.ToArray();
        var alternatives = string.Join("|", names.Select(Regex.Escape));
        var direct = new Regex($@"(EffectVisualEffect|ShowVisualEffect)\([^;]*VisualEffect\.({alternatives})\b");

        ServerSourceLines(root)
            .Where(entry => direct.IsMatch(entry.Line))
            .Select(entry => entry.Describe(root))
            .Should().BeEmpty("play these through VisualEffectPlayback, ShipSound or NearbySound instead of applying them");
    }

    [Test]
    public void EverySoundOnlyEffectStillReferenced_PlaysItsRowSound()
    {
        var root = FindRepositoryRoot();
        var soundOnly = SoundOnlyEffectNames(root);
        var reference = new Regex($@"VisualEffect\.({string.Join("|", soundOnly.Keys.Select(Regex.Escape))})\b");
        var referenced = ServerSourceLines(root)
            .SelectMany(entry => reference.Matches(entry.Line).Select(match => match.Groups[1].Value))
            .Distinct()
            .ToList();

        foreach (var name in referenced)
        {
            var effect = Enum.Parse<VisualEffect>(name);
            VisualEffectPlayback.SoundOnlyEffects.Should().ContainKey(effect,
                $"{name} is passed to a shared ability or ship helper, which must play its sound instead of applying it");
            VisualEffectPlayback.SoundOnlyEffects[effect].Should().BeEquivalentTo(soundOnly[name],
                $"{name} must keep the sound visualeffects.2da gives it");
        }
    }

    [Test]
    public void SharedAbilityVisuals_GoThroughVisualEffectPlayback()
    {
        var root = FindRepositoryRoot();
        foreach (var file in new[] { "Domain/Abilities/AbilityService.cs", "Feature/AbilityDefinition/NPC/InnateAbility.cs" })
        {
            var source = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", file));
            source.Should().NotMatchRegex(@"EffectVisualEffect\((areaVisualEffect|targetVisualEffect)\b",
                $"{file} must route definition-supplied effects through VisualEffectPlayback");
        }
    }

    [Test]
    public void EveryShipSound_HasItsSoundFile()
    {
        var root = FindRepositoryRoot();
        var baseGameSounds = new[] { ShipSound.ImpactRumble, ShipSound.InteriorImpact };
        var sounds = typeof(ShipSound)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToList();

        sounds.Should().HaveCount(9);
        foreach (var sound in sounds.Except(baseGameSounds))
        {
            File.Exists(Path.Combine(root, "SWLOR_Haks", "sw_sound", $"{sound}.wav"))
                .Should().BeTrue($"{sound}.wav must ship in the sound hak");
        }

        // The rumbles are the base game sounds of the stock screen shake rows.
        var soundOnly = SoundOnlyEffectRows(root);
        soundOnly[286].Should().BeEquivalentTo(ShipSound.ImpactRumble);
        soundOnly[356].Should().BeEquivalentTo(ShipSound.InteriorImpact);
    }

    private static Dictionary<string, string> SoundOnlyEffectNames(string root)
    {
        var rows = SoundOnlyEffectRows(root);
        return Enum.GetValues<VisualEffect>()
            .Where(effect => rows.ContainsKey((int)effect))
            .GroupBy(effect => effect.ToString())
            .ToDictionary(group => group.Key, group => rows[(int)group.First()]);
    }

    /// <summary>Fire-and-forget rows with a sound and no model, projectile or camera shake: row id to sound.</summary>
    private static Dictionary<int, string> SoundOnlyEffectRows(string root)
    {
        var table = File.ReadAllLines(Path.Combine(root, "SWLOR_Haks", "sw_2da", "visualeffects.2da"));
        var header = Regex.Split(table.First(line => line.TrimStart().StartsWith("Label ")).Trim(), @"\s+");
        var column = header.Select((name, index) => (name, index)).ToDictionary(pair => pair.name, pair => pair.index + 1);
        var rows = new Dictionary<int, string>();

        foreach (var line in table.Where(line => Regex.IsMatch(line, @"^\s*\d+\s")))
        {
            var fields = Regex.Matches(line, "\"[^\"]*\"|\\S+").Select(match => match.Value).ToArray();
            string Field(string name) => column[name] < fields.Length ? fields[column[name]] : "****";
            bool Empty(string name) => Field(name).StartsWith("***");

            if (Field("Type_FD") == "F" && ModelAndShakeColumns.All(Empty) && !Empty("SoundImpact"))
                rows[int.Parse(fields[0])] = Field("SoundImpact");
        }

        return rows;
    }

    private static IEnumerable<SourceLine> ServerSourceLines(string root)
    {
        var serverRoot = Path.Combine(root, "SWLOR.Game.Server");
        return Directory.EnumerateFiles(serverRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
            {
                var relative = Path.GetRelativePath(serverRoot, path).Replace('\\', '/');
                return !relative.StartsWith("bin/") && !relative.StartsWith("obj/");
            })
            .SelectMany(path => File.ReadLines(path).Select((line, index) => new SourceLine(path, index + 1, line)));
    }

    private sealed record SourceLine(string Path, int Number, string Line)
    {
        public string Describe(string root) => $"{System.IO.Path.GetRelativePath(root, Path)}:{Number}: {Line.Trim()}";
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(System.IO.Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate SWLOR_NWN repository root.");
    }
}
