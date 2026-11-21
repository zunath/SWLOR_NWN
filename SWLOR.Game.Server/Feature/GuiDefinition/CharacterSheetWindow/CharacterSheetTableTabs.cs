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
    /// The stats, resistances and crafting tables: the stats table is built from the four row parts in order.
    /// </summary>
    public class CharacterSheetTableTabs
    {
        private IBeastMasteryService BeastMastery { get; }
        private CharacterSheetAccuracyRows CharacterSheetAccuracyRows { get; }
        private CharacterSheetDamageEstimate CharacterSheetDamageEstimate { get; }
        private CharacterSheetDamageRows CharacterSheetDamageRows { get; }
        private CharacterSheetSurvivalRows CharacterSheetSurvivalRows { get; }
        private CharacterSheetUtilityRows CharacterSheetUtilityRows { get; }
        private IResistanceService Resistance { get; }
        private ISkillService Skill { get; }
        private IStatService Stat { get; }

        public CharacterSheetTableTabs(IBeastMasteryService beastMastery, CharacterSheetAccuracyRows characterSheetAccuracyRows, CharacterSheetDamageEstimate characterSheetDamageEstimate, CharacterSheetDamageRows characterSheetDamageRows, CharacterSheetSurvivalRows characterSheetSurvivalRows, CharacterSheetUtilityRows characterSheetUtilityRows, IResistanceService resistance, ISkillService skill, IStatService stat)
        {
            BeastMastery = beastMastery;
            CharacterSheetAccuracyRows = characterSheetAccuracyRows;
            CharacterSheetDamageEstimate = characterSheetDamageEstimate;
            CharacterSheetDamageRows = characterSheetDamageRows;
            CharacterSheetSurvivalRows = characterSheetSurvivalRows;
            CharacterSheetUtilityRows = characterSheetUtilityRows;
            Resistance = resistance;
            Skill = skill;
            Stat = stat;
        }

        public void RefreshCharacterStatsList(ICharacterSheetHost host)
        {
            var rows = new List<StatEntry>();
            void AddStat(string name, string value, string tooltip)
            {
                rows.Add(new StatEntry(name, value, tooltip));
            }

            var combatProfile = GetPrimaryCombatProfile(host);

            CharacterSheetSurvivalRows.AddRows(host, AddStat);
            CharacterSheetAccuracyRows.AddRows(host, AddStat, combatProfile.Skill);
            CharacterSheetDamageRows.AddRows(host, AddStat);
            CharacterSheetUtilityRows.AddRows(host, AddStat);

            host.RefreshStatsTable(rows);
        }

        private (AbilityType DamageAbility, AbilityType AccuracyAbilityOverride, SkillType Skill, uint AccuracyWeapon) GetPrimaryCombatProfile(ICharacterSheetHost host)
        {
            var mainHand = GetItemInSlot(InventorySlot.RightHand, host.Target);
            var mainHandType = GetBaseItemType(mainHand);
            var skill = Skill.GetWeaponSkill(host.Target, mainHand);

            if (BeastMastery.IsPlayerBeast(host.Target))
            {
                var beastType = BeastMastery.GetBeastType(host.Target);
                var beastDetails = BeastMastery.GetBeastDetail(beastType);
                var creatureArmor = GetItemInSlot(InventorySlot.CreatureArmor, host.Target);

                return (beastDetails.DamageStat, beastDetails.AccuracyStat, skill, creatureArmor);
            }

            return (CharacterSheetDamageEstimate.GetEquipmentDamageAbility(host.Target, mainHand), AbilityType.Invalid, skill, mainHand);
        }

        public void RefreshResistances(ICharacterSheetHost host)
        {
            var rows = Resistance.GetAllResistanceTypes().Select(resistanceType =>
            {
                var score = Resistance.GetResistance(host.Target, resistanceType);
                var takenPercent = (int)Math.Round(Resistance.CalculateResistanceDamageMultiplier(host.Target, resistanceType) * 100f);

                return new ResistanceEntry(
                    resistanceType.ToString(),
                    score.ToString(),
                    $"{takenPercent}% taken",
                    GetStatusDurationLabel(score));
            });

            host.RefreshResistancesTable(rows);
        }

        public void RefreshCraftingStats(ICharacterSheetHost host)
        {
            var rows = new List<CraftEntry>();
            var legacyControl = string.Empty;
            var legacyCraftsmanship = string.Empty;

            var index = 0;
            foreach (var (skillType, detail) in Skill.GetActiveCraftingSkills())
            {
                var control = Stat.CalculateControl(host.Target, skillType);
                var craft = Stat.CalculateCraftsmanship(host.Target, skillType);

                rows.Add(new CraftEntry(detail.Name, control.ToString(), craft.ToString()));

                legacyControl += index == 0 ? control.ToString() : $"/{control}";
                legacyCraftsmanship += index == 0 ? craft.ToString() : $"/{craft}";
                index++;
            }

            host.RefreshCraftingTable(rows);
            host.Control = legacyControl;
            host.Craftsmanship = legacyCraftsmanship;
        }
    }
}
