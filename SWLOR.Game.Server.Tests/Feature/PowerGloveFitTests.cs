using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWScript.Enum;
using FitVector = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Tests.Feature;

[TestFixture]
public class PowerGloveFitTests
{
    [TestCase("H", Gender.Male, 0, "r", 201, 0, "pmh0_handr201")]
    [TestCase("h", Gender.Female, 0, "l", 201, 1, "pfh0_handl201")]
    [TestCase("o", Gender.Male, 0, "r", 201, 1, "pmo0_handr201")]
    [TestCase("o", Gender.Female, 0, "r", 201, 0, "pfo0_handr201")]
    [TestCase("e", Gender.Both, 2, "l", 208, 2, "pme2_handl208")]
    [TestCase("h", Gender.Male, 0, "r", 1, 2, "pmh0_handr001")]
    [TestCase("h", Gender.Male, 0, "l", 2, 2, "pmh0_handl002")]
    [TestCase("o", Gender.Male, 0, "r", 201, 2, "pmo0_handr201")]
    [TestCase("h", Gender.Male, 0, "r", 1, 4, "pmh0_handr004")]
    [TestCase("h", Gender.Male, 0, "l", 1, 6, "pmh0_handl006")]
    [TestCase("d", Gender.Male, 0, "l", 209, -1, "pmd0_handl209")]
    [TestCase("****", Gender.Male, 0, "r", 1, 1, "")]
    [TestCase("", Gender.Male, 0, "r", 1, 1, "")]
    [TestCase("h", Gender.Male, 0, "r", 0, 0, "")]
    public void ActualHandModelHonorsArmorOverrideAndNaturalFallback(
        string race, Gender gender, int phenotype, string side, int natural, int armor, string expected)
    {
        Invoke<string>("ResolveHandModel", race, gender, phenotype, side, natural, armor).Should().Be(expected);
    }

    [TestCase("pmo2_handr201", 2, false, true, "pmo0_handr201")]
    [TestCase("pmo2_handl201", 2, false, true, "pmo0_handl201")]
    [TestCase("pfe2_handl208", 2, false, true, "pfe0_handl208")]
    [TestCase("pmh2_handr001", 2, true, true, "pmh2_handr001")]
    [TestCase("pmh2_handl250", 2, false, false, "pmh2_handl250")]
    [TestCase("pmo0_handr201", 0, false, true, "pmo0_handr201")]
    [TestCase("pmo1_handr201", 1, false, true, "pmo1_handr201")]
    [TestCase("", 2, false, true, "")]
    public void WideHandSelectionUsesNativeResourceFallbackOnlyWhenTheWidePartIsAbsent(
        string requested, int phenotype, bool requestedExists, bool normalExists, string expected)
    {
        Func<string, bool> available = model => model == requested ? requestedExists : normalExists;
        Invoke<string>("ResolveAvailableHandModel", requested, phenotype, available).Should().Be(expected);
    }

    [TestCase("pmh2", 1.30933f)]
    [TestCase("pfh2", 1.08836f)]
    [TestCase("pme2", 1.04746f)]
    [TestCase("pfe2", .87069f)]
    public void WideCuffsUseTheMeasuredHandInsteadOfTheUnknownHandMaximum(string body, float scale)
    {
        foreach (var appearance in new[] { 101, 102 })
        {
            Invoke<float>("ResolveScale", body + "_handr001", body + "_handl001", appearance)
                .Should().BeApproximately(scale, .000001f);
            PowerGloveHandFitCatalog.GetScale((body + "_handr001").ToUpperInvariant(), appearance)
                .Should().BeApproximately(scale, .000001f);
        }
    }

    [TestCase(101)]
    [TestCase(103)]
    [TestCase(107)]
    [TestCase(119)]
    public void AMeasuredOverrideRequiresBothHandsAndInvisibleAlwaysReleases(int appearance)
    {
        foreach (var unknown in new[] { "pmh2_handl250", "unmeasured", "", null })
        {
            Invoke<bool>("CanFitPair", "pmh2_handr001", unknown, appearance).Should().BeFalse();
            Invoke<bool>("CanFitPair", unknown, "pmh2_handl001", appearance).Should().BeFalse();
            Invoke<float>("ResolveScale", "pmh2_handr001", unknown, appearance).Should().Be(1f);
            Invoke<float>("ResolveScale", unknown, "pmh2_handl001", appearance).Should().Be(1f);
        }
        Invoke<float>("ResolveScale", "pmh2_handr001", "pmh2_handl001", 106).Should().Be(1f);
        AssertVector(GripTranslation("pmh2_handr001", 106, 1f), FitVector.Zero);
        AssertVector(GripTranslation("pmh2_handl001", 106, 1f), FitVector.Zero);
    }

    [Test]
    public void MixedWideAndNormalHandsShareScaleWithoutMovingTheMeasuredGripCenter()
    {
        const int appearance = 119;
        const string right = "pmh2_handr001";
        const string left = "pmo0_handl201";
        var scale = Invoke<float>("ResolveScale", right, left, appearance);
        scale.Should().Be(1.4f);
        Invoke<float>("ResolveScale", left, right, appearance).Should().Be(scale);
        var authored = new FitVector(-.00126715f, .035f, -.0100633f);
        var target = new FitVector(-.004734298f, .032013342f, -.036380943f);
        AssertVector(scale * authored + GripTranslation(right, appearance, scale), target);
        AssertVector(GripTranslation(right.ToUpperInvariant(), appearance, scale),
            GripTranslation(right, appearance, scale));
        AssertVector(GripTranslation(left, appearance, scale),
            GripTranslation(left, appearance, Invoke<float>("ResolveScale", "pmo0_handr201", left, appearance)));
    }

    [Test]
    public void WideBlastknuckleCentersItsBarWithoutChangingItsAuthoredSize()
    {
        Invoke<float>("ResolveScale", "pmh2_handr001", "pmh2_handl001", 103).Should().Be(1f);
        AssertVector(GripTranslation("pmh2_handr001", 103, 1f),
            new FitVector(.032550703f, -.0029866584f, .020286098f));
        AssertVector(GripTranslation("pmh0_handr001", 103, 1f), FitVector.Zero);
    }

    [TestCase("pfo0", 201, 104, 1.6129032f, .015f, -.01125f, .00375f, -.015f, -.01125f, .00375f)]
    [TestCase("pfo0", 201, 105, 1.724138f, .01125f, -.0225f, .0225f, -.015f, -.0225f, .01875f)]
    [TestCase("pfh0", 201, 105, 1.05f, -.01125f, -.02625f, 0f, -.0075f, -.0225f, .015f)]
    [TestCase("pfh0", 204, 103, 1f, .045488555f, -.020728618f, .077377297f, .032174461f, -.037205398f, .077603899f)]
    [TestCase("pfd0", 209, 104, 1.26f, 0f, -.0225f, -.00375f, 0f, -.00375f, .00375f)]
    [TestCase("pfd0", 209, 105, 1.26f, -.00375f, -.02625f, .0225f, -.00375f, -.00375f, .02625f)]
    [TestCase("pfd0", 1, 104, 1.14f, .023f, -.015f, .01875f, -.015f, -.02625f, .015f)]
    [TestCase("pfd0", 1, 105, 1.14f, .00375f, -.02625f, .0225f, -.02f, -.02625f, .01875f)]
    public void NormalSpeciesCorrectionsKeepWeaponSizeAndUseSeparateHandOffsets(string body, int hand,
        int appearance, float scale, float rx, float ry, float rz, float lx, float ly, float lz)
    {
        var right = $"{body}_handr{hand:D3}";
        var left = $"{body}_handl{hand:D3}";
        Invoke<float>("ResolveScale", right, left, appearance).Should().BeApproximately(scale, .000001f);
        AssertVector(GripTranslation(right, appearance, scale), new FitVector(rx, ry, rz));
        AssertVector(GripTranslation(left, appearance, scale), new FitVector(lx, ly, lz));
        Invoke<bool>("CanFitPair", right, "unmeasured", appearance).Should().BeFalse();
        Invoke<bool>("CanFitPair", "unmeasured", left, appearance).Should().BeFalse();
        AssertVector(GripTranslation(right, 106, 1f), FitVector.Zero);
        AssertVector(GripTranslation(left, 106, 1f), FitVector.Zero);
    }

    [TestCase(101)]
    [TestCase(102)]
    [TestCase(104)]
    [TestCase(105)]
    public void BothAttachmentsUseTheLargerHandWithoutChangingTheHumanBaseline(int appearance)
    {
        PowerGloveHandFitCatalog.GetScale("pmh0_handr001", appearance).Should().Be(1f);
        PowerGloveHandFitCatalog.GetScale("pmh0_handl001", appearance).Should().Be(1f);
        PowerGloveHandFitCatalog.GetScale("pfh0_handr109", appearance).Should().Be(1f);
        PowerGloveHandFitCatalog.GetScale("pfh0_handl109", appearance).Should().Be(1f);
        var larger = PowerGloveHandFitCatalog.GetScale("pme0_handl208", appearance);
        larger.Should().BeGreaterThan(1f);
        Invoke<float>("ResolveScale", "pmh0_handr001", "pme0_handl208", appearance).Should().Be(larger);
        Invoke<float>("ResolveScale", "pme0_handl208", "pmh0_handr001", appearance).Should().Be(larger);
    }

    [TestCase(101, .65f)]
    [TestCase(102, .60f)]
    [TestCase(104, .62f)]
    [TestCase(105, .58f)]
    public void UnmeasuredHandsRestoreTheOriginalSizeAndMeasuredFitsStayWithinIt(int appearance, float compact)
    {
        var maximum = 1f / compact;
        PowerGloveHandFitCatalog.GetScale("pmh0_handr999", appearance).Should().BeApproximately(maximum, .0001f);
        PowerGloveHandFitCatalog.GetScale(null, appearance).Should().BeApproximately(maximum, .0001f);
        foreach (var hand in new[] { "pmh0_handr001", "pmo0_handl201", "pmd0_handr209", "pfe0_handr208" })
        {
            var scale = PowerGloveHandFitCatalog.GetScale(hand, appearance);
            scale.Should().BeInRange(1f, maximum);
            PowerGloveHandFitCatalog.GetScale(hand.ToUpperInvariant(), appearance).Should().Be(scale);
        }
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(103)]
    [TestCase(106)]
    [TestCase(201)]
    public void BlastknuckleInvisibleAndOtherAppearancesAlwaysReleaseFit(int appearance)
    {
        Invoke<float>("ResolveScale", "pmo0_handr201", "pmo0_handl201", appearance).Should().Be(1f);
        PowerGloveHandFitCatalog.GetScale("unmeasured", appearance).Should().Be(1f);
    }

    [TestCase(107, 1.16f, 1.00f, 1.48f)]
    [TestCase(108, 1.22f, 1.24f, 1.86f)]
    [TestCase(109, 1.14f, 1.14f, 1.82f)]
    [TestCase(110, 1.00f, 1.00f, 1.00f)]
    [TestCase(111, 1.24f, 1.26f, 1.84f)]
    [TestCase(112, 1.00f, 1.00f, 1.00f)]
    [TestCase(113, 1.00f, 1.00f, 1.00f)]
    [TestCase(114, 1.22f, 1.20f, 1.76f)]
    [TestCase(115, 1.40f, 1.40f, 1.92f)]
    [TestCase(116, 1.00f, 1.00f, 1.00f)]
    [TestCase(117, 1.22f, 1.26f, 1.68f)]
    [TestCase(118, 1.00f, 1.00f, 1.00f)]
    [TestCase(119, 1.10f, 1.10f, 1.00f)]
    [TestCase(120, 1.10f, 1.10f, 1.00f)]
    [TestCase(121, 1.00f, 1.00f, 1.00f)]
    public void OpenFramesUseTheirOwnMeasuredClearanceOnBothHands(
        int appearance, float right, float left, float fallback)
    {
        foreach (var hand in new[] { "pmh0_handr001", "pmh0_handl001", "pfh0_handr001", "pfh0_handl001" })
            PowerGloveHandFitCatalog.GetScale(hand, appearance).Should().Be(1f);

        PowerGloveHandFitCatalog.GetScale("pmh0_handr201", appearance).Should().Be(right);
        PowerGloveHandFitCatalog.GetScale("PMH0_HANDL201", appearance).Should().Be(left);
        var pair = Math.Max(right, left);
        Invoke<float>("ResolveScale", "pmh0_handr201", "pmh0_handl201", appearance).Should().Be(pair);
        Invoke<float>("ResolveScale", "pmh0_handl201", "pmh0_handr201", appearance).Should().Be(pair);

        PowerGloveHandFitCatalog.GetScale("pmh0_handr999", appearance).Should().Be(fallback);
        PowerGloveHandFitCatalog.GetScale(null, appearance).Should().Be(fallback);
        Invoke<float>("ResolveScale", "pmh0_handr001", "unknown", appearance).Should().Be(fallback);
    }

    [TestCase(119, "pfe0_handr208", "pfe0_handl208", 1.30f)]
    [TestCase(121, "pmh0_handr045", "pmh0_handl045", 1.00f)]
    public void PairClearanceIncludesTheValidatedSharedScaleCorrection(
        int appearance, string right, string left, float expected)
    {
        Invoke<float>("ResolveScale", right, left, appearance).Should().Be(expected);
        Invoke<float>("ResolveScale", left, right, appearance).Should().Be(expected);
    }

    [Test]
    public void RepeatedGripRebasingPreservesAuthoredEditsAndReleasesItsOwnDelta()
    {
        var authored = new FitVector(.013f, -.021f, .007f);
        var current = authored;
        var previous = FitVector.Zero;
        for (var pass = 0; pass < 100; pass++)
        {
            var next = pass % 2 == 0
                ? new FitVector(-.017f, .009f, -.039f)
                : new FitVector(.023f, -.014f, -.051f);
            current = Invoke<FitVector>("RebaseTranslation", current, previous, next);
            AssertVector(current, authored + next);
            previous = next;
            if (pass != 49) continue;
            var edit = new FitVector(.004f, -.003f, .002f);
            authored += edit;
            current += edit;
        }
        AssertVector(Invoke<FitVector>("RebaseTranslation", current, previous, FitVector.Zero), authored);
    }

    [TestCase(110, 1.16f, 0f, -.015f)]
    [TestCase(112, 1.20f, 0f, -.025f)]
    [TestCase(113, 1.36f, 0f, -.015f)]
    [TestCase(116, 1.36f, 0f, -.025f)]
    [TestCase(118, 1.00f, 0f, -.015f)]
    [TestCase(119, 1.40f, -.001267151f, -.0100633f)]
    [TestCase(120, 1.24f, 0f, -.020f)]
    [TestCase(121, 1.48f, 0f, -.025f)]
    public void CorrectedGripsHaveIndependentHandOffsetsAndCompensateForTheSharedScale(
        int appearance, float catharScale, float authoredX, float authoredDepth)
    {
        foreach (var side in new[] { "r", "l" })
        {
            AssertVector(GripTranslation($"pmh0_hand{side}001", appearance, 1f), FitVector.Zero);
            AssertVector(GripTranslation($"pmh0_hand{side}001", appearance, 1.5f),
                -.5f * new FitVector(authoredX, .035f, authoredDepth));
            AssertVector(GripTranslation($"pmo0_hand{side}201".ToUpperInvariant(), appearance, catharScale),
                GripTranslation($"pmo0_hand{side}201", appearance, catharScale));
        }
        Invoke<float>("ResolveScale", "pmo0_handr201", "pmo0_handl201", appearance).Should().Be(catharScale);
        var right = GripTranslation("pmo0_handr201", appearance, catharScale);
        var left = GripTranslation("pmo0_handl201", appearance, catharScale);
        right.LengthSquared().Should().BeGreaterThan(0f);
        left.LengthSquared().Should().BeGreaterThan(0f);
        FitVector.Distance(right, left).Should().BeGreaterThan(.001f);
        AssertVector(GripTranslation("unmeasured", appearance, 1f), FitVector.Zero);
        AssertVector(GripTranslation(null, appearance, 1f), FitVector.Zero);
    }

    [TestCase(101)]
    [TestCase(102)]
    [TestCase(103)]
    [TestCase(104)]
    [TestCase(105)]
    [TestCase(106)]
    [TestCase(107)]
    [TestCase(108)]
    [TestCase(109)]
    [TestCase(111)]
    [TestCase(114)]
    [TestCase(115)]
    [TestCase(117)]
    public void OtherAppearancesNeverAcquireGripTranslation(int appearance)
    {
        AssertVector(GripTranslation("pmo0_handr201", appearance, 1.92f), FitVector.Zero);
        AssertVector(GripTranslation("pmo0_handl201", appearance, 1.92f), FitVector.Zero);
    }

    [TestCase(110)]
    [TestCase(112)]
    [TestCase(113)]
    [TestCase(116)]
    [TestCase(118)]
    [TestCase(119)]
    [TestCase(120)]
    [TestCase(121)]
    public void EitherUnmeasuredHandReleasesTheEntireCorrectedPair(int appearance)
    {
        Invoke<bool>("CanFitPair", "pmo0_handr201", "pmo0_handl201", appearance).Should().BeTrue();
        foreach (var unknown in new[] { "pmo0_handl250", "unknown", "", null })
        {
            Invoke<bool>("CanFitPair", "pmo0_handr201", unknown, appearance).Should().BeFalse();
            Invoke<bool>("CanFitPair", unknown, "pmo0_handl201", appearance).Should().BeFalse();
            Invoke<float>("ResolveScale", "pmo0_handr201", unknown, appearance).Should().Be(1f);
            Invoke<float>("ResolveScale", unknown, "pmo0_handl201", appearance).Should().Be(1f);
        }
    }

    [TestCase(101)]
    [TestCase(102)]
    [TestCase(104)]
    [TestCase(105)]
    [TestCase(107)]
    [TestCase(108)]
    [TestCase(109)]
    [TestCase(111)]
    [TestCase(114)]
    [TestCase(115)]
    [TestCase(117)]
    public void UnmeasuredHandDoesNotChangeOtherAppearancesExistingScalePolicy(int appearance)
    {
        Invoke<bool>("CanFitPair", "pmo0_handr201", "unknown", appearance).Should().BeTrue();
        var expected = Math.Max(PowerGloveHandFitCatalog.GetScale("pmo0_handr201", appearance),
            PowerGloveHandFitCatalog.GetScale("unknown", appearance));
        Invoke<float>("ResolveScale", "pmo0_handr201", "unknown", appearance).Should().Be(expected);
        Invoke<float>("ResolveScale", "unknown", "pmo0_handr201", appearance).Should().Be(expected);
    }

    private static FitVector GripTranslation(string hand, int appearance, float scale) =>
        (FitVector)typeof(PowerGloveFit).Assembly.GetType(
                "SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.PowerGloveGripFits")!
            .GetMethod("GetTranslation", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { hand, appearance, scale })!;

    private static void AssertVector(FitVector actual, FitVector expected)
    {
        actual.X.Should().BeApproximately(expected.X, .000001f);
        actual.Y.Should().BeApproximately(expected.Y, .000001f);
        actual.Z.Should().BeApproximately(expected.Z, .000001f);
    }

    private static T Invoke<T>(string method, params object[] arguments) =>
        (T)typeof(PowerGloveFit).GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, arguments)!;
}
