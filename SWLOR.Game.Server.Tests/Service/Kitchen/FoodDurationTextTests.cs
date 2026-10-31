using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.Game.Server.Tests.Service.Kitchen;

/// <summary>Covers the description line that makes a crafted food's bonus duration visible on the item.</summary>
public class FoodDurationTextTests
{
    [TestCase(300, "5 min")]
    [TestCase(150, "2 min 30 s")]
    [TestCase(75, "1 min 15 s")]
    [TestCase(60, "1 min")]
    [TestCase(45, "45 s")]
    [TestCase(1, "1 s")]
    public void TheBonusIsShownAsMinutesAndSeconds(int seconds, string expected)
    {
        FoodDurationText.Format(seconds).Should().Be(expected);
        FoodDurationText.Line(seconds).Should().Be($"Bonus Duration: +{expected}");
    }

    [Test]
    public void AFoodWithNoDescription_GetsJustTheLine()
    {
        FoodDurationText.AppendTo(string.Empty, 150).Should().Be("Bonus Duration: +2 min 30 s");
        FoodDurationText.AppendTo(null, 150).Should().Be("Bonus Duration: +2 min 30 s");
    }

    [Test]
    public void AFoodWithADescription_GetsTheLineAfterABlankLine()
    {
        FoodDurationText.AppendTo("A hearty meal.", 150).Should().Be("A hearty meal.\n\nBonus Duration: +2 min 30 s");
    }

    [Test]
    public void ApplyingTheBonusTwice_NeverDoublesTheLine()
    {
        var once = FoodDurationText.AppendTo("A hearty meal.", 150);

        var twice = FoodDurationText.AppendTo(once, 200);

        twice.Should().Be("A hearty meal.\n\nBonus Duration: +3 min 20 s");
    }

    [Test]
    public void NoBonus_AddsNoLineAndRemovesAStaleOne()
    {
        FoodDurationText.AppendTo("A hearty meal.", 0).Should().Be("A hearty meal.");
        FoodDurationText.AppendTo("A hearty meal.\n\nBonus Duration: +1 min", 0).Should().Be("A hearty meal.");
    }

    [Test]
    public void TheFullQualityBonusOfOneStepIsFiveMinutes()
    {
        var seconds = CraftOutcomeRules.CalculateFoodDurationBonusSeconds(1f);

        FoodDurationText.Line(seconds).Should().Be("Bonus Duration: +5 min");
    }

    [Test]
    public void TheCraftSessionWorldWritesTheLineWhereTheLocalIsSet()
    {
        // The item local is applied by NwnCraftSessionWorld, which needs the engine; keep the description call beside it.
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();

        var source = File.ReadAllText(Path.Combine(
            directory!.FullName, "SWLOR.Game.Server", "Service", "CraftService", "Minigame", "NwnCraftSessionWorld.cs"));

        source.Should().Contain("SetDescription(item, FoodDurationText.AppendTo(GetDescription(item), durationSeconds))");
    }
}
