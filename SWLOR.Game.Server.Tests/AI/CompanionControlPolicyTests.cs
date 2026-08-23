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
    public void ExplicitInteractionOrdersArePreservedOnlyWhileActive()
    {
        var now = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);

        CompanionControlPolicy.ShouldPreserveExplicitOrder(
                now.AddSeconds(30),
                now,
                ActionType.OpenDoor)
            .Should()
            .BeTrue();
        CompanionControlPolicy.ShouldPreserveExplicitOrder(
                now.AddSeconds(30),
                now,
                ActionType.Follow)
            .Should()
            .BeFalse();
        CompanionControlPolicy.ShouldPreserveExplicitOrder(
                now,
                now,
                ActionType.OpenDoor)
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

        CompanionControlPolicy.HasCombatProgress(false, 10f, 9.8f)
            .Should()
            .BeFalse();
        CompanionControlPolicy.HasCombatProgress(false, 10f, 9.7f)
            .Should()
            .BeTrue();
        CompanionControlPolicy.HasCombatProgress(true, 10f, 10f)
            .Should()
            .BeTrue();
    }

    [Test]
    public void ToggleAbilitiesKeepsTheNativeAssociateCommandValue()
    {
        ((int)AssociateCommand.ToggleAbilities).Should().Be(-21);
        AssociateCommand.ToggleCasting.Should().Be(AssociateCommand.ToggleAbilities);
        CompanionControl.AssociateAbilitiesLabelStrRef.Should().Be(8127);
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

}
