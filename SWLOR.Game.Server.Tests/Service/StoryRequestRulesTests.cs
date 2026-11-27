using System;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>A city's story request: who posts it, its length, and how a DM takes it and marks it done.</summary>
public class StoryRequestRulesTests
{
    private static CityStoryRequest Request(CityStoryRequestStatus status) => new()
    {
        Text = "Ruins by the road.",
        PosterPlayerId = "poster",
        PosterTitle = "Mayor",
        PostedUtc = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc),
        Status = status
    };

    #region Posting

    [TestCase(CityRole.Mayor)]
    [TestCase(CityRole.ActingMayor)]
    [TestCase(CityRole.Officer)]
    public void TheMayorAnActingMayorAndOfficersCanPost(CityRole role)
    {
        StoryRequestRules.CheckPost(role, "A smuggling plot needs a hand.").Should().BeEmpty();
    }

    [TestCase(CityRole.None)]
    [TestCase(CityRole.DepartedMayor)]
    public void NoOneElseCanPost(CityRole role)
    {
        StoryRequestRules.CheckPost(role, "A smuggling plot needs a hand.").Should().Contain("Only the mayor");
    }

    [Test]
    public void AnEmptyRequestIsRefused()
    {
        StoryRequestRules.CheckPost(CityRole.Mayor, "   ").Should().Contain("Write what you want");
        StoryRequestRules.CheckPost(CityRole.Mayor, null).Should().Contain("Write what you want");
    }

    [Test]
    public void ARequestIsAtMostSixHundredCharacters()
    {
        StoryRequestRules.MaxLength.Should().Be(600);
        StoryRequestRules.Normalize(new string('a', 900)).Should().HaveLength(600);
        StoryRequestRules.Normalize(new string('a', 600)).Should().HaveLength(600);
        StoryRequestRules.Normalize(new string('a', 40)).Should().HaveLength(40);
    }

    [Test]
    public void TheRequestIsTrimmed_AndKeepsOnlyLineBreaksOfTheControlCharacters()
    {
        StoryRequestRules.Normalize("  Ruins\r\nnear the road.\u0007  ").Should().Be("Ruins\nnear the road.");
        StoryRequestRules.Normalize(null).Should().BeEmpty();
    }

    [Test]
    public void ARequestOfOnlyControlCharactersIsEmpty()
    {
        StoryRequestRules.CheckPost(CityRole.Mayor, "\u0007\u0007").Should().Contain("Write what you want");
    }

    #endregion

    #region Status

    [Test]
    public void ADMCanTakeOnlyAnOpenRequest()
    {
        StoryRequestRules.CanTake(Request(CityStoryRequestStatus.Open)).Should().BeTrue();
        StoryRequestRules.CanTake(Request(CityStoryRequestStatus.Taken)).Should().BeFalse();
        StoryRequestRules.CanTake(Request(CityStoryRequestStatus.Done)).Should().BeFalse();
        StoryRequestRules.CanTake(null).Should().BeFalse();
    }

    [Test]
    public void ADMCanFinishAnOpenOrTakenRequest()
    {
        StoryRequestRules.CanFinish(Request(CityStoryRequestStatus.Open)).Should().BeTrue();
        StoryRequestRules.CanFinish(Request(CityStoryRequestStatus.Taken)).Should().BeTrue();
        StoryRequestRules.CanFinish(Request(CityStoryRequestStatus.Done)).Should().BeFalse();
        StoryRequestRules.CanFinish(null).Should().BeFalse();
    }

    [Test]
    public void OnlyAnOpenRequestIsOpen()
    {
        StoryRequestRules.IsOpen(Request(CityStoryRequestStatus.Open)).Should().BeTrue();
        StoryRequestRules.IsOpen(Request(CityStoryRequestStatus.Taken)).Should().BeFalse();
        StoryRequestRules.IsOpen(null).Should().BeFalse();
    }

    [Test]
    public void ANewRequestStartsOpen()
    {
        new CityStoryRequest().Status.Should().Be(CityStoryRequestStatus.Open);
    }

    [Test]
    public void EachStatusHasAName()
    {
        StoryRequestRules.DescribeStatus(CityStoryRequestStatus.Open).Should().Be("Open");
        StoryRequestRules.DescribeStatus(CityStoryRequestStatus.Taken).Should().Be("Taken");
        StoryRequestRules.DescribeStatus(CityStoryRequestStatus.Done).Should().Be("Done");
    }

    #endregion

    #region DM feed

    [Test]
    public void TheFeedDescriptionNamesTheCityAndThePoster()
    {
        var description = StoryRequestRules.BuildFeedDescription("Dawnreach", "Town", 2, "Aria Vale", "Mayor", "Ruins by the road.");

        description.Should().Contain("**City**: Dawnreach");
        description.Should().Contain("**City Level**: Town (Lvl. 2)");
        description.Should().Contain("**Posted by**: Aria Vale (Mayor)");
        description.Should().EndWith("Ruins by the road.");
    }

    [Test]
    public void WithoutACharacterName_TheFeedShowsTheTitle()
    {
        StoryRequestRules.BuildFeedDescription("Dawnreach", "Town", 2, string.Empty, "Officer", "Hello.")
            .Should().Contain("**Posted by**: Officer\n");
    }

    #endregion
}
