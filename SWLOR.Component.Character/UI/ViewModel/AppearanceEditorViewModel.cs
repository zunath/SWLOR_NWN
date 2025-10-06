using SWLOR.Component.Character.Definitions.AppearanceDefinition.ItemAppearance;
using SWLOR.Component.Character.Definitions.AppearanceDefinition.RacialAppearance;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWScript.Constants;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.UI.Events;
using SWLOR.Shared.Domain.UI.Payloads;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.Events.Events.Player;
using SWLOR.Shared.UI.Component;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Character.UI.ViewModel
{
    public partial class AppearanceEditorViewModel :
        GuiViewModelBase<AppearanceEditorViewModel, AppearanceEditorPayload>,
        IGuiRefreshable<EquipItemRefreshEvent>
    {
        private readonly IDatabaseService _db;
        private readonly IEventAggregator _eventAggregator;
        private readonly IEventsPluginService _eventsPlugin;

        public AppearanceEditorViewModel(IGuiService guiService, IDatabaseService db, IEventAggregator eventAggregator, IEventsPluginService eventsPlugin) : base(guiService)
        {
            _db = db;
            _eventAggregator = eventAggregator;
            _eventsPlugin = eventsPlugin;
        }
        
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
        private static readonly Dictionary<BaseItemType, IWeaponAppearanceDefinition> _weaponAppearances = new();
        private Dictionary<int, int> _partIdToIndex = new();

        private const string OutfitBarrelTag = "OUTFIT_BARREL";

        private uint _target;
        private bool _isMetalPalette;

        private ItemAppearanceArmorColorType _selectedColorChannel;
        private ColorTarget _colorTarget;
        public void LoadAppearances()
        {
            LoadRacialAppearances();
            LoadArmorAppearances();
            LoadWeaponAppearances();
        }

        public void CloseAppearanceWindowOnPossessionBefore()
        {
            var dm = OBJECT_SELF;
            var isUnpossess = StringToObject(_eventsPlugin.GetEventData("TARGET")) == OBJECT_INVALID;

            if (isUnpossess)
            {
                var uiTarget = GetMaster(dm);

                _guiService.CloseWindow(dm, GuiWindowType.AppearanceEditor, uiTarget);
            }
            else
            {
                if (_guiService.IsWindowOpen(dm, GuiWindowType.AppearanceEditor))
                    _guiService.TogglePlayerWindow(dm, GuiWindowType.AppearanceEditor);
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
            _racialAppearances[AppearanceType.Ewok] = new EwokRacialAppearanceDefinition();
            
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
            _armorAppearances[AppearanceType.Ewok] = new GeneralArmorAppearanceDefinition();
        }

        private static void LoadWeaponAppearances()
        {
            _weaponAppearances[BaseItemType.Dagger] = new DaggerAppearanceDefinition();
            _weaponAppearances[BaseItemType.Electroblade] = new ElectrobladeAppearanceDefinition();
            _weaponAppearances[BaseItemType.GreatSword] = new GreatSwordAppearanceDefinition();
            _weaponAppearances[BaseItemType.Katar] = new KatarAppearanceDefinition();
            _weaponAppearances[BaseItemType.LargeShield] = new LargeShieldAppearanceDefinition();
            _weaponAppearances[BaseItemType.Longsword] = new LongswordAppearanceDefinition();
            _weaponAppearances[BaseItemType.Pistol] = new PistolAppearanceDefinition();
            _weaponAppearances[BaseItemType.QuarterStaff] = new QuarterstaffAppearanceDefinition();
            _weaponAppearances[BaseItemType.Rifle] = new RifleAppearanceDefinition();
            _weaponAppearances[BaseItemType.Shuriken] = new ShurikenAppearanceDefinition();
            _weaponAppearances[BaseItemType.ShortSpear] = new SpearAppearanceDefinition();
            _weaponAppearances[BaseItemType.TwoBladedSword] = new TwinBladeAppearanceDefinition();
            _weaponAppearances[BaseItemType.TwinElectroBlade] = new TwinElectrobladeAppearanceDefinition();
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
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftShoulder))
                    AdjustArmorPart(ItemAppearanceArmorType.LeftShoulder, 0);
            }
        }

        public int LeftBicepSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftBicep))
                    AdjustArmorPart(ItemAppearanceArmorType.LeftBicep, 0);
            }
        }
        public int LeftForearmSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftForearm))
                    AdjustArmorPart(ItemAppearanceArmorType.LeftForearm, 0);
            }
        }
        public int LeftHandSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftHand))
                    AdjustArmorPart(ItemAppearanceArmorType.LeftHand, 0);
            }
        }
        public int LeftThighSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftThigh))
                    AdjustArmorPart(ItemAppearanceArmorType.LeftThigh, 0);
            }
        }
        public int LeftShinSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftShin))
                    AdjustArmorPart(ItemAppearanceArmorType.LeftShin, 0);
            }
        }
        public int LeftFootSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftFoot))
                    AdjustArmorPart(ItemAppearanceArmorType.LeftFoot, 0);
            }
        }
        public int RightShoulderSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightShoulder))
                    AdjustArmorPart(ItemAppearanceArmorType.RightShoulder, 0);
            }
        }
        public int RightBicepSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightBicep))
                    AdjustArmorPart(ItemAppearanceArmorType.RightBicep, 0);
            }
        }
        public int RightForearmSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightForearm))
                    AdjustArmorPart(ItemAppearanceArmorType.RightForearm, 0);
            }
        }
        public int RightHandSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightHand))
                    AdjustArmorPart(ItemAppearanceArmorType.RightHand, 0);
            }
        }
        public int RightThighSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightThigh))
                    AdjustArmorPart(ItemAppearanceArmorType.RightThigh, 0);
            }
        }
        public int RightShinSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightShin))
                    AdjustArmorPart(ItemAppearanceArmorType.RightShin, 0);
            }
        }
        public int RightFootSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightFoot))
                    AdjustArmorPart(ItemAppearanceArmorType.RightFoot, 0);
            }
        }
        public int NeckSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.Neck))
                    AdjustArmorPart(ItemAppearanceArmorType.Neck, 0);
            }
        }
        public int ChestSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.Torso))
                    AdjustArmorPart(ItemAppearanceArmorType.Torso, 0);
            }
        }
        public int BeltSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.Belt))
                    AdjustArmorPart(ItemAppearanceArmorType.Belt, 0);
            }
        }
        public int PelvisSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.Pelvis))
                    AdjustArmorPart(ItemAppearanceArmorType.Pelvis, 0);
            }
        }
        public int RobeSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.Robe))
                    AdjustArmorPart(ItemAppearanceArmorType.Robe, 0);
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

            // Armors must have parts that are publicly available.
            // If any single part ID is not in the list of available parts, the entire outfit cannot be edited.
            if (SelectedItemTypeIndex == 0)
            {
                var appearanceType = GetAppearanceType(_target);

                if (!_armorAppearances[appearanceType].Neck.Contains(GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.Neck)) ||
                    !_armorAppearances[appearanceType].Torso.Contains(GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.Torso)) ||
                    !_armorAppearances[appearanceType].Belt.Contains(GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.Belt)) ||
                    !_armorAppearances[appearanceType].Pelvis.Contains(GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.Pelvis)) ||

                    !_armorAppearances[appearanceType].Shoulder.Contains(GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftShoulder)) ||
                    !_armorAppearances[appearanceType].Bicep.Contains(GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftBicep)) ||
                    !_armorAppearances[appearanceType].Forearm.Contains(GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftForearm)) ||
                    !_armorAppearances[appearanceType].Hand.Contains(GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftHand)) ||
                    
                    !_armorAppearances[appearanceType].Thigh.Contains(GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftThigh)) ||
                    !_armorAppearances[appearanceType].Shin.Contains(GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftShin)) ||
                    !_armorAppearances[appearanceType].Foot.Contains(GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftFoot)) ||

                    !_armorAppearances[appearanceType].Shoulder.Contains(GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightShoulder)) ||
                    !_armorAppearances[appearanceType].Bicep.Contains(GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightBicep)) ||
                    !_armorAppearances[appearanceType].Forearm.Contains(GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightForearm)) ||
                    !_armorAppearances[appearanceType].Hand.Contains(GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightHand)) ||

                    !_armorAppearances[appearanceType].Thigh.Contains(GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightThigh)) ||
                    !_armorAppearances[appearanceType].Shin.Contains(GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightShin)) ||
                    !_armorAppearances[appearanceType].Foot.Contains(GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightFoot)) ||

                    !_armorAppearances[appearanceType].Robe.Contains(GetItemAppearance(GetItem(), ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.Robe)))
                {
                    return false;
                }
            }
            
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
                    var partId = GetItemAppearance(item, ItemModelColorType.SimpleModel, -1);
                    if (!appearance.SimpleParts.Contains(partId))
                        return false;
                }
                else
                {
                    var topId = GetItemAppearance(item, ItemModelColorType.WeaponModel, (int)ItemAppearanceWeaponType.Top);
                    var middleId = GetItemAppearance(item, ItemModelColorType.WeaponModel, (int)ItemAppearanceWeaponType.Middle);
                    var bottomId = GetItemAppearance(item, ItemModelColorType.WeaponModel, (int)ItemAppearanceWeaponType.Bottom);
                    var topColor = GetItemAppearance(item, ItemModelColorType.WeaponColor, (int)ItemAppearanceWeaponType.Top);
                    var middleColor = GetItemAppearance(item, ItemModelColorType.WeaponColor, (int)ItemAppearanceWeaponType.Middle);
                    var bottomColor = GetItemAppearance(item, ItemModelColorType.WeaponColor, (int)ItemAppearanceWeaponType.Bottom);

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
                return GetItemInSlot(InventorySlotType.Chest, _target);
            }
            else if (SelectedItemTypeIndex == 1) // 1 = Helmet
            {
                return GetItemInSlot(InventorySlotType.Head, _target);
            }
            else if (SelectedItemTypeIndex == 2) // 2 = Cloak
            {
                return GetItemInSlot(InventorySlotType.Cloak, _target);
            }
            else if (SelectedItemTypeIndex == 3) // 3 = Weapon (Main Hand)
            {
                return GetItemInSlot(InventorySlotType.RightHand, _target);
            }
            else if (SelectedItemTypeIndex == 4) // 4 = Weapon (Off Hand)
            {
                return GetItemInSlot(InventorySlotType.LeftHand, _target);
            }

            return OBJECT_INVALID;
        }

        private void LoadBodyParts()
        {
            var appearanceType = GetAppearanceType(_target);
            var gender = GetGender(_target);

            if (!_racialAppearances.ContainsKey(appearanceType))
            {
                _guiService.TogglePlayerWindow(_target, GuiWindowType.AppearanceEditor);
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
                        case GenderType.Male:
                            partIds = appearance.MaleHeads;
                            break;
                        default:
                            partIds = appearance.FemaleHeads;
                            break;
                    }

                    selectedPartId = GetCreatureBodyPart(CreaturePartType.Head, _target);
                    break;
                case 1: // Torso
                    partIds = appearance.Torsos;
                    selectedPartId = GetCreatureBodyPart(CreaturePartType.Torso, _target);
                    break;
                case 2: // Pelvis
                    partIds = appearance.Pelvis;
                    selectedPartId = GetCreatureBodyPart(CreaturePartType.Pelvis, _target);
                    break;
                case 3: // Right Bicep
                    partIds = appearance.RightBicep;
                    selectedPartId = GetCreatureBodyPart(CreaturePartType.RightBicep, _target);
                    break;
                case 4: // Right Forearm
                    partIds = appearance.RightForearm;
                    selectedPartId = GetCreatureBodyPart(CreaturePartType.RightForearm, _target);
                    break;
                case 5: // Right Hand
                    partIds = appearance.RightHand;
                    selectedPartId = GetCreatureBodyPart(CreaturePartType.RightHand, _target);
                    break;
                case 6: // Right Thigh
                    partIds = appearance.RightThigh;
                    selectedPartId = GetCreatureBodyPart(CreaturePartType.RightThigh, _target);
                    break;
                case 7: // Right Shin
                    partIds = appearance.RightShin;
                    selectedPartId = GetCreatureBodyPart(CreaturePartType.RightShin, _target);
                    break;
                case 8: // Right Foot
                    partIds = appearance.RightFoot;
                    selectedPartId = GetCreatureBodyPart(CreaturePartType.RightFoot, _target);
                    break;
                case 9: // Left Bicep
                    partIds = appearance.LeftBicep;
                    selectedPartId = GetCreatureBodyPart(CreaturePartType.LeftBicep, _target);
                    break;
                case 10: // Left Forearm
                    partIds = appearance.LeftForearm;
                    selectedPartId = GetCreatureBodyPart(CreaturePartType.LeftForearm, _target);
                    break;
                case 11: // Left Hand
                    partIds = appearance.LeftHand;
                    selectedPartId = GetCreatureBodyPart(CreaturePartType.LeftHand, _target);
                    break;
                case 12: // Left Thigh
                    partIds = appearance.LeftThigh;
                    selectedPartId = GetCreatureBodyPart(CreaturePartType.LeftThigh, _target);
                    break;
                case 13: // Left Shin
                    partIds = appearance.LeftShin;
                    selectedPartId = GetCreatureBodyPart(CreaturePartType.LeftShin, _target);
                    break;
                case 14: // Left Foot
                    partIds = appearance.LeftFoot;
                    selectedPartId = GetCreatureBodyPart(CreaturePartType.LeftFoot, _target);
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
            var dbPlayer = _db.Get<Player>(playerId);
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

                NeckSelection = GetItemAppearance(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.Neck);
                ChestSelection = GetItemAppearance(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.Torso);
                BeltSelection = GetItemAppearance(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.Belt);
                PelvisSelection = GetItemAppearance(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.Pelvis);
                RobeSelection = GetItemAppearance(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.Robe);

                LeftShoulderSelection = GetItemAppearance(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftShoulder);
                LeftBicepSelection = GetItemAppearance(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftBicep);
                LeftForearmSelection = GetItemAppearance(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftForearm);
                LeftHandSelection = GetItemAppearance(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftHand);
                LeftThighSelection = GetItemAppearance(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftThigh);
                LeftShinSelection = GetItemAppearance(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftShin);
                LeftFootSelection = GetItemAppearance(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftFoot);

                RightShoulderSelection = GetItemAppearance(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightShoulder);
                RightBicepSelection = GetItemAppearance(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightBicep);
                RightForearmSelection = GetItemAppearance(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightForearm);
                RightHandSelection = GetItemAppearance(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightHand);
                RightThighSelection = GetItemAppearance(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightThigh);
                RightShinSelection = GetItemAppearance(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightShin);
                RightFootSelection = GetItemAppearance(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightFoot);

                UpdateAllColors();

                return;
            }
            else if (SelectedItemTypeIndex == 1) // 1 = Helmet
            {
                partIds = _armorAppearances[appearanceType].Helmet;
                selectedPartId = GetItemAppearance(item, ItemModelColorType.SimpleModel, -1);
            }
            else if (SelectedItemTypeIndex == 2) // 2 = Cloak
            {
                partIds = _armorAppearances[appearanceType].Cloak;
                selectedPartId = GetItemAppearance(item, ItemModelColorType.SimpleModel, -1);
            }
            else if (SelectedItemTypeIndex == 3 || SelectedItemTypeIndex == 4) // 3 = Weapon (Main Hand), 4 = Weapon (Off Hand)
            {
                int offset;

                if (_weaponAppearances[type].IsSimple)
                {
                    partIds = _weaponAppearances[type].SimpleParts;
                    selectedPartId = GetItemAppearance(item, ItemModelColorType.SimpleModel, -1);
                }
                else
                {
                    switch (SelectedPartCategoryIndex)
                    {
                        case 0: // 0 = Top
                            partIds = _weaponAppearances[type].TopParts;
                            selectedPartId = GetItemAppearance(item, ItemModelColorType.WeaponModel, (int)ItemAppearanceWeaponType.Top);
                            offset = GetItemAppearance(item, ItemModelColorType.WeaponColor, (int)ItemAppearanceWeaponType.Top);
                            break;
                        case 1: // 1 = Middle
                            partIds = _weaponAppearances[type].MiddleParts;
                            selectedPartId = GetItemAppearance(item, ItemModelColorType.WeaponModel, (int)ItemAppearanceWeaponType.Middle);
                            offset = GetItemAppearance(item, ItemModelColorType.WeaponColor, (int)ItemAppearanceWeaponType.Middle);
                            break;
                        case 2: // 2 = Bottom
                            partIds = _weaponAppearances[type].BottomParts;
                            selectedPartId = GetItemAppearance(item, ItemModelColorType.WeaponModel, (int)ItemAppearanceWeaponType.Bottom);
                            offset = GetItemAppearance(item, ItemModelColorType.WeaponColor, (int)ItemAppearanceWeaponType.Bottom);
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
            var dbPlayer = _db.Get<Player>(playerId);

            ShowHelmet = dbPlayer.Settings.ShowHelmet;
            ShowCloak = dbPlayer.Settings.ShowCloak;
            _lastModifiedItem = OBJECT_INVALID;
        };

        public Action OnDecreaseAppearanceScale() => () =>
        {
            var appearanceType = GetAppearanceType(_target);
            if (!_racialAppearances.ContainsKey(appearanceType))
            {
                _guiService.TogglePlayerWindow(_target, GuiWindowType.AppearanceEditor);
                return;
            }

            var appearance = _racialAppearances[appearanceType];
            var scale = GetObjectVisualTransform(_target, ObjectVisualTransformType.Scale);
            const float Increment = 0.01f;

            if (scale - Increment < appearance.MinimumScale)
            {
                SendMessageToPC(_target, "You cannot decrease your height any further.");
            }
            else
            {
                SetObjectVisualTransform(_target, ObjectVisualTransformType.Scale, scale - Increment);
                SendMessageToPC(_target, $"Height: {GetObjectVisualTransform(_target, ObjectVisualTransformType.Scale)}");
            }
        };
        public Action OnIncreaseAppearanceScale() => () =>
        {
            var appearanceType = GetAppearanceType(_target);
            if (!_racialAppearances.ContainsKey(appearanceType))
            {
                _guiService.TogglePlayerWindow(_target, GuiWindowType.AppearanceEditor);
                return;
            }

            var appearance = _racialAppearances[appearanceType];

            var scale = GetObjectVisualTransform(_target, ObjectVisualTransformType.Scale);
            const float Increment = 0.01f;

            if (scale + Increment > appearance.MaximumScale)
            {
                SendMessageToPC(_target, "You cannot increase your height any further.");
            }
            else
            {
                SetObjectVisualTransform(_target, ObjectVisualTransformType.Scale, scale + Increment);
                SendMessageToPC(_target, $"Height: {GetObjectVisualTransform(_target, ObjectVisualTransformType.Scale)}");
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

        private InventorySlotType GetInventorySlot()
        {
            var slot = InventorySlotType.Invalid;

            switch (SelectedItemTypeIndex)
            {
                case 0: // 0 = Chest
                    slot = InventorySlotType.Chest;
                    break;
                case 1: // 1 = Head
                    slot = InventorySlotType.Head;
                    break;
                case 2: // 2 = Cloak
                    slot = InventorySlotType.Cloak;
                    break;
                case 3: // 3 = Weapon (Main Hand)
                    slot = InventorySlotType.RightHand;
                    break;
                case 4: // 4 = Weapon (Off Hand)
                    slot = InventorySlotType.LeftHand;
                    break;
            }

            return slot;
        }

        private ItemModelColorType GetModelType()
        {
            var modelType = ItemModelColorType.ArmorModel;

            switch (SelectedItemTypeIndex)
            {
                case 0: // 0 = Chest
                    modelType = ItemModelColorType.ArmorModel;
                    break;
                case 1: // 1 = Head
                    modelType = ItemModelColorType.SimpleModel;
                    break;
                case 2: // 2 = Cloak
                    modelType = ItemModelColorType.SimpleModel;
                    break;
                case 3: // 3 = Weapon (Main Hand)
                    modelType = ItemModelColorType.WeaponModel;
                    break;
                case 4: // 4 = Weapon (Off Hand)
                    modelType = ItemModelColorType.WeaponModel;
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
                copy = CopyItemAndModify(copy, ItemModelColorType.WeaponColor, type, colorId, true);
                partId %= 100;

                // Note: DestroyObject gets run at the end of the process so it's fine to queue up a call to destroy this temporary copy here.
                DestroyObject(oldCopy);
                DestroyObject(copy);
            }

            if (_weaponAppearances.ContainsKey(itemType) && _weaponAppearances[itemType].IsSimple)
            {
                copy = CopyItemAndModify(copy, ItemModelColorType.SimpleModel, type, partId, true);
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
                        SetColor(_target, ColorChannelType.Skin, colorId);
                        break;
                    case 1: //  1 = Hair
                        SetColor(_target, ColorChannelType.Hair, colorId);
                        break;
                    case 2: // 2 = Tattoo 1
                        SetColor(_target, ColorChannelType.Tattoo1, colorId);
                        break;
                    case 3: // 3 = Tattoo 2
                        SetColor(_target, ColorChannelType.Tattoo2, colorId);
                        break;
                }
            }
            // Helmet/Cloak - Cloth 1, Cloth 2, Leather 1, Leather 2, Metal 1, Metal 2
            else if (IsEquipmentSelected && (SelectedItemTypeIndex == 1 || SelectedItemTypeIndex == 2))
            {
                switch (SelectedColorCategoryIndex)
                {
                    case 0: // 0 = Leather 1
                        ModifyHelmetCloakColor(ItemAppearanceArmorColorType.Leather1, colorId);
                        break;
                    case 1: // 1 = Leather 2
                        ModifyHelmetCloakColor(ItemAppearanceArmorColorType.Leather2, colorId);
                        break;
                    case 2: // 2 = Cloth 1
                        ModifyHelmetCloakColor(ItemAppearanceArmorColorType.Cloth1, colorId);
                        break;
                    case 3: // 3 = Cloth 2
                        ModifyHelmetCloakColor(ItemAppearanceArmorColorType.Cloth2, colorId);
                        break;
                    case 4: // 4 = Metal 1
                        ModifyHelmetCloakColor(ItemAppearanceArmorColorType.Metal1, colorId);
                        break;
                    case 5: // 5 = Metal 2
                        ModifyHelmetCloakColor(ItemAppearanceArmorColorType.Metal2, colorId);
                        break;
                }
            }
        };

        private void ModifyHelmetCloakColor(ItemAppearanceArmorColorType colorChannel, int colorId)
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            var slot = GetInventorySlot();
            var item = GetItem();
            var copy = CopyItemAndModify(item, ItemModelColorType.ArmorColor, (int)colorChannel, colorId, true);

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
                        case GenderType.Male:
                            SetCreatureBodyPart(CreaturePartType.Head, appearance.MaleHeads[SelectedPartIndex], _target);
                            break;
                        default:
                            SetCreatureBodyPart(CreaturePartType.Head, appearance.FemaleHeads[SelectedPartIndex], _target);
                            break;
                    }
                    break;
                case 1: // Torso
                    SetCreatureBodyPart(CreaturePartType.Torso, appearance.Torsos[SelectedPartIndex], _target);
                    break;
                case 2: // Pelvis
                    SetCreatureBodyPart(CreaturePartType.Pelvis, appearance.Pelvis[SelectedPartIndex], _target);
                    break;
                case 3: // Right Bicep
                    SetCreatureBodyPart(CreaturePartType.RightBicep, appearance.RightBicep[SelectedPartIndex], _target);
                    break;
                case 4: // Right Forearm
                    SetCreatureBodyPart(CreaturePartType.RightForearm, appearance.RightForearm[SelectedPartIndex], _target);
                    break;
                case 5: // Right Hand
                    SetCreatureBodyPart(CreaturePartType.RightHand, appearance.RightHand[SelectedPartIndex], _target);
                    break;
                case 6: // Right Thigh
                    SetCreatureBodyPart(CreaturePartType.RightThigh, appearance.RightThigh[SelectedPartIndex], _target);
                    break;
                case 7: // Right Shin
                    SetCreatureBodyPart(CreaturePartType.RightShin, appearance.RightShin[SelectedPartIndex], _target);
                    break;
                case 8: // Right Foot
                    SetCreatureBodyPart(CreaturePartType.RightFoot, appearance.RightFoot[SelectedPartIndex], _target);
                    break;
                case 9: // Left Bicep
                    SetCreatureBodyPart(CreaturePartType.LeftBicep, appearance.LeftBicep[SelectedPartIndex], _target);
                    break;
                case 10: // Left Forearm
                    SetCreatureBodyPart(CreaturePartType.LeftForearm, appearance.LeftForearm[SelectedPartIndex], _target);
                    break;
                case 11: // Left Hand
                    SetCreatureBodyPart(CreaturePartType.LeftHand, appearance.LeftHand[SelectedPartIndex], _target);
                    break;
                case 12: // Left Thigh
                    SetCreatureBodyPart(CreaturePartType.LeftThigh, appearance.LeftThigh[SelectedPartIndex], _target);
                    break;
                case 13: // Left Shin
                    SetCreatureBodyPart(CreaturePartType.LeftShin, appearance.LeftShin[SelectedPartIndex], _target);
                    break;
                case 14: // Left Foot
                    SetCreatureBodyPart(CreaturePartType.LeftFoot, appearance.LeftFoot[SelectedPartIndex], _target);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(SelectedPartIndex));
            }

            _eventAggregator.Publish(new OnAppearanceEdit(), _target);
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
                ModifyItemPart((int)ItemAppearanceArmorType.Invalid, _armorAppearances[appearanceType].Helmet[SelectedPartIndex]);
            }
            else if (SelectedItemTypeIndex == 2) // 2 = Cloak
            {
                ModifyItemPart((int)ItemAppearanceArmorType.Invalid, _armorAppearances[appearanceType].Cloak[SelectedPartIndex]);
            }
            else if (SelectedItemTypeIndex == 3 || SelectedItemTypeIndex == 4) // 3 = Weapon (Main Hand), 4 = Weapon (Off Hand)
            {
                int color;
                int partId;

                if (_weaponAppearances[itemType].IsSimple)
                {
                    partId = _weaponAppearances[itemType].SimpleParts[SelectedPartIndex];
                    ModifyItemPart((int)ItemModelColorType.SimpleModel, partId);
                }
                else
                {
                    switch (SelectedPartCategoryIndex)
                    {
                        case 0: // Top
                            color = _weaponAppearances[itemType].TopParts[SelectedPartIndex] / 100;
                            partId = _weaponAppearances[itemType].TopParts[SelectedPartIndex] % 100;
                            ModifyItemPart((int)ItemAppearanceWeaponType.Top, partId, color);
                            break;
                        case 1: // Middle
                            color = _weaponAppearances[itemType].MiddleParts[SelectedPartIndex] / 100;
                            partId = _weaponAppearances[itemType].MiddleParts[SelectedPartIndex] % 100;
                            ModifyItemPart((int)ItemAppearanceWeaponType.Middle, partId, color);
                            break;
                        case 2: // Bottom
                            color = _weaponAppearances[itemType].BottomParts[SelectedPartIndex] / 100;
                            partId = _weaponAppearances[itemType].BottomParts[SelectedPartIndex] % 100;
                            ModifyItemPart((int)ItemAppearanceWeaponType.Bottom, partId, color);
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
            _guiService.TogglePlayerWindow(_target, GuiWindowType.Outfits);
        };

        public Action OnCloseWindow() => () =>
        {
            if (GetIsDM(_target) || GetIsDMPossessed(_target) || !GetIsPC(_target))
                return;

            var playerId = GetObjectUUID(_target);
            var dbPlayer = _db.Get<Player>(playerId);

            SetObjectVisualTransform(_target, ObjectVisualTransformType.Scale, dbPlayer.AppearanceScale);
        };

        public Action OnClickSaveSettings() => () =>
        {
            var playerId = GetObjectUUID(_target);
            var dbPlayer = _db.Get<Player>(playerId);

            dbPlayer.Settings.ShowCloak = ShowCloak;
            dbPlayer.Settings.ShowHelmet = ShowHelmet;

            var newHeight = GetObjectVisualTransform(_target, ObjectVisualTransformType.Scale);
            dbPlayer.AppearanceScale = newHeight;

            _db.Set(dbPlayer);
            SendMessageToPC(_target, ColorToken.Green("Appearance settings saved successfully."));

            UpdateArmorDisplay();
        };

        public Action OnClickColorPalette(int colorId) => () =>
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            if (!GetBaseItemFitsInInventory(BaseItemType.Armor, _target))
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
                item = CopyItemAndModify(item, ItemModelColorType.ArmorColor, (int)_selectedColorChannel, colorId, true);
                AssignCommand(_target, () => ActionEquipItem(item, InventorySlotType.Chest));
            }
            else
            {
                var item = GetItem();
                DestroyObject(item);

                var armorModel = GetArmorModelType(_colorTarget);
                var index = CalculatePerPartColorIndex(armorModel, _selectedColorChannel);
                item = CopyItemAndModify(item, ItemModelColorType.ArmorColor, index, colorId, true);
                AssignCommand(_target, () => ActionEquipItem(item, InventorySlotType.Chest));
            }

            ChangeColor(_colorTarget, _selectedColorChannel, colorId);
        };

        public Action OnClickClearColor(ColorTarget colorTarget, ItemAppearanceArmorColorType colorChannel) => () =>
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
            item = CopyItemAndModify(item, ItemModelColorType.ArmorColor, index, 255, true);
            AssignCommand(_target, () => ActionEquipItem(item, InventorySlotType.Chest));

            ChangeColor(colorTarget, colorChannel, 255);
        };

        private ItemAppearanceArmorType GetArmorModelType(ColorTarget colorTarget)
        {
            var armorModel = ItemAppearanceArmorType.Invalid;
            switch (colorTarget)
            {
                case ColorTarget.LeftShoulder:
                    armorModel = ItemAppearanceArmorType.LeftShoulder;
                    break;
                case ColorTarget.LeftBicep:
                    armorModel = ItemAppearanceArmorType.LeftBicep;
                    break;
                case ColorTarget.LeftForearm:
                    armorModel = ItemAppearanceArmorType.LeftForearm;
                    break;
                case ColorTarget.LeftHand:
                    armorModel = ItemAppearanceArmorType.LeftHand;
                    break;
                case ColorTarget.LeftThigh:
                    armorModel = ItemAppearanceArmorType.LeftThigh;
                    break;
                case ColorTarget.LeftShin:
                    armorModel = ItemAppearanceArmorType.LeftShin;
                    break;
                case ColorTarget.LeftFoot:
                    armorModel = ItemAppearanceArmorType.LeftFoot;
                    break;
                case ColorTarget.RightShoulder:
                    armorModel = ItemAppearanceArmorType.RightShoulder;
                    break;
                case ColorTarget.RightBicep:
                    armorModel = ItemAppearanceArmorType.RightBicep;
                    break;
                case ColorTarget.RightForearm:
                    armorModel = ItemAppearanceArmorType.RightForearm;
                    break;
                case ColorTarget.RightHand:
                    armorModel = ItemAppearanceArmorType.RightHand;
                    break;
                case ColorTarget.RightThigh:
                    armorModel = ItemAppearanceArmorType.RightThigh;
                    break;
                case ColorTarget.RightShin:
                    armorModel = ItemAppearanceArmorType.RightShin;
                    break;
                case ColorTarget.RightFoot:
                    armorModel = ItemAppearanceArmorType.RightFoot;
                    break;
                case ColorTarget.Neck:
                    armorModel = ItemAppearanceArmorType.Neck;
                    break;
                case ColorTarget.Chest:
                    armorModel = ItemAppearanceArmorType.Torso;
                    break;
                case ColorTarget.Belt:
                    armorModel = ItemAppearanceArmorType.Belt;
                    break;
                case ColorTarget.Pelvis:
                    armorModel = ItemAppearanceArmorType.Pelvis;
                    break;
                case ColorTarget.Robe:
                    armorModel = ItemAppearanceArmorType.Robe;
                    break;
            }

            return armorModel;
        }

        private int CalculatePerPartColorIndex(ItemAppearanceArmorType armorModel, ItemAppearanceArmorColorType colorChannel)
        {
            return (int)ItemAppearanceArmorColorType.NumColors + (int)armorModel * (int)ItemAppearanceArmorColorType.NumColors + (int)colorChannel;
        }

        public Action OnClickColorTarget(ColorTarget target, ItemAppearanceArmorColorType channel) => () =>
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            _colorTarget = target;
            _selectedColorChannel = channel;

            UpdateTargetedColor();

            // We only swap the palette if we're moving from a Cloth/Leather palette to a Metal palette or vice-versa.
            // This (slightly) works around a NUI issue where the palette will disappear when switching between Cloth/Leather and Metal.
            if ((channel == ItemAppearanceArmorColorType.Metal1 || channel == ItemAppearanceArmorColorType.Metal2) && !_isMetalPalette)
            {
                ChangePartialView(ArmorColorElement, ArmorColorsMetal);
                _isMetalPalette = true;
            }
            else if(_isMetalPalette && channel != ItemAppearanceArmorColorType.Metal1 && channel != ItemAppearanceArmorColorType.Metal2)
            {
                ChangePartialView(ArmorColorElement, ArmorColorsClothLeather);
                _isMetalPalette = false;
            }
        };

        private GuiRectangle BuildColorRegion(ColorTarget target, ItemAppearanceArmorColorType colorChannel)
        {
            var item = GetItem();
            int colorId;

            if (target == ColorTarget.Global)
            {
                colorId = GetItemAppearance(item, ItemModelColorType.ArmorColor, (int)colorChannel);
            }
            else
            {
                var armorModel = GetArmorModelType(target);
                var perPartColorIndex = CalculatePerPartColorIndex(armorModel, colorChannel);
                colorId = GetItemAppearance(item, ItemModelColorType.ArmorColor, perPartColorIndex);
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
                case ItemAppearanceArmorColorType.Leather1:
                    channelName = "Leather 1";
                    break;
                case ItemAppearanceArmorColorType.Leather2:
                    channelName = "Leather 2";
                    break;
                case ItemAppearanceArmorColorType.Cloth1:
                    channelName = "Cloth 1";
                    break;
                case ItemAppearanceArmorColorType.Cloth2:
                    channelName = "Cloth 2";
                    break;
                case ItemAppearanceArmorColorType.Metal1:
                    channelName = "Metal 1";
                    break;
                case ItemAppearanceArmorColorType.Metal2:
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

        private void AdjustArmorPart(ItemAppearanceArmorType partType, int adjustBy)
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
                case ItemAppearanceArmorType.RightFoot:
                    RightFootSelection = Adjust(RightFootOptions, RightFootSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Foot[ArmorValueToIndex(RightFootOptions, RightFootSelection)]);
                    break;
                case ItemAppearanceArmorType.LeftFoot:
                    LeftFootSelection = Adjust(LeftFootOptions, LeftFootSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Foot[ArmorValueToIndex(LeftFootOptions, LeftFootSelection)]);
                    break;
                case ItemAppearanceArmorType.RightShin:
                    RightShinSelection = Adjust(RightShinOptions, RightShinSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Shin[ArmorValueToIndex(RightShinOptions, RightShinSelection)]);
                    break;
                case ItemAppearanceArmorType.LeftShin:
                    LeftShinSelection = Adjust(LeftShinOptions, LeftShinSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Shin[ArmorValueToIndex(LeftShinOptions, LeftShinSelection)]);
                    break;
                case ItemAppearanceArmorType.LeftThigh:
                    LeftThighSelection = Adjust(LeftThighOptions, LeftThighSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Thigh[ArmorValueToIndex(LeftThighOptions, LeftThighSelection)]);
                    break;
                case ItemAppearanceArmorType.RightThigh:
                    RightThighSelection = Adjust(RightThighOptions, RightThighSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Thigh[ArmorValueToIndex(RightThighOptions, RightThighSelection)]);
                    break;
                case ItemAppearanceArmorType.Pelvis:
                    PelvisSelection = Adjust(PelvisOptions, PelvisSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Pelvis[ArmorValueToIndex(PelvisOptions, PelvisSelection)]);
                    break;
                case ItemAppearanceArmorType.Torso:
                    ChestSelection = Adjust(ChestOptions, ChestSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Torso[ArmorValueToIndex(ChestOptions, ChestSelection)]);
                    break;
                case ItemAppearanceArmorType.Belt:
                    BeltSelection = Adjust(BeltOptions, BeltSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Belt[ArmorValueToIndex(BeltOptions, BeltSelection)]);
                    break;
                case ItemAppearanceArmorType.Neck:
                    NeckSelection = Adjust(NeckOptions, NeckSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Neck[ArmorValueToIndex(NeckOptions, NeckSelection)]);
                    break;
                case ItemAppearanceArmorType.RightForearm:
                    RightForearmSelection = Adjust(RightForearmOptions, RightForearmSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Forearm[ArmorValueToIndex(RightForearmOptions, RightForearmSelection)]);
                    break;
                case ItemAppearanceArmorType.LeftForearm:
                    LeftForearmSelection = Adjust(LeftForearmOptions, LeftForearmSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Forearm[ArmorValueToIndex(LeftForearmOptions, LeftForearmSelection)]);
                    break;
                case ItemAppearanceArmorType.RightBicep:
                    RightBicepSelection = Adjust(RightBicepOptions, RightBicepSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Bicep[ArmorValueToIndex(RightBicepOptions, RightBicepSelection)]);
                    break;
                case ItemAppearanceArmorType.LeftBicep:
                    LeftBicepSelection = Adjust(LeftBicepOptions, LeftBicepSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Bicep[ArmorValueToIndex(LeftBicepOptions, LeftBicepSelection)]);
                    break;
                case ItemAppearanceArmorType.RightShoulder:
                    RightShoulderSelection = Adjust(RightShoulderOptions, RightShoulderSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Shoulder[ArmorValueToIndex(RightShoulderOptions, RightShoulderSelection)]);
                    break;
                case ItemAppearanceArmorType.LeftShoulder:
                    LeftShoulderSelection = Adjust(LeftShoulderOptions, LeftShoulderSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Shoulder[ArmorValueToIndex(LeftShoulderOptions, LeftShoulderSelection)]);
                    break;
                case ItemAppearanceArmorType.RightHand:
                    RightHandSelection = Adjust(RightHandOptions, RightHandSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Hand[ArmorValueToIndex(RightHandOptions, RightHandSelection)]);
                    break;
                case ItemAppearanceArmorType.LeftHand:
                    LeftHandSelection = Adjust(LeftHandOptions, LeftHandSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Hand[ArmorValueToIndex(LeftHandOptions, LeftHandSelection)]);
                    break;
                case ItemAppearanceArmorType.Robe:
                    RobeSelection = Adjust(RobeOptions, RobeSelection);
                    ModifyItemPart((int)partType, _armorAppearances[appearanceType].Robe[ArmorValueToIndex(RobeOptions, RobeSelection)]);
                    break;
            }

            _skipAdjustArmorPart = false;
        }

        public Action OnClickAdjustArmorPart(ItemAppearanceArmorType partType, int adjustBy) => () =>
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
            if (GetBaseItemFitsInInventory(BaseItemType.Armor, _target))
            {
                var copyToIndex = CalculatePerPartColorIndex(copyTo, ItemAppearanceArmorColorType.Cloth1);
                var copyFromIndex = CalculatePerPartColorIndex(copyFrom, ItemAppearanceArmorColorType.Cloth1);
                var newColor = GetItemAppearance(item, ItemModelColorType.ArmorColor, copyFromIndex);
                ChangeColor(copyToTarget, ItemAppearanceArmorColorType.Cloth1, newColor);
                item = CopyItemAndModify(item, ItemModelColorType.ArmorColor, copyToIndex, newColor, true);
                DestroyObject(item);
            }
            // Cloth 2
            if (GetBaseItemFitsInInventory(BaseItemType.Armor, _target))
            {
                var copyToIndex = CalculatePerPartColorIndex(copyTo, ItemAppearanceArmorColorType.Cloth2);
                var copyFromIndex = CalculatePerPartColorIndex(copyFrom, ItemAppearanceArmorColorType.Cloth2);
                var newColor = GetItemAppearance(item, ItemModelColorType.ArmorColor, copyFromIndex);
                ChangeColor(copyToTarget, ItemAppearanceArmorColorType.Cloth2, newColor);
                item = CopyItemAndModify(item, ItemModelColorType.ArmorColor, copyToIndex, newColor, true);
                DestroyObject(item);
            }
            // Leather 1
            if (GetBaseItemFitsInInventory(BaseItemType.Armor, _target))
            {
                var copyToIndex = CalculatePerPartColorIndex(copyTo, ItemAppearanceArmorColorType.Leather1);
                var copyFromIndex = CalculatePerPartColorIndex(copyFrom, ItemAppearanceArmorColorType.Leather1);
                var newColor = GetItemAppearance(item, ItemModelColorType.ArmorColor, copyFromIndex);
                ChangeColor(copyToTarget, ItemAppearanceArmorColorType.Leather1, newColor);
                item = CopyItemAndModify(item, ItemModelColorType.ArmorColor, copyToIndex, newColor, true);
                DestroyObject(item);
            }
            // Leather 2
            if (GetBaseItemFitsInInventory(BaseItemType.Armor, _target))
            {
                var copyToIndex = CalculatePerPartColorIndex(copyTo, ItemAppearanceArmorColorType.Leather2);
                var copyFromIndex = CalculatePerPartColorIndex(copyFrom, ItemAppearanceArmorColorType.Leather2);
                var newColor = GetItemAppearance(item, ItemModelColorType.ArmorColor, copyFromIndex);
                ChangeColor(copyToTarget, ItemAppearanceArmorColorType.Leather2, newColor);
                item = CopyItemAndModify(item, ItemModelColorType.ArmorColor, copyToIndex, newColor, true);
                DestroyObject(item);
            }
            // Metal 1
            if (GetBaseItemFitsInInventory(BaseItemType.Armor, _target))
            {
                var copyToIndex = CalculatePerPartColorIndex(copyTo, ItemAppearanceArmorColorType.Metal1);
                var copyFromIndex = CalculatePerPartColorIndex(copyFrom, ItemAppearanceArmorColorType.Metal1);
                var newColor = GetItemAppearance(item, ItemModelColorType.ArmorColor, copyFromIndex);
                ChangeColor(copyToTarget, ItemAppearanceArmorColorType.Metal1, newColor);
                item = CopyItemAndModify(item, ItemModelColorType.ArmorColor, copyToIndex, newColor, true);
                DestroyObject(item);
            }
            // Metal 2
            if (GetBaseItemFitsInInventory(BaseItemType.Armor, _target))
            {
                var copyToIndex = CalculatePerPartColorIndex(copyTo, ItemAppearanceArmorColorType.Metal2);
                var copyFromIndex = CalculatePerPartColorIndex(copyFrom, ItemAppearanceArmorColorType.Metal2);
                var newColor = GetItemAppearance(item, ItemModelColorType.ArmorColor, copyFromIndex);
                ChangeColor(copyToTarget, ItemAppearanceArmorColorType.Metal2, newColor);
                item = CopyItemAndModify(item, ItemModelColorType.ArmorColor, copyToIndex, newColor, true);
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
            item = CopyItemAndModify(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightShoulder, _armorAppearances[appearanceType].Shoulder[ArmorValueToIndex(RightShoulderOptions, RightShoulderSelection)], true);
            DestroyObject(item);

            RightBicepSelection = LeftBicepSelection;
            item = CopyItemAndModify(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightBicep, _armorAppearances[appearanceType].Bicep[ArmorValueToIndex(RightBicepOptions, RightBicepSelection)], true);
            DestroyObject(item);

            RightForearmSelection = LeftForearmSelection;
            item = CopyItemAndModify(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightForearm, _armorAppearances[appearanceType].Forearm[ArmorValueToIndex(RightForearmOptions, RightForearmSelection)], true);
            DestroyObject(item);

            RightHandSelection = LeftHandSelection;
            item = CopyItemAndModify(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightHand, _armorAppearances[appearanceType].Hand[ArmorValueToIndex(RightHandOptions, RightHandSelection)], true);
            DestroyObject(item);

            RightThighSelection = LeftThighSelection;
            item = CopyItemAndModify(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightThigh, _armorAppearances[appearanceType].Thigh[ArmorValueToIndex(RightThighOptions, RightThighSelection)], true);
            DestroyObject(item);

            RightShinSelection = LeftShinSelection;
            item = CopyItemAndModify(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightShin, _armorAppearances[appearanceType].Shin[ArmorValueToIndex(RightShinOptions, RightShinSelection)], true);
            DestroyObject(item);

            RightFootSelection = LeftFootSelection;
            item = CopyItemAndModify(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightFoot, _armorAppearances[appearanceType].Foot[ArmorValueToIndex(RightFootOptions, RightFootSelection)], true);

            // Copy the item from the outfit barrel back to the player.
            var updatedItem = CopyItem(item, _target, true);
            DestroyObject(item);
            
            AssignCommand(_target, () => ActionEquipItem(updatedItem, InventorySlotType.Chest));

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
            item = CopyItemAndModify(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftShoulder, _armorAppearances[appearanceType].Shoulder[ArmorValueToIndex(LeftShoulderOptions, LeftShoulderSelection)], true);
            DestroyObject(item);

            LeftBicepSelection = RightBicepSelection;
            item = CopyItemAndModify(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftBicep, _armorAppearances[appearanceType].Bicep[ArmorValueToIndex(LeftBicepOptions, LeftBicepSelection)], true);
            DestroyObject(item);

            LeftForearmSelection = RightForearmSelection;
            item = CopyItemAndModify(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftForearm, _armorAppearances[appearanceType].Forearm[ArmorValueToIndex(LeftForearmOptions, LeftForearmSelection)], true);
            DestroyObject(item);

            LeftHandSelection = RightHandSelection;
            item = CopyItemAndModify(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftHand, _armorAppearances[appearanceType].Hand[ArmorValueToIndex(LeftHandOptions, LeftHandSelection)], true);
            DestroyObject(item);

            LeftThighSelection = RightThighSelection;
            item = CopyItemAndModify(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftThigh, _armorAppearances[appearanceType].Thigh[ArmorValueToIndex(LeftThighOptions, LeftThighSelection)], true);
            DestroyObject(item);

            LeftShinSelection = RightShinSelection;
            item = CopyItemAndModify(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftShin, _armorAppearances[appearanceType].Shin[ArmorValueToIndex(LeftShinOptions, LeftShinSelection)], true);
            DestroyObject(item);

            LeftFootSelection = RightFootSelection;
            item = CopyItemAndModify(item, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftFoot, _armorAppearances[appearanceType].Foot[ArmorValueToIndex(LeftFootOptions, LeftFootSelection)], true);

            // Copy the item from the outfit barrel back to the player.
            var updatedItem = CopyItem(item, _target, true);
            DestroyObject(item);
            
            AssignCommand(_target, () => ActionEquipItem(updatedItem, InventorySlotType.Chest));

            DelayCommand(1f, () =>
            {
                IsCopyEnabled = true;
            });

            _skipAdjustArmorPart = false;
        };

        private void UpdateArmorDisplay()
        {
            var helmet = GetItemInSlot(InventorySlotType.Head, _target);
            if (GetIsObjectValid(helmet))
            {
                SetHiddenWhenEquipped(helmet, !ShowHelmet);
            }

            var cloak = GetItemInSlot(InventorySlotType.Cloak, _target);
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
