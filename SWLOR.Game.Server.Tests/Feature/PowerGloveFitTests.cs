using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

[TestFixture]
public class PowerGloveFitTests
{
    [TestCase("H", Gender.Male, 0, "r", 201, 0, "pmh0_handr201")]
    [TestCase("h", Gender.Female, 0, "l", 201, 1, "pfh0_handl001")]
    [TestCase("o", Gender.Female, 0, "r", 201, 0, "pfo0_handr201")]
    [TestCase("e", Gender.Both, 2, "l", 208, 2, "pme2_handl002")]
    [TestCase("d", Gender.Male, 0, "l", 209, -1, "pmd0_handl209")]
    [TestCase("****", Gender.Male, 0, "r", 1, 1, "")]
    [TestCase("", Gender.Male, 0, "r", 1, 1, "")]
    [TestCase("h", Gender.Male, 0, "r", 0, 0, "")]
    public void ActualHandModelHonorsArmorOverrideAndNaturalFallback(
        string race, Gender gender, int phenotype, string side, int natural, int armor, string expected)
    {
        Invoke<string>("ResolveHandModel", race, gender, phenotype, side, natural, armor).Should().Be(expected);
    }

    [TestCase(101)]
    [TestCase(102)]
    [TestCase(104)]
    [TestCase(105)]
    public void BothAttachmentsUseTheLargerHandWithoutChangingTheHumanBaseline(int appearance)
    {
        PowerGloveHandFitCatalog.GetScale("pmh0_handr001", appearance).Should().Be(1f);
        PowerGloveHandFitCatalog.GetScale("pmh0_handl001", appearance).Should().Be(1f);
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

    private static T Invoke<T>(string method, params object[] arguments) =>
        (T)typeof(PowerGloveFit).GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, arguments)!;
}
