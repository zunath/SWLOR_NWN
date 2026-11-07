using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

[TestFixture]
public class HornFitTests
{
    [TestCase(Gender.Male, .035f)]
    [TestCase(Gender.Female, -.028f)]
    public void HumanSlitherhornUsesItsOwnMouthFitWhetherOrNotItIsPlayed(Gender gender, float expected)
    {
        foreach (var resref in new[] { "slitherhorn", "SLITHERHORN" })
        foreach (var performing in new[] { false, true })
            Fit(resref, AppearanceType.Human, 0, gender, performing).TranslateZ.Should().Be(expected);
    }

    [TestCase("kloo_horn", AppearanceType.Human, Gender.Female)]
    [TestCase("KLOO_HORN", AppearanceType.Bothan, Gender.Female)]
    [TestCase("kloo_horn", AppearanceType.Cathar, Gender.Male)]
    [TestCase("kloo_horn", AppearanceType.Wookiee, Gender.Male)]
    [TestCase("kloo_horn", AppearanceType.Trandoshan, Gender.Male)]
    [TestCase("kloo_horn", AppearanceType.Rodian, Gender.Male)]
    [TestCase("kloo_horn", AppearanceType.MonCalamari, Gender.Female)]
    [TestCase("slitherhorn", AppearanceType.Cathar, Gender.Male)]
    public void BodiesTheClipMissesAreFittedOnlyWhilePlaying(string resref, AppearanceType appearance, Gender gender)
    {
        Fit(resref, appearance, 0, gender, true).Should().NotBe(default(HornFitOffset));
        Fit(resref, appearance, 0, gender, false).Should().Be(default(HornFitOffset));
    }

    [TestCase("kloo_horn", AppearanceType.Human, 0, Gender.Male)]
    [TestCase("kloo_horn", AppearanceType.Ewok, 0, Gender.Female)]
    [TestCase("kloo_horn", AppearanceType.Ugnaught, 0, Gender.Male)]
    [TestCase("kloo_horn", AppearanceType.Droid, 0, Gender.Female)]
    [TestCase("kloo_horn", AppearanceType.Human, 2, Gender.Female)]
    [TestCase("kloo_horn", AppearanceType.Human, 0, Gender.None)]
    [TestCase("slitherhorn", (AppearanceType)0, 0, Gender.Male)]
    [TestCase("slitherhorn", AppearanceType.Human, 2, Gender.Male)]
    [TestCase("slitherhorn", AppearanceType.Human, 0, Gender.None)]
    [TestCase("bandfill", AppearanceType.Human, 0, Gender.Female)]
    [TestCase("guitar", AppearanceType.Cathar, 0, Gender.Male)]
    [TestCase("", AppearanceType.Human, 0, Gender.Male)]
    public void BodiesThatAlreadyLineUpAndOtherItemsKeepTheirAuthoredTransform(string resref,
        AppearanceType appearance, int phenotype, Gender gender) =>
        Fit(resref, appearance, phenotype, gender, true).Should().Be(default(HornFitOffset));

    [Test]
    public void EveryPlayableSpeciesWithAMouthOnAFemaleBodyIsDecidedForTheKlooHorn()
    {
        var alreadyAtTheMouth = new[] { AppearanceType.Ewok, AppearanceType.Ugnaught, AppearanceType.Droid };
        foreach (var appearance in RacialAppearanceRegistry.GetAppearanceTypes())
        {
            var fit = Fit("kloo_horn", appearance, 0, Gender.Female, true);
            if (alreadyAtTheMouth.Contains(appearance))
                fit.Should().Be(default(HornFitOffset), appearance.ToString());
            else
                fit.Should().NotBe(default(HornFitOffset), appearance.ToString());
        }
    }

    [TestCase(.013f)]
    [TestCase(-.021f)]
    public void RepeatedRefreshAndReloadDoNotAccumulateTheFit(float authored)
    {
        var fit = Fit("slitherhorn", AppearanceType.Human, 0, Gender.Male, false).TranslateZ;
        var current = Rebase(authored, 0f, fit);
        for (var refresh = 0; refresh < 20; refresh++)
            current = Rebase(current, fit, fit);
        current.Should().BeApproximately(authored + fit, .000001f);
        Rebase(current, fit, 0f).Should().BeApproximately(authored, .000001f);
    }

    [Test]
    public void ChangingGenderAndUnequippingPreservesIndependentCosmeticEdits()
    {
        const float authored = -.012f;
        const float editWhileEquipped = .007f;
        var male = Fit("slitherhorn", AppearanceType.Human, 0, Gender.Male, false).TranslateZ;
        var female = Fit("slitherhorn", AppearanceType.Human, 0, Gender.Female, false).TranslateZ;
        var current = Rebase(authored, 0f, male) + editWhileEquipped;
        current = Rebase(current, male, female);
        current.Should().BeApproximately(authored + editWhileEquipped + female, .000001f);
        current = Rebase(current, female, 0f);
        current.Should().BeApproximately(authored + editWhileEquipped, .000001f);
        Rebase(current, 0f, male).Should().BeApproximately(authored + editWhileEquipped + male, .000001f);
    }

    [Test]
    public void SwitchingToAnUnfittedBodyRemovesTheSavedFit()
    {
        const float authored = .019f;
        var previous = Fit("slitherhorn", AppearanceType.Human, 0, Gender.Female, false).TranslateZ;
        var next = Fit("slitherhorn", (AppearanceType)0, 0, Gender.Female, false).TranslateZ;
        Rebase(authored + previous, previous, next).Should().BeApproximately(authored, .000001f);
    }

    [Test]
    public void EndingAPerformanceRemovesOnlyThePlayingFit()
    {
        const float cosmeticRotation = 4f;
        var playing = Fit("kloo_horn", AppearanceType.Human, 0, Gender.Female, true);
        var carried = Fit("kloo_horn", AppearanceType.Human, 0, Gender.Female, false);
        var rotation = Rebase(cosmeticRotation, carried.RotateY, playing.RotateY);
        Rebase(rotation, playing.RotateY, carried.RotateY).Should().BeApproximately(cosmeticRotation, .000001f);
    }

    [Test]
    public void CopiedLocalsDoNotSubtractTheOriginalItemsFitFromAResetTransform()
    {
        var saved = new HornFitOffset(0f, 0f, .035f, 0f);
        var copied = Owned("original-item", "copied-item", saved);
        copied.Should().Be(default(HornFitOffset));
        Rebase(0f, copied.TranslateZ, saved.TranslateZ).Should().Be(saved.TranslateZ);
    }

    [Test]
    public void ReloadingTheSameItemRetainsItsOwnedOffset()
    {
        const float authored = -.014f;
        var saved = new HornFitOffset(0f, 0f, -.028f, 0f);
        var owned = Owned("persisted-item", "persisted-item", saved);
        Rebase(authored + saved.TranslateZ, owned.TranslateZ, saved.TranslateZ)
            .Should().BeApproximately(authored + saved.TranslateZ, .000001f);
        Rebase(authored + saved.TranslateZ, owned.TranslateZ, 0f).Should().BeApproximately(authored, .000001f);
    }

    private static HornFitOffset Fit(string resref, AppearanceType appearance, int phenotype, Gender gender, bool performing) =>
        (HornFitOffset)Invoke("ResolveFit", resref, appearance, phenotype, gender, performing);

    private static HornFitOffset Owned(string fittedItem, string currentItem, HornFitOffset fit) =>
        (HornFitOffset)Invoke("ResolveOwnedFit", fittedItem, currentItem, fit);

    private static float Rebase(float current, float previous, float next) =>
        (float)Invoke("RebaseOffset", current, previous, next);

    private static object Invoke(string method, params object[] arguments) =>
        typeof(HornFit).GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, arguments)!;
}
