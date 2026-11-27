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
    /// The stats table rows for accuracy, evasion, attack and critical adjustments.
    /// </summary>
    public class CharacterSheetAccuracyRows
    {
        private IBeastMasteryService BeastMastery { get; }
        private CharacterSheetWeaponTiming CharacterSheetWeaponTiming { get; }
        private ICombatAbilityBonusService CombatAbilityBonus { get; }
        private ICombatAbilityHitAdjustmentService CombatAbilityHitAdjustment { get; }
        private ICombatDamageModifiersService CombatDamageModifiers { get; }
        private ICombatRatingService CombatRating { get; }
        private IDBService DB { get; }
        private ISkillService Skill { get; }
        private IStatService Stat { get; }

        public CharacterSheetAccuracyRows(IBeastMasteryService beastMastery, CharacterSheetWeaponTiming characterSheetWeaponTiming, ICombatAbilityBonusService combatAbilityBonus, ICombatAbilityHitAdjustmentService combatAbilityHitAdjustment, ICombatDamageModifiersService combatDamageModifiers, ICombatRatingService combatRating, IDBService db, ISkillService skill, IStatService stat)
        {
            BeastMastery = beastMastery;
            CharacterSheetWeaponTiming = characterSheetWeaponTiming;
            CombatAbilityBonus = combatAbilityBonus;
            CombatAbilityHitAdjustment = combatAbilityHitAdjustment;
            CombatDamageModifiers = combatDamageModifiers;
            CombatRating = combatRating;
            DB = db;
            Skill = skill;
            Stat = stat;
        }

        public void AddRows(ICharacterSheetHost host, Action<string, string, string> addStat, SkillType skill)
        {
            addStat("Ability Accuracy", FormatPercent(Stat.GetStatAdjustment(host.Target, StatType.PhysicalAndForceAbilityHitChancePercentAdjustment)), "Direct percentage-point change to hit chance for weapon-skill and Force-skill ability hit checks only. Does not affect Mimicry abilities or the underlying Accuracy rating.");
            addStat("Accuracy %", FormatPercent(Stat.GetStatAdjustment(host.Target, StatType.AccuracyPercentAdjustment)), "Percentage bonus or penalty applied to the underlying Accuracy rating for attacks and ability hit checks, including Force and Mimicry. It is not a direct percentage-point change to hit chance and is already included in the Main-hand Accuracy, Off-hand Accuracy, and Force Accuracy ratings shown on the Attributes tab.");
            addStat("Evasion %", FormatPercent(Stat.GetStatAdjustment(host.Target, StatType.EvasionPercentAdjustment)), "Bonus or penalty applied to Evasion. Already included in the Evasion shown on the Attributes tab.");
            addStat("Attack %", FormatPercent(Stat.GetStatAdjustment(host.Target, StatType.AttackPercentAdjustment)), "General bonus or penalty applied to Attack, including Force Attack.");
            addStat("Force Attack %", FormatPercent(CombatRating.GetAttackPercentAdjustment(host.Target, SkillType.Force)), "Combined general and Force-specific Attack bonus or penalty. Already included in Force Attack on the Attributes tab.");
            var offhandWeapon = BeastMastery.IsPlayerBeast(host.Target) ? OBJECT_INVALID : CharacterSheetWeaponTiming.GetOffhandAttackWeapon(host);
            addStat("Critical Rate", FormatPercent(GetCriticalRate(host, skill)), FormatOffHandTooltip(
                "Increases the chance to score a critical hit. Actual chance varies by target Vitality.",
                GetIsObjectValid(offhandWeapon) ? FormatPercent(GetCriticalRate(host, Skill.GetWeaponSkill(host.Target, offhandWeapon))) : null));
            addStat("Next Ability Crit", FormatPercent(CombatAbilityBonus.GetPersistentNextSkillAbilityCriticalRateBonus(host.Target, skill)), "Conditional Critical Rate reserved for the next matching ability. It does not affect ordinary auto-attacks and is consumed only when the ability critically hits.");
            addStat("Assault Gadget Crit", FormatPercent(GetAssaultGadgetCriticalRate(host)), "Current Assault Gadget ability critical chance before target-specific bonuses. Includes the 5% baseline, Gadget Harness, Tactical Uplink, and other Devices ability bonuses; capped at 50%.");
            addStat("Gadget DMG", DeviceAbilityEffects.GetAssaultGadgetWeaponDamageEquivalent(host.Target).ToString(), "DMG added to every Assault Gadget ability, the way weapon DMG is added to weapon abilities; Overload Barrage adds it only to its single-target strike. Rises with Devices rank: 6 below rank 10, then 10, 15, 19, 24, and 28 at ranks 10, 20, 30, 40, and 50. Droids use their level in place of Devices rank.");
            addStat("Critical Damage", FormatPercent(Stat.GetStatAdjustment(host.Target, StatType.CriticalDamagePercentAdjustment)), "Increases the amount of damage a critical hit deals.");
        }

        private int GetCriticalRate(ICharacterSheetHost host, SkillType skillType)
        {
            var criticalRateAdjustment = Stat.GetStatAdjustment(
                host.Target,
                StatType.CriticalRatePercentAdjustment);
            criticalRateAdjustment += CombatDamageModifiers.GetSkillCriticalRatePercentAdjustment(host.Target, skillType);

            return CombatRules.CalculateCriticalRate(
                GetAbilityScore(host.Target, AbilityType.Perception),
                GetAbilityScore(host.Target, AbilityType.Vitality),
                GetSkillRank(host, skillType),
                criticalRateAdjustment);
        }

        private int GetAssaultGadgetCriticalRate(ICharacterSheetHost host)
        {
            return CombatAbilityHitAdjustment.GetAbilityCriticalRate(
                host.Target,
                SkillType.Devices,
                false,
                Stat.GetStatAdjustment(host.Target, StatType.AssaultGadgetCriticalRatePercentAdjustment));
        }

        private int GetSkillRank(ICharacterSheetHost host, SkillType skillType)
        {
            if (IsPlayerCharacterTarget(host))
            {
                if (skillType == SkillType.Invalid)
                    return 0;

                var dbPlayer = GetPlayerEntity(host);
                return dbPlayer.Skills.TryGetValue(skillType, out var skill)
                    ? skill.Rank
                    : 0;
            }

            var npcStats = Stat.GetNPCStats(host.Target);
            return npcStats.Skills.TryGetValue(skillType, out var rank)
                ? rank
                : npcStats.Level;
        }

        private static bool IsPlayerCharacterTarget(ICharacterSheetHost host)
        {
            return GetIsPC(host.Target) && !GetIsDM(host.Target);
        }

        private Player GetPlayerEntity(ICharacterSheetHost host)
        {
            return DB.Get<Player>(GetObjectUUID(host.Target));
        }
    }
}
