using NWN.Native.API;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using ObjectType = NWN.Native.API.ObjectType;
using SWLOR.Game.Server.Composition;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// What a landed native weapon roll triggers: the ordinary-hit traits of its shot, the combat point, the on-hit
    /// effects and the damage-dealt event, now or at a projectile's impact.
    /// </summary>
    public static class DamageRollHitEffects
    {
        internal static void CompleteShot(CNWSCreature attacker, CNWSObject defender, RangedWeaponAttackContext.Roll shot,
            WeaponDamageProfile damageProfile, SkillType weaponSkillType, bool isLandedAttack, int totalDamage,
            Action<Action<int>> weaponImpactScheduler)
        {
            shot.Damage = totalDamage;
            shot.DamageType = damageProfile.DamageType;
            if (shot.Ordinary && !shot.Completed && isLandedAttack)
            {
                var attackerId = attacker.m_idSelf;
                var defenderId = defender.m_idSelf;
                var rollId = shot.Id;
                var nativeTwinPistol = weaponSkillType == SkillType.TwinPistol &&
                                       !RangedWeaponAttackContext.IsSimulated(attackerId);
                void RecordOrdinaryHit(int _)
                {
                    PairedRangedTraits.RecordOrdinaryHit(attackerId, defenderId, rollId);
                    OrdinaryAttackTraits.RecordHit(attackerId, defenderId, rollId);
                    if (nativeTwinPistol)
                        TwinPistolMastery.RecordOrdinaryHit(attackerId, defenderId, false);
                }
                if (weaponImpactScheduler == null) RecordOrdinaryHit(totalDamage);
                else weaponImpactScheduler(RecordOrdinaryHit);
            }
            shot.Completed = true;
        }

        internal static void ApplyLandedHitEffects(CNWSCreature attacker, CNWSObject defender, CNWSItem weapon,
            CNWSCombatAttackData pAttackData, WeaponDamageProfile damageProfile, SkillType weaponSkillType,
            int effectiveCritical, int totalDamage, Action<Action<int>> weaponImpactScheduler)
        {
            var attackerId = attacker.m_idSelf;
            var defenderId = defender.m_idSelf;
            var weaponId = weapon?.m_idSelf ?? OBJECT_INVALID;
            var creatureTarget = defender.m_nObjectType == (int)ObjectType.Creature;
            var damageableTarget = defender.m_bPlotObject == 0;
            var rolledType = damageProfile.DamageType;
            var nativeDamageApplication = weaponImpactScheduler == null;

            // A bare-handed swing with no arms item or natural weapon has no item on-hit
            // event to award its combat point, so it is credited to Unarmed here.
            if (weapon == null && pAttackData.m_bRangedAttack == 0 && creatureTarget)
                CombatPoint.RecordWeaponAttackPoint(attackerId, defenderId, OBJECT_INVALID, SkillType.Unarmed);

            void ApplyWeaponHitEffects(int rolledDamage)
            {
                if (!GetIsObjectValid(attackerId) || !GetIsObjectValid(defenderId)) return;
                using var damageDerivedHealing = GameServices.CombatResources.BeginDamageDerivedHealing(attackerId, defenderId);
                if (creatureTarget)
                {
                    if (rolledDamage > 0)
                        GameServices.CombatFeedback.SendTemporaryHitPointDamageFeedback(attackerId, defenderId, rolledDamage);
                    GameServices.CombatHitEffects.ApplyCriticalHitEffects(attackerId, defenderId, rolledDamage, effectiveCritical, true, weaponSkillType);
                }
                if (rolledDamage > 0 && damageableTarget)
                {
                    // Native weapon damage needs a pending registration; managed projectile
                    // effects restore the captured attribution around their actual application.
                    if (nativeDamageApplication && CombatRecorder.IsRecording)
                        CombatRecorder.ExpectWeaponDamage(attackerId, defenderId, rolledDamage, weaponSkillType);
                    PublishDamageDealtEvent(attackerId, defenderId, weaponId, rolledDamage, weaponSkillType, rolledType);
                }
            }
            if (weaponImpactScheduler == null) ApplyWeaponHitEffects(totalDamage);
            else weaponImpactScheduler(ApplyWeaponHitEffects);
        }

        private static void PublishDamageDealtEvent(uint attacker, uint defender, uint weapon, int damage, SkillType skillType, CombatDamageType damageType)
        {
            GameServices.CombatHitEffects.ApplyDamageDealtEffects(attacker, defender, damage, skillType, damageType);

            EventsPlugin.PushEventData("DEFENDER", ObjectToString(defender));
            EventsPlugin.PushEventData("WEAPON", ObjectToString(weapon));
            EventsPlugin.PushEventData("DAMAGE", damage.ToString());
            EventsPlugin.PushEventData("DAMAGE_TYPE", ((int)damageType).ToString());

            EventsPlugin.SignalEvent("SWLOR_ON_DAMAGE", attacker);
        }
    }
}
