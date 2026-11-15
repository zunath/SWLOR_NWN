using NWN.Native.API;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using AttackType = SWLOR.Game.Server.Enumeration.AttackType;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// The defender's side of the native attack roll: the defender's evasion, and the deflection roll a weapon hit must
    /// pass.
    /// </summary>
    public static class AttackRollDefense
    {
        internal static int GetDefenderEvasion(CNWSCreature attacker, CNWSCreature defender, SkillType abilitySkillType)
        {
            var defenderEvasion = Stat.GetEvasionNative(defender, abilitySkillType);
            defenderEvasion = GameServices.CombatDamageModifiers.ApplySideAttackEvasionIgnore(
                attacker.m_idSelf,
                defender.m_idSelf,
                abilitySkillType,
                defenderEvasion);
            return defenderEvasion;
        }

        internal static DeflectionSource CheckDeflection(
            bool isHit,
            uint attackType,
            SkillType weaponSkillType,
            CNWSCreature attacker,
            CNWSCreature defender)
        {
            if (!isHit ||
                weaponSkillType == SkillType.Invalid ||
                !GameServices.CombatDamageTaken.IsHostileAttackSource(defender.m_idSelf, attacker.m_idSelf) ||
                UsePerkFeat.HasQueuedWeaponAbility(attacker.m_idSelf, weaponSkillType, attacker.m_pcCombatRound.m_nCurrentAttack))
                return DeflectionSource.None;

            var (source, deflectChance) = GetDeflectionChance(defender, attackType);
            if (deflectChance <= 0)
                return DeflectionSource.None;

            var deflectRoll = Random.D100(1);
            var deflected = deflectRoll <= deflectChance;
            if (deflected)
            {
                Stat.ApplyDeflectionEffectsNative(defender, source);

                var deflectionName = CombatRules.GetDeflectionResultName(source);
                attacker.SendFeedbackString(new CExoString(AttackRollFeedback.BuildDeflectionFeedback(attacker.m_idSelf, attacker, defender, deflectionName)));
                defender.SendFeedbackString(new CExoString(AttackRollFeedback.BuildDeflectionFeedback(defender.m_idSelf, attacker, defender, deflectionName)));
            }

            Log.Write(LogGroup.Attack, $"Deflect roll: {deflectRoll}, Chance: {deflectChance}, Hit: {!deflected}");

            return deflected ? source : DeflectionSource.None;
        }

        private static (DeflectionSource Source, int Chance) GetDeflectionChance(CNWSCreature defender, uint attackType)
        {
            var shieldDeflection = Stat.GetShieldDeflectionChanceNative(defender);
            if (shieldDeflection > 0)
                return (DeflectionSource.Shield, shieldDeflection);

            if (attackType == (uint)AttackType.Ranged)
            {
                var rangedDeflection = Stat.GetRangedDeflectionChanceNative(defender);
                return rangedDeflection > 0
                    ? (DeflectionSource.Ranged, rangedDeflection)
                    : (DeflectionSource.None, 0);
            }

            var meleeDeflection = Stat.GetMeleeDeflectionChanceNative(defender);
            if (meleeDeflection > 0)
                return (DeflectionSource.Melee, meleeDeflection);

            return (DeflectionSource.None, 0);
        }
    }
}
