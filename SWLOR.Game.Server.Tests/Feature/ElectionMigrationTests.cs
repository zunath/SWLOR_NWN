using System;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>Server migration 32's election step: stage dates for an open election and citizen-since for citizens.</summary>
public class ElectionMigrationTests
{
    private static readonly DateTime ElectionStart = new(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc);

    [Test]
    public void Migration32_RunsAfterTheDatabaseLoads()
    {
        var migration = new _32_PlayerCityRevamp();

        migration.Version.Should().Be(32);
        migration.ExecutionType.Should().Be(MigrationExecutionType.PostDatabaseLoad);
    }

    [Test]
    public void OpenElection_GetsItsStageDatesFromTheOldCalendar()
    {
        var election = new Election { Stage = ElectionStageType.Registration };

        var changed = ElectionMigration.MigrateElection(election, ElectionStart);

        changed.Should().BeTrue();
        election.Kind.Should().Be(ElectionKind.Regular);
        election.RegistrationOpenedUtc.Should().Be(ElectionStart);
        election.RegistrationClosesUtc.Should().Be(ElectionStart.AddDays(14), "an election opened for 14 days of registration");
        election.VotingClosesUtc.Should().Be(ElectionStart.AddDays(21));
    }

    [Test]
    public void OpenElection_KeepsTheOldDatesEvenThoughTheNewCalendarIsShorter()
    {
        var election = new Election { Stage = ElectionStageType.Registration };
        ElectionMigration.MigrateElection(election, ElectionStart);

        // Eight days in, the 7-day calendar would have closed registration, but this election was told 14.
        ElectionRules.GetDueAction(election.Stage, ElectionStart.AddDays(8), election.RegistrationClosesUtc, election.VotingClosesUtc)
            .Should().Be(ElectionStageAction.None);
        ElectionRules.GetDueAction(election.Stage, ElectionStart.AddDays(14), election.RegistrationClosesUtc, election.VotingClosesUtc)
            .Should().Be(ElectionStageAction.CloseRegistration);
    }

    [Test]
    public void ElectionAlreadyInVoting_FinishesOnTheDateTheOldCalendarGave()
    {
        var election = new Election { Stage = ElectionStageType.Voting };
        election.CandidatePlayerIds.Add("a");
        election.CandidatePlayerIds.Add("b");

        ElectionMigration.MigrateElection(election, ElectionStart);

        election.IsConfidenceVote.Should().BeFalse("a voting election under the old rules always had two or more candidates");
        ElectionRules.GetDueAction(election.Stage, ElectionStart.AddDays(20), election.RegistrationClosesUtc, election.VotingClosesUtc)
            .Should().Be(ElectionStageAction.None);
        ElectionRules.GetDueAction(election.Stage, ElectionStart.AddDays(21), election.RegistrationClosesUtc, election.VotingClosesUtc)
            .Should().Be(ElectionStageAction.CloseVoting);
    }

    [Test]
    public void ElectionAlreadyInVoting_FinishesWithoutPlatformsSoItsWinnerKeepsTheCitysEdicts()
    {
        var election = new Election { Stage = ElectionStageType.Voting };
        election.CandidatePlayerIds.Add("a");
        election.CandidatePlayerIds.Add("b");

        ElectionMigration.MigrateElection(election, ElectionStart);

        election.Platforms.Should().BeEmpty("the migration adds no platforms to a vote already under way");
        ElectionPlatformRules.GetPlatformToEnact(election.Platforms, "a").Should().BeNull("its winner fills Edict slots from Manage City");
        ElectionPlatformRules.GetPlatformToEnact(election.Platforms, "b").Should().BeNull();
    }

    [Test]
    public void ElectionStillInRegistration_AcceptsPlatformsWhileItsCandidatesHaveNone()
    {
        var election = new Election { Stage = ElectionStageType.Registration };
        election.CandidatePlayerIds.Add("a");

        ElectionMigration.MigrateElection(election, ElectionStart);

        election.Platforms.Should().BeEmpty("a candidate who registered earlier adds a platform by editing theirs");
        ElectionRules.IsRegistrationOpen(election.Stage, ElectionStart.AddDays(10), election.RegistrationClosesUtc)
            .Should().BeTrue("registration is still open under the 14-day calendar, so candidates can add platforms");
        ElectionPlatformRules.GetPlatformToEnact(election.Platforms, "a").Should().BeNull("until they add one, winning leaves the Edicts alone");

        election.Platforms["a"] = new ElectionPlatform();
        ElectionPlatformRules.GetPlatformToEnact(election.Platforms, "a").Should().NotBeNull();
    }

    [Test]
    public void ElectionWhoseCityIsGone_UsesItsCreationTime()
    {
        var election = new Election();

        ElectionMigration.MigrateElection(election, null);

        election.RegistrationOpenedUtc.Should().Be(election.DateCreated);
        election.RegistrationClosesUtc.Should().Be(election.DateCreated.AddDays(14));
    }

    [Test]
    public void RunningTheMigrationAgainChangesNothing()
    {
        var election = new Election();
        ElectionMigration.MigrateElection(election, ElectionStart);
        var opened = election.RegistrationOpenedUtc;
        var closes = election.RegistrationClosesUtc;

        var changed = ElectionMigration.MigrateElection(election, ElectionStart.AddDays(3));

        changed.Should().BeFalse();
        election.RegistrationOpenedUtc.Should().Be(opened);
        election.RegistrationClosesUtc.Should().Be(closes);
    }

    [Test]
    public void AnElectionThatAlreadyHasNewDatesIsNotTouched()
    {
        var opened = new DateTime(2026, 11, 3, 0, 0, 0, DateTimeKind.Utc);
        var election = new Election
        {
            Kind = ElectionKind.SnapConfidence,
            RegistrationOpenedUtc = opened,
            RegistrationClosesUtc = opened.AddDays(7),
            VotingClosesUtc = opened.AddDays(14)
        };

        ElectionMigration.MigrateElection(election, ElectionStart).Should().BeFalse();
        election.Kind.Should().Be(ElectionKind.SnapConfidence);
        election.RegistrationClosesUtc.Should().Be(opened.AddDays(7));
    }

    [Test]
    public void CurrentCitizens_QualifyAtOnce()
    {
        var citizen = new Player { CitizenPropertyId = "city" };

        ElectionMigration.MigrateCitizen(citizen).Should().BeTrue();

        citizen.CitizenSinceUtc.Should().Be(ElectionMigration.LongStandingCitizenUtc);
        ElectionRules.IsEligibleVoter("city", "city", false, citizen.CitizenSinceUtc, ElectionStart).Should().BeTrue();
    }

    [Test]
    public void CitizensWithoutACity_AreLeftAlone()
    {
        ElectionMigration.MigrateCitizen(new Player { CitizenPropertyId = string.Empty }).Should().BeFalse();
        ElectionMigration.MigrateCitizen(new Player { CitizenPropertyId = null }).Should().BeFalse();
        ElectionMigration.MigrateCitizen(new Player { CitizenPropertyId = Guid.Empty.ToString() }).Should().BeFalse();
    }

    [Test]
    public void ACitizenWhoAlreadyHasATime_KeepsIt()
    {
        var joined = new DateTime(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc);
        var citizen = new Player { CitizenPropertyId = "city", CitizenSinceUtc = joined };

        ElectionMigration.MigrateCitizen(citizen).Should().BeFalse();
        citizen.CitizenSinceUtc.Should().Be(joined);
    }
}
