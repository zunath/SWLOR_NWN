using NUnit.Framework;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service
{
    [TestFixture]
    public class WeaponImpactProfileTests
    {
        private static WeaponShotProfile Gun(uint id, int damage, CombatDamageType type) =>
            new(id, SkillType.TwinPistol, damage, type, 120, 120, AbilityType.Perception, "captured");

        [Test]
        public void EachHandUsesItsOwnProfileAndNestedScopesRestoreTheOriginalSource()
        {
            var main = Gun(11, 22, CombatDamageType.Physical);
            var companion = Gun(12, 14, CombatDamageType.Electrical);
            var activation = new WeaponImpactActivation();
            using (var first = new WeaponImpactProfile(1, main, activation))
            {
                Assert.That(WeaponImpactProfile.Get(1).Weapon, Is.SameAs(main));
                using (var second = new WeaponImpactProfile(1, companion, activation, 5))
                {
                    Assert.That(WeaponImpactProfile.Get(1).Weapon.Damage, Is.EqualTo(14));
                    Assert.That(WeaponImpactProfile.Get(1).Weapon.DamageType, Is.EqualTo(CombatDamageType.Electrical));
                    Assert.That(WeaponImpactProfile.Get(1).AccuracyAdjustment, Is.EqualTo(5));
                    Assert.That(WeaponImpactProfile.Get(2), Is.Null);
                }
                Assert.That(WeaponImpactProfile.Get(1).Weapon, Is.SameAs(main));
            }
            Assert.That(WeaponImpactProfile.Get(1), Is.Null);
        }

        [Test]
        public void MultiHandImpactsPermitOneProcPerTargetAndOneCriticalNotificationPerCast()
        {
            var activation = new WeaponImpactActivation();
            Assert.That(activation.TryDamageEffects(100), Is.True);
            Assert.That(activation.TryDamageEffects(100), Is.False);
            Assert.That(activation.TryDamageEffects(101), Is.True);
            Assert.That(activation.TryHitRiders(100), Is.True);
            Assert.That(activation.TryHitRiders(100), Is.False);
            Assert.That(activation.TryHitRiders(101), Is.True);
            Assert.That(activation.TryCriticalNotification(), Is.True);
            Assert.That(activation.TryCriticalNotification(), Is.False);
            Assert.That(new WeaponImpactActivation().TryCriticalNotification(), Is.True);
        }

        [Test]
        public void NativeRollIdentityCannotLeakAcrossActorsTargetsOrReusedAttackSlots()
        {
            var first = RangedWeaponAttackContext.BeginRoll(7, 11, 100, 0);
            var second = RangedWeaponAttackContext.BeginRoll(7, 12, 100, 0);
            Assert.That(second.Id, Is.GreaterThan(first.Id));
            Assert.That(RangedWeaponAttackContext.GetRoll(7, 100, 0).Weapon, Is.EqualTo(12));
            Assert.That(RangedWeaponAttackContext.GetRoll(7, 101, 0), Is.Null);
            Assert.That(RangedWeaponAttackContext.GetRoll(7, 100, 1), Is.Null);
            Assert.That(RangedWeaponAttackContext.GetRoll(8, 100, 0), Is.Null);
            RangedWeaponAttackContext.Clear(7);
            Assert.That(RangedWeaponAttackContext.GetRoll(7, 100, 0), Is.Null);
        }
    }
}
