using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

[TestFixture]
public class SlitherhornFitTests
{
    [TestCase(Gender.Male, .035f)]
    [TestCase(Gender.Female, -.028f)]
    public void HumanProfilesUseTheirOwnMouthFit(Gender gender, float expected)
    {
        foreach (var resref in new[] { "slitherhorn", "SLITHERHORN" })
            Offset(resref, AppearanceType.Human, 0, gender).Should().Be(expected);
    }

    [TestCase("kloo_horn", AppearanceType.Human, 0, Gender.Male)]
    [TestCase("bandfill", AppearanceType.Human, 0, Gender.Female)]
    [TestCase("fanfar", AppearanceType.Human, 0, Gender.Female)]
    [TestCase("mandoviol", AppearanceType.Human, 0, Gender.Male)]
    [TestCase("", AppearanceType.Human, 0, Gender.Male)]
    [TestCase("slitherhorn", (AppearanceType)0, 0, Gender.Male)]
    [TestCase("slitherhorn", AppearanceType.Human, 2, Gender.Male)]
    [TestCase("slitherhorn", AppearanceType.Human, 0, Gender.None)]
    public void UnmeasuredBodiesAndOtherInstrumentsKeepTheirAuthoredTransform(string resref,
        AppearanceType appearance, int phenotype, Gender gender) =>
        Offset(resref, appearance, phenotype, gender).Should().Be(0f);

    [TestCase(.013f)]
    [TestCase(-.021f)]
    public void RepeatedRefreshAndReloadDoNotAccumulateTheFit(float authored)
    {
        var fit = Offset("slitherhorn", AppearanceType.Human, 0, Gender.Male);
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
        var male = Offset("slitherhorn", AppearanceType.Human, 0, Gender.Male);
        var female = Offset("slitherhorn", AppearanceType.Human, 0, Gender.Female);
        var current = Rebase(authored, 0f, male) + editWhileEquipped;
        current = Rebase(current, male, female);
        current.Should().BeApproximately(authored + editWhileEquipped + female, .000001f);
        current = Rebase(current, female, 0f);
        current.Should().BeApproximately(authored + editWhileEquipped, .000001f);
        Rebase(current, 0f, male).Should().BeApproximately(authored + editWhileEquipped + male, .000001f);
    }

    [Test]
    public void SwitchingToAnUnmeasuredBodyRemovesTheSavedFit()
    {
        const float authored = .019f;
        var previous = Offset("slitherhorn", AppearanceType.Human, 0, Gender.Female);
        var next = Offset("slitherhorn", (AppearanceType)0, 0, Gender.Female);
        Rebase(authored + previous, previous, next).Should().BeApproximately(authored, .000001f);
    }

    private static float Offset(string resref, AppearanceType appearance, int phenotype, Gender gender) =>
        Invoke("ResolveOffset", resref, appearance, phenotype, gender);

    private static float Rebase(float current, float previous, float next) =>
        Invoke("RebaseOffset", current, previous, next);

    private static float Invoke(string method, params object[] arguments) =>
        (float)typeof(SlitherhornFit).GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, arguments)!;
}
