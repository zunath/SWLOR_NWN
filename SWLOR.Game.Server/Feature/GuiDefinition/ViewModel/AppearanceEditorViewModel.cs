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
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public partial class AppearanceEditorViewModel :
        GuiViewModelBase<AppearanceEditorViewModel, AppearanceEditorPayload>,
        IGuiRefreshable<EquipItemRefreshEvent>
    {
        public enum ColorTarget
        {
            Invalid = 0,
            Global = 1,
            LeftShoulder = 2,
            LeftBicep = 3,
            LeftForearm = 4,
            LeftHand = 5,
            LeftThigh = 6,
            LeftShin = 7,
            LeftFoot = 8,
            RightShoulder = 9,
            RightBicep = 10,
            RightForearm = 11,
            RightHand = 12,
            RightThigh = 13,
            RightShin = 14,
            RightFoot = 15,
            Neck = 16,
            Chest = 17,
            Belt = 18,
            Pelvis = 19,
            Robe = 20
        }

        public const string MainPartialElement = "MAIN_PARTIAL_VIEW";
        public const string EditorPartialElement = "EDITOR_PARTIAL_VIEW";
        public const string ArmorColorElement = "ARMOR_COLOR_VIEW";

        public const string EditorHeaderPartial = "APPEARANCE_EDITOR_HEADER_PARTIAL";
        public const string EditorMainPartial = "APPEARANCE_EDITOR_MAIN_PARTIAL";
        public const string EditorArmorPartial = "APPEARANCE_EDITOR_ARMOR_PARTIAL";
        public const string SettingsPartial = "SETTINGS_PARTIAL";
        public const string ArmorColorsClothLeather = "APPEARANCE_EDITOR_COLORS_CLOTH_LEATHER";
        public const string ArmorColorsMetal = "APPEARANCE_EDITOR_COLORS_METAL";

        public const int TextureColorsPerRow = 16;
        public const int ColorSize = 16; // 16x16 colors on the sprite sheet
        private const int ColorWidthCells = 16;
        private const int ColorHeightCells = 11;

        private static readonly Dictionary<AppearanceType, IArmorAppearanceDefinition> _armorAppearances = new();
        private static readonly Dictionary<AppearanceType, IRacialAppearanceDefinition> _racialAppearances = new();
        private static readonly Dictionary<BaseItem, IWeaponAppearanceDefinition> _weaponAppearances = new();
        private Dictionary<int, int> _partIdToIndex = new();

        private const string OutfitBarrelTag = "OUTFIT_BARREL";

        private uint _target;

        private AppearanceArmorColor _selectedColorChannel;
        private ColorTarget _colorTarget;

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

        private bool _skipAdjustArmorPart;

        public bool IsCopyEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string ColorTargetText
        {
            get => Get<string>();
            set => Set(value);
        }

        public int LeftShoulderSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShoulder))
                    AdjustArmorPart(AppearanceArmor.LeftShoulder, 0);
            }
        }

        public int LeftBicepSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftBicep))
                    AdjustArmorPart(AppearanceArmor.LeftBicep, 0);
            }
        }
        public int LeftForearmSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftForearm))
                    AdjustArmorPart(AppearanceArmor.LeftForearm, 0);
            }
        }
        public int LeftHandSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand))
                    AdjustArmorPart(AppearanceArmor.LeftHand, 0);
            }
        }
        public int LeftThighSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftThigh))
                    AdjustArmorPart(AppearanceArmor.LeftThigh, 0);
            }
        }
        public int LeftShinSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShin))
                    AdjustArmorPart(AppearanceArmor.LeftShin, 0);
            }
        }
        public int LeftFootSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftFoot))
                    AdjustArmorPart(AppearanceArmor.LeftFoot, 0);
            }
        }
        public int RightShoulderSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShoulder))
                    AdjustArmorPart(AppearanceArmor.RightShoulder, 0);
            }
        }
        public int RightBicepSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightBicep))
                    AdjustArmorPart(AppearanceArmor.RightBicep, 0);
            }
        }
        public int RightForearmSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightForearm))
                    AdjustArmorPart(AppearanceArmor.RightForearm, 0);
            }
        }
        public int RightHandSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand))
                    AdjustArmorPart(AppearanceArmor.RightHand, 0);
            }
        }
        public int RightThighSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightThigh))
                    AdjustArmorPart(AppearanceArmor.RightThigh, 0);
            }
        }
        public int RightShinSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShin))
                    AdjustArmorPart(AppearanceArmor.RightShin, 0);
            }
        }
        public int RightFootSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightFoot))
                    AdjustArmorPart(AppearanceArmor.RightFoot, 0);
            }
        }
        public int NeckSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck))
                    AdjustArmorPart(AppearanceArmor.Neck, 0);
            }
        }
        public int ChestSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso))
                    AdjustArmorPart(AppearanceArmor.Torso, 0);
            }
        }
        public int BeltSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Belt))
                    AdjustArmorPart(AppearanceArmor.Belt, 0);
            }
        }
        public int PelvisSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis))
                    AdjustArmorPart(AppearanceArmor.Pelvis, 0);
            }
        }
        public int RobeSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe))
                    AdjustArmorPart(AppearanceArmor.Robe, 0);
            }
        }

        public GuiBindingList<GuiComboEntry> LeftShoulderOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> LeftBicepOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> LeftForearmOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> LeftHandOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> LeftThighOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> LeftShinOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }
        public GuiBindingList<GuiComboEntry> LeftFootOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> RightShoulderOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> RightBicepOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> RightForearmOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> RightHandOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> RightThighOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> RightShinOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> RightFootOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> NeckOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> ChestOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> BeltOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> PelvisOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> RobeOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
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
                LoadItemTypeEditor();
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

            _colorTarget = ColorTarget.Invalid;
            RegisterColorMappings();
            ChangePartialView(MainPartialElement, EditorHeaderPartial);
            IsAppearanceSelected = true;
            IsEquipmentSelected = false;
            IsSettingsSelected = false;
            IsColorPickerVisible = true;
            IsCopyEnabled = true;
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
            else
            {
                IsSettingsVisible = false;
            }
        }

        private void StartArmorClientWatches()
        {
            WatchOnClient(model => model.LeftShoulderSelection);
            WatchOnClient(model => model.LeftBicepSelection);
            WatchOnClient(model => model.LeftForearmSelection);
            WatchOnClient(model => model.LeftHandSelection);
            WatchOnClient(model => model.LeftThighSelection);
            WatchOnClient(model => model.LeftShinSelection);
            WatchOnClient(model => model.LeftFootSelection);

            WatchOnClient(model => model.RightShoulderSelection);
            WatchOnClient(model => model.RightBicepSelection);
            WatchOnClient(model => model.RightForearmSelection);
            WatchOnClient(model => model.RightHandSelection);
            WatchOnClient(model => model.RightThighSelection);
            WatchOnClient(model => model.RightShinSelection);
            WatchOnClient(model => model.RightFootSelection);

            WatchOnClient(model => model.NeckSelection);
            WatchOnClient(model => model.ChestSelection);
            WatchOnClient(model => model.BeltSelection);
            WatchOnClient(model => model.PelvisSelection);
            WatchOnClient(model => model.RobeSelection);
        }

        private void LoadItemTypeEditor()
        {
            if (IsEquipmentSelected && SelectedItemTypeIndex == 0) // 0 = Armor
            {
                ChangePartialView(EditorPartialElement, EditorArmorPartial);
                ChangePartialView(ArmorColorElement, ArmorColorsClothLeather);
                IsCopyEnabled = true;
            }
            else // Helmet, Cloak, Weapon (Main), Weapon (Off)
            {
                ChangePartialView(EditorPartialElement, EditorMainPartial);
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
                    return;
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

            var item = GetItem();
            int[] partIds;
            int selectedPartId;
            var appearanceType = GetAppearanceType(_target);
            var type = GetBaseItemType(item);

            if (SelectedItemTypeIndex == 0) // 0 = Armor
            {
                NeckOptions = _armorAppearances[appearanceType].NeckOptions;
                ChestOptions = _armorAppearances[appearanceType].TorsoOptions;
                BeltOptions = _armorAppearances[appearanceType].BeltOptions;
                PelvisOptions = _armorAppearances[appearanceType].PelvisOptions;
                RobeOptions = _armorAppearances[appearanceType].RobeOptions;

                LeftShoulderOptions = _armorAppearances[appearanceType].ShoulderOptions;
                LeftBicepOptions = _armorAppearances[appearanceType].BicepOptions;
                LeftForearmOptions = _armorAppearances[appearanceType].ForearmOptions;
                LeftHandOptions = _armorAppearances[appearanceType].HandOptions;
                LeftThighOptions = _armorAppearances[appearanceType].ThighOptions;
                LeftShinOptions = _armorAppearances[appearanceType].ShinOptions;
                LeftFootOptions = _armorAppearances[appearanceType].FootOptions;

                RightShoulderOptions = _armorAppearances[appearanceType].ShoulderOptions;
                RightBicepOptions = _armorAppearances[appearanceType].BicepOptions;
                RightForearmOptions = _armorAppearances[appearanceType].ForearmOptions;
                RightHandOptions = _armorAppearances[appearanceType].HandOptions;
                RightThighOptions = _armorAppearances[appearanceType].ThighOptions;
                RightShinOptions = _armorAppearances[appearanceType].ShinOptions;
                RightFootOptions = _armorAppearances[appearanceType].FootOptions;

                NeckSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck);
                ChestSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso);
                PelvisSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis);
                RobeSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe);

                LeftShoulderSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShoulder);
                LeftBicepSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftBicep);
                LeftForearmSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftForearm);
                LeftHandSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand);
                LeftThighSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftThigh);
                LeftShinSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShin);
                LeftFootSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftFoot);

                RightShoulderSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShoulder);
                RightBicepSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightBicep);
                RightForearmSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightForearm);
                RightHandSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand);
                RightThighSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightThigh);
                RightShinSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShin);
                RightFootSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightFoot);

                UpdateAllColors();

                return;
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
            ChangePartialView(MainPartialElement, EditorHeaderPartial);
            IsAppearanceSelected = true;
            IsEquipmentSelected = false;
            IsSettingsSelected = false;
            ToggleItemEquippedFlags();
            LoadItemTypeEditor();

            LoadColorCategoryOptions();
            LoadPartCategoryOptions();
            SelectedColorCategoryIndex = 0;
            _lastModifiedItem = OBJECT_INVALID;
            LoadBodyParts();
        };

        public Action OnSelectEquipment() => () =>
        {
            ChangePartialView(MainPartialElement, EditorHeaderPartial);
            IsAppearanceSelected = false;
            IsEquipmentSelected = true;
            IsSettingsSelected = false;
            ToggleItemEquippedFlags();
            LoadItemTypeEditor();

            LoadColorCategoryOptions();
            LoadPartCategoryOptions();
            LoadItemParts();
            SelectedColorCategoryIndex = 0;
            _lastModifiedItem = OBJECT_INVALID;

            _colorTarget = ColorTarget.Invalid;
            ColorTargetText = string.Empty;
            
            // If we don't delay the watch, NUI will reset values of some parts back to default (first item in the list)
            // This is related to the dropdown menu options for each part type.
            DelayCommand(3f, StartArmorClientWatches);
        };

        public Action OnSelectSettings() => () =>
        {
            ChangePartialView(MainPartialElement, SettingsPartial);
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
            // Helmet/Cloak - Cloth 1, Cloth 2, Leather 1, Leather 2, Metal 1, Metal 2
            else if (IsEquipmentSelected && (SelectedItemTypeIndex == 1 || SelectedItemTypeIndex == 2))
            {
                switch (SelectedColorCategoryIndex)
                {
                    case 0: // 0 = Leather 1
                        ModifyHelmetCloakColor(AppearanceArmorColor.Leather1, colorId);
                        break;
                    case 1: // 1 = Leather 2
                        ModifyHelmetCloakColor(AppearanceArmorColor.Leather2, colorId);
                        break;
                    case 2: // 2 = Cloth 1
                        ModifyHelmetCloakColor(AppearanceArmorColor.Cloth1, colorId);
                        break;
                    case 3: // 3 = Cloth 2
                        ModifyHelmetCloakColor(AppearanceArmorColor.Cloth2, colorId);
                        break;
                    case 4: // 4 = Metal 1
                        ModifyHelmetCloakColor(AppearanceArmorColor.Metal1, colorId);
                        break;
                    case 5: // 5 = Metal 2
                        ModifyHelmetCloakColor(AppearanceArmorColor.Metal2, colorId);
                        break;
                }
            }
        };

        private void ModifyHelmetCloakColor(AppearanceArmorColor colorChannel, int colorId)
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

            if (SelectedItemTypeIndex == 1) // 1 = Helmet
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

        public Action OnClickColorPalette(int colorId) => () =>
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            if (!GetBaseItemFitsInInventory(BaseItem.Armor, _target))
            {
                SendMessageToPC(Player, "Not enough space to modify item.");
                return;
            }

            if (_colorTarget == ColorTarget.Invalid)
                return;

            if (_colorTarget == ColorTarget.Global)
            {
                var item = GetItem();
                DestroyObject(item);
                item = CopyItemAndModify(item, ItemAppearanceType.ArmorColor, (int)_selectedColorChannel, colorId, true);
                AssignCommand(_target, () => ActionEquipItem(item, InventorySlot.Chest));
            }
            else
            {
                var item = GetItem();
                DestroyObject(item);

                var armorModel = GetArmorModelType(_colorTarget);
                var index = CalculatePerPartColorIndex(armorModel, _selectedColorChannel);
                item = CopyItemAndModify(item, ItemAppearanceType.ArmorColor, index, colorId, true);
                AssignCommand(_target, () => ActionEquipItem(item, InventorySlot.Chest));
            }

            ChangeColor(_colorTarget, _selectedColorChannel, colorId);
        };

        public Action OnClickClearColor(ColorTarget colorTarget, AppearanceArmorColor colorChannel) => () =>
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            if (colorTarget == ColorTarget.Invalid)
                return;

            // Right clicks only.
            var payload = NuiGetEventPayload();
            var button = JsonGetInt(JsonObjectGet(payload, "mouse_btn"));
            if (button != 2)
                return;

            var item = GetItem();
            DestroyObject(item);

            var armorModel = GetArmorModelType(colorTarget);
            var index = CalculatePerPartColorIndex(armorModel, colorChannel);
            item = CopyItemAndModify(item, ItemAppearanceType.ArmorColor, index, 255, true);
            AssignCommand(_target, () => ActionEquipItem(item, InventorySlot.Chest));

            ChangeColor(colorTarget, colorChannel, 255);
        };

        private AppearanceArmor GetArmorModelType(ColorTarget colorTarget)
        {
            var armorModel = AppearanceArmor.Invalid;
            switch (colorTarget)
            {
                case ColorTarget.LeftShoulder:
                    armorModel = AppearanceArmor.LeftShoulder;
                    break;
                case ColorTarget.LeftBicep:
                    armorModel = AppearanceArmor.LeftBicep;
                    break;
                case ColorTarget.LeftForearm:
                    armorModel = AppearanceArmor.LeftForearm;
                    break;
                case ColorTarget.LeftHand:
                    armorModel = AppearanceArmor.LeftHand;
                    break;
                case ColorTarget.LeftThigh:
                    armorModel = AppearanceArmor.LeftThigh;
                    break;
                case ColorTarget.LeftShin:
                    armorModel = AppearanceArmor.LeftShin;
                    break;
                case ColorTarget.LeftFoot:
                    armorModel = AppearanceArmor.LeftFoot;
                    break;
                case ColorTarget.RightShoulder:
                    armorModel = AppearanceArmor.RightShoulder;
                    break;
                case ColorTarget.RightBicep:
                    armorModel = AppearanceArmor.RightBicep;
                    break;
                case ColorTarget.RightForearm:
                    armorModel = AppearanceArmor.RightForearm;
                    break;
                case ColorTarget.RightHand:
                    armorModel = AppearanceArmor.RightHand;
                    break;
                case ColorTarget.RightThigh:
                    armorModel = AppearanceArmor.RightThigh;
                    break;
                case ColorTarget.RightShin:
                    armorModel = AppearanceArmor.RightShin;
                    break;
                case ColorTarget.RightFoot:
                    armorModel = AppearanceArmor.RightFoot;
                    break;
                case ColorTarget.Neck:
                    armorModel = AppearanceArmor.Neck;
                    break;
                case ColorTarget.Chest:
                    armorModel = AppearanceArmor.Torso;
                    break;
                case ColorTarget.Belt:
                    armorModel = AppearanceArmor.Belt;
                    break;
                case ColorTarget.Pelvis:
                    armorModel = AppearanceArmor.Pelvis;
                    break;
                case ColorTarget.Robe:
                    armorModel = AppearanceArmor.Robe;
                    break;
            }

            return armorModel;
        }

        private int CalculatePerPartColorIndex(AppearanceArmor armorModel, AppearanceArmorColor colorChannel)
        {
            return (int)AppearanceArmorColor.NumColors + (int)armorModel * (int)AppearanceArmorColor.NumColors + (int)colorChannel;
        }

        public Action OnClickColorTarget(ColorTarget target, AppearanceArmorColor channel) => () =>
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            _colorTarget = target;
            _selectedColorChannel = channel;

            UpdateTargetedColor();

            if (channel == AppearanceArmorColor.Metal1 || channel == AppearanceArmorColor.Metal2)
            {
                ChangePartialView(ArmorColorElement, ArmorColorsMetal);
            }
            else
            {
                ChangePartialView(ArmorColorElement, ArmorColorsClothLeather);
            }
        };

        private GuiRectangle BuildColorRegion(ColorTarget target, AppearanceArmorColor colorChannel)
        {
            var item = GetItem();
            int colorId;

            if (target == ColorTarget.Global)
            {
                colorId = GetItemAppearance(item, ItemAppearanceType.ArmorColor, (int)colorChannel);
            }
            else
            {
                var armorModel = GetArmorModelType(target);
                var perPartColorIndex = CalculatePerPartColorIndex(armorModel, colorChannel);
                colorId = GetItemAppearance(item, ItemAppearanceType.ArmorColor, perPartColorIndex);
            }

            var (x, y) = ColorIdToCoordinates(colorId);

            return new GuiRectangle(x * ColorSize, y * ColorSize, ColorSize, ColorSize);
        }

        private void UpdateAllColors()
        {
            foreach (var (target, regions) in _colorMappings)
            {
                foreach (var (channel, detail) in regions)
                {
                    GetType().GetProperty(detail.PropertyName)?.SetValue(this, BuildColorRegion(target, channel));
                }
            }
        }

        private void UpdateTargetedColor()
        {
            string targetName;
            string channelName;

            switch (_colorTarget)
            {
                case ColorTarget.Global:
                    targetName = "Global";
                    break;
                case ColorTarget.LeftShoulder:
                    targetName = "Left Shoulder";
                    break;
                case ColorTarget.LeftBicep:
                    targetName = "Left Bicep";
                    break;
                case ColorTarget.LeftForearm:
                    targetName = "Left Forearm";
                    break;
                case ColorTarget.LeftHand:
                    targetName = "Left Hand";
                    break;
                case ColorTarget.LeftThigh:
                    targetName = "Left Thigh";
                    break;
                case ColorTarget.LeftShin:
                    targetName = "Left Shin";
                    break;
                case ColorTarget.LeftFoot:
                    targetName = "Left Foot";
                    break;
                case ColorTarget.RightShoulder:
                    targetName = "Right Shoulder";
                    break;
                case ColorTarget.RightBicep:
                    targetName = "Right Bicep";
                    break;
                case ColorTarget.RightForearm:
                    targetName = "Right Forearm";
                    break;
                case ColorTarget.RightHand:
                    targetName = "Right Hand";
                    break;
                case ColorTarget.RightThigh:
                    targetName = "Right Thigh";
                    break;
                case ColorTarget.RightShin:
                    targetName = "Right Shin";
                    break;
                case ColorTarget.RightFoot:
                    targetName = "Right Foot";
                    break;
                case ColorTarget.Neck:
                    targetName = "Neck";
                    break;
                case ColorTarget.Chest:
                    targetName = "Chest";
                    break;
                case ColorTarget.Belt:
                    targetName = "Belt";
                    break;
                case ColorTarget.Pelvis:
                    targetName = "Pelvis";
                    break;
                case ColorTarget.Robe:
                    targetName = "Robe";
                    break;
                default:
                    targetName = "Unknown";
                    break;
            }

            switch (_selectedColorChannel)
            {
                case AppearanceArmorColor.Leather1:
                    channelName = "Leather 1";
                    break;
                case AppearanceArmorColor.Leather2:
                    channelName = "Leather 2";
                    break;
                case AppearanceArmorColor.Cloth1:
                    channelName = "Cloth 1";
                    break;
                case AppearanceArmorColor.Cloth2:
                    channelName = "Cloth 2";
                    break;
                case AppearanceArmorColor.Metal1:
                    channelName = "Metal 1";
                    break;
                case AppearanceArmorColor.Metal2:
                    channelName = "Metal 2";
                    break;
                default:
                    channelName = "Unknown";
                    break;
            }

            ColorTargetText = $"Selected: {targetName} - {channelName}";
        }

        private int ArmorValueToIndex(GuiBindingList<GuiComboEntry> options, int value)
        {
            return options.IndexOf(options.Single(x => x.Value == value));
        }

        private void AdjustArmorPart(AppearanceArmor partType, int adjustBy)
        {
            _skipAdjustArmorPart = true;
            var appearanceType = GetAppearanceType(_target);

            int Adjust(GuiBindingList<GuiComboEntry> options, int selectionIndex)
            {
                var index = ArmorValueToIndex(options, selectionIndex) + adjustBy;
                if (index >= options.Count)
                    index = options.Count - 1;
                else if (index < 0)
                    index = 0;

                return options[index].Value;
            }

            switch (partType)
            {
                case AppearanceArmor.RightFoot:
                    RightFootSelection = Adjust(RightFootOptions, RightFootSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Foot[ArmorValueToIndex(RightFootOptions, RightFootSelection)]);
                    break;
                case AppearanceArmor.LeftFoot:
                    LeftFootSelection = Adjust(LeftFootOptions, LeftFootSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Foot[ArmorValueToIndex(LeftFootOptions, LeftFootSelection)]);
                    break;
                case AppearanceArmor.RightShin:
                    RightShinSelection = Adjust(RightShinOptions, RightShinSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Shin[ArmorValueToIndex(RightShinOptions, RightShinSelection)]);
                    break;
                case AppearanceArmor.LeftShin:
                    LeftShinSelection = Adjust(LeftShinOptions, LeftShinSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Shin[ArmorValueToIndex(LeftShinOptions, LeftShinSelection)]);
                    break;
                case AppearanceArmor.LeftThigh:
                    LeftThighSelection = Adjust(LeftThighOptions, LeftThighSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Thigh[ArmorValueToIndex(LeftThighOptions, LeftThighSelection)]);
                    break;
                case AppearanceArmor.RightThigh:
                    RightThighSelection = Adjust(RightThighOptions, RightThighSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Thigh[ArmorValueToIndex(RightThighOptions, RightThighSelection)]);
                    break;
                case AppearanceArmor.Pelvis:
                    PelvisSelection = Adjust(PelvisOptions, PelvisSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Pelvis[ArmorValueToIndex(PelvisOptions, PelvisSelection)]);
                    break;
                case AppearanceArmor.Torso:
                    ChestSelection = Adjust(ChestOptions, ChestSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Torso[ArmorValueToIndex(ChestOptions, ChestSelection)]);
                    break;
                case AppearanceArmor.Belt:
                    BeltSelection = Adjust(BeltOptions, BeltSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Belt[ArmorValueToIndex(BeltOptions, BeltSelection)]);
                    break;
                case AppearanceArmor.Neck:
                    NeckSelection = Adjust(NeckOptions, NeckSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Neck[ArmorValueToIndex(NeckOptions, NeckSelection)]);
                    break;
                case AppearanceArmor.RightForearm:
                    RightForearmSelection = Adjust(RightForearmOptions, RightForearmSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Forearm[ArmorValueToIndex(RightForearmOptions, RightForearmSelection)]);
                    break;
                case AppearanceArmor.LeftForearm:
                    LeftForearmSelection = Adjust(LeftForearmOptions, LeftForearmSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Forearm[ArmorValueToIndex(LeftForearmOptions, LeftForearmSelection)]);
                    break;
                case AppearanceArmor.RightBicep:
                    RightBicepSelection = Adjust(RightBicepOptions, RightBicepSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Bicep[ArmorValueToIndex(RightBicepOptions, RightBicepSelection)]);
                    break;
                case AppearanceArmor.LeftBicep:
                    LeftBicepSelection = Adjust(LeftBicepOptions, LeftBicepSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Bicep[ArmorValueToIndex(LeftBicepOptions, LeftBicepSelection)]);
                    break;
                case AppearanceArmor.RightShoulder:
                    RightShoulderSelection = Adjust(RightShoulderOptions, RightShoulderSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Shoulder[ArmorValueToIndex(RightShoulderOptions, RightShoulderSelection)]);
                    break;
                case AppearanceArmor.LeftShoulder:
                    LeftShoulderSelection = Adjust(LeftShoulderOptions, LeftShoulderSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Shoulder[ArmorValueToIndex(LeftShoulderOptions, LeftShoulderSelection)]);
                    break;
                case AppearanceArmor.RightHand:
                    RightHandSelection = Adjust(RightHandOptions, RightHandSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Hand[ArmorValueToIndex(RightHandOptions, RightHandSelection)]);
                    break;
                case AppearanceArmor.LeftHand:
                    LeftHandSelection = Adjust(LeftHandOptions, LeftHandSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Hand[ArmorValueToIndex(LeftHandOptions, LeftHandSelection)]);
                    break;
                case AppearanceArmor.Robe:
                    RobeSelection = Adjust(RobeOptions, RobeSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Robe[ArmorValueToIndex(RobeOptions, RobeSelection)]);
                    break;
            }

            _skipAdjustArmorPart = false;
        }

        public Action OnClickAdjustArmorPart(AppearanceArmor partType, int adjustBy) => () =>
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            AdjustArmorPart(partType, adjustBy);
        };

        private uint GetOutfitBarrel()
        {
            var barrel = GetObjectByTag(OutfitBarrelTag);
            return barrel;
        }
        
        private void CopyColors(ref uint item, ColorTarget copyToTarget, ColorTarget copyFromTarget)
        {
            var copyFrom = GetArmorModelType(copyFromTarget);
            var copyTo = GetArmorModelType(copyToTarget);

            // Cloth 1
            if (GetBaseItemFitsInInventory(BaseItem.Armor, _target))
            {
                var copyToIndex = CalculatePerPartColorIndex(copyTo, AppearanceArmorColor.Cloth1);
                var copyFromIndex = CalculatePerPartColorIndex(copyFrom, AppearanceArmorColor.Cloth1);
                var newColor = GetItemAppearance(item, ItemAppearanceType.ArmorColor, copyFromIndex);
                ChangeColor(copyToTarget, AppearanceArmorColor.Cloth1, newColor);
                item = CopyItemAndModify(item, ItemAppearanceType.ArmorColor, copyToIndex, newColor, true);
                DestroyObject(item);
            }
            // Cloth 2
            if (GetBaseItemFitsInInventory(BaseItem.Armor, _target))
            {
                var copyToIndex = CalculatePerPartColorIndex(copyTo, AppearanceArmorColor.Cloth2);
                var copyFromIndex = CalculatePerPartColorIndex(copyFrom, AppearanceArmorColor.Cloth2);
                var newColor = GetItemAppearance(item, ItemAppearanceType.ArmorColor, copyFromIndex);
                ChangeColor(copyToTarget, AppearanceArmorColor.Cloth2, newColor);
                item = CopyItemAndModify(item, ItemAppearanceType.ArmorColor, copyToIndex, newColor, true);
                DestroyObject(item);
            }
            // Leather 1
            if (GetBaseItemFitsInInventory(BaseItem.Armor, _target))
            {
                var copyToIndex = CalculatePerPartColorIndex(copyTo, AppearanceArmorColor.Leather1);
                var copyFromIndex = CalculatePerPartColorIndex(copyFrom, AppearanceArmorColor.Leather1);
                var newColor = GetItemAppearance(item, ItemAppearanceType.ArmorColor, copyFromIndex);
                ChangeColor(copyToTarget, AppearanceArmorColor.Leather1, newColor);
                item = CopyItemAndModify(item, ItemAppearanceType.ArmorColor, copyToIndex, newColor, true);
                DestroyObject(item);
            }
            // Leather 2
            if (GetBaseItemFitsInInventory(BaseItem.Armor, _target))
            {
                var copyToIndex = CalculatePerPartColorIndex(copyTo, AppearanceArmorColor.Leather2);
                var copyFromIndex = CalculatePerPartColorIndex(copyFrom, AppearanceArmorColor.Leather2);
                var newColor = GetItemAppearance(item, ItemAppearanceType.ArmorColor, copyFromIndex);
                ChangeColor(copyToTarget, AppearanceArmorColor.Leather2, newColor);
                item = CopyItemAndModify(item, ItemAppearanceType.ArmorColor, copyToIndex, newColor, true);
                DestroyObject(item);
            }
            // Metal 1
            if (GetBaseItemFitsInInventory(BaseItem.Armor, _target))
            {
                var copyToIndex = CalculatePerPartColorIndex(copyTo, AppearanceArmorColor.Metal1);
                var copyFromIndex = CalculatePerPartColorIndex(copyFrom, AppearanceArmorColor.Metal1);
                var newColor = GetItemAppearance(item, ItemAppearanceType.ArmorColor, copyFromIndex);
                ChangeColor(copyToTarget, AppearanceArmorColor.Metal1, newColor);
                item = CopyItemAndModify(item, ItemAppearanceType.ArmorColor, copyToIndex, newColor, true);
                DestroyObject(item);
            }
            // Metal 2
            if (GetBaseItemFitsInInventory(BaseItem.Armor, _target))
            {
                var copyToIndex = CalculatePerPartColorIndex(copyTo, AppearanceArmorColor.Metal2);
                var copyFromIndex = CalculatePerPartColorIndex(copyFrom, AppearanceArmorColor.Metal2);
                var newColor = GetItemAppearance(item, ItemAppearanceType.ArmorColor, copyFromIndex);
                ChangeColor(copyToTarget, AppearanceArmorColor.Metal2, newColor);
                item = CopyItemAndModify(item, ItemAppearanceType.ArmorColor, copyToIndex, newColor, true);
                DestroyObject(item);
            }
        }

        public Action OnClickCopyToRight() => () =>
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            var appearanceType = GetAppearanceType(_target);

            _skipAdjustArmorPart = true;
            IsCopyEnabled = false;

            var item = GetItem();

            // Copy the outfit to the temporary barrel to ensure there is space to apply all modifications.
            var outfitBarrel = GetOutfitBarrel();
            var copy = CopyItem(item, outfitBarrel, true);
            DestroyObject(item);
            item = copy;

            // Color modification
            CopyColors(ref item, ColorTarget.RightShoulder, ColorTarget.LeftShoulder);
            CopyColors(ref item, ColorTarget.RightBicep, ColorTarget.LeftBicep);
            CopyColors(ref item, ColorTarget.RightForearm, ColorTarget.LeftForearm);
            CopyColors(ref item, ColorTarget.RightHand, ColorTarget.LeftHand);
            CopyColors(ref item, ColorTarget.RightThigh, ColorTarget.LeftThigh);
            CopyColors(ref item, ColorTarget.RightShin, ColorTarget.LeftShin);
            CopyColors(ref item, ColorTarget.RightFoot, ColorTarget.LeftFoot);

            // Part modification
            RightShoulderSelection = LeftShoulderSelection;
            item = CopyItemAndModify(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShoulder, _armorAppearances[appearanceType].Shoulder[ArmorValueToIndex(RightShoulderOptions, RightShoulderSelection)], true);
            DestroyObject(item);

            RightBicepSelection = LeftBicepSelection;
            item = CopyItemAndModify(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightBicep, _armorAppearances[appearanceType].Bicep[ArmorValueToIndex(RightBicepOptions, RightBicepSelection)], true);
            DestroyObject(item);

            RightForearmSelection = LeftForearmSelection;
            item = CopyItemAndModify(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightForearm, _armorAppearances[appearanceType].Forearm[ArmorValueToIndex(RightForearmOptions, RightForearmSelection)], true);
            DestroyObject(item);

            RightHandSelection = LeftHandSelection;
            item = CopyItemAndModify(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand, _armorAppearances[appearanceType].Hand[ArmorValueToIndex(RightHandOptions, RightHandSelection)], true);
            DestroyObject(item);

            RightThighSelection = LeftThighSelection;
            item = CopyItemAndModify(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightThigh, _armorAppearances[appearanceType].Thigh[ArmorValueToIndex(RightThighOptions, RightThighSelection)], true);
            DestroyObject(item);

            RightShinSelection = LeftShinSelection;
            item = CopyItemAndModify(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShin, _armorAppearances[appearanceType].Shin[ArmorValueToIndex(RightShinOptions, RightShinSelection)], true);
            DestroyObject(item);

            RightFootSelection = LeftFootSelection;
            item = CopyItemAndModify(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightFoot, _armorAppearances[appearanceType].Foot[ArmorValueToIndex(RightFootOptions, RightFootSelection)], true);

            // Copy the item from the outfit barrel back to the player.
            var updatedItem = CopyItem(item, _target, true);
            DestroyObject(item);
            
            AssignCommand(_target, () => ActionEquipItem(updatedItem, InventorySlot.Chest));

            DelayCommand(1f, () =>
            {
                IsCopyEnabled = true;
            });

            _skipAdjustArmorPart = false;
        };


        public Action OnClickCopyToLeft() => () =>
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            var appearanceType = GetAppearanceType(_target);

            _skipAdjustArmorPart = true;
            IsCopyEnabled = false;

            var item = GetItem();

            // Copy the outfit to the temporary barrel to ensure there is space to apply all modifications.
            var outfitBarrel = GetOutfitBarrel();
            var copy = CopyItem(item, outfitBarrel, true);
            DestroyObject(item);
            item = copy;

            // Color modification
            CopyColors(ref item, ColorTarget.LeftShoulder, ColorTarget.RightShoulder);
            CopyColors(ref item, ColorTarget.LeftBicep, ColorTarget.RightBicep);
            CopyColors(ref item, ColorTarget.LeftForearm, ColorTarget.RightForearm);
            CopyColors(ref item, ColorTarget.LeftHand, ColorTarget.RightHand);
            CopyColors(ref item, ColorTarget.LeftThigh, ColorTarget.RightThigh);
            CopyColors(ref item, ColorTarget.LeftShin, ColorTarget.RightShin);
            CopyColors(ref item, ColorTarget.LeftFoot, ColorTarget.RightFoot);

            // Part modification
            LeftShoulderSelection = RightShoulderSelection;
            item = CopyItemAndModify(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShoulder, _armorAppearances[appearanceType].Shoulder[ArmorValueToIndex(LeftShoulderOptions, LeftShoulderSelection)], true);
            DestroyObject(item);

            LeftBicepSelection = RightBicepSelection;
            item = CopyItemAndModify(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftBicep, _armorAppearances[appearanceType].Bicep[ArmorValueToIndex(LeftBicepOptions, LeftBicepSelection)], true);
            DestroyObject(item);

            LeftForearmSelection = RightForearmSelection;
            item = CopyItemAndModify(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftForearm, _armorAppearances[appearanceType].Forearm[ArmorValueToIndex(LeftForearmOptions, LeftForearmSelection)], true);
            DestroyObject(item);

            LeftHandSelection = RightHandSelection;
            item = CopyItemAndModify(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand, _armorAppearances[appearanceType].Hand[ArmorValueToIndex(LeftHandOptions, LeftHandSelection)], true);
            DestroyObject(item);

            LeftThighSelection = RightThighSelection;
            item = CopyItemAndModify(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftThigh, _armorAppearances[appearanceType].Thigh[ArmorValueToIndex(LeftThighOptions, LeftThighSelection)], true);
            DestroyObject(item);

            LeftShinSelection = RightShinSelection;
            item = CopyItemAndModify(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShin, _armorAppearances[appearanceType].Shin[ArmorValueToIndex(LeftShinOptions, LeftShinSelection)], true);
            DestroyObject(item);

            LeftFootSelection = RightFootSelection;
            item = CopyItemAndModify(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftFoot, _armorAppearances[appearanceType].Foot[ArmorValueToIndex(LeftFootOptions, LeftFootSelection)], true);

            // Copy the item from the outfit barrel back to the player.
            var updatedItem = CopyItem(item, _target, true);
            DestroyObject(item);
            
            AssignCommand(_target, () => ActionEquipItem(updatedItem, InventorySlot.Chest));

            DelayCommand(1f, () =>
            {
                IsCopyEnabled = true;
            });

            _skipAdjustArmorPart = false;
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
