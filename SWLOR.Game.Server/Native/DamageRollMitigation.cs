using NWN.Native.API;
using SWLOR.Game.Server.Domain.Combat;
using AttackType = SWLOR.Game.Server.Enumeration.AttackType;
using SWLOR.Game.Server.Composition;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// A native weapon hit landing on a creature: the engine's damage immunity, resistance and reduction against the
    /// attack's damage power, Guard, and the shared damage-taken pipeline, now or at a projectile's impact.
    /// </summary>
    public static class DamageRollMitigation
    {
        // Only managed IDs and launch values enter the delayed closure. Native wrappers are
        // reacquired at impact, after the caller has validated the projectile's target and area.
        internal static Func<int> CaptureDamageTakenResolution(uint attackerId, uint defenderId,
            int damage, CombatDamageType damageType, uint damageFlags, byte damagePower, uint attackType,
            bool isLandedAttack, int targetStatusDamageAdjustment, int guardedHitEnmityBonus)
        {
            var resolved = false;
            var result = 0;
            return () =>
            {
                if (resolved) return result;
                resolved = true;
                var attacker = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(attackerId)?.AsNWSCreature();
                var defender = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(defenderId)?.AsNWSCreature();
                if (attacker == null || defender == null) return result;
                result = ApplyCreatureDamageTaken(attacker, defender, damage, damageType, damageFlags, damagePower,
                    attackType, isLandedAttack, targetStatusDamageAdjustment, guardedHitEnmityBonus);
                return result;
            };
        }

        internal static int ApplyCreatureDamageTaken(CNWSCreature attacker, CNWSCreature target,
            int damage, CombatDamageType damageType, uint damageFlags, byte damagePower, uint attackType,
            bool isLandedAttack, int targetStatusDamageAdjustment, int guardedHitEnmityBonus)
        {
            // Native physical effects can have finite absorption budgets. Resolve those,
            // Guard reactions and the shared damage-taken pipeline at the same impact boundary.
            if (damageType.IsPhysicalDamageType())
            {
                var bRangedAttack = attackType == (uint)AttackType.Ranged ? 1 : 0;
                damage = target.DoDamageImmunity(attacker, damage, damageFlags, 0, 1);
                damage = target.DoDamageResistance(attacker, damage, damageFlags, 0, 1, 1, bRangedAttack);
                damage = target.DoDamageReduction(attacker, damage, damagePower, 0, 1, bRangedAttack);
            }

            damage = GameServices.CombatDamageTaken.ApplyGuardedHitModifiers(
                target.m_idSelf,
                attacker.m_idSelf,
                damage,
                damageType,
                isLandedAttack);
            if (isLandedAttack && damage > 0 && attackType == (uint)AttackType.Melee)
            {
                GameServices.CombatDamageTaken.ApplyMeleeDamageTakenEffects(target.m_idSelf, attacker.m_idSelf);
            }

            damage = GameServices.CombatDamageTaken.ApplyDamageTakenModifiers(
                target.m_idSelf,
                damage,
                attacker.m_idSelf,
                damageType,
                targetStatusDamagePercentAdjustment: targetStatusDamageAdjustment,
                isLandedAttack: isLandedAttack,
                typedLeadershipReductionAlreadyApplied: true);
            if (isLandedAttack)
            {
                GameServices.CombatAbilityBonus.ApplyNextAttackGuardedHitEnmityBonus(
                    attacker.m_idSelf,
                    target.m_idSelf,
                    guardedHitEnmityBonus);
            }
            return damage;
        }

        internal static byte ResolveDamagePower(CNWSCreature attacker, CNWSObject target, int offHand)
        {
            var attack = attacker.m_pcCombatRound.GetAttack(attacker.m_pcCombatRound.m_nCurrentAttack);
            var attackTypeBeforePower = attack.m_nWeaponAttackType;
            if (offHand != 0 && PowerGloveEquipment.IsActivePair(attacker.m_idSelf))
            {
                // The cosmetic left glove carries no enhancement; penetrate with the owned glove's main-hand scope.
                try
                {
                    attack.m_nWeaponAttackType = 1;
                    return attacker.CalculateDamagePower(target, 0);
                }
                finally { attack.m_nWeaponAttackType = attackTypeBeforePower; }
            }

            if (!RangedWeaponAttackContext.IsSimulated(attacker.m_idSelf))
                return attacker.CalculateDamagePower(target, offHand);

            try
            {
                // GetWeaponPower prefers the attack-data hand over its boolean argument.
                // Select the companion's native effect scope only for penetration; the
                // borrowed roll remains a ranged launcher everywhere else.
                attack.m_nWeaponAttackType = 2;
                return attacker.CalculateDamagePower(target, 1);
            }
            finally { attack.m_nWeaponAttackType = attackTypeBeforePower; }
        }
    }
}
