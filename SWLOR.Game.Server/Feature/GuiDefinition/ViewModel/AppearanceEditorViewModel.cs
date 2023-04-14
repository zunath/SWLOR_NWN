using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class AppearanceEditorViewModel :
        GuiViewModelBase<AppearanceEditorViewModel, AppearanceEditorPayload>,
        IGuiRefreshable<EquipItemRefreshEvent>
    {
        public const string PartialElement = "PARTIAL_VIEW";
        public const string EditorPartial = "APPEARANCE_EDITOR_PARTIAL";
        public const string SettingsPartial = "SETTINGS_PARTIAL";

        private const int ColorWidthCells = 16;
        private const int ColorHeightCells = 11;

        private static readonly Dictionary<AppearanceType, IArmorAppearanceDefinition> _armorAppearances = new();
        private static readonly Dictionary<AppearanceType, IRacialAppearanceDefinition> _racialAppearances = new();
        private static readonly Dictionary<BaseItem, IWeaponAppearanceDefinition> _weaponAppearances = new();
        private Dictionary<int, int> _partIdToIndex = new();

        private uint _target;

        [NWNEventHandler("mod_load")]
        public static void LoadAppearances()
        {
            LoadRacialAppearances();
            LoadArmorAppearances();
            LoadWeaponAppearances();
        }

        [NWNEventHandler("dm_poss_bef")]
        [NWNEventHandler("dm_possfull_bef")]
        public static void CloseAppearanceWindowOnPossessionBefore()
        {
            var dm = OBJECT_SELF;
            var isUnpossess = StringToObject(EventsPlugin.GetEventData("TARGET")) == OBJECT_INVALID;

            if (isUnpossess)
            {
                var uiTarget = GetMaster(dm);

                Gui.CloseWindow(dm, GuiWindowType.AppearanceEditor, uiTarget);
            }
            else
            {
                if (Gui.IsWindowOpen(dm, GuiWindowType.AppearanceEditor))
                    Gui.TogglePlayerWindow(dm, GuiWindowType.AppearanceEditor);
            }
        }

        private static void LoadRacialAppearances()
        {
            _racialAppearances[AppearanceType.Human] = new HumanRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Bothan] = new BothanRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Chiss] = new ChissRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Zabrak] = new ZabrakRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Twilek] = new TwilekRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Mirialan] = new MirialanRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Echani] = new EchaniRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.KelDor] = new KelDorRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Cyborg] = new CyborgRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Cathar] = new CatharRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Rodian] = new RodianRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Trandoshan] = new TrandoshanRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Togruta] = new TogrutaRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Wookiee] = new WookieeRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.MonCalamari] = new MonCalamariRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Ugnaught] = new UgnaughtRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Droid] = new DroidRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Nautolan] = new NautolanRacialAppearanceDefinition();
        }

        private static void LoadArmorAppearances()
        {
            _armorAppearances[AppearanceType.Human] = new GeneralArmorAppearanceDefinition();
            _armorAppearances[AppearanceType.Bothan] = new GeneralArmorAppearanceDefinition();
            _armorAppearances[AppearanceType.Chiss] = new GeneralArmorAppearanceDefinition();
            _armorAppearances[AppearanceType.Zabrak] = new GeneralArmorAppearanceDefinition();
            _armorAppearances[AppearanceType.Twilek] = new GeneralArmorAppearanceDefinition();
            _armorAppearances[AppearanceType.Mirialan] = new GeneralArmorAppearanceDefinition();
            _armorAppearances[AppearanceType.Echani] = new GeneralArmorAppearanceDefinition();
            _armorAppearances[AppearanceType.KelDor] = new GeneralArmorAppearanceDefinition();
            _armorAppearances[AppearanceType.Cyborg] = new GeneralArmorAppearanceDefinition();
            _armorAppearances[AppearanceType.Cathar] = new GeneralArmorAppearanceDefinition();
            _armorAppearances[AppearanceType.Rodian] = new GeneralArmorAppearanceDefinition();
            _armorAppearances[AppearanceType.Trandoshan] = new GeneralArmorAppearanceDefinition();
            _armorAppearances[AppearanceType.Togruta] = new GeneralArmorAppearanceDefinition();
            _armorAppearances[AppearanceType.Wookiee] = new GeneralArmorAppearanceDefinition();
            _armorAppearances[AppearanceType.MonCalamari] = new GeneralArmorAppearanceDefinition();
            _armorAppearances[AppearanceType.Ugnaught] = new GeneralArmorAppearanceDefinition();
            _armorAppearances[AppearanceType.Droid] = new DroidArmorAppearanceDefinition();
            _armorAppearances[AppearanceType.Nautolan] = new GeneralArmorAppearanceDefinition();
        }

        private static void LoadWeaponAppearances()
        {
            _weaponAppearances[BaseItem.Dagger] = new DaggerAppearanceDefinition();
            _weaponAppearances[BaseItem.Electroblade] = new ElectrobladeAppearanceDefinition();
            _weaponAppearances[BaseItem.GreatSword] = new GreatSwordAppearanceDefinition();
            _weaponAppearances[BaseItem.Katar] = new KatarAppearanceDefinition();
            _weaponAppearances[BaseItem.LargeShield] = new LargeShieldAppearanceDefinition();
            _weaponAppearances[BaseItem.Longsword] = new LongswordAppearanceDefinition();
            _weaponAppearances[BaseItem.Pistol] = new PistolAppearanceDefinition();
            _weaponAppearances[BaseItem.QuarterStaff] = new QuarterstaffAppearanceDefinition();
            _weaponAppearances[BaseItem.Rifle] = new RifleAppearanceDefinition();
            _weaponAppearances[BaseItem.Shuriken] = new ShurikenAppearanceDefinition();
            _weaponAppearances[BaseItem.ShortSpear] = new SpearAppearanceDefinition();
            _weaponAppearances[BaseItem.TwoBladedSword] = new TwinBladeAppearanceDefinition();
            _weaponAppearances[BaseItem.TwinElectroBlade] = new TwinElectrobladeAppearanceDefinition();
        }

        public bool IsAppearanceSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsEquipmentSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsSettingsSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsSettingsVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string ColorSheetResref
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool HasItemEquipped
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool DoesNotHaveItemEquipped
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsColorPickerVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool ShowHelmet
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool ShowCloak
        {
            get => Get<bool>();
            set => Set(value);
        }

        public GuiBindingList<string> ColorCategoryOptions
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> PartCategoryOptions
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> ColorCategorySelected
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<bool> PartCategorySelected
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public int SelectedColorCategoryIndex
        {
            get => Get<int>();
            set
            {
                Set(value);

                if (IsAppearanceSelected)
                {
                    if (value == 0) // 0 = Skin Color
                    {
                        ColorSheetResref = "gui_pal_skin";
                    }
                    else if (value == 1) // 1 = Hair Color
                    {
                        ColorSheetResref = "gui_pal_hair01";
                    }
                    else if (value == 2) // 2 = Tattoo Color 1
                    {
                        ColorSheetResref = "gui_pal_tattoo";
                    }
                    else if (value == 3) // 3 = Tattoo Color 2
                    {
                        ColorSheetResref = "gui_pal_tattoo";
                    }
                }
                else if (IsEquipmentSelected)
                {
                    if (value == 0) // 0 = Leather 1
                    {
                        ColorSheetResref = "gui_pal_tattoo";
                    }
                    else if (value == 1) // 1 = Leather 2
                    {
                        ColorSheetResref = "gui_pal_tattoo";
                    }
                    else if (value == 2) // 2 = Cloth 1
                    {
                        ColorSheetResref = "gui_pal_tattoo";
                    }
                    else if (value == 3) // 3 = Cloth 2
                    {
                        ColorSheetResref = "gui_pal_tattoo";
                    }
                    else if (value == 4) // 4 = Metal 1
                    {
                        ColorSheetResref = "gui_pal_armor01";
                    }
                    else if (value == 5) // 5 = Metal 2
                    {
                        ColorSheetResref = "gui_pal_armor01";
                    }
                }
            }
        }

        public int SelectedItemTypeIndex
        {
            get => Get<int>();
            set
            {
                Set(value);
                ToggleItemEquippedFlags();
                LoadColorCategoryOptions();
                LoadPartCategoryOptions();
                LoadItemParts();
                _lastModifiedItem = OBJECT_INVALID;
            }
        }

        public int SelectedPartCategoryIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        public GuiBindingList<string> PartOptions
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> PartSelected
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public int SelectedPartIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        private bool IsValidItem()
        {
            // Treated as a valid item if we're not in the item customization page.
            if (IsAppearanceSelected)
                return true;

            // The item must be valid, not cursed, not plot.
            var item = GetItem();
            if (!GetIsObjectValid(item))
                return false;

            if (GetItemCursedFlag(item) || GetPlotFlag(item))
                return false;

            // Weapons must be registered in the weapon appearances list in order to show up.
            // Also, if it has an appearance on the top, middle,or bottom model which isn't available in the menu, we treat it as invalid.
            if (SelectedItemTypeIndex == 3 || SelectedItemTypeIndex == 4) // 3 = Weapon (Main Hand), 4 = Weapon (Off Hand)
            {
                var itemType = GetBaseItemType(item);
                if (!_weaponAppearances.ContainsKey(itemType))
                    return false;

                var appearance = _weaponAppearances[itemType];

                if (appearance.IsSimple)
                {
                    var partId = GetItemAppearance(item, ItemAppearanceType.SimpleModel, -1);
                    if (!appearance.SimpleParts.Contains(partId))
                        return false;
                }
                else
                {
                    var topId = GetItemAppearance(item, ItemAppearanceType.WeaponModel, (int)AppearanceWeapon.Top);
                    var middleId = GetItemAppearance(item, ItemAppearanceType.WeaponModel, (int)AppearanceWeapon.Middle);
                    var bottomId = GetItemAppearance(item, ItemAppearanceType.WeaponModel, (int)AppearanceWeapon.Bottom);
                    var topColor = GetItemAppearance(item, ItemAppearanceType.WeaponColor, (int)AppearanceWeapon.Top);
                    var middleColor = GetItemAppearance(item, ItemAppearanceType.WeaponColor, (int)AppearanceWeapon.Middle);
                    var bottomColor = GetItemAppearance(item, ItemAppearanceType.WeaponColor, (int)AppearanceWeapon.Bottom);

                    var topPartId = topId + topColor * 100;
                    var middlePartId = middleId + middleColor * 100;
                    var bottomPartId = bottomId + bottomColor * 100;

                    if (!appearance.TopParts.Contains(topPartId) ||
                        !appearance.MiddleParts.Contains(middlePartId) ||
                        !appearance.BottomParts.Contains(bottomPartId))
                        return false;
                }
            }

            return true;
        }

        private void ToggleItemEquippedFlags()
        {
            var hasItemEquipped = IsValidItem();
            HasItemEquipped = hasItemEquipped;
            DoesNotHaveItemEquipped = !hasItemEquipped;
        }

        protected override void Initialize(AppearanceEditorPayload initialPayload)
        {
            _target = Player;
            if (GetIsObjectValid(initialPayload.Target))
            {
                _target = initialPayload.Target;
            }

            ChangePartialView(PartialElement, EditorPartial);
            IsAppearanceSelected = true;
            IsEquipmentSelected = false;
            IsSettingsSelected = false;
            IsColorPickerVisible = true;
            ToggleItemEquippedFlags();
            LoadColorCategoryOptions();
            LoadPartCategoryOptions();
            SelectedColorCategoryIndex = 0;
            SelectedPartCategoryIndex = 0;
            SelectedPartIndex = 0;
            SelectedItemTypeIndex = 0;
            ColorCategorySelected[0] = true;
            PartCategorySelected[0] = true;
            LoadBodyParts();
            LoadSettings();

            WatchOnClient(model => model.SelectedColorCategoryIndex);
            WatchOnClient(model => model.SelectedPartCategoryIndex);
            WatchOnClient(model => model.SelectedPartIndex);
            WatchOnClient(model => model.SelectedItemTypeIndex);

            if (GetIsPC(_target) && !GetIsDM(_target) && !GetIsDMPossessed(_target))
            {
                IsSettingsVisible = true;
                WatchOnClient(model => model.ShowHelmet);
                WatchOnClient(model => model.ShowCloak);
            }
        }

        private void LoadColorCategoryOptions()
        {
            if (DoesNotHaveItemEquipped)
                return;

            var colorCategoryOptions = new GuiBindingList<string>();

            if (IsAppearanceSelected)
            {
                colorCategoryOptions.Add("Skin Color");
                colorCategoryOptions.Add("Hair Color");
                colorCategoryOptions.Add("Tattoo 1 Color");
                colorCategoryOptions.Add("Tattoo 2 Color");

                IsColorPickerVisible = true;
            }
            else if (IsEquipmentSelected)
            {
                if (SelectedItemTypeIndex == 3 || SelectedItemTypeIndex == 4) // 3 & 4 = Weapon (Main or Off Hand)
                {
                    colorCategoryOptions.Add("Weapon");
                    IsColorPickerVisible = false;
                }
                else
                {
                    colorCategoryOptions.Add("Leather 1");
                    colorCategoryOptions.Add("Leather 2");
                    colorCategoryOptions.Add("Cloth 1");
                    colorCategoryOptions.Add("Cloth 2");
                    colorCategoryOptions.Add("Metal 1");
                    colorCategoryOptions.Add("Metal 2");

                    IsColorPickerVisible = true;
                }
            }

            var colorCategorySelected = new GuiBindingList<bool>();

            foreach (var unused in colorCategoryOptions)
            {
                colorCategorySelected.Add(false);
            }

            ColorCategoryOptions = colorCategoryOptions;
            ColorCategorySelected = colorCategorySelected;

            SelectedColorCategoryIndex = 0;
            ColorCategorySelected[SelectedColorCategoryIndex] = true;
        }

        private void LoadPartCategoryOptions()
        {
            if (DoesNotHaveItemEquipped)
                return;

            var partCategoryOptions = new GuiBindingList<string>();

            if (IsAppearanceSelected)
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
            else if (IsEquipmentSelected)
            {
                if (SelectedItemTypeIndex == 0) // 0 = Armor
                {
                    partCategoryOptions.Add("Neck");
                    partCategoryOptions.Add("Torso");
                    partCategoryOptions.Add("Belt");
                    partCategoryOptions.Add("Pelvis");

                    partCategoryOptions.Add("Right Shoulder");
                    partCategoryOptions.Add("Right Bicep");
                    partCategoryOptions.Add("Right Forearm");
                    partCategoryOptions.Add("Right Hand");
                    partCategoryOptions.Add("Right Thigh");
                    partCategoryOptions.Add("Right Shin");
                    partCategoryOptions.Add("Right Foot");

                    partCategoryOptions.Add("Left Shoulder");
                    partCategoryOptions.Add("Left Bicep");
                    partCategoryOptions.Add("Left Forearm");
                    partCategoryOptions.Add("Left Hand");
                    partCategoryOptions.Add("Left Thigh");
                    partCategoryOptions.Add("Left Shin");
                    partCategoryOptions.Add("Left Foot");

                    partCategoryOptions.Add("Robe");
                }
                else if (SelectedItemTypeIndex == 1) // 1 = Helmet
                {
                    partCategoryOptions.Add("Helmet");
                }
                else if (SelectedItemTypeIndex == 2) // 2 = Cloak
                {
                    partCategoryOptions.Add("Cloak");
                }
                else if (SelectedItemTypeIndex == 3 || SelectedItemTypeIndex == 4) // 3 = Weapon (Main Hand), 4 = Weapon (Off Hand)
                {
                    var item = GetItem();
                    var type = GetBaseItemType(item);
                    var partAppearance = _weaponAppearances[type];

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

            PartCategoryOptions = partCategoryOptions;
            PartCategorySelected = partCategorySelected;

            SelectedPartCategoryIndex = 0;
            PartCategorySelected[SelectedPartCategoryIndex] = true;
        }

        private (GuiBindingList<string>, GuiBindingList<bool>) GetPartLists(int[] partIds)
        {
            var partNames = new GuiBindingList<string>();
            var partSelected = new GuiBindingList<bool>();
            var partIdToIndex = new Dictionary<int, int>();
            var index = 0;

            foreach (var partId in partIds)
            {
                var partIndex = partId;

                partNames.Add($"Part #{partId}");
                partSelected.Add(false);
                partIdToIndex[partIndex] = index;
                index++;
            }

            _partIdToIndex = partIdToIndex;
            return (partNames, partSelected);
        }

        private uint GetItem()
        {
            if (SelectedItemTypeIndex == 0) // 0 = Armor
            {
                return GetItemInSlot(InventorySlot.Chest, _target);
            }
            else if (SelectedItemTypeIndex == 1) // 1 = Helmet
            {
                return GetItemInSlot(InventorySlot.Head, _target);
            }
            else if (SelectedItemTypeIndex == 2) // 2 = Cloak
            {
                return GetItemInSlot(InventorySlot.Cloak, _target);
            }
            else if (SelectedItemTypeIndex == 3) // 3 = Weapon (Main Hand)
            {
                return GetItemInSlot(InventorySlot.RightHand, _target);
            }
            else if (SelectedItemTypeIndex == 4) // 4 = Weapon (Off Hand)
            {
                return GetItemInSlot(InventorySlot.LeftHand, _target);
            }

            return OBJECT_INVALID;
        }

        private void LoadBodyParts()
        {
            var appearanceType = GetAppearanceType(_target);
            var gender = GetGender(_target);

            if (!_racialAppearances.ContainsKey(appearanceType))
            {
                Gui.TogglePlayerWindow(_target, GuiWindowType.AppearanceEditor);
                return;
            }

            var appearance = _racialAppearances[appearanceType];
            int[] partIds;
            int selectedPartId;

            switch (SelectedPartCategoryIndex)
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

                    selectedPartId = GetCreatureBodyPart(CreaturePart.Head, _target);
                    break;
                case 1: // Torso
                    partIds = appearance.Torsos;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.Torso, _target);
                    break;
                case 2: // Pelvis
                    partIds = appearance.Pelvis;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.Pelvis, _target);
                    break;
                case 3: // Right Bicep
                    partIds = appearance.RightBicep;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightBicep, _target);
                    break;
                case 4: // Right Forearm
                    partIds = appearance.RightForearm;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightForearm, _target);
                    break;
                case 5: // Right Hand
                    partIds = appearance.RightHand;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightHand, _target);
                    break;
                case 6: // Right Thigh
                    partIds = appearance.RightThigh;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightThigh, _target);
                    break;
                case 7: // Right Shin
                    partIds = appearance.RightShin;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightShin, _target);
                    break;
                case 8: // Right Foot
                    partIds = appearance.RightFoot;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightFoot, _target);
                    break;
                case 9: // Left Bicep
                    partIds = appearance.LeftBicep;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftBicep, _target);
                    break;
                case 10: // Left Forearm
                    partIds = appearance.LeftForearm;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftForearm, _target);
                    break;
                case 11: // Left Hand
                    partIds = appearance.LeftHand;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftHand, _target);
                    break;
                case 12: // Left Thigh
                    partIds = appearance.LeftThigh;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftThigh, _target);
                    break;
                case 13: // Left Shin
                    partIds = appearance.LeftShin;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftShin, _target);
                    break;
                case 14: // Left Foot
                    partIds = appearance.LeftFoot;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftFoot, _target);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(SelectedPartIndex));
            }

            var (partNames, partSelected) = GetPartLists(partIds);

            PartOptions = partNames;
            PartSelected = partSelected;
            SelectedPartIndex = _partIdToIndex[selectedPartId];
            PartSelected[SelectedPartIndex] = true;
        }

        private void LoadSettings()
        {
            if (GetIsDM(_target) || GetIsDMPossessed(_target))
                return;

            var playerId = GetObjectUUID(_target);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            ShowHelmet = dbPlayer.Settings.ShowHelmet;
            ShowCloak = dbPlayer.Settings.ShowCloak;
        }

        private void LoadItemParts()
        {
            if (DoesNotHaveItemEquipped)
                return;

            int[] partIds;
            int selectedPartId;
            var item = GetItem();
            var type = GetBaseItemType(item);
            var appearanceType = GetAppearanceType(_target);

            if (SelectedItemTypeIndex == 0) // 0 = Armor
            {
                switch (SelectedPartCategoryIndex)
                {
                    case 0: // Neck
                        partIds = _armorAppearances[appearanceType].Neck;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck);
                        break;
                    case 1: // Torso
                        partIds = _armorAppearances[appearanceType].Torso;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso);
                        break;
                    case 2: // Belt
                        partIds = _armorAppearances[appearanceType].Belt;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Belt);
                        break;
                    case 3: // Pelvis
                        partIds = _armorAppearances[appearanceType].Pelvis;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis);
                        break;
                    case 4: // Right Shoulder
                        partIds = _armorAppearances[appearanceType].Shoulder;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShoulder);
                        break;
                    case 5: // Right Bicep
                        partIds = _armorAppearances[appearanceType].Bicep;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightBicep);
                        break;
                    case 6: // Right Forearm
                        partIds = _armorAppearances[appearanceType].Forearm;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightForearm);
                        break;
                    case 7: // Right Hand
                        partIds = _armorAppearances[appearanceType].Hand;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand);
                        break;
                    case 8: // Right Thigh
                        partIds = _armorAppearances[appearanceType].Thigh;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightThigh);
                        break;
                    case 9: // Right Shin
                        partIds = _armorAppearances[appearanceType].Shin;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShin);
                        break;
                    case 10: // Right Foot
                        partIds = _armorAppearances[appearanceType].Foot;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightFoot);
                        break;
                    case 11: // Left Shoulder
                        partIds = _armorAppearances[appearanceType].Shoulder;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShoulder);
                        break;
                    case 12: // Left Bicep
                        partIds = _armorAppearances[appearanceType].Bicep;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftBicep);
                        break;
                    case 13: // Left Forearm
                        partIds = _armorAppearances[appearanceType].Forearm;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftForearm);
                        break;
                    case 14: // Left Hand
                        partIds = _armorAppearances[appearanceType].Hand;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand);
                        break;
                    case 15: // Left Thigh
                        partIds = _armorAppearances[appearanceType].Thigh;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftThigh);
                        break;
                    case 16: // Left Shin
                        partIds = _armorAppearances[appearanceType].Shin;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShin);
                        break;
                    case 17: // Left Foot
                        partIds = _armorAppearances[appearanceType].Foot;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftFoot);
                        break;
                    case 18: // Robe
                        partIds = _armorAppearances[appearanceType].Robe;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(SelectedPartIndex));
                }
            }
            else if (SelectedItemTypeIndex == 1) // 1 = Helmet
            {
                partIds = _armorAppearances[appearanceType].Helmet;
                selectedPartId = GetItemAppearance(item, ItemAppearanceType.SimpleModel, -1);
            }
            else if (SelectedItemTypeIndex == 2) // 2 = Cloak
            {
                partIds = _armorAppearances[appearanceType].Cloak;
                selectedPartId = GetItemAppearance(item, ItemAppearanceType.SimpleModel, -1);
            }
            else if (SelectedItemTypeIndex == 3 || SelectedItemTypeIndex == 4) // 3 = Weapon (Main Hand), 4 = Weapon (Off Hand)
            {
                int offset;

                if (_weaponAppearances[type].IsSimple)
                {
                    partIds = _weaponAppearances[type].SimpleParts;
                    selectedPartId = GetItemAppearance(item, ItemAppearanceType.SimpleModel, -1);
                }
                else
                {
                    switch (SelectedPartCategoryIndex)
                    {
                        case 0: // 0 = Top
                            partIds = _weaponAppearances[type].TopParts;
                            selectedPartId = GetItemAppearance(item, ItemAppearanceType.WeaponModel, (int)AppearanceWeapon.Top);
                            offset = GetItemAppearance(item, ItemAppearanceType.WeaponColor, (int)AppearanceWeapon.Top);
                            break;
                        case 1: // 1 = Middle
                            partIds = _weaponAppearances[type].MiddleParts;
                            selectedPartId = GetItemAppearance(item, ItemAppearanceType.WeaponModel, (int)AppearanceWeapon.Middle);
                            offset = GetItemAppearance(item, ItemAppearanceType.WeaponColor, (int)AppearanceWeapon.Middle);
                            break;
                        case 2: // 2 = Bottom
                            partIds = _weaponAppearances[type].BottomParts;
                            selectedPartId = GetItemAppearance(item, ItemAppearanceType.WeaponModel, (int)AppearanceWeapon.Bottom);
                            offset = GetItemAppearance(item, ItemAppearanceType.WeaponColor, (int)AppearanceWeapon.Bottom);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(SelectedPartIndex));
                    }

                    selectedPartId = offset * 100 + selectedPartId;
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(SelectedItemTypeIndex));
            }

            if (selectedPartId <= -1)
                return;

            var (partNames, partSelected) = GetPartLists(partIds);

            PartOptions = partNames;
            PartSelected = partSelected;
            SelectedPartIndex = _partIdToIndex[selectedPartId];
            PartSelected[SelectedPartIndex] = true;
        }

        public Action OnSelectAppearance() => () =>
        {
            ChangePartialView(PartialElement, EditorPartial);
            IsAppearanceSelected = true;
            IsEquipmentSelected = false;
            IsSettingsSelected = false;
            ToggleItemEquippedFlags();

            LoadColorCategoryOptions();
            LoadPartCategoryOptions();
            SelectedColorCategoryIndex = 0;
            _lastModifiedItem = OBJECT_INVALID;
            LoadBodyParts();
        };

        public Action OnSelectEquipment() => () =>
        {
            ChangePartialView(PartialElement, EditorPartial);
            IsAppearanceSelected = false;
            IsEquipmentSelected = true;
            IsSettingsSelected = false;
            ToggleItemEquippedFlags();

            LoadColorCategoryOptions();
            LoadPartCategoryOptions();
            LoadItemParts();
            SelectedColorCategoryIndex = 0;
            _lastModifiedItem = OBJECT_INVALID;
        };

        public Action OnSelectSettings() => () =>
        {
            ChangePartialView(PartialElement, SettingsPartial);
            IsAppearanceSelected = false;
            IsEquipmentSelected = false;
            IsSettingsSelected = true;

            var playerId = GetObjectUUID(_target);
            var dbPlayer = DB.Get<Player>(playerId);

            ShowHelmet = dbPlayer.Settings.ShowHelmet;
            ShowCloak = dbPlayer.Settings.ShowCloak;
            _lastModifiedItem = OBJECT_INVALID;
        };

        public Action OnDecreaseAppearanceScale() => () =>
        {
            var appearanceType = GetAppearanceType(_target);
            if (!_racialAppearances.ContainsKey(appearanceType))
            {
                Gui.TogglePlayerWindow(_target, GuiWindowType.AppearanceEditor);
                return;
            }

            var appearance = _racialAppearances[appearanceType];
            var scale = GetObjectVisualTransform(_target, ObjectVisualTransform.Scale);
            const float Increment = 0.01f;

            if (scale - Increment < appearance.MinimumScale)
            {
                SendMessageToPC(_target, "You cannot decrease your height any further.");
            }
            else
            {
                SetObjectVisualTransform(_target, ObjectVisualTransform.Scale, scale - Increment);
                SendMessageToPC(_target, $"Height: {GetObjectVisualTransform(_target, ObjectVisualTransform.Scale)}");
            }
        };
        public Action OnIncreaseAppearanceScale() => () =>
        {
            var appearanceType = GetAppearanceType(_target);
            if (!_racialAppearances.ContainsKey(appearanceType))
            {
                Gui.TogglePlayerWindow(_target, GuiWindowType.AppearanceEditor);
                return;
            }

            var appearance = _racialAppearances[appearanceType];

            var scale = GetObjectVisualTransform(_target, ObjectVisualTransform.Scale);
            const float Increment = 0.01f;

            if (scale + Increment > appearance.MaximumScale)
            {
                SendMessageToPC(_target, "You cannot increase your height any further.");
            }
            else
            {
                SetObjectVisualTransform(_target, ObjectVisualTransform.Scale, scale + Increment);
                SendMessageToPC(_target, $"Height: {GetObjectVisualTransform(_target, ObjectVisualTransform.Scale)}");
            }
        };

        public Action OnSelectColorCategory() => () =>
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            var index = NuiGetEventArrayIndex();
            ColorCategorySelected[SelectedColorCategoryIndex] = false;

            SelectedColorCategoryIndex = index;
            ColorCategorySelected[index] = true;
        };

        public Action OnSelectPartCategory() => () =>
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            var index = NuiGetEventArrayIndex();
            PartCategorySelected[SelectedPartCategoryIndex] = false;

            SelectedPartCategoryIndex = index;
            PartCategorySelected[index] = true;

            if (IsAppearanceSelected)
            {
                LoadBodyParts();
            }
            else if (IsEquipmentSelected)
            {
                LoadItemParts();
            }
        };

        // Tracking the last modified item is done to avoid an issue where disruption in the client's network
        // will result in the wrong equipped item being destroyed.
        private uint _lastModifiedItem = OBJECT_INVALID;

        private InventorySlot GetInventorySlot()
        {
            var slot = InventorySlot.Invalid;

            switch (SelectedItemTypeIndex)
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
                    slot = InventorySlot.LeftHand;
                    break;
            }

            return slot;
        }

        private ItemAppearanceType GetModelType()
        {
            var modelType = ItemAppearanceType.ArmorModel;

            switch (SelectedItemTypeIndex)
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

        private void ModifyItemColor(AppearanceArmorColor colorChannel, int colorId)
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            var slot = GetInventorySlot();
            var item = GetItem();
            var copy = CopyItemAndModify(item, ItemAppearanceType.ArmorColor, (int)colorChannel, colorId, true);

            if (item != _lastModifiedItem && _lastModifiedItem != OBJECT_INVALID)
            {
                DestroyObject(_lastModifiedItem);
            }
            else
            {
                DestroyObject(item);
            }

            AssignCommand(_target, () =>
            {
                ClearAllActions();
                ActionEquipItem(copy, slot);
            });

            _lastModifiedItem = copy;
        }

        private void ModifyItemPart(int type, int partId, int colorId = -1)
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            var slot = GetInventorySlot();
            var item = GetItem();
            var itemType = GetBaseItemType(item);
            var modelType = GetModelType();
            var copy = item;

            if (colorId > -1)
            {
                var oldCopy = copy;
                copy = CopyItemAndModify(copy, ItemAppearanceType.WeaponColor, type, colorId, true);
                partId %= 100;

                // Note: DestroyObject gets run at the end of the process so it's fine to queue up a call to destroy this temporary copy here.
                DestroyObject(oldCopy);
                DestroyObject(copy);
            }

            if (_weaponAppearances.ContainsKey(itemType) && _weaponAppearances[itemType].IsSimple)
            {
                copy = CopyItemAndModify(copy, ItemAppearanceType.SimpleModel, type, partId, true);
            }
            else
            {
                copy = CopyItemAndModify(copy, modelType, type, partId, true);
            }

            DestroyObject(item);

            if (item != _lastModifiedItem && _lastModifiedItem != OBJECT_INVALID)
            {
                DestroyObject(_lastModifiedItem);
            }
            else
            {
                DestroyObject(item);
            }

            AssignCommand(_target, () =>
            {
                ClearAllActions();
                ActionEquipItem(copy, slot);
            });

            _lastModifiedItem = copy;
        }

        public Action OnSelectColor() => () =>
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            var scale = GetPlayerDeviceProperty(_target, PlayerDevicePropertyType.GuiScale) / 100.0f;
            var payload = NuiGetEventPayload();
            var mousePosition = JsonObjectGet(payload, "mouse_pos");
            var jsonX = JsonObjectGet(mousePosition, "x");
            var jsonY = JsonObjectGet(mousePosition, "y");
            var x = (float)Convert.ToDouble(JsonDump(jsonX)) / scale;
            var y = (float)Convert.ToDouble(JsonDump(jsonY)) / scale;
            var tileWidth = 16f * scale;
            var tileHeight = 16f * scale;
            var cellX = (int)(x * scale / tileWidth);
            var cellY = (int)(y * scale / tileHeight);

            if (cellX < 0)
                cellX = 0;
            else if (cellX > ColorWidthCells)
                cellX = ColorWidthCells;

            if (cellY < 0)
                cellY = 0;
            else if (cellY > ColorHeightCells)
                cellY = ColorHeightCells;

            var colorId = cellX + cellY * ColorWidthCells;

            // Appearance - Skin, Hair, or Tattoo
            if (IsAppearanceSelected)
            {
                switch (SelectedColorCategoryIndex)
                {
                    case 0: // 0 = Skin
                        SetColor(_target, ColorChannel.Skin, colorId);
                        break;
                    case 1: //  1 = Hair
                        SetColor(_target, ColorChannel.Hair, colorId);
                        break;
                    case 2: // 2 = Tattoo 1
                        SetColor(_target, ColorChannel.Tattoo1, colorId);
                        break;
                    case 3: // 3 = Tattoo 2
                        SetColor(_target, ColorChannel.Tattoo2, colorId);
                        break;
                }
            }
            // Armor - Cloth 1, Cloth 2, Leather 1, Leather 2, Metal 1, Metal 2
            else if (IsEquipmentSelected)
            {
                switch (SelectedColorCategoryIndex)
                {
                    case 0: // 0 = Leather 1
                        ModifyItemColor(AppearanceArmorColor.Leather1, colorId);
                        break;
                    case 1: // 1 = Leather 2
                        ModifyItemColor(AppearanceArmorColor.Leather2, colorId);
                        break;
                    case 2: // 2 = Cloth 1
                        ModifyItemColor(AppearanceArmorColor.Cloth1, colorId);
                        break;
                    case 3: // 3 = Cloth 2
                        ModifyItemColor(AppearanceArmorColor.Cloth2, colorId);
                        break;
                    case 4: // 4 = Metal 1
                        ModifyItemColor(AppearanceArmorColor.Metal1, colorId);
                        break;
                    case 5: // 5 = Metal 2
                        ModifyItemColor(AppearanceArmorColor.Metal2, colorId);
                        break;
                }
            }
        };

        private void LoadBodyPart()
        {
            var appearanceType = GetAppearanceType(_target);
            var gender = GetGender(_target);
            var appearance = _racialAppearances[appearanceType];

            switch (SelectedPartCategoryIndex)
            {
                case 0: // Head
                    switch (gender)
                    {
                        case Gender.Male:
                            SetCreatureBodyPart(CreaturePart.Head, appearance.MaleHeads[SelectedPartIndex], _target);
                            break;
                        default:
                            SetCreatureBodyPart(CreaturePart.Head, appearance.FemaleHeads[SelectedPartIndex], _target);
                            break;
                    }
                    break;
                case 1: // Torso
                    SetCreatureBodyPart(CreaturePart.Torso, appearance.Torsos[SelectedPartIndex], _target);
                    break;
                case 2: // Pelvis
                    SetCreatureBodyPart(CreaturePart.Pelvis, appearance.Pelvis[SelectedPartIndex], _target);
                    break;
                case 3: // Right Bicep
                    SetCreatureBodyPart(CreaturePart.RightBicep, appearance.RightBicep[SelectedPartIndex], _target);
                    break;
                case 4: // Right Forearm
                    SetCreatureBodyPart(CreaturePart.RightForearm, appearance.RightForearm[SelectedPartIndex], _target);
                    break;
                case 5: // Right Hand
                    SetCreatureBodyPart(CreaturePart.RightHand, appearance.RightHand[SelectedPartIndex], _target);
                    break;
                case 6: // Right Thigh
                    SetCreatureBodyPart(CreaturePart.RightThigh, appearance.RightThigh[SelectedPartIndex], _target);
                    break;
                case 7: // Right Shin
                    SetCreatureBodyPart(CreaturePart.RightShin, appearance.RightShin[SelectedPartIndex], _target);
                    break;
                case 8: // Right Foot
                    SetCreatureBodyPart(CreaturePart.RightFoot, appearance.RightFoot[SelectedPartIndex], _target);
                    break;
                case 9: // Left Bicep
                    SetCreatureBodyPart(CreaturePart.LeftBicep, appearance.LeftBicep[SelectedPartIndex], _target);
                    break;
                case 10: // Left Forearm
                    SetCreatureBodyPart(CreaturePart.LeftForearm, appearance.LeftForearm[SelectedPartIndex], _target);
                    break;
                case 11: // Left Hand
                    SetCreatureBodyPart(CreaturePart.LeftHand, appearance.LeftHand[SelectedPartIndex], _target);
                    break;
                case 12: // Left Thigh
                    SetCreatureBodyPart(CreaturePart.LeftThigh, appearance.LeftThigh[SelectedPartIndex], _target);
                    break;
                case 13: // Left Shin
                    SetCreatureBodyPart(CreaturePart.LeftShin, appearance.LeftShin[SelectedPartIndex], _target);
                    break;
                case 14: // Left Foot
                    SetCreatureBodyPart(CreaturePart.LeftFoot, appearance.LeftFoot[SelectedPartIndex], _target);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(SelectedPartIndex));
            }

            ExecuteScript("appearance_edit", _target);
        }

        private void LoadArmorPart()
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;
            var item = GetItem();
            var itemType = GetBaseItemType(item);
            var appearanceType = GetAppearanceType(_target);

            if (SelectedItemTypeIndex == 0) // 0 = Armor
            {
                switch (SelectedPartCategoryIndex)
                {
                    case 0: // Neck
                        ModifyItemPart((int)AppearanceArmor.Neck, _armorAppearances[appearanceType].Neck[SelectedPartIndex]);
                        break;
                    case 1: // Torso
                        ModifyItemPart((int)AppearanceArmor.Torso, _armorAppearances[appearanceType].Torso[SelectedPartIndex]);
                        break;
                    case 2: // Belt
                        ModifyItemPart((int)AppearanceArmor.Belt, _armorAppearances[appearanceType].Belt[SelectedPartIndex]);
                        break;
                    case 3: // Pelvis
                        ModifyItemPart((int)AppearanceArmor.Pelvis, _armorAppearances[appearanceType].Pelvis[SelectedPartIndex]);
                        break;
                    case 4: // Right Shoulder
                        ModifyItemPart((int)AppearanceArmor.RightShoulder, _armorAppearances[appearanceType].Shoulder[SelectedPartIndex]);
                        break;
                    case 5: // Right Bicep
                        ModifyItemPart((int)AppearanceArmor.RightBicep, _armorAppearances[appearanceType].Bicep[SelectedPartIndex]);
                        break;
                    case 6: // Right Forearm
                        ModifyItemPart((int)AppearanceArmor.RightForearm, _armorAppearances[appearanceType].Forearm[SelectedPartIndex]);
                        break;
                    case 7: // Right Hand
                        ModifyItemPart((int)AppearanceArmor.RightHand, _armorAppearances[appearanceType].Hand[SelectedPartIndex]);
                        break;
                    case 8: // Right Thigh
                        ModifyItemPart((int)AppearanceArmor.RightThigh, _armorAppearances[appearanceType].Thigh[SelectedPartIndex]);
                        break;
                    case 9: // Right Shin
                        ModifyItemPart((int)AppearanceArmor.RightShin, _armorAppearances[appearanceType].Shin[SelectedPartIndex]);
                        break;
                    case 10: // Right Foot
                        ModifyItemPart((int)AppearanceArmor.RightFoot, _armorAppearances[appearanceType].Foot[SelectedPartIndex]);
                        break;
                    case 11: // Left Shoulder
                        ModifyItemPart((int)AppearanceArmor.LeftShoulder, _armorAppearances[appearanceType].Shoulder[SelectedPartIndex]);
                        break;
                    case 12: // Left Bicep
                        ModifyItemPart((int)AppearanceArmor.LeftBicep, _armorAppearances[appearanceType].Bicep[SelectedPartIndex]);
                        break;
                    case 13: // Left Forearm
                        ModifyItemPart((int)AppearanceArmor.LeftForearm, _armorAppearances[appearanceType].Forearm[SelectedPartIndex]);
                        break;
                    case 14: // Left Hand
                        ModifyItemPart((int)AppearanceArmor.LeftHand, _armorAppearances[appearanceType].Hand[SelectedPartIndex]);
                        break;
                    case 15: // Left Thigh
                        ModifyItemPart((int)AppearanceArmor.LeftThigh, _armorAppearances[appearanceType].Thigh[SelectedPartIndex]);
                        break;
                    case 16: // Left Shin
                        ModifyItemPart((int)AppearanceArmor.LeftShin, _armorAppearances[appearanceType].Shin[SelectedPartIndex]);
                        break;
                    case 17: // Left Foot
                        ModifyItemPart((int)AppearanceArmor.LeftFoot, _armorAppearances[appearanceType].Foot[SelectedPartIndex]);
                        break;
                    case 18: // Robe
                        ModifyItemPart((int)AppearanceArmor.Robe, _armorAppearances[appearanceType].Robe[SelectedPartIndex]);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(SelectedPartIndex));
                }
            }
            else if (SelectedItemTypeIndex == 1) // 1 = Helmet
            {
                ModifyItemPart((int)AppearanceArmor.Invalid, _armorAppearances[appearanceType].Helmet[SelectedPartIndex]);
            }
            else if (SelectedItemTypeIndex == 2) // 2 = Cloak
            {
                ModifyItemPart((int)AppearanceArmor.Invalid, _armorAppearances[appearanceType].Cloak[SelectedPartIndex]);
            }
            else if (SelectedItemTypeIndex == 3 || SelectedItemTypeIndex == 4) // 3 = Weapon (Main Hand), 4 = Weapon (Off Hand)
            {
                int color;
                int partId;

                if (_weaponAppearances[itemType].IsSimple)
                {
                    partId = _weaponAppearances[itemType].SimpleParts[SelectedPartIndex];
                    ModifyItemPart((int)ItemAppearanceType.SimpleModel, partId);
                }
                else
                {
                    switch (SelectedPartCategoryIndex)
                    {
                        case 0: // Top
                            color = _weaponAppearances[itemType].TopParts[SelectedPartIndex] / 100;
                            partId = _weaponAppearances[itemType].TopParts[SelectedPartIndex] % 100;
                            ModifyItemPart((int)AppearanceWeapon.Top, partId, color);
                            break;
                        case 1: // Middle
                            color = _weaponAppearances[itemType].MiddleParts[SelectedPartIndex] / 100;
                            partId = _weaponAppearances[itemType].MiddleParts[SelectedPartIndex] % 100;
                            ModifyItemPart((int)AppearanceWeapon.Middle, partId, color);
                            break;
                        case 2: // Bottom
                            color = _weaponAppearances[itemType].BottomParts[SelectedPartIndex] / 100;
                            partId = _weaponAppearances[itemType].BottomParts[SelectedPartIndex] % 100;
                            ModifyItemPart((int)AppearanceWeapon.Bottom, partId, color);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(SelectedPartIndex));
                    }
                }
            }
        }

        private void LoadPart()
        {
            if (SelectedPartIndex <= -1)
                return;

            if (IsAppearanceSelected)
            {
                LoadBodyPart();
            }
            else if (IsEquipmentSelected)
            {
                LoadArmorPart();
            }
        }

        public Action OnSelectPart() => () =>
        {
            var index = NuiGetEventArrayIndex();

            PartSelected[SelectedPartIndex] = false;
            SelectedPartIndex = index;
            PartSelected[index] = true;

            LoadPart();
        };

        public Action OnPreviousPart() => () =>
        {
            var newPartIndex = SelectedPartIndex - 1;
            if (newPartIndex < 0)
                newPartIndex = 0;

            PartSelected[SelectedPartIndex] = false;
            SelectedPartIndex = newPartIndex;
            PartSelected[SelectedPartIndex] = true;
            LoadPart();
        };

        public Action OnNextPart() => () =>
        {
            var newPartIndex = SelectedPartIndex + 1;
            if (newPartIndex > _partIdToIndex.Count - 1)
                newPartIndex = _partIdToIndex.Count - 1;

            PartSelected[SelectedPartIndex] = false;
            SelectedPartIndex = newPartIndex;
            PartSelected[SelectedPartIndex] = true;
            LoadPart();
        };

        public Action OnClickOutfits() => () =>
        {
            Gui.TogglePlayerWindow(_target, GuiWindowType.Outfits);
        };

        public Action OnCloseWindow() => () =>
        {
            if (GetIsDM(_target) || GetIsDMPossessed(_target) || !GetIsPC(_target))
                return;

            var playerId = GetObjectUUID(_target);
            var dbPlayer = DB.Get<Player>(playerId);

            SetObjectVisualTransform(_target, ObjectVisualTransform.Scale, dbPlayer.AppearanceScale);
        };

        public Action OnClickSaveSettings() => () =>
        {
            var playerId = GetObjectUUID(_target);
            var dbPlayer = DB.Get<Player>(playerId);

            dbPlayer.Settings.ShowCloak = ShowCloak;
            dbPlayer.Settings.ShowHelmet = ShowHelmet;

            var newHeight = GetObjectVisualTransform(_target, ObjectVisualTransform.Scale);
            dbPlayer.AppearanceScale = newHeight;

            DB.Set(dbPlayer);
            SendMessageToPC(_target, ColorToken.Green("Appearance settings saved successfully."));

            UpdateArmorDisplay();
        };

        private void UpdateArmorDisplay()
        {
            var helmet = GetItemInSlot(InventorySlot.Head, _target);
            if (GetIsObjectValid(helmet))
            {
                SetHiddenWhenEquipped(helmet, !ShowHelmet);
            }

            var cloak = GetItemInSlot(InventorySlot.Cloak, _target);
            if (GetIsObjectValid(cloak))
            {
                SetHiddenWhenEquipped(cloak, !ShowCloak);
            }
        }

        public void Refresh(EquipItemRefreshEvent payload)
        {
            _lastModifiedItem = OBJECT_INVALID;
        }
    }
}
