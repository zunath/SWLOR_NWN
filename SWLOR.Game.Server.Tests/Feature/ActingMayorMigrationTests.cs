using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Server migration 32's Acting Mayor step: every city starts with no Acting Mayor, no step-down, no story request, and no
/// council summons, and the ids the Acting Mayor slice reserved are used as assigned.
/// </summary>
public class ActingMayorMigrationTests
{
    [Test]
    public void ANewCityStartsWithNoActingMayorAndNoStepDown()
    {
        var city = new WorldProperty();

        city.ActingMayorPlayerId.Should().BeEmpty();
        city.ActingMayorEndsUtc.Should().BeNull();
        city.StepDown.Should().BeNull();
        city.StoryRequest.Should().BeNull();
        city.CouncilSummons.Should().BeNull();
    }

    [Test]
    public void ACityRecordFromBeforeTheActingMayor_IsStartedEmpty()
    {
        var city = new WorldProperty { ActingMayorPlayerId = null };

        ActingMayorMigration.MigrateCity(city).Should().BeTrue();

        city.ActingMayorPlayerId.Should().BeEmpty();
        city.ActingMayorEndsUtc.Should().BeNull();
        ActingMayorMigration.MigrateCity(city).Should().BeFalse("running the migration again changes nothing");
    }

    [Test]
    public void AnEndTimeLeftWithoutAPlayer_IsCleared()
    {
        var city = new WorldProperty { ActingMayorPlayerId = string.Empty, ActingMayorEndsUtc = DateTime.UtcNow.AddDays(3) };

        ActingMayorMigration.MigrateCity(city).Should().BeTrue();

        city.ActingMayorEndsUtc.Should().BeNull();
    }

    [Test]
    public void ACityThatAlreadyHasAnActingMayorAStepDownAndARequest_KeepsThem()
    {
        var ends = DateTime.UtcNow.AddDays(9);
        var city = new WorldProperty
        {
            ActingMayorPlayerId = "acting",
            ActingMayorEndsUtc = ends,
            StepDown = new CityStepDown { InterimHolderPlayerId = "interim", Stage = CityStepDownStage.Pending },
            StoryRequest = new CityStoryRequest { Text = "Ruins." }
        };

        ActingMayorMigration.MigrateCity(city).Should().BeFalse();

        city.ActingMayorPlayerId.Should().Be("acting");
        city.ActingMayorEndsUtc.Should().Be(ends);
        city.StepDown.InterimHolderPlayerId.Should().Be("interim");
        city.StoryRequest.Text.Should().Be("Ruins.");
    }

    [Test]
    public void TheStepRunsInsideTheOneCityRevampVersion()
    {
        typeof(_32_PlayerCityRevamp).Assembly.GetTypes()
            .Where(type => typeof(SWLOR.Game.Server.Service.MigrationService.IServerMigration).IsAssignableFrom(type) && type.Name.StartsWith("_32_"))
            .Should().ContainSingle("the Acting Mayor adds a step to the existing version 32 class, never a second one");
    }

    [Test]
    public void TheDMCityWindowUsesItsReservedId()
    {
        ((int)GuiWindowType.DMCities).Should().Be(113);
    }

    [Test]
    public void TheStatusAndStageValuesNeverChange()
    {
        ((int)CityStepDownStage.Pending).Should().Be(1);
        ((int)CityStepDownStage.Accepted).Should().Be(2);
        ((int)CityStoryRequestStatus.Open).Should().Be(1);
        ((int)CityStoryRequestStatus.Taken).Should().Be(2);
        ((int)CityStoryRequestStatus.Done).Should().Be(3);
    }
}
