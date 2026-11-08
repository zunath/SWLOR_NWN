using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using FitVector = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Tests.Feature;

public class WeaponFitTests
{
    [Test]
    public void RepeatedRefreshAndChangingProfilesPreserveTheAuthoredTranslation()
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
    public void ReloadRetainsTheAppliedDeltaButACopiedItemDoesNotSubtractItsSourcesDelta()
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
    public void FitSelectionUsesTheRenderedHandAndDoesNotBorrowAHiddenCostumeHand(string race, Gender gender,
        string side, int natural, int armor, bool handHiddenByRobe, string expected) =>
        Invoke<string>(Catalog, "ResolveHandModel", race, gender, side, natural, armor, handHiddenByRobe).Should().Be(expected);

    [TestCase(BaseItem.Rifle, 999, 101, 101, "pmh0_handr001")]
    [TestCase(BaseItem.PowerGlove, 310, 101, 101, "pmh0_handr001")]
    [TestCase(BaseItem.LargeShield, 310, 101, 101, "pmh0_handl001")]
    [TestCase(BaseItem.Sling, 310, 102, 101, "pmh0_handr001")]
    [TestCase(BaseItem.Sling, 310, 101, 102, "pmh0_handr001")]
    [TestCase(BaseItem.Sling, 310, 101, 101, "unknown")]
    [TestCase(BaseItem.Sling, 310, 101, 101, "")]
    public void UnmeasuredAssembliesKeepTheirExistingTransforms(BaseItem type, int middle, int top, int bottom, string hand) =>
        Fit(type, middle, top, bottom, hand).Should().Be(FitVector.Zero);

    [Test]
    public void GeneratedTwinUsesItsOwnHandsSharedFit()
    {
        foreach (var hand in new[] { "pmh0_handl001", "pfh0_handl001" })
            Fit(BaseItem.CompanionPistol, 310, 101, 101, hand).Should().Be(Fit(BaseItem.Sling, 310, 101, 101, hand));
    }

    [Test]
    public void ProfilesAreExplicitNormalHandMeasurementsAndLookupsIgnoreResourceCase()
    {
        var profiles = (Dictionary<(BaseItem Type, int Top, int Middle, int Bottom, string Hand, string SupportHand), FitVector>)Catalog
            .GetField("Fits", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        profiles.Should().NotBeEmpty();
        foreach (var (key, offset) in profiles)
        {
            key.Hand.Should().MatchRegex("^p[mf][heodz]0_hand[rl][0-9]{3}$");
            float.IsFinite(offset.X).Should().BeTrue();
            float.IsFinite(offset.Y).Should().BeTrue();
            float.IsFinite(offset.Z).Should().BeTrue();
            Invoke<FitVector>(Catalog, "GetFit", key.Type, key.Middle, key.Top, key.Bottom, key.Hand.ToUpperInvariant(),
                key.SupportHand.ToUpperInvariant()).Should().Be(offset);
            if (key.Type == BaseItem.Rifle)
                Invoke<FitVector>(Catalog, "GetFit", key.Type, key.Middle, 101, 101, key.Hand, "unmeasured")
                    .Should().Be(FitVector.Zero);
            else key.SupportHand.Should().BeEmpty();
        }
    }

    [Test]
    public void WookieePistolCorrectionIsSpecificToTheMeasuredHandAndAssembly()
    {
        Fit(BaseItem.Sling, 310, 101, 101, "pme0_handr214").Should().Be(new FitVector(0f, 0f, .035f));
        Fit(BaseItem.Sling, 101, 101, 101, "pme0_handr214").Should().Be(new FitVector(0f, 0f, .035f));
        Fit(BaseItem.Sling, 404, 101, 101, "pme0_handr214").Should().Be(new FitVector(0f, 0f, .035f));
        foreach (var hand in new[] { "pfe0_handr214", "pme0_handl214", "pme0_handr208", "pmh0_handr214" })
            Fit(BaseItem.Sling, 310, 101, 101, hand).Should().Be(FitVector.Zero);
        Fit(BaseItem.Sling, 999, 101, 101, "pme0_handr214").Should().Be(FitVector.Zero);
        Fit(BaseItem.Sling, 701, 101, 101, "pme0_handr214").Should().Be(new FitVector(0f, 0f, .035f));
        Fit(BaseItem.Sling, 310, 102, 101, "pme0_handr214").Should().Be(FitVector.Zero);
        Fit(BaseItem.Rifle, 310, 101, 101, "pme0_handr214").Should().Be(FitVector.Zero);
    }

    [Test]
    public void MonCalamariCorrectionDoesNotTransferToOtherHandsOrAssemblies()
    {
        Fit(BaseItem.Sling, 310, 101, 101, "pfh0_handr204").Should().Be(new FitVector(0f, 0f, .035f));
        foreach (var hand in new[] { "pmh0_handr204", "pfh0_handl204", "pfh0_handr001", "pfe0_handr204" })
            Fit(BaseItem.Sling, 310, 101, 101, hand).Should().Be(FitVector.Zero);
        Fit(BaseItem.Sling, 310, 102, 101, "pfh0_handr204").Should().Be(FitVector.Zero);
        Fit(BaseItem.Sling, 999, 101, 101, "pfh0_handr204").Should().Be(FitVector.Zero);
    }

    [Test]
    public void TheInspectedBaselinesKeepTheirAuthoredAttachment()
    {
        Fit(BaseItem.Sling, 310, 101, 101, "pmh0_handr002").Should().Be(FitVector.Zero);
        foreach (var hand in new[] { "pmh0_handr001", "pfh0_handr002", "pme0_handr002", "pmh0_handl002", "pmh2_handr002" })
            Fit(BaseItem.Sling, 310, 101, 101, hand).Should().Be(FitVector.Zero);
        Fit(BaseItem.Sling, 310, 102, 101, "pmh0_handr002").Should().Be(FitVector.Zero);
        Fit(BaseItem.Sling, 310, 101, 102, "pmh0_handr002").Should().Be(FitVector.Zero);
        Fit(BaseItem.Rifle, 310, 101, 101, "pmh0_handr002").Should().Be(FitVector.Zero);
        Fit(BaseItem.Sling, 309, 101, 101, "pmh0_handr002").Should().Be(FitVector.Zero);
    }

    private static Type Catalog => typeof(WeaponFit).Assembly.GetType(
        "SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.WeaponGripFits")!;

    private static FitVector Fit(BaseItem type, int middle, int top, int bottom, string hand) =>
        Invoke<FitVector>(Catalog, "GetFit", type, middle, top, bottom, hand, hand.Replace("_handr", "_handl"));

    private static T Invoke<T>(Type type, string method, params object[] args) =>
        (T)type.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, args)!;

    private static void AssertVector(FitVector actual, FitVector expected)
    {
        actual.X.Should().BeApproximately(expected.X, .000001f);
        actual.Y.Should().BeApproximately(expected.Y, .000001f);
        actual.Z.Should().BeApproximately(expected.Z, .000001f);
    }
}
