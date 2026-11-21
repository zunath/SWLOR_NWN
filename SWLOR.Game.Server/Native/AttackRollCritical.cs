using NWN.Native.API;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using ImmunityType = NWN.Native.API.ImmunityType;
using Player = SWLOR.Game.Server.Entity.Player;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using static SWLOR.NWN.API.NWScript.NWScript;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using static SWLOR.Game.Server.Composition.GameServices;
using static SWLOR.Game.Server.Native.ResolveAttackRoll;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// The result of a native weapon roll that hit: a queued weapon ability's regular hit, or the critical roll with its
    /// rate, the defender's downgrade and critical-hit immunity.
    /// </summary>
    public static class AttackRollCritical
    {
        internal static void ResolveHitResult(CNWSCreature attacker, CNWSCreature defender,
            CNWSCreatureStats attackerStats, CNWSCombatAttackData pAttackData, CNWSItem weapon,
            SkillType weaponSkillType, int autoAttackCycleCriticalRate)
        {
            if (UsePerkFeat.HasQueuedWeaponAbility(attacker.m_idSelf, weaponSkillType, attacker.m_pcCombatRound.m_nCurrentAttack))
            {
                Log.Write(LogGroup.Attack, $"Queued weapon ability hit - attack result 1");
                pAttackData.m_nAttackResult = AttackResultRegularHit;
            }
            else
            {
                var criticalStat = attackerStats.GetDEXStat();
                var criticalRoll = Random.D100(1);
                var criticalModifier = CalculateCriticalRateModifier(attacker, defender, weaponSkillType);
                criticalModifier += GameServices.CombatAbilityBonus.ConsumeNextAttackGuardedHitCriticalRateBonus(attacker.m_idSelf);
                criticalModifier += GameServices.CombatAttackTiming.ConsumeNextAutoAttackCriticalRateBonus(attacker.m_idSelf, weaponSkillType);
                criticalModifier += GameServices.CombatAbilityBonus.PrepareOpeningAutoAttack(attacker.m_idSelf, weaponSkillType);
                criticalModifier += GameServices.CombatRateModifiers.GetAutoAttackCriticalRateAdjustment(
                    attacker.m_idSelf,
                    defender.m_idSelf,
                    autoAttackCycleCriticalRate);
                criticalModifier += GameServices.CombatRateModifiers.GetSideAttackCriticalRateAdjustment(attacker.m_idSelf, defender.m_idSelf, weaponSkillType);
                criticalModifier += GameServices.CombatRateModifiers.GetBackAttackCriticalRateAdjustment(attacker.m_idSelf, defender.m_idSelf, weaponSkillType);
                var criticalSkillRank = GetCriticalSkillRank(attacker, weapon);
                var criticalRate = CombatRules.CalculateCriticalRate(
                    criticalStat,
                    defender.m_pStats.GetCONStat(),
                    criticalSkillRank,
                    criticalModifier);

                // Critical
                if (criticalRoll <= criticalRate)
                {
                    Log.Write(LogGroup.Attack, $"Critical hit");

                    // Critical Hit - populate variables for feedback
                    pAttackData.m_bCriticalThreat = 1;
                    pAttackData.m_nThreatRoll = 1;

                    if (GameServices.CombatDamageModifiers.TryUseIncomingCriticalHitDowngrade(defender.m_idSelf, 1))
                    {
                        Log.Write(LogGroup.Attack, $"Critical hit downgraded by defender stats");
                        TemporaryStatModifier.Replace(
                            defender.m_idSelf,
                            StatType.CurrentIncomingAttackMinimumDamage,
                            1,
                            6,
                            StatType.CurrentIncomingAttackMinimumDamage);
                        pAttackData.m_nAttackResult = AttackResultRegularHit;
                    }
                    else if (defender.m_pStats.GetEffectImmunity((byte)ImmunityType.CriticalHit, attacker) == 1)
                    {
                        Log.Write(LogGroup.Attack, $"Immune to critical hits");
                        // Immune!
                        var defenderName = PlayerName.GetDisplayName(attacker.m_idSelf, defender.m_idSelf);
                        attacker.SendFeedbackString(new CExoString($"{defenderName} is immune to critical hits!"));
                        pAttackData.m_nAttackResult = AttackResultRegularHit;
                    }
                    else
                    {
                        Log.Write(LogGroup.Attack, $"Not immune to critical hits - dealing crit damage");
                        pAttackData.m_nAttackResult = AttackResultCriticalHit;
                    }
                }
                // Regular Hit
                else
                {
                    Log.Write(LogGroup.Attack, $"Regular hit - attack result 1");
                    pAttackData.m_nAttackResult = AttackResultRegularHit;
                }
            }
        }

        private static int CalculateCriticalRateModifier(CNWSCreature attacker, CNWSCreature defender, SkillType skillType)
        {
            var criticalModifier = Stat.GetStatAdjustment(attacker.m_idSelf, StatType.CriticalRatePercentAdjustment);
            criticalModifier += GameServices.CombatDamageModifiers.GetSkillCriticalRatePercentAdjustment(attacker.m_idSelf, skillType);
            criticalModifier += GameServices.CombatAbilityHit.GetCriticalRateAgainstSunderedTargetAdjustment(attacker.m_idSelf, defender.m_idSelf);

            Log.Write(LogGroup.Attack, $"SWLOR crit rate modifier: {criticalModifier}");

            return criticalModifier;
        }

        private static int GetCriticalSkillRank(CNWSCreature attacker, CNWSItem weapon)
        {
            var skillType = weapon == null
                ? SkillType.Invalid
                : GameServices.Skill.GetWeaponSkill(attacker.m_idSelf, weapon.m_idSelf);

            if (attacker.m_bPlayerCharacter == 1 && !GetIsDMPossessed(attacker.m_idSelf))
            {
                if (skillType == SkillType.Invalid)
                    return 0;

                var playerId = attacker.m_pUUID.GetOrAssignRandom().ToString();
                var dbPlayer = DB.Get<Player>(playerId);

                return dbPlayer?.Skills.TryGetValue(skillType, out var skill) == true
                    ? skill.Rank
                    : 0;
            }

            var npcStats = Stat.GetNPCStatsNative(attacker);

            return skillType != SkillType.Invalid && npcStats.Skills.TryGetValue(skillType, out var skillRank)
                ? skillRank
                : npcStats.Level;
        }
    }
}
