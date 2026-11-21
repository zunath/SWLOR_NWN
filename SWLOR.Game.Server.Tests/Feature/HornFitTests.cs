using System;
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
    [TestCase("panpipes", AppearanceType.Human, Gender.Male)]
    [TestCase("PANPIPES", AppearanceType.Human, Gender.Female)]
    [TestCase("panpipes", AppearanceType.Cathar, Gender.Male)]
    [TestCase("panpipes", AppearanceType.Ugnaught, Gender.Male)]
    [TestCase("panpipes", AppearanceType.Ewok, Gender.Female)]
    [TestCase("tambourine", AppearanceType.Wookiee, Gender.Male)]
    [TestCase("TAMBOURINE", AppearanceType.Wookiee, Gender.Female)]
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
    [TestCase("tambourine", AppearanceType.Human, 0, Gender.Female)]
    [TestCase("tambourine", AppearanceType.Cathar, 0, Gender.Male)]
    [TestCase("panpipes", AppearanceType.Human, 2, Gender.Female)]
    [TestCase("panpipes", AppearanceType.Human, 0, Gender.None)]
    [TestCase("", AppearanceType.Human, 0, Gender.Male)]
    public void BodiesThatAlreadyLineUpAndOtherItemsKeepTheirAuthoredTransform(string resref,
        AppearanceType appearance, int phenotype, Gender gender) =>
        Fit(resref, appearance, phenotype, gender, true).Should().Be(default(HornFitOffset));

    [TestCase(AppearanceType.Cathar, Gender.Male, .085f, 0f, 0f)]
    [TestCase(AppearanceType.Ugnaught, Gender.Male, .04f, -.03f, -.06f)]
    [TestCase(AppearanceType.Ugnaught, Gender.Female, .04f, -.03f, -.06f)]
    [TestCase(AppearanceType.Ewok, Gender.Male, .04f, -.03f, -.06f)]
    [TestCase(AppearanceType.Ewok, Gender.Female, .04f, -.03f, -.06f)]
    [TestCase(AppearanceType.Cathar, Gender.Female, .085f, 0f, 0f)]
    [TestCase(AppearanceType.Human, Gender.Female, 0f, .03f, 0f)]
    [TestCase(AppearanceType.Cyborg, Gender.Female, 0f, .03f, 0f)]
    [TestCase(AppearanceType.Twilek, Gender.Female, 0f, .03f, 0f)]
    [TestCase(AppearanceType.Trandoshan, Gender.Female, 0f, .03f, 0f)]
    [TestCase(AppearanceType.MonCalamari, Gender.Female, 0f, .03f, 0f)]
    [TestCase(AppearanceType.Wookiee, Gender.Male, .05f, .03f, 0f)]
    [TestCase(AppearanceType.Wookiee, Gender.Female, .05f, 0f, 0f)]
    [TestCase(AppearanceType.Twilek, Gender.Male, 0f, 0f, 0f)]
    [TestCase(AppearanceType.Bothan, Gender.Male, 0f, 0f, 0f)]
    [TestCase(AppearanceType.Droid, Gender.Male, .085f, 0f, 0f)]
    [TestCase(AppearanceType.Droid, Gender.Female, .085f, 0f, 0f)]
    public void PanpipesMoveToTheMouthOnlyOnBodiesWhoseHandsMissIt(AppearanceType appearance, Gender gender,
        float x, float y, float z) =>
        Fit("panpipes", appearance, 0, gender, true).Should().Be(new HornFitOffset(x, y, z, 0f, 180f));

    [TestCase(Gender.Male)]
    [TestCase(Gender.Female)]
    public void TheWookieeTambourineSlidesBackUnderTheStrikingPalm(Gender gender) =>
        Fit("tambourine", AppearanceType.Wookiee, 0, gender, true).Should().Be(new HornFitOffset(.08f, .08f, 0f, 0f));

    [Test]
    public void PanpipeRowFacesInwardForEveryPlayableBodyOnlyWhilePlaying()
    {
        foreach (var appearance in RacialAppearanceRegistry.GetAppearanceTypes())
        foreach (var gender in new[] { Gender.Male, Gender.Female })
        {
            Fit("panpipes", appearance, 0, gender, true).RotateZ.Should().Be(180f, appearance.ToString());
            Fit("panpipes", appearance, 0, gender, true).RotateY.Should().Be(0f);
            Fit("panpipes", appearance, 0, gender, false).Should().Be(default(HornFitOffset));
        }
    }

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

    // Centreline points measured from the two horn models (wmgfs_m_211 is the Jessoon, wmgfs_m_201 the kloo horn).
    private static readonly (double X, double Y, double Z) JessoonMouthpiece = (-.060d, .318d, .035d);
    private static readonly (double X, double Y, double Z) JessoonShaftLow = (.077d, -.26d, -.025d);
    private static readonly (double X, double Y, double Z) JessoonShaftHigh = (-.055d, .14d, -.001d);
    private static readonly (double X, double Y, double Z) KlooMouthpiece = (-.155d, .367d, -.007d);
    private static readonly (double X, double Y, double Z) KlooShaftLow = (.159d, -.55d, -.021d);
    private static readonly (double X, double Y, double Z) KlooShaftHigh = (-.067d, .21d, -.011d);

    [Test]
    public void TheJessoonLiesInTheKlooHornsHandsWithItsMouthpieceAtTheKlooHornsMouthpiece()
    {
        var fit = HornFit.JessoonOnKlooHorn;
        Distance(Apply(fit, JessoonMouthpiece), KlooMouthpiece).Should().BeLessThan(.01d);

        // Both hands close on the kloo horn's shaft from the grip to 14 cm above it.
        var low = Apply(fit, JessoonShaftLow);
        var high = Apply(fit, JessoonShaftHigh);
        var klooDirection = Unit(Minus(KlooShaftHigh, KlooShaftLow));
        var grip = Plus(KlooShaftLow, Scale(klooDirection, -Dot(KlooShaftLow, klooDirection)));
        foreach (var along in new[] { -.03d, 0d, .03d, .08d, .11d, .14d })
            DistanceToLine(Plus(grip, Scale(klooDirection, along)), low, high).Should().BeLessThan(.01d, along.ToString());
    }

    // Full-height female bodies whose hands close on the Jessoon's slim shaft only at the grip.
    private static readonly AppearanceType[] GripFemaleBodies =
    {
        AppearanceType.Human, AppearanceType.Chiss, AppearanceType.Zabrak, AppearanceType.Twilek, AppearanceType.Mirialan,
        AppearanceType.Echani, AppearanceType.KelDor, AppearanceType.Cyborg, AppearanceType.Trandoshan, AppearanceType.Togruta,
        AppearanceType.Nautolan, AppearanceType.Bothan, AppearanceType.Rodian, AppearanceType.MonCalamari
    };

    [Test]
    public void TheJessoonGoesWhereEachBodysKlooHornGoesOnlyWhilePlaying()
    {
        foreach (var appearance in RacialAppearanceRegistry.GetAppearanceTypes())
        foreach (var gender in new[] { Gender.Male, Gender.Female })
        {
            Fit("jessoon", appearance, 0, gender, false).Should().Be(default(HornFitOffset));
            if (appearance == AppearanceType.Cathar && gender == Gender.Male || appearance == AppearanceType.Ugnaught ||
                gender == Gender.Female && GripFemaleBodies.Contains(appearance))
                continue;

            var kloo = Fit("kloo_horn", appearance, 0, gender, true);
            var jessoon = Fit("JESSOON", appearance, 0, gender, true);
            foreach (var point in new[] { JessoonMouthpiece, JessoonShaftLow, JessoonShaftHigh })
                Distance(Apply(jessoon, point), Apply(kloo, Apply(HornFit.JessoonOnKlooHorn, point)))
                    .Should().BeLessThan(.0005d, $"{appearance} {gender}");
        }
    }

    [Test]
    public void OnFullHeightFemaleBodiesTheJessoonKeepsTheKlooHornsTiltWithItsShaftAtTheGrip()
    {
        foreach (var appearance in GripFemaleBodies)
        {
            var onKloo = HornFitTransform.Compose(Fit("kloo_horn", appearance, 0, Gender.Female, true), HornFit.JessoonOnKlooHorn);
            var jessoon = Fit("jessoon", appearance, 0, Gender.Female, true);

            // The same tilt moves every point of the Jessoon by one shift...
            var shift = Minus(Apply(jessoon, JessoonShaftLow), Apply(onKloo, JessoonShaftLow));
            foreach (var point in new[] { JessoonMouthpiece, JessoonShaftHigh })
                Distance(Minus(Apply(jessoon, point), Apply(onKloo, point)), shift).Should().BeLessThan(.0005d, appearance.ToString());

            // ...which takes back the kloo horn's slide across the hand, 3 cm or more on every body.
            jessoon.TranslateX.Should().Be(0f, appearance.ToString());
            jessoon.TranslateZ.Should().Be(0f, appearance.ToString());
            jessoon.TranslateY.Should().BeApproximately(onKloo.TranslateY, 1e-6f, appearance.ToString());
            Distance(shift, (0d, 0d, 0d)).Should().BeGreaterThan(.03d, appearance.ToString());
        }
    }

    [TestCase(Gender.Male)]
    [TestCase(Gender.Female)]
    public void TheUgnaughtJessoonSlidesSevenCentimetresDownItsShaftToTheLowMouth(Gender gender)
    {
        var aligned = HornFit.JessoonOnKlooHorn;
        var jessoon = Fit("jessoon", AppearanceType.Ugnaught, 0, gender, true);
        var down = Scale(Unit(Minus(Apply(aligned, JessoonShaftLow), Apply(aligned, JessoonShaftHigh))), .07d);
        foreach (var point in new[] { JessoonMouthpiece, JessoonShaftLow, JessoonShaftHigh })
            Distance(Minus(Apply(jessoon, point), Apply(aligned, point)), down).Should().BeLessThan(.001d);
    }

    [Test]
    public void TheCatharMaleJessoonTakesNoKlooHornBodyFit() =>
        Fit("jessoon", AppearanceType.Cathar, 0, Gender.Male, true).Should().Be(HornFit.JessoonOnKlooHorn);

    private static (double X, double Y, double Z) Apply(HornFitOffset fit, (double X, double Y, double Z) point) =>
        HornFitTransform.Apply(fit, point.X, point.Y, point.Z);

    private static (double X, double Y, double Z) Minus((double X, double Y, double Z) a, (double X, double Y, double Z) b) =>
        (a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    private static (double X, double Y, double Z) Plus((double X, double Y, double Z) a, (double X, double Y, double Z) b) =>
        (a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    private static (double X, double Y, double Z) Scale((double X, double Y, double Z) a, double by) =>
        (a.X * by, a.Y * by, a.Z * by);

    private static double Dot((double X, double Y, double Z) a, (double X, double Y, double Z) b) =>
        a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    private static (double X, double Y, double Z) Unit((double X, double Y, double Z) a) => Scale(a, 1d / Math.Sqrt(Dot(a, a)));

    private static double Distance((double X, double Y, double Z) a, (double X, double Y, double Z) b) =>
        Math.Sqrt(Dot(Minus(a, b), Minus(a, b)));

    private static double DistanceToLine((double X, double Y, double Z) point, (double X, double Y, double Z) a,
        (double X, double Y, double Z) b)
    {
        var direction = Unit(Minus(b, a));
        var offset = Minus(point, a);
        return Distance(offset, Scale(direction, Dot(offset, direction)));
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
