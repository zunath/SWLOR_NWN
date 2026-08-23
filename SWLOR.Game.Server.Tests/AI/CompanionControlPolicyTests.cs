using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CompanionControlService;
using SWLOR.NWN.API.NWScript.Enum;
using AssociateCommand = SWLOR.NWN.API.NWScript.Enum.Associate.Command;

namespace SWLOR.Game.Server.Tests.AI;

public class CompanionControlPolicyTests
{
    [Test]
    public void FollowAndGuardUseTheirDesignedCombatTethers()
    {
        CompanionControlPolicy.GetTetherMeters(
                CompanionMode.Follow,
                CompanionEngagementType.Defensive)
            .Should()
            .Be(15f);

        CompanionControlPolicy.GetTetherMeters(
                CompanionMode.Guard,
                CompanionEngagementType.Defensive)
            .Should()
            .Be(8f);
    }

    [Test]
    public void AttackNearestIsOneShotAndUsesItsOwnRange()
    {
        CompanionControlPolicy.GetTetherMeters(
                CompanionMode.StandGround,
                CompanionEngagementType.AttackNearest)
            .Should()
            .Be(15f);
        CompanionControlPolicy.ReturnsToFollowWhenComplete(CompanionEngagementType.AttackNearest)
            .Should()
            .BeTrue();
        CompanionControlPolicy.ReturnsToFollowWhenComplete(CompanionEngagementType.Defensive)
            .Should()
            .BeFalse();
    }

    [Test]
    public void BlockedPathTimesOutAtFiveSeconds()
    {
        var startedAt = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);

        CompanionControlPolicy.HasPathingTimedOut(startedAt, startedAt.AddSeconds(4.999))
            .Should()
            .BeFalse();
        CompanionControlPolicy.HasPathingTimedOut(startedAt, startedAt.AddSeconds(5))
            .Should()
            .BeTrue();
    }

    [Test]
    public void ToggleAbilitiesKeepsTheNativeAssociateCommandValue()
    {
        ((int)AssociateCommand.ToggleAbilities).Should().Be(-21);
        AssociateCommand.ToggleCasting.Should().Be(AssociateCommand.ToggleAbilities);

        var repositoryRoot = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "SWLOR.Game.Server",
            "Service",
            "CompanionControlService",
            "CompanionControl.cs"));
        source.Should().Contain("AssociateCastingLabelStrRef = 8127");
        source.Should().Contain("PlayerPlugin.SetTlkOverride(player, AssociateCastingLabelStrRef, \"abilities\")");
    }

    [Test]
    public void HealCommandOptionsAreDeclaredByAbilityMetadata()
    {
        Ability.CacheData();
        var healingAbilities = Ability.GetAllAbilityDetails()
            .Where(x => x.Value.IsHealingAbility)
            .ToDictionary(x => x.Key, x => x.Value);

        healingAbilities.Keys.Should().Contain(new[]
        {
            FeatType.Innervate1,
            FeatType.EmergencyTriage1,
            FeatType.Infusion1,
            FeatType.MedKit1,
            FeatType.Resuscitation1,
            FeatType.Benevolence1,
            FeatType.Renewal1
        });
        healingAbilities.Values.Should().OnlyContain(ability =>
            !ability.IsHostileAbility &&
            ability.IsSingleTargetAbility &&
            ability.RequiresTarget);
    }

    [Test]
    public void CompanionEventsDoNotRunNativeAutonomousCombatScripts()
    {
        var repositoryRoot = FindRepositoryRoot();
        var droid = File.ReadAllText(Path.Combine(repositoryRoot, "SWLOR.Game.Server", "Service", "Droid.cs"));
        var beast = File.ReadAllText(Path.Combine(repositoryRoot, "SWLOR.Game.Server", "Service", "BeastMastery.cs"));

        foreach (var source in new[] { droid, beast })
        {
            source.Should().NotContain("x0_ch_hen_combat");
            source.Should().NotContain("x0_ch_hen_heart");
            source.Should().NotContain("x0_ch_hen_percep");
            source.Should().NotContain("x0_ch_hen_attack");
            source.Should().NotContain("x0_ch_hen_damage");
            source.Should().Contain("CompanionControl.HandleConversation");
            source.Should().Contain("CompanionControl.ProcessCombatRound");
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
