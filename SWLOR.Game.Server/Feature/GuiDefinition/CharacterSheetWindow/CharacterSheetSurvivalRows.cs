using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using static SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow.CharacterSheetFormat;

namespace SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow
{
    /// <summary>
    /// The stats table rows for regeneration, deflection, guard, damage taken and defense percentages.
    /// </summary>
    public class CharacterSheetSurvivalRows
    {
        private ICombatDamageTakenService CombatDamageTaken { get; }
        private ICombatRatingService CombatRating { get; }
        private IDBService DB { get; }
        private IStatService Stat { get; }

        public CharacterSheetSurvivalRows(ICombatDamageTakenService combatDamageTaken, ICombatRatingService combatRating, IDBService db, IStatService stat)
        {
            CombatDamageTaken = combatDamageTaken;
            CombatRating = combatRating;
            DB = db;
            Stat = stat;
        }

        public void AddRows(ICharacterSheetHost host, Action<string, string, string> addStat)
        {
            addStat("HP Regen", GetHPRegenValue(host).ToString(), "Amount of HP restored automatically by natural regeneration.");
            addStat("FP Regen", GetFPRegenValue(host).ToString(), "Amount of FP restored automatically by natural regeneration.");
            addStat("STM Regen", GetStaminaRegenValue(host).ToString(), "Amount of STM restored automatically by natural regeneration.");
            addStat("Combat Readiness", FormatPercent(Stat.GetCombatReadinessPercent(host.Target)), "Increases activated ability damage, healing, and temporary HP. Does not reduce cooldowns.");
            addStat("Melee Deflection", FormatPercent(CombatRating.GetMeleeDeflectionChance(host.Target)), "Chance to negate a hostile melee weapon auto-attack while wielding a weapon without a shield.");
            addStat("Ranged Deflection", FormatPercent(CombatRating.GetRangedDeflectionChance(host.Target)), "Chance to negate a hostile ranged weapon auto-attack while wielding a weapon without a shield.");
            addStat("Shield Deflection", FormatPercent(CombatRating.GetShieldDeflectionChance(host.Target)), "Chance to negate either a hostile melee or ranged weapon auto-attack while equipped with a shield. Shield Deflection replaces weapon deflection while the shield is equipped.");
            addStat("Guard", FormatPercent(CombatRating.GetGuardChance(host.Target)), "Chance to reduce damage and increase enmity gain.");
            addStat("Guard Reduction", FormatPercent(CombatDamageTaken.GetGuardDamageReductionPercent(host.Target)), "Amount of damage removed from a hit when Guard succeeds.");
            addStat("Phys. Taken", FormatPercent(GetDamageTakenPercent(host, CombatDamageType.Physical)), "Incoming physical damage modifier after damage-taken effects. Lower is better.");
            addStat("Force Taken", FormatPercent(GetDamageTakenPercent(host, CombatDamageType.Force)), "Incoming Force damage modifier after damage-taken effects. Lower is better.");
            addStat("Physical DEF %", FormatPercent(CombatRating.GetDefensePercentAdjustment(host.Target, CombatDamageType.Physical)), "Bonus or penalty applied to Physical DEF. Already included in the Physical DEF shown on the Attributes tab.");
            addStat("Force DEF %", FormatPercent(CombatRating.GetDefensePercentAdjustment(host.Target, CombatDamageType.Force)), "Bonus or penalty applied to Force DEF. Already included in the Force DEF shown on the Attributes tab.");
        }

        private static bool IsPlayerCharacterTarget(ICharacterSheetHost host)
        {
            return GetIsPC(host.Target) && !GetIsDM(host.Target);
        }

        private Player GetPlayerEntity(ICharacterSheetHost host)
        {
            return DB.Get<Player>(GetObjectUUID(host.Target));
        }

        private int GetHPRegenValue(ICharacterSheetHost host)
        {
            var bonus = Stat.GetStatAdjustment(host.Target, StatType.HPRegen);
            if (!IsPlayerCharacterTarget(host))
                return bonus;

            var dbPlayer = GetPlayerEntity(host);
            return dbPlayer.HPRegen + GetAbilityScore(host.Target, AbilityType.Vitality) + bonus;
        }

        private int GetFPRegenValue(ICharacterSheetHost host)
        {
            var bonus = Stat.GetStatAdjustment(host.Target, StatType.FPRegen);
            if (!IsPlayerCharacterTarget(host))
                return bonus;

            var dbPlayer = GetPlayerEntity(host);
            return 1 + dbPlayer.FPRegen + GetAbilityScore(host.Target, AbilityType.Willpower) / 4 + bonus;
        }

        private int GetStaminaRegenValue(ICharacterSheetHost host)
        {
            var bonus = Stat.GetStatAdjustment(host.Target, StatType.StaminaRegen);
            if (!IsPlayerCharacterTarget(host))
                return bonus;

            var dbPlayer = GetPlayerEntity(host);
            return NaturalRegeneration.BaseStaminaRegenPerThirtySeconds + dbPlayer.STMRegen + GetAbilityScore(host.Target, AbilityType.Might) / 4 + bonus;
        }

        private int GetDamageTakenPercent(ICharacterSheetHost host, CombatDamageType damageType)
        {
            var typeAdjustment = damageType switch
            {
                CombatDamageType.Physical =>
                    Stat.GetStatAdjustment(host.Target, StatType.PhysicalDamageTakenPercentAdjustment),
                CombatDamageType.Force =>
                    Stat.GetStatAdjustment(host.Target, StatType.ForceDamageTakenPercentAdjustment),
                _ => 0
            };
            var leadershipAdjustment = damageType switch
            {
                CombatDamageType.Physical =>
                    Stat.GetStatAdjustment(host.Target, StatType.LeadershipPhysicalDamageTakenPercentAdjustment) +
                    Stat.GetStatAdjustment(host.Target, StatType.LeadershipRecoveryPhysicalDamageTakenPercentAdjustment),
                CombatDamageType.Force =>
                    Stat.GetStatAdjustment(host.Target, StatType.LeadershipForceDamageTakenPercentAdjustment) +
                    Stat.GetStatAdjustment(host.Target, StatType.LeadershipRecoveryForceDamageTakenPercentAdjustment),
                _ => Stat.GetStatAdjustment(host.Target, StatType.LeadershipOtherDamageTakenPercentAdjustment)
            };

            var percent = ApplyDamageTakenPercentAdjustment(100, typeAdjustment);
            percent = ApplyDamageTakenPercentAdjustment(percent, leadershipAdjustment);
            return ApplyDamageTakenPercentAdjustment(
                percent,
                Stat.GetStatAdjustment(host.Target, StatType.DamageTakenPercentAdjustment));
        }

        private static int ApplyDamageTakenPercentAdjustment(int percent, int adjustment)
        {
            if (percent <= 0 || adjustment <= -100)
                return 0;

            if (adjustment == 0)
                return percent;

            return Math.Max(0, percent + (int)Math.Ceiling(percent * (adjustment / 100f)));
        }
    }
}
