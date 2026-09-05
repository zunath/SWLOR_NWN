using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public partial class AppearanceEditorViewModel :
        GuiViewModelBase<AppearanceEditorViewModel, AppearanceEditorPayload>,
        IGuiRefreshable<EquipItemRefreshEvent>,
        IGuiRefreshable<UnequipItemRefreshEvent>,
        IGuiRefreshable<AppearanceChangedRefreshEvent>
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
        public const string ArmorColorElement = "ARMOR_COLOR_VIEW";

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
        private IReadOnlyList<TintMapMaterialSelection> _tintMapSelections = Array.Empty<TintMapMaterialSelection>();
        private bool _loadingTintColor;
        private bool _applyingTintColor;
        private int _tintEditGeneration;
        private bool _tintControlBindingsWatched;
        private string _tintComponentCorrection;
        private readonly Dictionary<(uint Source, ColorTarget Part, TintMapLayerType Layer),
            (TintMapColor Requested, TintMapColor Applied, int PaletteId)> _tintInputs = new();

        private const string OutfitBarrelTag = "OUTFIT_BARREL";

        private uint _target;
        private bool _isMetalPalette;

        private AppearanceArmorColor _selectedColorChannel;
        private ColorTarget _colorTarget;

        private const int AppearanceTabId = 0;
        private const int EquipmentTabId = 1;
        private const int SettingsTabId = 2;
        private const int SimpleEquipmentTabId = 3;
        private static readonly GuiTabGroup<AppearanceEditorViewModel, AppearanceEditorPayload> EditorTabs =
            new GuiTabGroup<AppearanceEditorViewModel, AppearanceEditorPayload>()
                .AddTab(AppearanceTabId, EditorMainPartial)
                .AddTab(EquipmentTabId, EditorArmorPartial)
                .AddTab(SettingsTabId, SettingsPartial)
                .AddTab(SimpleEquipmentTabId, EditorMainPartial);
        private static readonly GuiToggleGroupSync EditorToggles = new(AppearanceTabId, EquipmentTabId);
        private static readonly GuiToggleGroupSync SettingsToggles = new(SettingsTabId);
        private int _selectedTabId = AppearanceTabId;
        private int _armorBindingGeneration;
        private bool _armorClientBindingsWatched;
        private int _loadedItemTypeIndex;

        public int EditorTabToggleValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                EditorToggles.HandleClientChange(value, SelectEditorTab);
            }
        }

        public int SettingsTabToggleValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                SettingsToggles.HandleClientChange(value, SelectEditorTab);
            }
        }

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void LoadAppearances()
        {
            LoadRacialAppearances();
            LoadArmorAppearances();
            LoadWeaponAppearances();
        }

        [NWNEventHandler(ScriptName.OnDMPossessBefore)]
        [NWNEventHandler(ScriptName.OnDMPossessFullPowerBefore)]
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
            _weaponAppearances[BaseItem.Dagger] = new DaggerAppearanceDefinition();
            _weaponAppearances[BaseItem.Electroblade] = new ElectrobladeAppearanceDefinition();
            _weaponAppearances[BaseItem.GreatSword] = new GreatSwordAppearanceDefinition();
            _weaponAppearances[BaseItem.Katar] = new KatarAppearanceDefinition();
            _weaponAppearances[BaseItem.LargeShield] = new LargeShieldAppearanceDefinition();
            _weaponAppearances[BaseItem.Longsword] = new LongswordAppearanceDefinition();
            var pistolAppearance = new PistolAppearanceDefinition();
            _weaponAppearances[BaseItem.Pistol] = pistolAppearance;
            _weaponAppearances[BaseItem.Sling] = pistolAppearance;
            _weaponAppearances[BaseItem.LegacyPistol] = pistolAppearance;
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

        public bool IsTintMapAvailable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsCustomTintAvailable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public GuiColor SelectedTintColor
        {
            get => Get<GuiColor>();
            set
            {
                Set(value);
                if (value == null)
                    return;

                if (!_loadingTintColor)
                    _tintEditGeneration++;
                ApplyCustomTintColor(value, synchronizeComponents: true);
            }
        }

        public string CustomTintRed
        {
            get => Get<string>();
            set => SetCustomTintComponent(value, nameof(CustomTintRed));
        }

        public string CustomTintGreen
        {
            get => Get<string>();
            set => SetCustomTintComponent(value, nameof(CustomTintGreen));
        }

        public string CustomTintBlue
        {
            get => Get<string>();
            set => SetCustomTintComponent(value, nameof(CustomTintBlue));
        }

        public string ClosestTintPresetText
        {
            get => Get<string>();
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
                if (_armorClientBindingsWatched && !_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShoulder))
                    AdjustArmorPart(AppearanceArmor.LeftShoulder, 0);
            }
        }

        public int LeftBicepSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_armorClientBindingsWatched && !_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftBicep))
                    AdjustArmorPart(AppearanceArmor.LeftBicep, 0);
            }
        }
        public int LeftForearmSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_armorClientBindingsWatched && !_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftForearm))
                    AdjustArmorPart(AppearanceArmor.LeftForearm, 0);
            }
        }
        public int LeftHandSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_armorClientBindingsWatched && !_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand))
                    AdjustArmorPart(AppearanceArmor.LeftHand, 0);
            }
        }
        public int LeftThighSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_armorClientBindingsWatched && !_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftThigh))
                    AdjustArmorPart(AppearanceArmor.LeftThigh, 0);
            }
        }
        public int LeftShinSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_armorClientBindingsWatched && !_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShin))
                    AdjustArmorPart(AppearanceArmor.LeftShin, 0);
            }
        }
        public int LeftFootSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_armorClientBindingsWatched && !_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftFoot))
                    AdjustArmorPart(AppearanceArmor.LeftFoot, 0);
            }
        }
        public int RightShoulderSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_armorClientBindingsWatched && !_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShoulder))
                    AdjustArmorPart(AppearanceArmor.RightShoulder, 0);
            }
        }
        public int RightBicepSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_armorClientBindingsWatched && !_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightBicep))
                    AdjustArmorPart(AppearanceArmor.RightBicep, 0);
            }
        }
        public int RightForearmSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_armorClientBindingsWatched && !_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightForearm))
                    AdjustArmorPart(AppearanceArmor.RightForearm, 0);
            }
        }
        public int RightHandSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_armorClientBindingsWatched && !_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand))
                    AdjustArmorPart(AppearanceArmor.RightHand, 0);
            }
        }
        public int RightThighSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_armorClientBindingsWatched && !_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightThigh))
                    AdjustArmorPart(AppearanceArmor.RightThigh, 0);
            }
        }
        public int RightShinSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_armorClientBindingsWatched && !_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShin))
                    AdjustArmorPart(AppearanceArmor.RightShin, 0);
            }
        }
        public int RightFootSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_armorClientBindingsWatched && !_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightFoot))
                    AdjustArmorPart(AppearanceArmor.RightFoot, 0);
            }
        }
        public int NeckSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_armorClientBindingsWatched && !_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck))
                    AdjustArmorPart(AppearanceArmor.Neck, 0);
            }
        }
        public int ChestSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_armorClientBindingsWatched && !_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso))
                    AdjustArmorPart(AppearanceArmor.Torso, 0);
            }
        }
        public int BeltSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_armorClientBindingsWatched && !_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Belt))
                    AdjustArmorPart(AppearanceArmor.Belt, 0);
            }
        }
        public int PelvisSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_armorClientBindingsWatched && !_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis))
                    AdjustArmorPart(AppearanceArmor.Pelvis, 0);
            }
        }
        public int RobeSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_armorClientBindingsWatched && !_skipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe))
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
                if (value is < 0 or > 4)
                {
                    Set(_loadedItemTypeIndex);
                    return;
                }
                Set(value);
                // Client hydration updates the binding before calling its property setter.
                // Track the loaded page separately so an echo cannot reset a selected armor part.
                if (value == _loadedItemTypeIndex)
                    return;
                _loadedItemTypeIndex = value;
                SuspendArmorClientWatches();
                if (value == 0)
                {
                    _colorTarget = ColorTarget.Global;
                    _selectedColorChannel = AppearanceArmorColor.Leather1;
                }
                ToggleItemEquippedFlags();
                LoadColorCategoryOptions();
                LoadPartCategoryOptions();
                LoadItemParts();
                if (IsEquipmentSelected && value == 0)
                    UpdateTargetedColor();
                LoadItemTypeEditor();
                LoadTintMapEditor();
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

                if (!_armorAppearances[appearanceType].Neck.Contains(GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck)) ||
                    !_armorAppearances[appearanceType].Torso.Contains(GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso)) ||
                    !_armorAppearances[appearanceType].Belt.Contains(GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Belt)) ||
                    !_armorAppearances[appearanceType].Pelvis.Contains(GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis)) ||

                    !_armorAppearances[appearanceType].Shoulder.Contains(GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShoulder)) ||
                    !_armorAppearances[appearanceType].Bicep.Contains(GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftBicep)) ||
                    !_armorAppearances[appearanceType].Forearm.Contains(GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftForearm)) ||
                    !_armorAppearances[appearanceType].Hand.Contains(GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand)) ||

                    !_armorAppearances[appearanceType].Thigh.Contains(GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftThigh)) ||
                    !_armorAppearances[appearanceType].Shin.Contains(GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShin)) ||
                    !_armorAppearances[appearanceType].Foot.Contains(GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftFoot)) ||

                    !_armorAppearances[appearanceType].Shoulder.Contains(GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShoulder)) ||
                    !_armorAppearances[appearanceType].Bicep.Contains(GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightBicep)) ||
                    !_armorAppearances[appearanceType].Forearm.Contains(GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightForearm)) ||
                    !_armorAppearances[appearanceType].Hand.Contains(GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand)) ||

                    !_armorAppearances[appearanceType].Thigh.Contains(GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightThigh)) ||
                    !_armorAppearances[appearanceType].Shin.Contains(GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShin)) ||
                    !_armorAppearances[appearanceType].Foot.Contains(GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightFoot)) ||

                    !_armorAppearances[appearanceType].Robe.Contains(GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe)))
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
            _tintInputs.Clear();
            _tintComponentCorrection = null;
            ClosestTintPresetText = string.Empty;
            _tintEditGeneration++;
            _armorBindingGeneration++;
            _armorClientBindingsWatched = false;
            _tintControlBindingsWatched = false;
            _target = Player;
            if (GetIsObjectValid(initialPayload.Target))
            {
                _target = initialPayload.Target;
            }

            _colorTarget = ColorTarget.Global;
            _selectedColorChannel = AppearanceArmorColor.Leather1;
            _selectedTabId = AppearanceTabId;
            Set(0, nameof(EditorTabToggleValue));
            Set(-1, nameof(SettingsTabToggleValue));
            Set(0, nameof(SelectedItemTypeIndex));
            _loadedItemTypeIndex = 0;
            RegisterColorMappings();
            IsAppearanceSelected = true;
            IsEquipmentSelected = false;
            IsSettingsSelected = false;
            IsColorPickerVisible = true;
            IsCopyEnabled = true;
            IsCustomTintAvailable = false;
            ToggleItemEquippedFlags();
            LoadColorCategoryOptions();
            LoadPartCategoryOptions();
            SelectedColorCategoryIndex = 0;
            SelectedPartCategoryIndex = 0;
            SelectedPartIndex = 0;
            ColorCategorySelected[0] = true;
            PartCategorySelected[0] = true;
            LoadBodyParts();
            RefreshTintMapAvailability();
            LoadTintMapEditor();
            ShowHelmet = true;
            ShowCloak = true;
            LoadSettings();
            ColorTargetText = string.Empty;
            IsSettingsVisible = GetIsPC(_target) && !GetIsDM(_target) && !GetIsDMPossessed(_target);
            LoadItemTypeEditor();

            WatchOnClient(model => model.EditorTabToggleValue);
            WatchOnClient(model => model.SettingsTabToggleValue);
            WatchOnClient(model => model.SelectedColorCategoryIndex);
            WatchOnClient(model => model.SelectedPartCategoryIndex);
            WatchOnClient(model => model.SelectedPartIndex);
            WatchOnClient(model => model.SelectedItemTypeIndex);
            WatchOnClient(model => model.SelectedTintColor);
            WatchOnClient(model => model.CustomTintRed);
            WatchOnClient(model => model.CustomTintGreen);
            WatchOnClient(model => model.CustomTintBlue);
            _tintControlBindingsWatched = true;

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

        private IReadOnlyList<TintMapMaterialSelection> GetCurrentTintMapSelections()
        {
            return TintMapModelResolver.GetCurrentSelections(_target);
        }

        private void RefreshTintMapAvailability()
        {
            _tintMapSelections = GetCurrentTintMapSelections();
            IsTintMapAvailable = _tintMapSelections.Count > 0;
        }

        private void LoadTintMapEditor()
        {
            // Resetting legacy overrides is part of applying a color. Do not replace the
            // pending input with the old color halfway through that operation.
            if (_applyingTintColor)
                return;
            _tintEditGeneration++;
            _tintComponentCorrection = null;
            RefreshTintMapAvailability();
            if (!TryGetEditableTintSelections(out var selections, out var layerType, out _))
            {
                IsCustomTintAvailable = false;
                ClosestTintPresetText = string.Empty;
                SetSelectedTintColor(GuiColor.Grey);
                return;
            }

            IsCustomTintAvailable = true;
            if (IsEquipmentSelected && SelectedItemTypeIndex == 0)
            {
                // Native armor colors remain editable even when the current model does not
                // expose that channel. Empty part swatches inherit the global default.
                var paletteId = _colorTarget == ColorTarget.Global
                    ? 255
                    : GetArmorSwatchColor(GetItem(), GetArmorModelType(_colorTarget), _selectedColorChannel);
                if (paletteId == 255)
                    paletteId = GetItemAppearance(GetItem(), ItemAppearanceType.ArmorColor, (int)_selectedColorChannel);
                var globalColor = TintMapPaletteColors.GetColor(layerType, paletteId);
                SetLoadedTintColor(globalColor, layerType, paletteId);
                return;
            }
            if (selections.Count == 0)
            {
                var paletteId = IsAppearanceSelected
                    ? GetColor(_target, (ColorChannel)SelectedColorCategoryIndex)
                    : GetItemAppearance(GetItem(), ItemAppearanceType.ArmorColor, SelectedColorCategoryIndex);
                var fallbackColor = TintMapPaletteColors.GetColor(layerType, paletteId);
                SetLoadedTintColor(fallbackColor, layerType, paletteId);
                return;
            }
            var effectiveColors = selections
                .Select(selection => TintMapService.GetEffectiveDisplayColor(
                    _target,
                    selection,
                    layerType))
                .ToList();
            var distinctColors = effectiveColors
                .Distinct()
                .ToList();

            if (distinctColors.Count == 1)
            {
                var color = distinctColors[0];
                var paletteId = IsAppearanceSelected
                    ? GetColor(_target, (ColorChannel)SelectedColorCategoryIndex)
                    : GetItemAppearance(GetItem(), ItemAppearanceType.ArmorColor, SelectedColorCategoryIndex);
                SetLoadedTintColor(color, layerType, paletteId);
                return;
            }

            ClosestTintPresetText = "Multiple colors";
            SetSelectedTintColor(GuiColor.Grey);
        }

        private (uint Source, ColorTarget Part, TintMapLayerType Layer) GetTintInputKey(
            TintMapLayerType layerType,
            bool resolveInheritance = false)
        {
            var source = IsAppearanceSelected ? _target : GetItem();
            var part = IsEquipmentSelected && SelectedItemTypeIndex == 0
                ? _colorTarget
                : ColorTarget.Global;
            if (resolveInheritance && part != ColorTarget.Global &&
                GetArmorSwatchColor(source, GetArmorModelType(part), _selectedColorChannel) == 255)
                part = ColorTarget.Global;
            return (source, part, layerType);
        }

        private void SetLoadedTintColor(TintMapColor color, TintMapLayerType layerType, int paletteId)
        {
            var key = GetTintInputKey(layerType, resolveInheritance: true);
            var inputColor = color;
            if (_tintInputs.TryGetValue(key, out var input))
            {
                // Remember input only for this editor session and while the underlying dye
                // still matches. External edits and replacement items must remain authoritative.
                if (input.Applied == color && input.PaletteId == paletteId)
                    inputColor = input.Requested;
                else
                    _tintInputs.Remove(key);
            }
            SetSelectedTintColor(new GuiColor(inputColor.Red, inputColor.Green, inputColor.Blue));
            // Persisted material tints can override the native palette fallback.
            UpdateClosestTintPreset(layerType, TintMapPaletteColors.GetClosestColorId(layerType, color));
        }

        private void UpdateClosestTintPreset(TintMapLayerType layerType, int paletteId)
        {
            var color = TintMapPaletteColors.GetColor(layerType, paletteId);
            ClosestTintPresetText = $"Closest preset: {color.Red}, {color.Green}, {color.Blue}";
        }

        private void SetSelectedTintColor(
            GuiColor color,
            bool synchronizeComponents = true)
        {
            SynchronizeTintControlBindings(() =>
            {
                _loadingTintColor = true;
                try
                {
                    if (synchronizeComponents)
                        SelectedTintColor = color;
                    else
                        Set(color, nameof(SelectedTintColor));
                }
                finally
                {
                    _loadingTintColor = false;
                }
            });
        }

        private void ApplyCustomTintColor(
            GuiColor value,
            bool synchronizeComponents)
        {
            if (_loadingTintColor)
            {
                if (synchronizeComponents)
                    SynchronizeCustomTintComponents(value);
                return;
            }
            if (!TryGetEditableTintSelections(out _, out var layerType, out _))
                return;

            var requestedColor = new TintMapColor(value.R, value.G, value.B);
            var paletteColorId = TintMapPaletteColors.GetClosestColorId(layerType, requestedColor);
            _applyingTintColor = true;
            try
            {
                if (!ApplySelectedPaletteColor(paletteColorId, reloadEditor: false))
                    return;
            }
            finally
            {
                _applyingTintColor = false;
            }

            // Palette conversion must never become the next RGB input. In particular, a
            // pause after typing R=1 must not replace it (or G/B) with palette midtones.
            var appliedColor = TintMapPaletteColors.GetColor(layerType, paletteColorId);
            _tintInputs[GetTintInputKey(layerType)] = (requestedColor, appliedColor, paletteColorId);
            SetSelectedTintColor(value, synchronizeComponents);
            UpdateClosestTintPreset(layerType, paletteColorId);
        }

        private void SynchronizeCustomTintComponents(GuiColor color)
        {
            SynchronizeTintControlBindings(() =>
            {
                var wasLoading = _loadingTintColor;
                _loadingTintColor = true;
                try
                {
                    Set(color.R.ToString(), nameof(CustomTintRed));
                    Set(color.G.ToString(), nameof(CustomTintGreen));
                    Set(color.B.ToString(), nameof(CustomTintBlue));
                }
                finally
                {
                    _loadingTintColor = wasLoading;
                }
            });
        }

        private void SynchronizeTintControlBindings(Action update)
        {
            var restoreWatches = _tintControlBindingsWatched;
            if (restoreWatches)
                SetTintControlBindingsWatched(false);

            try
            {
                update();
            }
            finally
            {
                if (restoreWatches)
                    SetTintControlBindingsWatched(true);
            }
        }

        private void SetTintControlBindingsWatched(bool watched)
        {
            NuiSetBindWatch(Player, WindowToken, nameof(SelectedTintColor), watched);
            NuiSetBindWatch(Player, WindowToken, nameof(CustomTintRed), watched);
            NuiSetBindWatch(Player, WindowToken, nameof(CustomTintGreen), watched);
            NuiSetBindWatch(Player, WindowToken, nameof(CustomTintBlue), watched);
            _tintControlBindingsWatched = watched;
        }

        private void SetCustomTintComponent(string value, string propertyName)
        {
            var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
            var normalized = int.TryParse(digits, out var component)
                ? component <= byte.MaxValue ? digits : byte.MaxValue.ToString()
                : string.Empty;
            Set(normalized, propertyName);

            if (_loadingTintColor)
                return;

            _tintComponentCorrection = normalized == value ? null : propertyName;

            // Keep the three inputs as a draft while typing; applying each digit used to
            // reload the old color and overwrite the other two channels mid-edit.
            var generation = ++_tintEditGeneration;
            var token = WindowToken;
            DelayCommand(0.4f, () =>
            {
                if (generation == _tintEditGeneration && token == WindowToken &&
                    Gui.IsWindowOpen(Player, WindowType))
                    CommitCustomTintComponents();
            });
        }

        private void CommitCustomTintComponents()
        {
            if (!byte.TryParse(CustomTintRed, out var red) ||
                !byte.TryParse(CustomTintGreen, out var green) ||
                !byte.TryParse(CustomTintBlue, out var blue))
                return;
            _tintEditGeneration++;
            ApplyCustomTintColor(new GuiColor(red, green, blue), synchronizeComponents: false);
        }

        protected override void OnClientPropertyUpdated(string propertyName)
        {
            if (_tintComponentCorrection == propertyName)
            {
                _tintComponentCorrection = null;
                // Correct invalid input after SkipNotify ends. Valid input is already on
                // the client: echoing it (or the other fields) can reset an active edit.
                SynchronizeTintControlBindings(() => OnPropertyChanged(propertyName));
            }
        }

        private bool TryGetEditableTintSelections(
            out IReadOnlyList<TintMapMaterialSelection> selections,
            out TintMapLayerType layerType,
            out TintMapLayerDefinition layer)
        {
            selections = Array.Empty<TintMapMaterialSelection>();
            layerType = default;
            layer = null;

            if (!TryGetSelectedTintLayer(out layerType))
                return false;

            if (IsEquipmentSelected && !IsValidItem())
                return false;

            var paletteSource = IsAppearanceSelected ? _target : GetItem();
            if (!GetIsObjectValid(paletteSource))
                return false;

            var restrictToArmorPart = IsEquipmentSelected &&
                                      SelectedItemTypeIndex == 0 &&
                                      _colorTarget != ColorTarget.Global;
            var armorPart = restrictToArmorPart
                ? GetArmorModelType(_colorTarget)
                : AppearanceArmor.Invalid;
            var selectedLayerType = layerType;
            selections = _tintMapSelections
                .Where(selection =>
                    selection.GetPaletteSource(selectedLayerType) == paletteSource &&
                    selection.Material.Layers.Contains(selectedLayerType) &&
                    (!restrictToArmorPart || selection.ArmorPart == armorPart))
                .ToList();
            // The native channel exists independently of the currently visible material.
            layer = TintMapMaterialRegistry.GetLayer(layerType);
            return true;
        }

        private bool TryGetSelectedTintLayer(out TintMapLayerType layerType)
        {
            layerType = default;
            if (IsAppearanceSelected)
            {
                layerType = SelectedColorCategoryIndex switch
                {
                    0 => TintMapLayerType.Skin,
                    1 => TintMapLayerType.Hair,
                    2 => TintMapLayerType.Tattoo1,
                    3 => TintMapLayerType.Tattoo2,
                    _ => default
                };
                return SelectedColorCategoryIndex is >= 0 and <= 3;
            }

            if (!IsEquipmentSelected)
                return false;

            if (SelectedItemTypeIndex == 0)
            {
                if (_colorTarget == ColorTarget.Invalid)
                    return false;

                layerType = _selectedColorChannel switch
                {
                    AppearanceArmorColor.Leather1 => TintMapLayerType.Leather1,
                    AppearanceArmorColor.Leather2 => TintMapLayerType.Leather2,
                    AppearanceArmorColor.Cloth1 => TintMapLayerType.Cloth1,
                    AppearanceArmorColor.Cloth2 => TintMapLayerType.Cloth2,
                    AppearanceArmorColor.Metal1 => TintMapLayerType.Metal1,
                    AppearanceArmorColor.Metal2 => TintMapLayerType.Metal2,
                    _ => default
                };
                return _selectedColorChannel is >= AppearanceArmorColor.Leather1 and <= AppearanceArmorColor.Metal2;
            }

            if (SelectedItemTypeIndex is not (1 or 2))
                return false;

            layerType = SelectedColorCategoryIndex switch
            {
                0 => TintMapLayerType.Leather1,
                1 => TintMapLayerType.Leather2,
                2 => TintMapLayerType.Cloth1,
                3 => TintMapLayerType.Cloth2,
                4 => TintMapLayerType.Metal1,
                5 => TintMapLayerType.Metal2,
                _ => default
            };
            return SelectedColorCategoryIndex is >= 0 and <= 5;
        }

        private void StartArmorClientWatches()
        {
            if (!Gui.IsWindowOpen(Player, WindowType) || !IsEquipmentSelected ||
                SelectedItemTypeIndex != 0 || !HasItemEquipped)
                return;
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
            _armorClientBindingsWatched = true;
        }

        private void SuspendArmorClientWatches()
        {
            _armorBindingGeneration++;
            if (!_armorClientBindingsWatched)
                return;
            foreach (var target in _colorMappings.Keys.Where(target => target != ColorTarget.Global))
                NuiSetBindWatch(Player, WindowToken, target + "Selection", false);
            _armorClientBindingsWatched = false;
        }

        private void ResumeArmorClientWatches()
        {
            var generation = _armorBindingGeneration;
            // Option-list replacement can transiently select the first client combo entry.
            // Keep that hydration outside the watched editing path, including repeated tab visits.
            DelayCommand(3f, () =>
            {
                if (generation == _armorBindingGeneration)
                    StartArmorClientWatches();
            });
        }

        private void LoadItemTypeEditor()
        {
            SuspendArmorClientWatches();
            var partialTabId = IsSettingsSelected
                ? SettingsTabId
                : IsEquipmentSelected
                    ? SelectedItemTypeIndex == 0 ? EquipmentTabId : SimpleEquipmentTabId
                    : AppearanceTabId;
            EditorTabs.Select(this, MainPartialElement, partialTabId, OnEditorPartialApplied);
        }

        private void OnEditorPartialApplied()
        {
            SuspendArmorClientWatches();
            var isArmorEditor = IsEquipmentSelected && SelectedItemTypeIndex == 0;
            SynchronizeTintControlBindings(() =>
            {
                if (isArmorEditor)
                {
                    IsCopyEnabled = true;
                    RestoreArmorPalette();
                }

                // Tab replacement still needs current bindings after its nested palette.
                // Resizing does not replace controls or pause their watches.
                RepublishBindings();
            });
            if (isArmorEditor)
                ResumeArmorClientWatches();
        }

        private void RestoreArmorPalette()
        {
            if (!Gui.IsWindowOpen(Player, WindowType) || !IsEquipmentSelected || SelectedItemTypeIndex != 0)
                return;
            _isMetalPalette = _selectedColorChannel is AppearanceArmorColor.Metal1 or AppearanceArmorColor.Metal2;
            ChangePartialView(ArmorColorElement, _isMetalPalette ? ArmorColorsMetal : ArmorColorsClothLeather);
        }

        protected override void OnModalClosedRestore()
        {
            EditorToggles.SyncTo(_selectedTabId, value => EditorTabToggleValue = value);
            SettingsToggles.SyncTo(_selectedTabId, value => SettingsTabToggleValue = value);
            LoadItemTypeEditor();
        }

        private void SelectEditorTab(int tabId)
        {
            if (tabId is < AppearanceTabId or > SettingsTabId ||
                tabId == SettingsTabId && !IsSettingsVisible)
                return;
            _selectedTabId = tabId;
            EditorToggles.SyncTo(tabId, value => EditorTabToggleValue = value);
            SettingsToggles.SyncTo(tabId, value => SettingsTabToggleValue = value);
            IsAppearanceSelected = tabId == AppearanceTabId;
            IsEquipmentSelected = tabId == EquipmentTabId;
            IsSettingsSelected = tabId == SettingsTabId;
            _lastModifiedItem = OBJECT_INVALID;
            SuspendArmorClientWatches();

            if (IsSettingsSelected)
            {
                LoadSettings();
                LoadItemTypeEditor();
                return;
            }

            ToggleItemEquippedFlags();
            LoadColorCategoryOptions();
            LoadPartCategoryOptions();
            if (IsAppearanceSelected)
                LoadBodyParts();
            else
            {
                _colorTarget = ColorTarget.Global;
                _selectedColorChannel = AppearanceArmorColor.Leather1;
                LoadItemParts();
                UpdateTargetedColor();
            }
            LoadItemTypeEditor();
            LoadTintMapEditor();
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
            if (!IsEquipmentSelected)
                return;
            SuspendArmorClientWatches();
            var wasSkipping = _skipAdjustArmorPart;
            _skipAdjustArmorPart = true;
            try
            {
                PopulateItemParts();
            }
            finally
            {
                _skipAdjustArmorPart = wasSkipping;
            }
            if (SelectedItemTypeIndex == 0 && HasItemEquipped)
                ResumeArmorClientWatches();
        }

        private void PopulateItemParts()
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
                BeltSelection = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Belt);
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

        public Action OnSelectAppearance() => () => SelectEditorTab(AppearanceTabId);

        public Action OnSelectEquipment() => () => SelectEditorTab(EquipmentTabId);

        public Action OnSelectSettings() => () => SelectEditorTab(SettingsTabId);

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
            LoadTintMapEditor();
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
            var armorPart = modelType == ItemAppearanceType.ArmorModel
                ? (AppearanceArmor)type
                : AppearanceArmor.Invalid;
            var previousTintSelections = GetCurrentTintMapSelections()
                .Where(selection =>
                    selection.PaletteSource == item &&
                    selection.ArmorPart == armorPart)
                .ToList();
            var tintCarry = TintMapService.CaptureItemCustomColors(item, previousTintSelections);
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
            TintMapService.QueueItemCustomColorCarry(
                _target, item, copy, Player, slot, armorPart, tintCarry);

            _lastModifiedItem = copy;
        }

        private int GetSelectedPaletteColorId()
        {
            var scale = GetPlayerDeviceProperty(Player, PlayerDevicePropertyType.GuiScale) / 100.0f;
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

            cellX = Math.Clamp(cellX, 0, ColorWidthCells - 1);
            cellY = Math.Clamp(cellY, 0, ColorHeightCells - 1);

            return Math.Min(cellX + cellY * ColorWidthCells, TintMapMaterialRegistry.PaletteColorCount - 1);
        }

        public Action OnSelectColor() => () =>
        {
            var colorId = GetSelectedPaletteColorId();
            if (ApplySelectedPaletteColor(colorId))
                SynchronizeCustomTintControlsToPaletteColor(colorId);
        };

        private void SynchronizeCustomTintControlsToPaletteColor(int colorId)
        {
            if (!TryGetSelectedTintLayer(out var layerType))
                return;

            var color = TintMapPaletteColors.GetColor(layerType, colorId);
            SetSelectedTintColor(new GuiColor(color.Red, color.Green, color.Blue));
            UpdateClosestTintPreset(layerType, colorId);
        }

        private bool ApplySelectedPaletteColor(int colorId, bool reloadEditor = true)
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return false;

            if (colorId < 0 || colorId >= TintMapMaterialRegistry.PaletteColorCount)
                return false;

            // An explicit preset replaces the remembered input even when it maps to the
            // same native row. Picker/RGB commits store their new input after applying it.
            if (!_applyingTintColor && TryGetSelectedTintLayer(out var layerType))
                _tintInputs.Remove(GetTintInputKey(layerType));

            if (IsEquipmentSelected && SelectedItemTypeIndex == 0)
                return ApplyArmorPaletteColor(colorId);

            if (!IsAppearanceSelected &&
                (!IsEquipmentSelected || (SelectedItemTypeIndex != 1 && SelectedItemTypeIndex != 2)))
            {
                return false;
            }

            ResetCurrentCustomTintOverrides();

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

            TintMapService.ApplyCurrentColors(_target);
            if (reloadEditor)
                LoadTintMapEditor();
            return true;
        }

        private void ResetCurrentCustomTintOverrides(int? paletteColor = null)
        {
            if (!TryGetSelectedTintLayer(out var selectedLayerType))
                return;

            if (!TryGetEditableTintSelections(out var selections, out var layerType, out var layer) || selections.Count == 0)
            {
                // A model can stop exposing a semantic channel (for example, a hairless
                // head). Selecting a preset still means the persisted custom channel is
                // being replaced, even though there is no current material to reset.
                if (IsAppearanceSelected && TintMapVariable.IsCreatureColorLayer(selectedLayerType))
                {
                    TintMapService.ResetCreatureCustomColor(_target, selectedLayerType);
                    LoadTintMapEditor();
                }
                else if (IsEquipmentSelected && SelectedItemTypeIndex is >= 0 and <= 2)
                {
                    var item = GetItem();
                    if (GetIsObjectValid(item))
                    {
                        TintMapService.ResetInactiveItemCustomColor(
                            _target,
                            item,
                            selectedLayerType,
                            SelectedItemTypeIndex != 0 || _colorTarget == ColorTarget.Global
                                ? AppearanceArmor.Invalid
                                : GetArmorModelType(_colorTarget));
                        LoadTintMapEditor();
                    }
                }

                return;
            }

            if (IsEquipmentSelected &&
                SelectedItemTypeIndex == 0 &&
                _colorTarget == ColorTarget.Global)
            {
                if (selections.Count == 0)
                    TintMapService.ResetInactiveItemCustomColor(_target, GetItem(), layerType, AppearanceArmor.Invalid);
                else
                    TintMapService.ResetGlobalItemCustomColor(_target, selections, layerType);
                LoadTintMapEditor();
            }
            else
            {
                ResetCustomTintOverrides(selections, layerType, layer, paletteColor);
            }
        }

        private void ResetCustomTintOverrides(
            IReadOnlyList<TintMapMaterialSelection> selections,
            TintMapLayerType layerType,
            TintMapLayerDefinition layer,
            int? paletteColor = null)
        {
            if (TintMapVariable.IsCreatureColorLayer(layerType))
            {
                TintMapService.ResetCreatureCustomColor(_target, layerType);
            }
            else
            {
                foreach (var selection in selections)
                {
                    TintMapService.ResetColor(_target, selection, layerType, paletteColor);
                }
            }

            LoadTintMapEditor();
        }

        private void ModifyHelmetCloakColor(AppearanceArmorColor colorChannel, int colorId)
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            var item = GetItem();
            SetItemColorInPlace(item, (int)colorChannel, colorId);
        }

        private void SetItemColorInPlace(uint item, int colorIndex, int colorId)
        {
            if (!GetIsObjectValid(item))
                return;

            ItemPlugin.SetItemAppearance(
                item,
                ItemAppearanceType.ArmorColor,
                colorIndex,
                colorId,
                updateCreatureAppearance: true);
            Droid.UpdateEquippedItemSnapshot(_target, item);
            TintMapService.ApplyCurrentColors(_target);
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

            TintMapService.CarryStoredCreatureCustomColors(_target);
            ExecuteScript(ScriptName.OnAppearanceEdit, _target);
            LoadTintMapEditor();
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
            _tintEditGeneration++;
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
            if (ApplySelectedPaletteColor(colorId))
                SynchronizeCustomTintControlsToPaletteColor(colorId);
        };

        private bool ApplyArmorPaletteColor(int colorId)
        {
            if (_colorTarget == ColorTarget.Invalid)
                return false;

            ResetCurrentCustomTintOverrides(colorId);
            var item = GetItem();
            if (!GetIsObjectValid(item))
                return false;

            int colorIndex;

            if (_colorTarget == ColorTarget.Global)
            {
                colorIndex = (int)_selectedColorChannel;
            }
            else
            {
                var armorModel = GetArmorModelType(_colorTarget);
                colorIndex = ArmorColorIndexCalculator.CalculatePerPart(armorModel, _selectedColorChannel);
                SetLocalInt(
                    item,
                    ArmorColorIndexCalculator.GetPerPartOverrideVariableName(
                        armorModel,
                        _selectedColorChannel),
                    1);
            }

            SetItemColorInPlace(item, colorIndex, colorId);
            ChangeColor(_colorTarget, _selectedColorChannel, colorId);
            return true;
        }

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

            ResetArmorColorToInheritance(colorTarget, colorChannel);
        };

        private void ResetArmorColorToInheritance(ColorTarget colorTarget, AppearanceArmorColor colorChannel)
        {
            ResetCustomTintOverrides(colorTarget, colorChannel);

            var item = GetItem();
            var armorModel = GetArmorModelType(colorTarget);
            var index = ArmorColorIndexCalculator.CalculatePerPart(armorModel, colorChannel);
            DeleteLocalInt(
                item,
                ArmorColorIndexCalculator.GetPerPartOverrideVariableName(armorModel, colorChannel));

            SetItemColorInPlace(item, index, 255);
            ChangeColor(colorTarget, colorChannel, 255);
            if (_colorTarget == colorTarget && _selectedColorChannel == colorChannel)
                LoadTintMapEditor();
        }

        private void ResetCustomTintOverrides(
            ColorTarget colorTarget,
            AppearanceArmorColor colorChannel)
        {
            if (!TryGetArmorTintLayer(colorChannel, out var layerType))
                return;

            var item = GetItem();
            if (!GetIsObjectValid(item))
                return;

            _tintInputs.Remove((item, colorTarget, layerType));

            var armorPart = colorTarget == ColorTarget.Global
                ? AppearanceArmor.Invalid
                : GetArmorModelType(colorTarget);
            var selections = _tintMapSelections
                .Where(selection =>
                    selection.GetPaletteSource(layerType) == item &&
                    selection.Material.Layers.Contains(layerType) &&
                    (armorPart == AppearanceArmor.Invalid || selection.ArmorPart == armorPart))
                .ToList();
            if (selections.Count == 0)
            {
                TintMapService.ResetInactiveItemCustomColor(
                    _target,
                    item,
                    layerType,
                    armorPart);
                return;
            }

            foreach (var selection in selections)
            {
                TintMapService.ResetColorToInheritance(_target, selection, layerType);
            }
        }

        private static bool TryGetArmorTintLayer(
            AppearanceArmorColor colorChannel,
            out TintMapLayerType layerType)
        {
            layerType = colorChannel switch
            {
                AppearanceArmorColor.Leather1 => TintMapLayerType.Leather1,
                AppearanceArmorColor.Leather2 => TintMapLayerType.Leather2,
                AppearanceArmorColor.Cloth1 => TintMapLayerType.Cloth1,
                AppearanceArmorColor.Cloth2 => TintMapLayerType.Cloth2,
                AppearanceArmorColor.Metal1 => TintMapLayerType.Metal1,
                AppearanceArmorColor.Metal2 => TintMapLayerType.Metal2,
                _ => default
            };
            return colorChannel is >= AppearanceArmorColor.Leather1 and <= AppearanceArmorColor.Metal2;
        }

        private int GetArmorSwatchColor(
            uint item,
            AppearanceArmor armorPart,
            AppearanceArmorColor colorChannel)
        {
            var perPartColorIndex = ArmorColorIndexCalculator.CalculatePerPart(armorPart, colorChannel);
            var perPartColor = GetItemAppearance(
                item,
                ItemAppearanceType.ArmorColor,
                perPartColorIndex);
            var hasExplicitOverride = GetLocalInt(
                item,
                ArmorColorIndexCalculator.GetPerPartOverrideVariableName(
                    armorPart,
                    colorChannel)) > 0;
            var baseline = armorPart == AppearanceArmor.Robe
                ? GetLocalInt(item, TintMapNativePaletteProjection.BaselineName(perPartColorIndex)) : 0;
            var lastApplied = armorPart == AppearanceArmor.Robe
                ? GetLocalInt(item, TintMapNativePaletteProjection.LastAppliedName(perPartColorIndex)) : 0;
            int? materialColor = null;
            if (TryGetArmorTintLayer(colorChannel, out var layer))
            {
                foreach (var selection in _tintMapSelections.Where(selection =>
                             selection.ArmorPart == armorPart && selection.GetPaletteSource(layer) == item &&
                             selection.Material.Layers.Contains(layer)))
                {
                    var savedColor = GetLocalInt(item, TintMapVariable.GetName(selection.Material.Resref, layer));
                    if (TintMapColor.TryFromStoredValue(savedColor, out var color))
                        materialColor = TintMapPaletteColors.GetClosestColorId(layer, color);
                    else if (savedColor is > 0 and <= TintMapMaterialRegistry.PaletteColorCount)
                        materialColor = savedColor - 1;
                    if (materialColor.HasValue)
                        break;
                }
            }
            return ResolveArmorSwatchColorId(perPartColor, hasExplicitOverride, baseline, lastApplied, materialColor);
        }

        private static int ResolveArmorSwatchColorId(
            int nativeColor, bool hasExplicitOverride, int baseline, int lastApplied, int? materialColor)
        {
            if (materialColor.HasValue)
                return materialColor.Value;
            var authoredColor = TintMapNativePaletteProjection.GetBaseline(nativeColor, baseline, lastApplied);
            return ArmorColorIndexCalculator.ShouldUsePerPartColor(authoredColor, hasExplicitOverride)
                ? authoredColor : 255;
        }

        private static void MarkPerPartColorOverride(
            uint item,
            AppearanceArmor armorPart,
            AppearanceArmorColor colorChannel)
        {
            SetLocalInt(
                item,
                ArmorColorIndexCalculator.GetPerPartOverrideVariableName(
                    armorPart,
                    colorChannel),
                1);
        }

        private static void ClearPerPartColorOverride(
            uint item,
            AppearanceArmor armorPart,
            AppearanceArmorColor colorChannel)
        {
            DeleteLocalInt(
                item,
                ArmorColorIndexCalculator.GetPerPartOverrideVariableName(
                    armorPart,
                    colorChannel));
        }

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

        public Action OnClickColorTarget(ColorTarget target, AppearanceArmorColor channel) => () =>
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            _colorTarget = target;
            _selectedColorChannel = channel;

            UpdateTargetedColor();

            // We only swap the palette if we're moving from a Cloth/Leather palette to a Metal palette or vice-versa.
            // This (slightly) works around a NUI issue where the palette will disappear when switching between Cloth/Leather and Metal.
            if ((channel == AppearanceArmorColor.Metal1 || channel == AppearanceArmorColor.Metal2) && !_isMetalPalette)
            {
                ChangePartialView(ArmorColorElement, ArmorColorsMetal);
                _isMetalPalette = true;
            }
            else if(_isMetalPalette && channel != AppearanceArmorColor.Metal1 && channel != AppearanceArmorColor.Metal2)
            {
                ChangePartialView(ArmorColorElement, ArmorColorsClothLeather);
                _isMetalPalette = false;
            }

            LoadTintMapEditor();
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
                colorId = GetArmorSwatchColor(item, armorModel, colorChannel);
            }
            return BuildPaletteRegion(colorId);
        }

        private void UpdateAllColors()
        {
            RefreshTintMapAvailability();
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

            ColorTargetText = $"{targetName} / {channelName}";
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

            foreach (var colorChannel in new[]
                     {
                         AppearanceArmorColor.Cloth1,
                         AppearanceArmorColor.Cloth2,
                         AppearanceArmorColor.Leather1,
                         AppearanceArmorColor.Leather2,
                         AppearanceArmorColor.Metal1,
                         AppearanceArmorColor.Metal2
                     })
            {
                CopyColor(ref item, copyToTarget, copyFrom, copyTo, colorChannel);
            }

            TintMapModelResolver.CopyArmorPartTintOverrides(
                _target,
                item,
                copyFrom,
                copyTo);
        }

        private void CopyColor(
            ref uint item,
            ColorTarget copyToTarget,
            AppearanceArmor copyFrom,
            AppearanceArmor copyTo,
            AppearanceArmorColor colorChannel)
        {
            if (!GetBaseItemFitsInInventory(BaseItem.Armor, _target))
                return;

            var copyFromIndex = ArmorColorIndexCalculator.CalculatePerPart(copyFrom, colorChannel);
            var sourceColor = GetItemAppearance(item, ItemAppearanceType.ArmorColor, copyFromIndex);
            var sourceHasExplicitOverride = GetLocalInt(
                item,
                ArmorColorIndexCalculator.GetPerPartOverrideVariableName(copyFrom, colorChannel)) > 0;
            var sourceUsesPerPartColor = ArmorColorIndexCalculator.ShouldUsePerPartColor(
                sourceColor,
                sourceHasExplicitOverride);
            var copyToIndex = ArmorColorIndexCalculator.CalculatePerPart(copyTo, colorChannel);

            ChangeColor(copyToTarget, colorChannel, sourceUsesPerPartColor ? sourceColor : 255);
            item = CopyItemAndModify(
                item,
                ItemAppearanceType.ArmorColor,
                copyToIndex,
                sourceUsesPerPartColor ? sourceColor : 255,
                true);
            if (sourceUsesPerPartColor)
                MarkPerPartColorOverride(item, copyTo, colorChannel);
            else
                ClearPerPartColorOverride(item, copyTo, colorChannel);
            DestroyObject(item);
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
            RefreshTintMapEditorAfterAppearanceChange();
        }

        public void Refresh(UnequipItemRefreshEvent payload)
        {
            _lastModifiedItem = OBJECT_INVALID;
            RefreshTintMapEditorAfterAppearanceChange();
        }

        public void Refresh(AppearanceChangedRefreshEvent payload)
        {
            _lastModifiedItem = OBJECT_INVALID;
            RefreshTintMapEditorAfterAppearanceChange();
        }

        private void RefreshTintMapEditorAfterAppearanceChange()
        {
            ToggleItemEquippedFlags();
            if (IsAppearanceSelected || IsEquipmentSelected)
                LoadTintMapEditor();
            else
                RefreshTintMapAvailability();
            if (IsEquipmentSelected && SelectedItemTypeIndex == 0 && HasItemEquipped)
            {
                UpdateAllColors();
                UpdateTargetedColor();
            }
        }
    }
}
