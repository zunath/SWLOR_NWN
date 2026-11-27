using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.Species;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceEditing
{
    /// <summary>
    /// The editor's option and part lists: the colour and part categories, the body and item part pages, the part the selected
    /// row wears, and the item the edited page is about.
    /// </summary>
    public class AppearancePartListService
    {
        private AppearanceItemEligibility AppearanceItemEligibility { get; }
        private AppearancePartBrowserService AppearancePartBrowserService { get; }
        private AppearancePartEditService AppearancePartEditService { get; }
        private AppearanceSettingsService AppearanceSettingsService { get; }
        private AppearanceSwatchService AppearanceSwatchService { get; }
        private AppearanceTintEditorService AppearanceTintEditorService { get; }
        private IPowerGloveEquipmentService PowerGloveEquipment { get; }
        private ITintMapCreatureColorService TintMapCreatureColor { get; }
        private ITwinPistolEquipmentService TwinPistolEquipment { get; }
        private IWeaponAppearanceCatalog IWeaponAppearanceCatalog { get; }

        public AppearancePartListService(AppearanceItemEligibility appearanceItemEligibility, AppearancePartBrowserService appearancePartBrowserService, AppearancePartEditService appearancePartEditService, AppearanceSettingsService appearanceSettingsService, AppearanceSwatchService appearanceSwatchService, AppearanceTintEditorService appearanceTintEditorService, IPowerGloveEquipmentService powerGloveEquipment, ITintMapCreatureColorService tintMapCreatureColor, ITwinPistolEquipmentService twinPistolEquipment, IWeaponAppearanceCatalog iWeaponAppearanceCatalog)
        {
            AppearanceItemEligibility = appearanceItemEligibility;
            AppearancePartBrowserService = appearancePartBrowserService;
            AppearancePartEditService = appearancePartEditService;
            AppearanceSettingsService = appearanceSettingsService;
            AppearanceSwatchService = appearanceSwatchService;
            AppearanceTintEditorService = appearanceTintEditorService;
            PowerGloveEquipment = powerGloveEquipment;
            TintMapCreatureColor = tintMapCreatureColor;
            TwinPistolEquipment = twinPistolEquipment;
            IWeaponAppearanceCatalog = iWeaponAppearanceCatalog;
        }

        public void LoadColorCategoryOptions(IAppearanceEditorHost host)
        {
            if (host.DoesNotHaveItemEquipped)
                return;

            var colorCategoryOptions = new GuiBindingList<string>();

            if (host.IsAppearanceSelected)
            {
                colorCategoryOptions.Add("Skin Color");
                colorCategoryOptions.Add("Hair Color");
                colorCategoryOptions.Add("Tattoo 1 Color");
                colorCategoryOptions.Add("Tattoo 2 Color");

                host.IsColorPickerVisible = true;
            }
            else if (host.IsEquipmentSelected)
            {
                if (host.SelectedItemTypeIndex == 3 || host.SelectedItemTypeIndex == 4) // 3 & 4 = Weapon (Main or Off Hand)
                {
                    colorCategoryOptions.Add("Weapon");
                    host.IsColorPickerVisible = false;
                }
                else
                {
                    colorCategoryOptions.Add("Leather 1");
                    colorCategoryOptions.Add("Leather 2");
                    colorCategoryOptions.Add("Cloth 1");
                    colorCategoryOptions.Add("Cloth 2");
                    colorCategoryOptions.Add("Metal 1");
                    colorCategoryOptions.Add("Metal 2");

                    host.IsColorPickerVisible = true;
                }
            }

            var colorCategorySelected = new GuiBindingList<bool>();

            foreach (var unused in colorCategoryOptions)
            {
                colorCategorySelected.Add(false);
            }

            host.ColorCategoryOptions = colorCategoryOptions;
            host.ColorCategorySelected = colorCategorySelected;

            host.SelectedColorCategoryIndex = 0;
            host.ColorCategorySelected[host.SelectedColorCategoryIndex] = true;
        }

        public void LoadPartCategoryOptions(IAppearanceEditorHost host)
        {
            if (host.DoesNotHaveItemEquipped)
                return;

            var partCategoryOptions = new GuiBindingList<string>();

            if (host.IsAppearanceSelected)
            {
                partCategoryOptions.Add("Head");
                partCategoryOptions.Add("Torso");
                partCategoryOptions.Add("Pelvis");
                partCategoryOptions.Add("Right Bicep");
                partCategoryOptions.Add("Right Forearm");
                partCategoryOptions.Add("Right Hand");
                partCategoryOptions.Add("Right Thigh");
                partCategoryOptions.Add("Right Shin");
                partCategoryOptions.Add("Right Foot");
                partCategoryOptions.Add("Left Bicep");
                partCategoryOptions.Add("Left Forearm");
                partCategoryOptions.Add("Left Hand");
                partCategoryOptions.Add("Left Thigh");
                partCategoryOptions.Add("Left Shin");
                partCategoryOptions.Add("Left Foot");
            }
            else if (host.IsEquipmentSelected)
            {
                if (host.SelectedItemTypeIndex == 0) // 0 = Armor
                {
                    return;
                }
                else if (host.SelectedItemTypeIndex == 1) // 1 = Helmet
                {
                    partCategoryOptions.Add("Helmet");
                }
                else if (host.SelectedItemTypeIndex == 2) // 2 = Cloak
                {
                    partCategoryOptions.Add("Cloak");
                }
                else if (host.SelectedItemTypeIndex == 3 || host.SelectedItemTypeIndex == 4) // 3 = Weapon (Main Hand), 4 = Weapon (Off Hand)
                {
                    var item = GetItem(host);
                    var type = GetBaseItemType(item);
                    var partAppearance = IWeaponAppearanceCatalog.Definitions[type];

                    if (partAppearance.IsSimple)
                    {
                        partCategoryOptions.Add("Simple");
                    }
                    else
                    {
                        partCategoryOptions.Add("Top");
                        partCategoryOptions.Add("Middle");
                        partCategoryOptions.Add("Bottom");
                    }
                }
            }

            var partCategorySelected = new GuiBindingList<bool>();

            foreach (var unused in partCategoryOptions)
            {
                partCategorySelected.Add(false);
            }

            host.PartCategoryOptions = partCategoryOptions;
            host.PartCategorySelected = partCategorySelected;

            host.SelectedPartCategoryIndex = 0;
            host.PartCategorySelected[host.SelectedPartCategoryIndex] = true;
        }

        private (GuiBindingList<string>, GuiBindingList<bool>) GetPartLists(IAppearanceEditorHost host, int[] partIds, Func<int, string> getPartName = null)
        {
            var partNames = new GuiBindingList<string>();
            var partSelected = new GuiBindingList<bool>();
            var partIdToIndex = new Dictionary<int, int>();
            var index = 0;

            foreach (var partId in partIds)
            {
                var partIndex = partId;

                partNames.Add(getPartName?.Invoke(partId) ?? $"Part #{partId}");
                partSelected.Add(false);
                partIdToIndex[partIndex] = index;
                index++;
            }

            host.Session.PartIdToIndex = partIdToIndex;
            host.Session.PartIds = partIds.ToList();
            return (partNames, partSelected);
        }

        public uint GetItem(IAppearanceEditorHost host)
        {
            if (host.SelectedItemTypeIndex == 0) // 0 = Armor
            {
                return GetItemInSlot(InventorySlot.Chest, host.Session.Target);
            }
            else if (host.SelectedItemTypeIndex == 1) // 1 = Helmet
            {
                return GetItemInSlot(InventorySlot.Head, host.Session.Target);
            }
            else if (host.SelectedItemTypeIndex == 2) // 2 = Cloak
            {
                return GetItemInSlot(InventorySlot.Cloak, host.Session.Target);
            }
            else if (host.SelectedItemTypeIndex == 3) // 3 = Weapon (Main Hand)
            {
                return GetItemInSlot(InventorySlot.RightHand, host.Session.Target);
            }
            else if (host.SelectedItemTypeIndex == 4) // 4 = Weapon (Off Hand)
            {
                return GetOffHandAppearanceOwner(host, GetItemInSlot(InventorySlot.LeftHand, host.Session.Target));
            }

            return OBJECT_INVALID;
        }

        // A generated off hand mirrors its owned main-hand item, so its edits apply to that item.
        public uint GetOffHandAppearanceOwner(IAppearanceEditorHost host, uint offHand) =>
            TwinPistolEquipment.ResolveAppearanceOwner(host.Session.Target,
                PowerGloveEquipment.ResolveAppearanceOwner(host.Session.Target, offHand));

        public bool IsValidItem(IAppearanceEditorHost host) =>
            // Treated as a valid item if we're not in the item customization page.
            host.IsAppearanceSelected || AppearanceItemEligibility.IsValid(host.Session.Target, GetItem(host), host.SelectedItemTypeIndex);

        public void LoadBodyParts(IAppearanceEditorHost host)
        {
            var appearanceType = GetAppearanceType(host.Session.Target);
            var gender = GetGender(host.Session.Target);

            if (!RacialAppearanceRegistry.TryGet(appearanceType, out var appearance))
            {
                AppearanceSettingsService.CloseForUnsupportedAppearance(host);
                return;
            }

            int[] partIds;
            int selectedPartId;

            switch (host.SelectedPartCategoryIndex)
            {
                case 0: // Head
                    switch (gender)
                    {
                        case Gender.Male:
                            partIds = appearance.MaleHeads;
                            break;
                        default:
                            partIds = appearance.FemaleHeads;
                            break;
                    }

                    selectedPartId = GetCreatureBodyPart(CreaturePart.Head, host.Session.Target);
                    break;
                case 1: // Torso
                    partIds = appearance.Torsos;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.Torso, host.Session.Target);
                    break;
                case 2: // Pelvis
                    partIds = appearance.Pelvis;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.Pelvis, host.Session.Target);
                    break;
                case 3: // Right Bicep
                    partIds = appearance.RightBicep;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightBicep, host.Session.Target);
                    break;
                case 4: // Right Forearm
                    partIds = appearance.RightForearm;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightForearm, host.Session.Target);
                    break;
                case 5: // Right Hand
                    partIds = appearance.RightHand;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightHand, host.Session.Target);
                    break;
                case 6: // Right Thigh
                    partIds = appearance.RightThigh;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightThigh, host.Session.Target);
                    break;
                case 7: // Right Shin
                    partIds = appearance.RightShin;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightShin, host.Session.Target);
                    break;
                case 8: // Right Foot
                    partIds = appearance.RightFoot;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightFoot, host.Session.Target);
                    break;
                case 9: // Left Bicep
                    partIds = appearance.LeftBicep;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftBicep, host.Session.Target);
                    break;
                case 10: // Left Forearm
                    partIds = appearance.LeftForearm;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftForearm, host.Session.Target);
                    break;
                case 11: // Left Hand
                    partIds = appearance.LeftHand;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftHand, host.Session.Target);
                    break;
                case 12: // Left Thigh
                    partIds = appearance.LeftThigh;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftThigh, host.Session.Target);
                    break;
                case 13: // Left Shin
                    partIds = appearance.LeftShin;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftShin, host.Session.Target);
                    break;
                case 14: // Left Foot
                    partIds = appearance.LeftFoot;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftFoot, host.Session.Target);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(AppearanceEditorViewModel.SelectedPartIndex));
            }

            var (partNames, partSelected) = GetPartLists(host, partIds);

            host.PartOptions = partNames;
            host.PartSelected = partSelected;
            host.SelectedPartIndex = host.Session.PartIdToIndex.TryGetValue(selectedPartId, out var selectedIndex) ? selectedIndex : -1;
            if (host.SelectedPartIndex >= 0)
                host.PartSelected[host.SelectedPartIndex] = true;
            AppearancePartBrowserService.RefreshPartBrowser(host);
        }

        public void LoadItemParts(IAppearanceEditorHost host)
        {
            if (!host.IsEquipmentSelected)
                return;
            AppearanceTintEditorService.SuspendArmorClientWatches(host);
            var wasSkipping = host.Session.SkipAdjustArmorPart;
            host.Session.SkipAdjustArmorPart = true;
            try
            {
                PopulateItemParts(host);
            }
            finally
            {
                host.Session.SkipAdjustArmorPart = wasSkipping;
            }
            if (host.SelectedItemTypeIndex == 0 && host.HasItemEquipped)
                AppearanceTintEditorService.ResumeArmorClientWatches(host);
        }

        private void PopulateItemParts(IAppearanceEditorHost host)
        {
            if (host.DoesNotHaveItemEquipped)
                return;

            var item = GetItem(host);
            int[] partIds;
            int selectedPartId;
            var appearanceType = GetAppearanceType(host.Session.Target);
            var type = GetBaseItemType(item);

            if (host.SelectedItemTypeIndex == 0) // 0 = Armor
            {
                GuiBindingList<GuiComboEntry> Available(AppearanceArmor part, IEnumerable<GuiComboEntry> options) =>
                    ArmorPartAppearance.GetAvailableOptions(host.Session.Target, part, options);
                var neckOptions = new GuiBindingList<GuiComboEntry>();
                foreach (var style in NeckAppearance.GetAvailableStyles(host.Session.Target, ArmorAppearanceCatalog.Definitions[appearanceType].Neck.Prepend(0)))
                    neckOptions.Add(new GuiComboEntry(style.ToString(), style));
                host.NeckOptions = neckOptions;
                host.ChestOptions = Available(AppearanceArmor.Torso, ArmorAppearanceCatalog.Definitions[appearanceType].TorsoOptions);
                host.BeltOptions = Available(AppearanceArmor.Belt, ArmorAppearanceCatalog.Definitions[appearanceType].BeltOptions);
                host.PelvisOptions = Available(AppearanceArmor.Pelvis, ArmorAppearanceCatalog.Definitions[appearanceType].PelvisOptions);
                var robeOptions = new GuiBindingList<GuiComboEntry>();
                foreach (var style in RobeAppearance.GetAvailableStyles(host.Session.Target, ArmorAppearanceCatalog.Definitions[appearanceType].Robe))
                    robeOptions.Add(new GuiComboEntry(style.ToString(), style));
                host.RobeOptions = robeOptions;
                var repairedNeck = NeckAppearance.RemoveUnavailableNeck(host.Session.Target, item);
                var repairedParts = ArmorPartAppearance.RemoveUnavailableParts(host.Session.Target, item);
                if (RobeAppearance.RemoveUnavailableRobe(host.Session.Target, item) || repairedNeck || repairedParts)
                    EquippedItemAppearance.Refresh(host.Session.Target, item);

                host.LeftShoulderOptions = Available(AppearanceArmor.LeftShoulder, ArmorAppearanceCatalog.Definitions[appearanceType].ShoulderOptions);
                host.LeftBicepOptions = Available(AppearanceArmor.LeftBicep, ArmorAppearanceCatalog.Definitions[appearanceType].BicepOptions);
                host.LeftForearmOptions = Available(AppearanceArmor.LeftForearm, ArmorAppearanceCatalog.Definitions[appearanceType].ForearmOptions);
                host.LeftHandOptions = Available(AppearanceArmor.LeftHand, ArmorAppearanceCatalog.Definitions[appearanceType].HandOptions);
                host.LeftThighOptions = Available(AppearanceArmor.LeftThigh, ArmorAppearanceCatalog.Definitions[appearanceType].ThighOptions);
                host.LeftShinOptions = Available(AppearanceArmor.LeftShin, ArmorAppearanceCatalog.Definitions[appearanceType].ShinOptions);
                host.LeftFootOptions = Available(AppearanceArmor.LeftFoot, ArmorAppearanceCatalog.Definitions[appearanceType].FootOptions);

                host.RightShoulderOptions = Available(AppearanceArmor.RightShoulder, ArmorAppearanceCatalog.Definitions[appearanceType].ShoulderOptions);
                host.RightBicepOptions = Available(AppearanceArmor.RightBicep, ArmorAppearanceCatalog.Definitions[appearanceType].BicepOptions);
                host.RightForearmOptions = Available(AppearanceArmor.RightForearm, ArmorAppearanceCatalog.Definitions[appearanceType].ForearmOptions);
                host.RightHandOptions = Available(AppearanceArmor.RightHand, ArmorAppearanceCatalog.Definitions[appearanceType].HandOptions);
                host.RightThighOptions = Available(AppearanceArmor.RightThigh, ArmorAppearanceCatalog.Definitions[appearanceType].ThighOptions);
                host.RightShinOptions = Available(AppearanceArmor.RightShin, ArmorAppearanceCatalog.Definitions[appearanceType].ShinOptions);
                host.RightFootOptions = Available(AppearanceArmor.RightFoot, ArmorAppearanceCatalog.Definitions[appearanceType].FootOptions);

                host.NeckSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck);
                host.ChestSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso);
                host.BeltSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Belt);
                host.PelvisSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis);
                host.RobeSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe);

                host.LeftShoulderSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShoulder);
                host.LeftBicepSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftBicep);
                host.LeftForearmSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftForearm);
                host.LeftHandSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand);
                host.LeftThighSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftThigh);
                host.LeftShinSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShin);
                host.LeftFootSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftFoot);

                host.RightShoulderSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShoulder);
                host.RightBicepSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightBicep);
                host.RightForearmSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightForearm);
                host.RightHandSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand);
                host.RightThighSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightThigh);
                host.RightShinSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShin);
                host.RightFootSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightFoot);

                AppearanceSwatchService.UpdateAllColors(host);
                AppearanceSwatchService.SyncSelectedPartSwatches(host);
                AppearancePartBrowserService.RefreshPartBrowser(host);

                return;
            }
            else if (host.SelectedItemTypeIndex == 1) // 1 = Helmet
            {
                partIds = ArmorAppearanceCatalog.Definitions[appearanceType].Helmet;
                selectedPartId = GetItemAppearance(item, ItemAppearanceType.SimpleModel, -1);
            }
            else if (host.SelectedItemTypeIndex == 2) // 2 = Cloak
            {
                partIds = ArmorAppearanceCatalog.Definitions[appearanceType].Cloak;
                selectedPartId = GetItemAppearance(item, ItemAppearanceType.SimpleModel, -1);
            }
            else if (host.SelectedItemTypeIndex == 3 || host.SelectedItemTypeIndex == 4) // 3 = Weapon (Main Hand), 4 = Weapon (Off Hand)
            {
                if (IWeaponAppearanceCatalog.Definitions[type].IsSimple)
                {
                    partIds = IWeaponAppearanceCatalog.Definitions[type].SimpleParts;
                    selectedPartId = GetItemAppearance(item, ItemAppearanceType.SimpleModel, -1);
                }
                else
                {
                    var weaponPart = host.SelectedPartCategoryIndex switch
                    {
                        0 => AppearanceWeapon.Top,
                        1 => AppearanceWeapon.Middle,
                        2 => AppearanceWeapon.Bottom,
                        _ => throw new ArgumentOutOfRangeException(nameof(AppearanceEditorViewModel.SelectedPartIndex))
                    };
                    partIds = AppearanceItemEligibility.GetWeaponPartOptions(item, IWeaponAppearanceCatalog.Definitions[type], weaponPart);
                    selectedPartId = AppearanceItemEligibility.GetWeaponPartId(item, weaponPart);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(AppearanceEditorViewModel.SelectedItemTypeIndex));
            }

            if (selectedPartId <= -1)
                return;

            Func<int, string> getPartName = null;
            if (host.SelectedItemTypeIndex is 3 or 4)
            {
                var part = host.SelectedPartCategoryIndex switch
                {
                    0 => AppearanceWeapon.Top,
                    1 => AppearanceWeapon.Middle,
                    2 => AppearanceWeapon.Bottom,
                    _ => AppearanceWeapon.Invalid
                };
                getPartName = partId => IWeaponAppearanceCatalog.Definitions[type].GetPartName(part, partId);
            }
            var (partNames, partSelected) = GetPartLists(host, partIds, getPartName);

            host.PartOptions = partNames;
            host.PartSelected = partSelected;
            host.SelectedPartIndex = host.Session.PartIdToIndex.TryGetValue(selectedPartId, out var selectedIndex) ? selectedIndex : -1;
            if (host.SelectedPartIndex >= 0)
                host.PartSelected[host.SelectedPartIndex] = true;
            AppearanceSwatchService.RefreshItemBaseSwatches(host);
            AppearancePartBrowserService.RefreshPartBrowser(host);
        }

        public void SelectPartCategory(IAppearanceEditorHost host)
        {
            host.ToggleItemEquippedFlags();
            if (host.DoesNotHaveItemEquipped)
                return;

            var index = NuiGetEventArrayIndex();
            host.PartCategorySelected[host.SelectedPartCategoryIndex] = false;

            host.SelectedPartCategoryIndex = index;
            host.PartCategorySelected[index] = true;

            if (host.IsAppearanceSelected)
            {
                LoadBodyParts(host);
            }
            else if (host.IsEquipmentSelected)
            {
                LoadItemParts(host);
            }
        }

        private void LoadBodyPart(IAppearanceEditorHost host)
        {
            var appearanceType = GetAppearanceType(host.Session.Target);
            var gender = GetGender(host.Session.Target);
            if (!RacialAppearanceRegistry.TryGet(appearanceType, out var appearance))
                return;

            switch (host.SelectedPartCategoryIndex)
            {
                case 0: // Head
                    switch (gender)
                    {
                        case Gender.Male:
                            SetCreatureBodyPart(CreaturePart.Head, appearance.MaleHeads[host.SelectedPartIndex], host.Session.Target);
                            break;
                        default:
                            SetCreatureBodyPart(CreaturePart.Head, appearance.FemaleHeads[host.SelectedPartIndex], host.Session.Target);
                            break;
                    }
                    break;
                case 1: // Torso
                    SetCreatureBodyPart(CreaturePart.Torso, appearance.Torsos[host.SelectedPartIndex], host.Session.Target);
                    break;
                case 2: // Pelvis
                    SetCreatureBodyPart(CreaturePart.Pelvis, appearance.Pelvis[host.SelectedPartIndex], host.Session.Target);
                    break;
                case 3: // Right Bicep
                    BodyPartAppearance.Set(CreaturePart.RightBicep, appearance.RightBicep[host.SelectedPartIndex], host.Session.Target);
                    break;
                case 4: // Right Forearm
                    SetCreatureBodyPart(CreaturePart.RightForearm, appearance.RightForearm[host.SelectedPartIndex], host.Session.Target);
                    break;
                case 5: // Right Hand
                    BodyPartAppearance.Set(CreaturePart.RightHand, appearance.RightHand[host.SelectedPartIndex], host.Session.Target);
                    break;
                case 6: // Right Thigh
                    SetCreatureBodyPart(CreaturePart.RightThigh, appearance.RightThigh[host.SelectedPartIndex], host.Session.Target);
                    break;
                case 7: // Right Shin
                    SetCreatureBodyPart(CreaturePart.RightShin, appearance.RightShin[host.SelectedPartIndex], host.Session.Target);
                    break;
                case 8: // Right Foot
                    SetCreatureBodyPart(CreaturePart.RightFoot, appearance.RightFoot[host.SelectedPartIndex], host.Session.Target);
                    break;
                case 9: // Left Bicep
                    BodyPartAppearance.Set(CreaturePart.LeftBicep, appearance.LeftBicep[host.SelectedPartIndex], host.Session.Target);
                    break;
                case 10: // Left Forearm
                    SetCreatureBodyPart(CreaturePart.LeftForearm, appearance.LeftForearm[host.SelectedPartIndex], host.Session.Target);
                    break;
                case 11: // Left Hand
                    BodyPartAppearance.Set(CreaturePart.LeftHand, appearance.LeftHand[host.SelectedPartIndex], host.Session.Target);
                    break;
                case 12: // Left Thigh
                    SetCreatureBodyPart(CreaturePart.LeftThigh, appearance.LeftThigh[host.SelectedPartIndex], host.Session.Target);
                    break;
                case 13: // Left Shin
                    SetCreatureBodyPart(CreaturePart.LeftShin, appearance.LeftShin[host.SelectedPartIndex], host.Session.Target);
                    break;
                case 14: // Left Foot
                    SetCreatureBodyPart(CreaturePart.LeftFoot, appearance.LeftFoot[host.SelectedPartIndex], host.Session.Target);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(AppearanceEditorViewModel.SelectedPartIndex));
            }

            TintMapCreatureColor.CarryStoredCreatureCustomColors(host.Session.Target);
            ExecuteScript(ScriptName.OnAppearanceEdit, host.Session.Target);
            AppearanceTintEditorService.LoadTintMapEditor(host);
        }

        private void LoadArmorPart(IAppearanceEditorHost host)
        {
            host.ToggleItemEquippedFlags();
            if (host.DoesNotHaveItemEquipped)
                return;
            var item = GetItem(host);
            var itemType = GetBaseItemType(item);
            var appearanceType = GetAppearanceType(host.Session.Target);

            if (host.SelectedItemTypeIndex == 1) // 1 = Helmet
            {
                AppearancePartEditService.ModifyItemPart(host, (int)AppearanceArmor.Invalid, ArmorAppearanceCatalog.Definitions[appearanceType].Helmet[host.SelectedPartIndex]);
            }
            else if (host.SelectedItemTypeIndex == 2) // 2 = Cloak
            {
                AppearancePartEditService.ModifyItemPart(host, (int)AppearanceArmor.Invalid, ArmorAppearanceCatalog.Definitions[appearanceType].Cloak[host.SelectedPartIndex]);
            }
            else if (host.SelectedItemTypeIndex == 3 || host.SelectedItemTypeIndex == 4) // 3 = Weapon (Main Hand), 4 = Weapon (Off Hand)
            {
                if (IWeaponAppearanceCatalog.Definitions[itemType].IsSimple)
                {
                    AppearancePartEditService.ModifyItemPart(host, (int)ItemAppearanceType.SimpleModel, IWeaponAppearanceCatalog.Definitions[itemType].SimpleParts[host.SelectedPartIndex]);
                }
                else
                {
                    var weaponPart = host.SelectedPartCategoryIndex switch
                    {
                        0 => AppearanceWeapon.Top,
                        1 => AppearanceWeapon.Middle,
                        2 => AppearanceWeapon.Bottom,
                        _ => throw new ArgumentOutOfRangeException(nameof(AppearanceEditorViewModel.SelectedPartIndex))
                    };
                    var selected = AppearanceItemEligibility.GetWeaponPartOptions(item, IWeaponAppearanceCatalog.Definitions[itemType], weaponPart)[host.SelectedPartIndex];
                    AppearancePartEditService.ModifyItemPart(host, (int)weaponPart, selected % 100, selected / 100);
                }
            }
        }

        public void LoadPart(IAppearanceEditorHost host)
        {
            if (host.SelectedPartIndex <= -1)
                return;

            if (host.IsAppearanceSelected)
            {
                LoadBodyPart(host);
            }
            else if (host.IsEquipmentSelected)
            {
                LoadArmorPart(host);
            }
        }

        public void PreviousPart(IAppearanceEditorHost host)
        {
            var newPartIndex = host.SelectedPartIndex - 1;
            if (newPartIndex < 0)
                newPartIndex = 0;

            host.PartSelected[host.SelectedPartIndex] = false;
            host.SelectedPartIndex = newPartIndex;
            host.PartSelected[host.SelectedPartIndex] = true;
            LoadPart(host);
        }

        public void NextPart(IAppearanceEditorHost host)
        {
            var newPartIndex = host.SelectedPartIndex + 1;
            if (newPartIndex > host.Session.PartIdToIndex.Count - 1)
                newPartIndex = host.Session.PartIdToIndex.Count - 1;

            host.PartSelected[host.SelectedPartIndex] = false;
            host.SelectedPartIndex = newPartIndex;
            host.PartSelected[host.SelectedPartIndex] = true;
            LoadPart(host);
        }
    }
}
