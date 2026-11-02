using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.Game.Server.Tests.Service.Line;

/// <summary>Covers how a fish picks its next move, common and rare.</summary>
public class LineMovePatternTests
{
    [TestCase(0, FishMove.Run)]
    [TestCase(29, FishMove.Run)]
    [TestCase(30, FishMove.Dive)]
    [TestCase(54, FishMove.Dive)]
    [TestCase(55, FishMove.Thrash)]
    [TestCase(79, FishMove.Thrash)]
    [TestCase(80, FishMove.Tire)]
    [TestCase(99, FishMove.Tire)]
    public void ACommonFish_PicksEveryMoveFromTheSameWeights(int roll, FishMove expected)
    {
        var pattern = LineMovePattern.Common(20);

        foreach (FishMove? previous in new FishMove?[] { null, FishMove.Run, FishMove.Dive, FishMove.Thrash, FishMove.Tire })
            pattern.Roll(new ScriptedMinigameRandom(roll), previous).Should().Be(expected, $"after {previous}");
    }

    [Test]
    public void ACommonFishsRestWeight_IsWhatTheDifficultyGives()
    {
        LineMovePattern.Common(35).Weight(null, FishMove.Tire).Should().Be(35);
        LineMovePattern.Common(0).Weight(null, FishMove.Tire).Should().Be(2, "a fish always has some chance to rest");
        LineMovePattern.Common(35).Shares(null).Sum().Should().BeApproximately(100, 0.0001);
    }

    [TestCase(FishMove.Run, 44, FishMove.Run)]
    [TestCase(FishMove.Run, 45, FishMove.Dive)]
    [TestCase(FishMove.Run, 60, FishMove.Thrash)]
    [TestCase(FishMove.Run, 90, FishMove.Tire)]
    [TestCase(FishMove.Dive, 0, FishMove.Run)]
    [TestCase(FishMove.Dive, 15, FishMove.Dive)]
    [TestCase(FishMove.Dive, 35, FishMove.Thrash)]
    [TestCase(FishMove.Dive, 99, FishMove.Tire)]
    public void ARareFish_ChainsItsMoves(FishMove previous, int roll, FishMove expected)
    {
        LineMovePattern.Rare(20).Roll(new ScriptedMinigameRandom(roll), previous).Should().Be(expected);
    }

    [Test]
    public void ARareFish_KeepsRunningAfterARun_TurnsADiveIntoAThrash_AndRarelyRestsTwice()
    {
        var rare = LineMovePattern.Rare(20);
        var common = LineMovePattern.Common(20);

        rare.Shares(FishMove.Run)[0].Should().BeGreaterThan(common.Shares(FishMove.Run)[0]);
        rare.Shares(FishMove.Dive)[2].Should().BeGreaterThan(common.Shares(FishMove.Dive)[2]);
        rare.Shares(FishMove.Tire)[3].Should().BeLessThan(common.Shares(FishMove.Tire)[3]);
        rare.Shares(null)[3].Should().BeApproximately(common.Shares(null)[3], 0.0001, "the first move is picked like a common fish's");
    }

    [Test]
    public void ARareFishsRestWeight_MovesHalfAsFarAsACommonFishs()
    {
        var easy = LineMovePattern.Rare(40);
        var hard = LineMovePattern.Rare(20);

        (easy.Weight(null, FishMove.Tire) - hard.Weight(null, FishMove.Tire)).Should().Be(10);
        LineMovePattern.Rare(0).Weight(FishMove.Tire, FishMove.Tire).Should().BeGreaterThanOrEqualTo(2);
    }

    [Test]
    public void ThePatternForARarity_IsTheRarePatternOnlyForARareFish()
    {
        LineMovePattern.For(FishRarity.Rare, 20).Weight(FishMove.Run, FishMove.Run).Should().Be(45);
        LineMovePattern.For(FishRarity.Common, 20).Weight(FishMove.Run, FishMove.Run).Should().Be(30);
    }

    [Test]
    public void EveryRoll_PicksAMove_AndTheFirstMoveHasItsOwnRow()
    {
        var pattern = LineMovePattern.Rare(30);
        var picker = new SeededMinigameRandom(3);

        for (var index = 0; index < 500; index++)
        {
            var move = pattern.Roll(picker, index % 5 == 0 ? null : (FishMove)(index % 4 + 1));
            Enum.IsDefined(typeof(FishMove), move).Should().BeTrue();
        }
    }
}
