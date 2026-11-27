using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceEditing
{
    public class AppearancePartEditService
    {
        private AppearanceTintEditorService AppearanceTintEditorService { get; }
        private ITintMapCarryService TintMapCarry { get; }
        private IWeaponAppearanceCatalog IWeaponAppearanceCatalog { get; }

        public AppearancePartEditService(AppearanceTintEditorService appearanceTintEditorService, ITintMapCarryService tintMapCarry, IWeaponAppearanceCatalog iWeaponAppearanceCatalog)
        {
            AppearanceTintEditorService = appearanceTintEditorService;
            TintMapCarry = tintMapCarry;
            IWeaponAppearanceCatalog = iWeaponAppearanceCatalog;
        }

        public static InventorySlot GetInventorySlot(IAppearanceEditorHost host)
        {
            var slot = InventorySlot.Invalid;

            switch (host.SelectedItemTypeIndex)
            {
                case 0: // 0 = Chest
                    slot = InventorySlot.Chest;
                    break;
                case 1: // 1 = Head
                    slot = InventorySlot.Head;
                    break;
                case 2: // 2 = Cloak
                    slot = InventorySlot.Cloak;
                    break;
                case 3: // 3 = Weapon (Main Hand)
                    slot = InventorySlot.RightHand;
                    break;
                case 4: // 4 = Weapon (Off Hand)
                    var offHand = GetItemInSlot(InventorySlot.LeftHand, host.Session.Target);
                    slot = host.GetOffHandAppearanceOwner(offHand) != offHand
                        ? InventorySlot.RightHand : InventorySlot.LeftHand;
                    break;
            }

            return slot;
        }

        public static ItemAppearanceType GetModelType(IAppearanceEditorHost host)
        {
            var modelType = ItemAppearanceType.ArmorModel;

            switch (host.SelectedItemTypeIndex)
            {
                case 0: // 0 = Chest
                    modelType = ItemAppearanceType.ArmorModel;
                    break;
                case 1: // 1 = Head
                    modelType = ItemAppearanceType.SimpleModel;
                    break;
                case 2: // 2 = Cloak
                    modelType = ItemAppearanceType.SimpleModel;
                    break;
                case 3: // 3 = Weapon (Main Hand)
                    modelType = ItemAppearanceType.WeaponModel;
                    break;
                case 4: // 4 = Weapon (Off Hand)
                    modelType = ItemAppearanceType.WeaponModel;
                    break;
            }

            return modelType;
        }

        public void ModifyItemPart(IAppearanceEditorHost host, int type, int partId, int colorId = -1)
        {
            AppearanceTintEditorService.FlushPendingPickerColor(host);
            AppearanceTintEditorService.CommitCustomTintComponents(host);
            host.ToggleItemEquippedFlags();
            if (host.DoesNotHaveItemEquipped)
                return;

            var slot = GetInventorySlot(host);
            var item = host.GetItem();
            var itemType = GetBaseItemType(item);
            var modelType = GetModelType(host);
            var armorPart = modelType == ItemAppearanceType.ArmorModel
                ? (AppearanceArmor)type
                : AppearanceArmor.Invalid;
            var previousTintSelections = AppearanceTintEditorService.GetCurrentTintMapSelections(host)
                .Where(selection =>
                    selection.PaletteSource == item &&
                    selection.ArmorPart == armorPart)
                .ToList();
            var tintCarry = TintMapCarry.CaptureItemCustomColors(item, previousTintSelections);
            if (colorId > -1)
            {
                EquippedItemAppearance.Set(item, ItemAppearanceType.WeaponColor, type, colorId);
                partId %= 100;
            }

            if (IWeaponAppearanceCatalog.Definitions.TryGetValue(itemType, out var weapon) && weapon.IsSimple)
                modelType = ItemAppearanceType.SimpleModel;

            EquippedItemAppearance.Set(item, modelType, type, partId);
            // The original item stays equipped. Its new material keys are available now,
            // so carry custom tints before another model/preset click can arrive.
            TintMapCarry.QueueItemCustomColorCarry(
                host.Session.Target, item, item, host.Player, slot, armorPart, tintCarry, applyImmediately: true);
            EquippedItemAppearance.Refresh(host.Session.Target, item);
        }

        /// <summary>
        /// What an armor part selection does after its binding is set: a client change of the list (not the editor's own load or
        /// hydration) applies the chosen style to the item.
        /// </summary>
        public void ArmorSelectionChanged(IAppearanceEditorHost host, AppearanceArmor part, int value)
        {
            if (host.Session.ArmorClientBindingsWatched && !host.Session.SkipAdjustArmorPart && value != GetItemAppearance(host.GetItem(), ItemAppearanceType.ArmorModel, (int)part))
                AdjustArmorPart(host, part, 0);
        }

        public static AppearanceArmor GetArmorModelType(IAppearanceEditorHost host, AppearanceColorTarget colorTarget)
        {
            var armorModel = AppearanceArmor.Invalid;
            switch (colorTarget)
            {
                case AppearanceColorTarget.LeftShoulder:
                    armorModel = AppearanceArmor.LeftShoulder;
                    break;
                case AppearanceColorTarget.LeftBicep:
                    armorModel = AppearanceArmor.LeftBicep;
                    break;
                case AppearanceColorTarget.LeftForearm:
                    armorModel = AppearanceArmor.LeftForearm;
                    break;
                case AppearanceColorTarget.LeftHand:
                    armorModel = AppearanceArmor.LeftHand;
                    break;
                case AppearanceColorTarget.LeftThigh:
                    armorModel = AppearanceArmor.LeftThigh;
                    break;
                case AppearanceColorTarget.LeftShin:
                    armorModel = AppearanceArmor.LeftShin;
                    break;
                case AppearanceColorTarget.LeftFoot:
                    armorModel = AppearanceArmor.LeftFoot;
                    break;
                case AppearanceColorTarget.RightShoulder:
                    armorModel = AppearanceArmor.RightShoulder;
                    break;
                case AppearanceColorTarget.RightBicep:
                    armorModel = AppearanceArmor.RightBicep;
                    break;
                case AppearanceColorTarget.RightForearm:
                    armorModel = AppearanceArmor.RightForearm;
                    break;
                case AppearanceColorTarget.RightHand:
                    armorModel = AppearanceArmor.RightHand;
                    break;
                case AppearanceColorTarget.RightThigh:
                    armorModel = AppearanceArmor.RightThigh;
                    break;
                case AppearanceColorTarget.RightShin:
                    armorModel = AppearanceArmor.RightShin;
                    break;
                case AppearanceColorTarget.RightFoot:
                    armorModel = AppearanceArmor.RightFoot;
                    break;
                case AppearanceColorTarget.Neck:
                    armorModel = AppearanceArmor.Neck;
                    break;
                case AppearanceColorTarget.Chest:
                    armorModel = AppearanceArmor.Torso;
                    break;
                case AppearanceColorTarget.Belt:
                    armorModel = AppearanceArmor.Belt;
                    break;
                case AppearanceColorTarget.Pelvis:
                    armorModel = AppearanceArmor.Pelvis;
                    break;
                case AppearanceColorTarget.Robe:
                    armorModel = AppearanceArmor.Robe;
                    break;
            }

            return armorModel;
        }

        public static int? ResolveAdjustedArmorValue(GuiBindingList<GuiComboEntry> options, int value, int adjustBy)
        {
            if (options == null || options.Count == 0)
                return null;

            for (var index = 0; index < options.Count; index++)
            {
                if (options[index].Value == value)
                    return options[System.Math.Clamp(index + adjustBy, 0, options.Count - 1)].Value;
            }

            // Ignore stale client selections instead of changing an unrelated model.
            return null;
        }

        public void AdjustArmorPart(IAppearanceEditorHost host, AppearanceArmor partType, int adjustBy)
        {
            if (!host.IsEquipmentSelected || host.SelectedItemTypeIndex != 0 || !host.HasItemEquipped)
                return;

            if (partType == AppearanceArmor.Robe)
            {
                // A filtered option's value is the model ID; its index no longer
                // corresponds to the shared, unfiltered armor definition.
                var styles = RobeAppearance.GetAvailableStyles(host.Session.Target, ArmorAppearanceCatalog.Definitions[GetAppearanceType(host.Session.Target)].Robe);
                var requested = host.RobeSelection;
                var index = styles.ToList().IndexOf(requested);
                if (index < 0 || styles.Count == 0)
                {
                    host.LoadItemParts();
                    return;
                }
                var selected = styles[System.Math.Clamp(index + adjustBy, 0, styles.Count - 1)];
                var wasSkipping = host.Session.SkipAdjustArmorPart;
                host.Session.SkipAdjustArmorPart = true;
                try
                {
                    host.RobeSelection = selected;
                    ModifyItemPart(host, (int)partType, selected);
                }
                finally
                {
                    host.Session.SkipAdjustArmorPart = wasSkipping;
                }
                return;
            }
            void Adjust(GuiBindingList<GuiComboEntry> options, int selection, Action<int> setSelection)
            {
                var selected = ResolveAdjustedArmorValue(options, selection, adjustBy);
                if (!selected.HasValue)
                    return;

                setSelection(selected.Value);
                ModifyItemPart(host, (int)partType, selected.Value);
            }

            var wasSkippingAdjustment = host.Session.SkipAdjustArmorPart;
            host.Session.SkipAdjustArmorPart = true;
            try
            {
                var binding = host.GetArmorPartBinding(partType);
                if (binding.Options != null)
                    Adjust(binding.Options, binding.Selection, binding.Select);
            }
            finally
            {
                host.Session.SkipAdjustArmorPart = wasSkippingAdjustment;
            }
        }
    }
}
