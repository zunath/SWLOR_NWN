using NWN.Native.API;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using RacialType = SWLOR.NWN.API.NWScript.Enum.RacialType;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// The damage a native weapon roll deals to a creature: the defender's defense, the attack's DMG riders, the
    /// attack-versus-defense formula with its critical, the damage dealt modifiers, conversion and resistance.
    /// </summary>
    public static unsafe class DamageRollCreature
    {
        private const int ElectricalDroidMultiplier = 2;

        internal static int CalculateCreatureDamage(void* pTarget, CNWSCreature attacker, WeaponDamageProfile damageProfile,
            int attackerAttack, int attackerStat, int critical, int weaponDeltaCap,
            uint attackType, uint damageFlags, int bOffHand, SkillType skillType,
            bool isLandedAttack, out int effectiveCritical, Action<Action> convertedDamageScheduler,
            Action<Func<int>> damageTakenResolverCapture)
        {
            effectiveCritical = critical;
            var target = CNWSCreature.FromPointer(pTarget);
            var damageType = damageProfile.DamageType;
            var defenderAbility = damageType.GetDefenseAbilityType();
            var defenderStat = CombatRating.GetStatValueNative(target, defenderAbility);
            var damagePower = DamageRollMitigation.ResolveDamagePower(attacker, target, bOffHand);
            var defense = CombatRating.GetDefenseNative(target, damageType, defenderAbility);
            defense = GameServices.CombatDamageModifiers.ApplyStatusSourceDefenseModifiers(attacker.m_idSelf, target.m_idSelf, defense);
            defense = GameServices.CombatDamageTaken.ApplyIncomingPhysicalToForceDefenseConversion(
                target.m_idSelf,
                damageType,
                defense,
                () => GameServices.CombatDamageModifiers.ApplyStatusSourceDefenseModifiers(
                    attacker.m_idSelf,
                    target.m_idSelf,
                    CombatRating.GetDefenseNative(target, CombatDamageType.Force, CombatDamageType.Force.GetDefenseAbilityType())));
            defense = GameServices.CombatDamageModifiers.ApplyRangedAttackDefenseIgnore(attacker.m_idSelf, defense, skillType);
            // Discarded swings must not burn one-shot buffs or advance cycle counters — the engine
            // rolls damage for attacks it then throws away, so every consuming rider gates on the
            // attack having actually landed.
            var guardedHitBonuses = isLandedAttack
                ? GameServices.CombatAbilityBonus.ConsumeNextAttackGuardedHitAutoAttackBonuses(attacker.m_idSelf)
                : default;
            var statusAppliedNextAttackDamageBonus = isLandedAttack
                ? GameServices.CombatAbilityBonus.ConsumeStatusAppliedNextAttackDamageBonus(attacker.m_idSelf)
                : 0;
            var cycleDamageBonus = isLandedAttack
                ? GameServices.CombatHitEffects.ConsumeAutoAttackCycleDamageBonus(attacker.m_idSelf, skillType)
                : 0;
            var attackDamage = damageProfile.Damage +
                               MeleeTechnique.TakeOrdinaryPower(attacker.m_idSelf, target.m_idSelf, skillType,
                                   attacker.m_pcCombatRound.m_nCurrentAttack, isLandedAttack) +
                               GameServices.CombatQueries.GetRangedAttackDamageFlatAdjustment(attacker.m_idSelf, skillType) +
                               cycleDamageBonus +
                               guardedHitBonuses.DMGBonus +
                               statusAppliedNextAttackDamageBonus;

            Log.Write(LogGroup.Attack, $"DAMAGE: attacker damage attribute: {damageProfile.Damage} defender defense attribute: {defense}, defender racial type {target.m_pStats.m_nRace}");

            attackerAttack = GameServices.CombatDamageModifiers.ApplyTargetStatusAttackModifiers(attacker.m_idSelf, target.m_idSelf, attackerAttack, skillType);

            var damageRoll = GameServices.CombatDamageModifiers.CalculateDamageWithCriticalMitigation(
                target.m_idSelf,
                attackerAttack,
                attackDamage,
                attackerStat,
                defense,
                defenderStat,
                critical,
                weaponDeltaCap);
            var damage = damageRoll.Damage;
            effectiveCritical = damageRoll.CriticalRating;
            if (damageRoll.WasCriticalDowngraded)
            {
                GameServices.CombatFeedback.SendIncomingCriticalHitDowngradeFeedback(attacker.m_idSelf, target.m_idSelf);
            }

            var openingCriticalDamageAdjustment = isLandedAttack
                ? GameServices.CombatAbilityBonus.ConsumeOpeningAutoAttackCriticalDamageAdjustment(attacker.m_idSelf)
                : 0;
            damage = GameServices.CombatDamageModifiers.ApplyCriticalDamageModifier(
                attacker.m_idSelf,
                damage,
                effectiveCritical,
                skillType,
                target.m_idSelf,
                openingCriticalDamageAdjustment);

            if (isLandedAttack)
            {
                damage = GameServices.CombatHitEffects.ApplyAutoAttackDamageModifiers(attacker.m_idSelf, target.m_idSelf, damage, skillType);
            }
            var canApplyRandomFlatBonusesThisDamage = damage > 0;
            var pairedShot = RangedWeaponAttackContext.GetRoll(attacker.m_idSelf, target.m_idSelf,
                attacker.m_pcCombatRound.m_nCurrentAttack);

            damage = GameServices.CombatDamageModifiers.ApplyDamageDealtModifiers(
                attacker.m_idSelf,
                target.m_idSelf,
                damage,
                skillType,
                damageType,
                false,
                canApplyRandomFlatBonusesThisDamage,
                isLandedAttack,
                null,
                out var targetStatusDamageAdjustment,
                abilityDamagePercentAdjustment: pairedShot is { Ordinary: true }
                    ? pairedShot.Bonuses.DamagePercentAdjustment : 0,
                isOrdinaryWeaponAttack: pairedShot is { Ordinary: true });

            damage = MeleeTechnique.ApplyOrdinaryDamageModifiers(attacker.m_idSelf, damage, damageType);

            // Saber Ward / Aegis Eternal: re-type a share of the physical hit into a real Force
            // instance (mitigated by Force resistance, shown as Force) before physical resistance.
            if (isLandedAttack)
            {
                GameServices.CombatDamageTaken.ApplyIncomingPhysicalToForceConversion(attacker.m_idSelf, target.m_idSelf, damageType, ref damage, targetStatusDamageAdjustment, convertedDamageScheduler);
            }

            // Conversion must split first so each portion receives only its own typed Leadership channel.
            damage = GameServices.CombatDamageTaken.ApplyTypedLeadershipDamageTakenModifier(target.m_idSelf, damage, damageType);

            damage = Resistance.ApplyResistanceToDamageNative(target, damageType, damage);

            // Apply droid electrical damage bonus
            if (target.m_pStats.m_nRace == (ushort)RacialType.Robot &&
                damageType.TryGetElementalResistanceType(out var elementalResistanceType) &&
                elementalResistanceType == ResistanceType.Electrical)
            {
                damage *= ElectricalDroidMultiplier;
            }

            if (damageTakenResolverCapture != null)
            {
                damageTakenResolverCapture(DamageRollMitigation.CaptureDamageTakenResolution(attacker.m_idSelf, target.m_idSelf,
                    damage, damageType, damageFlags, damagePower, attackType, isLandedAttack,
                    targetStatusDamageAdjustment, guardedHitBonuses.EnmityBonus));
                return damage;
            }

            return DamageRollMitigation.ApplyCreatureDamageTaken(attacker, target, damage, damageType, damageFlags, damagePower,
                attackType, isLandedAttack, targetStatusDamageAdjustment, guardedHitBonuses.EnmityBonus);
        }
    }
}
