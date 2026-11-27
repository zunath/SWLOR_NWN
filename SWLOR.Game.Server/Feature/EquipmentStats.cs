using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.EquipmentStatActions;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using ItemProperty = SWLOR.NWN.API.Engine.ItemProperty;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature
{
    public static class EquipmentStats
    {
        /// <summary>
        /// The item property types equipping adds to the wearer.
        /// </summary>
        public static IReadOnlyCollection<ItemPropertyType> HandledPropertyTypes => Stat.EquipmentStatActionTypes;

        /// <summary>
        /// When the module loads, cache the actions taken for each type of custom item property.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterStatActions()
        {
            Stat.RegisterEquipmentStatAction(ItemPropertyType.HPBonus, (creature, item, ip, adding) => EquipmentVitalStatActions.ApplyHPBonus(creature, item, ip, adding));
            Stat.RegisterEquipmentStatAction(ItemPropertyType.FP, (creature, item, ip, adding) => EquipmentVitalStatActions.ApplyFPBonus(creature, item, ip, adding));
            Stat.RegisterEquipmentStatAction(ItemPropertyType.FPRegen, (creature, item, ip, adding) => EquipmentVitalStatActions.ApplyFPRegenBonus(creature, item, ip, adding));
            Stat.RegisterEquipmentStatAction(ItemPropertyType.Stamina, (creature, item, ip, adding) => EquipmentVitalStatActions.ApplySTMBonus(creature, item, ip, adding));
            Stat.RegisterEquipmentStatAction(ItemPropertyType.STMRegen, (creature, item, ip, adding) => EquipmentVitalStatActions.ApplySTMRegenBonus(creature, item, ip, adding));
            Stat.RegisterEquipmentStatAction(ItemPropertyType.CombatReadiness, (creature, item, ip, adding) => EquipmentVitalStatActions.ApplyCombatReadiness(creature, item, ip, adding));
            Stat.RegisterEquipmentStatAction(ItemPropertyType.Attack, (creature, item, ip, adding) => EquipmentRatingStatActions.ApplyAttack(creature, item, ip, adding));
            Stat.RegisterEquipmentStatAction(ItemPropertyType.Accuracy, (creature, item, ip, adding) => EquipmentRatingStatActions.ApplyAccuracy(creature, item, ip, adding));
            Stat.RegisterEquipmentStatAction(ItemPropertyType.ForceAttack, (creature, item, ip, adding) => EquipmentRatingStatActions.ApplyForceAttack(creature, item, ip, adding));
            Stat.RegisterEquipmentStatAction(ItemPropertyType.Defense, (creature, item, ip, adding) => EquipmentRatingStatActions.ApplyDefense(creature, item, ip, adding));
            Stat.RegisterEquipmentStatAction(ItemPropertyType.Resistance, (creature, item, ip, adding) => EquipmentRatingStatActions.ApplyResistance(creature, item, ip, adding));
            Stat.RegisterEquipmentStatAction(ItemPropertyType.Evasion, (creature, item, ip, adding) => EquipmentRatingStatActions.ApplyEvasion(creature, item, ip, adding));
            Stat.RegisterEquipmentStatAction(ItemPropertyType.Control, (creature, item, ip, adding) => EquipmentCraftStatActions.ApplyControl(creature, item, ip, adding));
            Stat.RegisterEquipmentStatAction(ItemPropertyType.Craftsmanship, (creature, item, ip, adding) => EquipmentCraftStatActions.ApplyCraftsmanship(creature, item, ip, adding));
            Stat.RegisterEquipmentStatAction(ItemPropertyType.CPBonus, (creature, item, ip, adding) => EquipmentCraftStatActions.ApplyCPBonus(creature, item, ip, adding));
            Stat.RegisterEquipmentStatAction(ItemPropertyType.Stealth, (creature, item, ip, adding) => EquipmentFieldStatActions.ApplyStealth(creature, item, ip, adding));
            Stat.RegisterEquipmentStatAction(ItemPropertyType.Detection, (creature, item, ip, adding) => EquipmentFieldStatActions.ApplyDetection(creature, item, ip, adding));
            Stat.RegisterEquipmentStatAction(ItemPropertyType.TrapBonus, (creature, item, ip, adding) => EquipmentFieldStatActions.ApplyTrapBonus(creature, item, ip, adding));
            Stat.RegisterEquipmentStatAction(ItemPropertyType.Disarm, (creature, item, ip, adding) => EquipmentFieldStatActions.ApplyTrapDisarm(creature, item, ip, adding));
            Stat.RegisterEquipmentStatAction(ItemPropertyType.PoisonBonus, (creature, item, ip, adding) => EquipmentFieldStatActions.ApplyPoisonBonus(creature, item, ip, adding));
            Stat.RegisterEquipmentStatAction(ItemPropertyType.Lockpicking, (creature, item, ip, adding) => EquipmentFieldStatActions.ApplyLockpicking(creature, item, ip, adding));
        }

        private const string DisplacedRecordStatsVariable = "EQUIPMENT_DISPLACED_RECORD_STATS";

        /// <summary>
        /// The equipment records an item's properties feed.
        /// </summary>
        private static EquipmentRecordStats GetRecordStats(uint item)
        {
            var stats = EquipmentRecordStats.None;
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                stats |= EquipmentStatTotals.GetRecordStat(GetItemPropertyType(ip));
            }

            return stats;
        }

        /// <summary>
        /// When an item is equipped, if it has any custom status, apply them now.
        /// This should be run in the "after" event because any restrictions should be checked first.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSWLORItemEquipValidBefore)]
        public static void ApplyStats()
        {
            var creature = OBJECT_SELF;
            if (GetIsDM(creature) || GetIsDMPossessed(creature)) return;

            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            var slot = (InventorySlot)Convert.ToInt32(EventsPlugin.GetEventData("SLOT"));

            // The unequip event doesn't fire if an item is being swapped out.
            // If there's an item in the slot, run the stat removals first.
            var existingItemInSlot = GetItemInSlot(slot, creature);
            if (GetIsObjectValid(existingItemInSlot))
            {
                ApplyItemStats(creature, existingItemInSlot, false);
                // The after event names only the incoming item; remember the records the displaced one fed.
                SetLocalInt(creature, DisplacedRecordStatsVariable,
                    GetLocalInt(creature, DisplacedRecordStatsVariable) | (int)GetRecordStats(existingItemInSlot));
            }

            ApplyItemStats(creature, item, true);
        }

        /// <summary>
        /// When an item is unequipped, if it has any custom stats, remove them now.
        /// </summary>
        [NWNEventHandler(ScriptName.OnItemUnequipBefore)]
        public static void RemoveStats()
        {
            var creature = OBJECT_SELF;
            if (GetIsDM(creature) || GetIsDMPossessed(creature)) return;

            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            // The native unequip raises this event for any item it is asked to unequip, including one that is
            // no longer worn, such as the second of two queued unequips. Only a worn item has stats to remove.
            if (Item.GetItemSlot(creature, item) == InventorySlot.Invalid)
                return;

            ApplyItemStats(creature, item, false);
        }

        /// <summary>
        /// Adds or removes every stat an item grants its wearer, exactly as equipping or unequipping it does.
        /// A Power Glove's stats count once per hand.
        /// </summary>
        /// <param name="creature">The wearer</param>
        /// <param name="item">The equipped item</param>
        /// <param name="isAdding">If true, the item's stats are added, if false they're removed</param>
        public static void ApplyItemStats(uint creature, uint item, bool isAdding)
        {
            var baseItem = GetBaseItemType(item);
            if (baseItem == BaseItem.CompanionPistol) return;
            var copies = PowerGloveEquipment.GetEquipmentStatCopies(baseItem);
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (!Stat.TryGetEquipmentStatAction(GetItemPropertyType(ip), out var action)) continue;
                for (var copy = 0; copy < copies; copy++)
                    action(creature, item, ip, isAdding);
            }
        }

        /// <summary>
        /// Adds or removes one amount of a stat for an equipped item, as a property of that size would on equip or unequip.
        /// </summary>
        /// <param name="creature">The wearer</param>
        /// <param name="item">The equipped item the amount belongs to</param>
        /// <param name="type">The stat's item property type</param>
        /// <param name="subtype">The property subtype, such as the Defense damage type</param>
        /// <param name="amount">The amount to add or remove</param>
        /// <param name="isAdding">If true, the amount is added, if false it's removed</param>
        public static void ApplyStatAmount(uint creature, uint item, ItemPropertyType type, int subtype, int amount, bool isAdding)
        {
            var baseItem = GetBaseItemType(item);
            if (baseItem == BaseItem.CompanionPistol) return;
            if (amount <= 0 || !Stat.TryGetEquipmentStatAction(type, out var action))
                return;
            for (var copy = 0; copy < PowerGloveEquipment.GetEquipmentStatCopies(baseItem); copy++)
                action(creature, item, ItemPropertyCustom(type, subtype, amount), isAdding);
        }

        [NWNEventHandler(ScriptName.OnItemEquipValidateAfter)]
        [NWNEventHandler(ScriptName.OnItemUnequipAfter)]
        public static void RefreshCombatReadinessAfterEquipmentChange()
        {
            // The native slots now describe the completed change. Reconciliation
            // also handles failed attempts and repeated events without delta drift.
            StatRecord.RefreshCombatReadinessEquipment(OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnItemEquipValidateAfter)]
        [NWNEventHandler(ScriptName.OnItemUnequipAfter)]
        public static void RefreshMaxHPAfterEquipmentChange()
        {
            // HP bonuses are added and removed before the change completes, so a failed change
            // leaves the record off. Rebuild it from the slots.
            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) != ItemPropertyType.HPBonus)
                    continue;

                // The status window refreshed before the change, from the uncorrected maximum.
                if (CreatureStat.RefreshPlayerMaxHP(OBJECT_SELF))
                    Gui.PublishRefreshEvent(OBJECT_SELF, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.HP));
                return;
            }
        }

        [NWNEventHandler(ScriptName.OnItemEquipValidateAfter)]
        [NWNEventHandler(ScriptName.OnItemUnequipAfter)]
        public static void RefreshEquipmentRecordsAfterEquipmentChange()
        {
            // Every record summed from gear is added and removed before the change completes.
            // Rebuild the ones this item, and any item it displaced, feed from the slots.
            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            var stats = GetRecordStats(item) | (EquipmentRecordStats)GetLocalInt(OBJECT_SELF, DisplacedRecordStatsVariable);
            DeleteLocalInt(OBJECT_SELF, DisplacedRecordStatsVariable);

            var corrected = StatRecord.RefreshPlayerEquipmentStats(OBJECT_SELF, stats);
            // The status window refreshed before the change, from the uncorrected maximums.
            if (corrected.HasFlag(EquipmentRecordStats.FP))
                Gui.PublishRefreshEvent(OBJECT_SELF, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.FP));
            if (corrected.HasFlag(EquipmentRecordStats.Stamina))
                Gui.PublishRefreshEvent(OBJECT_SELF, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.STM));
        }

    }
}
