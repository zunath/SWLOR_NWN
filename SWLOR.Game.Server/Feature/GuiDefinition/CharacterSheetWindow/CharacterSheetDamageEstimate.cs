using SWLOR.Game.Server.Domain.Foundation;
using System;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using static SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow.CharacterSheetFormat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow
{
    /// <summary>
    /// The damage estimate the sheet shows for a weapon, and the ability a weapon scales from.
    /// </summary>
    public class CharacterSheetDamageEstimate
    {
        private ICombatDamageModifiersService CombatDamageModifiers { get; }
        private ICombatQueriesService CombatQueries { get; }
        private ICombatRatingService CombatRating { get; }
        private IDBService DB { get; }
        private IItemService Item { get; }
        private IPairedRangedTraitsService PairedRangedTraits { get; }
        private ISkillService Skill { get; }
        private IStatService Stat { get; }

        public CharacterSheetDamageEstimate(ICombatDamageModifiersService combatDamageModifiers, ICombatQueriesService combatQueries, ICombatRatingService combatRating, IDBService db, IItemService item, IPairedRangedTraitsService pairedRangedTraits, ISkillService skill, IStatService stat)
        {
            CombatDamageModifiers = combatDamageModifiers;
            CombatQueries = combatQueries;
            CombatRating = combatRating;
            DB = db;
            Item = item;
            PairedRangedTraits = pairedRangedTraits;
            Skill = skill;
            Stat = stat;
        }

        public AbilityType GetEquipmentDamageAbility(uint creature, uint item)
        {
            var ability = CombatQueries.GetWeaponDamageAbilityType(creature, GetBaseItemType(item));
            for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
                if (GetItemPropertyType(property) == ItemPropertyType.DamageStat)
                    ability = (AbilityType)GetItemPropertySubType(property);
            return ability;
        }

        // Builds a damage estimate using the player's stats as a baseline.
        public (string, string) GetCombatInfo(ICharacterSheetHost host, uint item)
        {
            var itemType = GetBaseItemType(item);
            var skill = Skill.GetWeaponSkill(host.Target, item);
            int skillRank;

            if (GetIsPC(host.Target))
            {
                var playerId = GetObjectUUID(host.Target);
                var dbPlayer = DB.Get<Player>(playerId);
                skillRank = dbPlayer.Skills[skill].Rank;
            }
            else
            {
                var npcStats = Stat.GetNPCStats(host.Target);
                skillRank = npcStats.Level;
            }

            var damageAbility = GetEquipmentDamageAbility(host.Target, item);
            var damageStat = GetAbilityScore(host.Target, damageAbility);
            var itemDamage = Item.GetDMG(item);
            var effectiveDamage = WeaponDamage.GetEffectiveDMG(host.Target, item, itemDamage);
            var dmg = effectiveDamage + CombatQueries.GetMiscDMGBonus(host.Target, itemType);
            var dmgText = $"{dmg} DMG";
            var attack = CombatRating.GetAttack(host.Target, damageAbility, skill);
            var defense = CombatRating.CalculateDefense(damageStat, skillRank, 0);
            var (min, max) = CombatRules.CalculateDamageRange(attack, dmg, damageStat, defense, damageStat, 0);
            var ordinaryDamageBonus = PairedRangedTraits.GetOrdinaryDamagePercentAdjustment(host.Target, GetAttackTarget(host.Target));
            var ordinaryDamageAdjustment = Stat.GetStatAdjustment(host.Target, StatType.OrdinaryAttackDamagePercentAdjustment);
            min += (int)Math.Ceiling(min * (ordinaryDamageBonus / 100f));
            max += (int)Math.Ceiling(max * (ordinaryDamageBonus / 100f));
            min = CombatDamageModifiers.ApplyOrdinaryAttackDamageMultiplier(host.Target, min);
            max = CombatDamageModifiers.ApplyOrdinaryAttackDamageMultiplier(host.Target, max);
            var tooltip = $"Est. Damage: {min} - {max}";
            if (skill == SkillType.TwinPistol || ordinaryDamageBonus != 0 || ordinaryDamageAdjustment != 0)
                tooltip += $"\nAttack skill: {Skill.GetSkillDetails(skill).Name}\nDamage attribute: {damageAbility}" +
                           $"\nOrdinary-attack bonuses: {FormatPercent(ordinaryDamageBonus)}\nStance damage adjustment: {FormatPercent(ordinaryDamageAdjustment)}" +
                           "\nUses current ordinary-attack effects and the current attack target for Converging Fire." +
                           "\nEstimate assumes matching defensive stats; activated abilities use their own power.";
            if (effectiveDamage != itemDamage)
            {
                tooltip += $"\nWeapon DMG: {itemDamage}\nSingle Weapon: +{WeaponDamage.GetNaturalSingleWeaponPercent(host.Target)}%" +
                           $"\nDoublehand: +{Stat.GetStatAdjustment(host.Target, StatType.SingleWeaponDamagePercentAdjustment)}%" +
                           $"\nEffective weapon DMG: {effectiveDamage}";
            }

            return (dmgText, tooltip);
        }
    }
}
