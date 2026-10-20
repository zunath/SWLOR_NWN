using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class WeaponAttackEligibilityTests
{
    [Test]
    public void UnregisteredWeaponsKeepBothAttackPhases()
    {
        const BaseItem unregistered = (BaseItem)(-1001);
        WeaponAttackEligibility.IsAllowed(unregistered, 11, 22, WeaponAttackPhase.Start).Should().BeTrue();
        WeaponAttackEligibility.IsAllowed(unregistered, 11, 22, WeaponAttackPhase.Resolve).Should().BeTrue();
    }

    [Test]
    public void AStartOnlyRestrictionCanPreserveCommittedResolution()
    {
        const BaseItem type = (BaseItem)(-1002);
        try
        {
            WeaponAttackEligibility.RegisterRule(type, (actor, weapon, phase) =>
                actor == 11 && weapon == 22 && phase == WeaponAttackPhase.Resolve);
            WeaponAttackEligibility.IsAllowed(type, 11, 22, WeaponAttackPhase.Start).Should().BeFalse();
            WeaponAttackEligibility.IsAllowed(type, 11, 22, WeaponAttackPhase.Resolve).Should().BeTrue();
            WeaponAttackEligibility.IsAllowed(type, 33, 22, WeaponAttackPhase.Resolve).Should().BeFalse();
        }
        finally { WeaponAttackEligibility.RegisterRule(type, (_, _, _) => true); }
    }

    [Test]
    public void ReRegisteringAWeaponClassReplacesItsPreviousRestriction()
    {
        const BaseItem type = (BaseItem)(-1003);
        try
        {
            WeaponAttackEligibility.RegisterRule(type, (_, _, _) => false);
            WeaponAttackEligibility.IsAllowed(type, 11, 22, WeaponAttackPhase.Start).Should().BeFalse();
            WeaponAttackEligibility.RegisterRule(type, (_, _, _) => true);
            WeaponAttackEligibility.IsAllowed(type, 11, 22, WeaponAttackPhase.Start).Should().BeTrue();
            WeaponAttackEligibility.IsAllowed(type, 11, 22, WeaponAttackPhase.Resolve).Should().BeTrue();
        }
        finally { WeaponAttackEligibility.RegisterRule(type, (_, _, _) => true); }
    }
}
