using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using FitVector = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Tests.Feature;

public class WeaponFitTests
{
    [Test]
    public void RepeatedRefreshAndReleasePreserveAuthoredTranslationAndRotation()
    {
        var authored = new FitVector(.013f, -.021f, .007f);
        var first = new FitVector(.01f, .02f, -.03f);
        var next = new FitVector(-.02f, .03f, .01f);
        var current = authored;
        var previous = FitVector.Zero;
        foreach (var fit in new[] { first, first, first, next, next, FitVector.Zero, FitVector.Zero })
        {
            current = Invoke<FitVector>(typeof(WeaponFit), "RebaseOffset", current, previous, fit);
            AssertVector(current, authored + fit);
            previous = fit;
        }
    }

    [Test]
    public void ScaleRefreshAndReleasePreserveAuthoredScale()
    {
        var current = 1.03f;
        var previous = 1f;
        foreach (var fit in new[] { .9f, .9f, 1.1f, 1f, 1f })
        {
            current = Invoke<float>(typeof(WeaponFit), "RebaseScale", current, previous, fit);
            current.Should().BeApproximately(1.03f * fit, .000001f);
            previous = fit;
        }
    }

    [Test]
    public void ReloadRetainsTheOwnedDeltaAndCopiesDoNotSubtractTheirSourcesDelta()
    {
        var saved = new FitVector(.01f, .02f, -.03f);
        Invoke<FitVector>(typeof(WeaponFit), "ResolveOwnedOffset", "item-a", "item-a", saved).Should().Be(saved);
        foreach (var identity in new[] { "item-b", "" })
            Invoke<FitVector>(typeof(WeaponFit), "ResolveOwnedOffset", "item-a", identity, saved).Should().Be(FitVector.Zero);
        Invoke<FitVector>(typeof(WeaponFit), "ResolveOwnedOffset", "", "item-a", saved).Should().Be(FitVector.Zero);
    }

    [TestCase("h", Gender.Male, "r", 1, 0, false, "pmh0_handr001")]
    [TestCase("H", Gender.Female, "l", 2, 2, false, "pfh0_handl002")]
    [TestCase("e", Gender.Male, "r", 201, 110, false, "pme0_handr110")]
    [TestCase("d", Gender.Female, "l", 1, 151, false, "pfd0_handl151")]
    [TestCase("h", Gender.Male, "r", 1, 0, true, "")]
    [TestCase("h", Gender.None, "r", 1, 0, false, "")]
    [TestCase("", Gender.Male, "r", 1, 0, false, "")]
    [TestCase("h", Gender.Male, "x", 1, 0, false, "")]
    [TestCase("h", Gender.Male, "r", 0, 0, false, "")]
    public void SelectionUsesTheRenderedHand(string race, Gender gender, string side, int natural, int armor,
        bool hidden, string expected) =>
        Invoke<string>(Catalog, "ResolveHandModel", race, gender, side, natural, armor, hidden).Should().Be(expected);

    [TestCase(BaseItem.Rifle, 999, 101, 101, "pmh0_handr001")]
    [TestCase(BaseItem.PowerGlove, 310, 101, 101, "pmh0_handr001")]
    [TestCase(BaseItem.LargeShield, 310, 101, 101, "pmh0_handl001")]
    [TestCase(BaseItem.Sling, 310, 102, 101, "pmh0_handr001")]
    [TestCase(BaseItem.Sling, 310, 101, 102, "pmh0_handr001")]
    [TestCase(BaseItem.Sling, 999, 101, 101, "pmh0_handr001")]
    [TestCase(BaseItem.Sling, 310, 101, 101, "unknown")]
    [TestCase(BaseItem.Sling, 310, 101, 101, "")]
    public void MissingMeasurementsAreReportedAndLeaveAuthoredPlacement(BaseItem type, int middle,
        int top, int bottom, string hand)
    {
        var args = new object[] { type, middle, top, bottom, hand, "", null! };
        Invoke<bool>(Catalog, "TryGetPlacement", args).Should().BeFalse();
        args[6].Should().Be(WeaponGripFit.Identity);
        Placement(type, middle, top, bottom, hand).Should().Be(WeaponGripFit.Identity);
    }

    [Test]
    public void EveryPistolAndNativeNormalHandHasAnExplicitMeasurementAtAuthoredSize()
    {
        var root = RepositoryRoot();
        var hands = new[] { "sw_pt_rhand", "sw_pt_lhand" }
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(root, "SWLOR_Haks", folder), "*.mdl"))
            .Select(Path.GetFileNameWithoutExtension).Select(name => name!.ToLowerInvariant())
            .Where(name => Regex.IsMatch(name, "^p[mf][heodz]0_hand[rl][0-9]{3}$")).ToArray();
        var selected = ReachableHands(root, hands.ToHashSet());
        foreach (var columns in File.ReadLines(Path.Combine(root, "SWLOR_Haks", "sw_2da", "griprender.2da"))
                     .Skip(3).Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => Regex.Split(line.Trim(), @"\s+")))
        {
            columns.Should().HaveCount(3);
            if (selected.Contains(columns[1].ToLowerInvariant())) selected.Add(columns[2].ToLowerInvariant());
        }
        hands.Should().NotBeEmpty();
        selected.Should().NotBeEmpty();
        foreach (var hand in selected)
        foreach (var model in new PistolAppearanceDefinition().MiddleParts)
        {
            var args = new object[] { BaseItem.Sling, model, 101, 101, hand, "", null! };
            Invoke<bool>(Catalog, "TryGetPlacement", args).Should().BeTrue($"{hand}, pistol {model} needs a measurement");
            var fit = (WeaponGripFit)args[6];
            fit.Scale.Should().Be(1f, "grip placement retains authored weapon size");
            foreach (var value in new[] { fit.Translation.X, fit.Translation.Y, fit.Translation.Z,
                         fit.Rotation.X, fit.Rotation.Y, fit.Rotation.Z })
                float.IsFinite(value).Should().BeTrue();
            Placement(BaseItem.Sling, model, 101, 101, hand.ToUpperInvariant()).Should().Be(fit);
            foreach (var alias in new[] { BaseItem.Pistol, BaseItem.LegacyPistol, BaseItem.CompanionPistol })
                Placement(alias, model, 101, 101, hand).Should().Be(fit);
        }
    }

    [Test]
    public void FemaleMonCalamari301SeatsTheActualHandleNeckAtFullSize()
    {
        var fit = Placement(BaseItem.Sling, 301, 101, 101, "pfh0_handr204");
        AssertVector(fit.Translation, new FitVector(.003f, -.04f, .02f));
        AssertVector(fit.Rotation, FitVector.Zero);
        fit.Scale.Should().Be(1f);
        Placement(BaseItem.Sling, 301, 101, 101, "pfh0_handl204").Should().NotBe(fit,
            "the native left hand has its own geometry and attachment");
    }

    [TestCase(90f, 0f, 0f, 1f, 0f, 0f, 0f, -1f, 0f)]
    [TestCase(0f, 90f, 0f, 0f, 1f, 0f, 0f, 0f, -1f)]
    [TestCase(0f, 0f, 90f, 0f, 0f, 1f, -1f, 0f, 0f)]
    public void NativeHeadingPitchAndRollUseTheClientVerifiedModelAxes(float heading, float pitch, float roll,
        float x, float y, float z, float tx, float ty, float tz)
    {
        // Visible native axis markers reproduce these rotations. Treating the
        // API's RotateX/Y/Z as MDL X/Y/Z seats a rotated neck at the wrong point.
        var frameType = typeof(WeaponFit).Assembly.GetType(
            "SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.WeaponGripFrame")!;
        var frame = Activator.CreateInstance(frameType, FitVector.Zero, new FitVector(heading, pitch, roll))!;
        var seated = Invoke<WeaponGripFit>(Catalog, "SeatHandle", frame, new FitVector(x, y, z));
        AssertVector(seated.Translation, new FitVector(tx, ty, tz));
    }

    private static Type Catalog => typeof(WeaponFit).Assembly.GetType(
        "SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.WeaponGripFits")!;
    private static WeaponGripFit Placement(BaseItem type, int middle, int top, int bottom, string hand) =>
        Invoke<WeaponGripFit>(Catalog, "GetPlacement", type, middle, top, bottom, hand, "");
    private static T Invoke<T>(Type type, string method, params object[] args) =>
        (T)type.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, args)!;
    private static HashSet<string> ReachableHands(string root, HashSet<string> models)
    {
        var lines = File.ReadAllLines(Path.Combine(root, "SWLOR_Haks", "sw_2da", "appearance.2da"));
        var column = Array.IndexOf(Regex.Split(lines[2].Trim(), @"\s+"), "RACE") + 1;
        var races = lines.Skip(3).Select(line => Regex.Matches(line, @"""[^""]*""|\S+").Select(m => m.Value).ToArray())
            .Where(c => c.Length > column && int.TryParse(c[0], out _) && c[column].Length == 1)
            .ToDictionary(c => int.Parse(c[0]), c => c[column].ToLowerInvariant());
        var selected = new HashSet<string>();
        foreach (var appearance in RacialAppearanceRegistry.GetAppearanceTypes())
        {
            RacialAppearanceRegistry.TryGet(appearance, out var definition).Should().BeTrue();
            foreach (var gender in new[] { 'm', 'f' })
            foreach (var side in new[] { 'r', 'l' })
            {
                var prefix = $"p{gender}{races[(int)appearance]}0_hand{side}";
                var natural = side == 'r' ? definition.RightHand : definition.LeftHand;
                foreach (var part in natural)
                {
                    var hand = prefix + $"{part:D3}";
                    models.Should().Contain(hand, $"{appearance} {gender} natural {side} hand {part}");
                    selected.Add(hand);
                }
                foreach (var part in ArmorAppearanceCatalog.Definitions[appearance].Hand.Where(p => p > 2))
                {
                    var hand = prefix + $"{part:D3}";
                    if (models.Contains(hand)) selected.Add(hand);
                }
            }
        }
        return selected;
    }
    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln"))) return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
    private static void AssertVector(FitVector actual, FitVector expected)
    {
        actual.X.Should().BeApproximately(expected.X, .000001f);
        actual.Y.Should().BeApproximately(expected.Y, .000001f);
        actual.Z.Should().BeApproximately(expected.Z, .000001f);
    }
}
