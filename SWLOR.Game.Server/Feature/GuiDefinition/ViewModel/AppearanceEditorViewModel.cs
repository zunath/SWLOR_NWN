using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceEditing;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.Species;
using SWLOR.Game.Server.Feature.AppearanceDefinition.Preview;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class AppearanceEditorViewModel :
        GuiViewModelBase<AppearanceEditorViewModel, AppearanceEditorPayload>,
        IGuiRefreshable<EquipItemRefreshEvent>,
        IGuiRefreshable<UnequipItemRefreshEvent>,
        IGuiRefreshable<AppearanceChangedRefreshEvent>,
        IGuiRefreshable<ArmorDisplayChangedRefreshEvent>,
        IAppearanceEditorHost
    {
        private AppearanceColorService AppearanceColorService { get; }
        private AppearanceOutfitService AppearanceOutfitService { get; }
        private AppearancePartBrowserService AppearancePartBrowserService { get; }
        private AppearancePartEditService AppearancePartEditService { get; }
        private AppearancePartListService AppearancePartListService { get; }
        private AppearanceSettingsService AppearanceSettingsService { get; }
        private AppearanceSwatchService AppearanceSwatchService { get; }
        private AppearanceTintEditorService AppearanceTintEditorService { get; }
        private IGuiService Gui { get; }

        public AppearanceEditorViewModel(AppearanceColorService appearanceColorService, AppearanceOutfitService appearanceOutfitService, AppearancePartBrowserService appearancePartBrowserService, AppearancePartEditService appearancePartEditService, AppearancePartListService appearancePartListService, AppearanceSettingsService appearanceSettingsService, AppearanceSwatchService appearanceSwatchService, AppearanceTintEditorService appearanceTintEditorService, IGuiService gui)
        {
            AppearanceColorService = appearanceColorService;
            AppearanceOutfitService = appearanceOutfitService;
            AppearancePartBrowserService = appearancePartBrowserService;
            AppearancePartEditService = appearancePartEditService;
            AppearancePartListService = appearancePartListService;
            AppearanceSettingsService = appearanceSettingsService;
            AppearanceSwatchService = appearanceSwatchService;
            AppearanceTintEditorService = appearanceTintEditorService;
            Gui = gui;
        }

        public const string MainPartialElement = "MAIN_PARTIAL_VIEW";

        public const string EditorMainPartial = "APPEARANCE_EDITOR_MAIN_PARTIAL";
        public const string EditorWeaponPartial = "APPEARANCE_EDITOR_WEAPON_PARTIAL";
        public const string EditorArmorPartial = "APPEARANCE_EDITOR_ARMOR_PARTIAL";
        public const string SettingsPartial = "SETTINGS_PARTIAL";

        private readonly AppearanceEditorSession _session = new();
        private const int AppearanceTabId = 0;
        private const int EquipmentTabId = 1;
        private const int SettingsTabId = 2;
        private const int SimpleEquipmentTabId = 3;
        private const int WeaponEquipmentTabId = 4;
        private const int OutfitsTabId = 5;
        private static readonly GuiTabGroup<AppearanceEditorViewModel, AppearanceEditorPayload> EditorTabs =
            new GuiTabGroup<AppearanceEditorViewModel, AppearanceEditorPayload>()
                .AddTab(AppearanceTabId, EditorMainPartial)
                .AddTab(EquipmentTabId, EditorArmorPartial)
                .AddTab(SettingsTabId, SettingsPartial)
                .AddTab(SimpleEquipmentTabId, EditorArmorPartial)
                .AddTab(WeaponEquipmentTabId, EditorWeaponPartial)
                .AddTab(OutfitsTabId, OutfitsPartial);
        private static readonly GuiToggleGroupSync EditorToggles = new(AppearanceTabId, EquipmentTabId);
        private static readonly GuiToggleGroupSync SettingsToggles = new(SettingsTabId);
        private static readonly GuiToggleGroupSync OutfitsToggles = new(OutfitsTabId);

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
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void LoadAppearances()
        {
            RacialAppearanceRegistry.EnsureLoaded();
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

        public bool IsCustomTintEditable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string CustomTintTooltip
        {
            get => Get<string>();
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

                if (_session.LoadingTintColor)
                {
                    AppearanceTintEditorService.SynchronizeCustomTintComponents(this, value);
                    return;
                }

                if (!AppearanceTintEditorService.QueueTintPickerColor(this, value))
                {
                    DelayCommand(0.1f, AppearanceTintEditorService.StageTintPickerColor(this, value));
                    return;
                }
                if (_session.PickerFlushScheduled)
                    return;
                _session.PickerFlushScheduled = true;
                var token = WindowToken;
                DelayCommand(0.1f, () =>
                {
                    _session.PickerFlushScheduled = false;
                    if (token == WindowToken && Gui.IsWindowOpen(Player, WindowType))
                        AppearanceTintEditorService.FlushPendingPickerColor(this);
                });
            }
        }

        public string CustomTintRed
        {
            get => Get<string>();
            set => AppearanceTintEditorService.SetCustomTintComponent(this, value, nameof(CustomTintRed));
        }

        public string CustomTintGreen
        {
            get => Get<string>();
            set => AppearanceTintEditorService.SetCustomTintComponent(this, value, nameof(CustomTintGreen));
        }

        public string CustomTintBlue
        {
            get => Get<string>();
            set => AppearanceTintEditorService.SetCustomTintComponent(this, value, nameof(CustomTintBlue));
        }

        public bool IsSettingsVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsPlayerSettingsVisible
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

        public bool IsArmorDisplayVisible
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
                AppearancePartEditService.ArmorSelectionChanged(this, AppearanceArmor.LeftShoulder, value);
            }
        }

        public int LeftBicepSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                AppearancePartEditService.ArmorSelectionChanged(this, AppearanceArmor.LeftBicep, value);
            }
        }
        public int LeftForearmSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                AppearancePartEditService.ArmorSelectionChanged(this, AppearanceArmor.LeftForearm, value);
            }
        }
        public int LeftHandSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                AppearancePartEditService.ArmorSelectionChanged(this, AppearanceArmor.LeftHand, value);
            }
        }
        public int LeftThighSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                AppearancePartEditService.ArmorSelectionChanged(this, AppearanceArmor.LeftThigh, value);
            }
        }
        public int LeftShinSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                AppearancePartEditService.ArmorSelectionChanged(this, AppearanceArmor.LeftShin, value);
            }
        }
        public int LeftFootSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                AppearancePartEditService.ArmorSelectionChanged(this, AppearanceArmor.LeftFoot, value);
            }
        }
        public int RightShoulderSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                AppearancePartEditService.ArmorSelectionChanged(this, AppearanceArmor.RightShoulder, value);
            }
        }
        public int RightBicepSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                AppearancePartEditService.ArmorSelectionChanged(this, AppearanceArmor.RightBicep, value);
            }
        }
        public int RightForearmSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                AppearancePartEditService.ArmorSelectionChanged(this, AppearanceArmor.RightForearm, value);
            }
        }
        public int RightHandSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                AppearancePartEditService.ArmorSelectionChanged(this, AppearanceArmor.RightHand, value);
            }
        }
        public int RightThighSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                AppearancePartEditService.ArmorSelectionChanged(this, AppearanceArmor.RightThigh, value);
            }
        }
        public int RightShinSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                AppearancePartEditService.ArmorSelectionChanged(this, AppearanceArmor.RightShin, value);
            }
        }
        public int RightFootSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                AppearancePartEditService.ArmorSelectionChanged(this, AppearanceArmor.RightFoot, value);
            }
        }
        public int NeckSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                AppearancePartEditService.ArmorSelectionChanged(this, AppearanceArmor.Neck, value);
            }
        }
        public int ChestSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                AppearancePartEditService.ArmorSelectionChanged(this, AppearanceArmor.Torso, value);
            }
        }
        public int BeltSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                AppearancePartEditService.ArmorSelectionChanged(this, AppearanceArmor.Belt, value);
            }
        }
        public int PelvisSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                AppearancePartEditService.ArmorSelectionChanged(this, AppearanceArmor.Pelvis, value);
            }
        }
        public int RobeSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                AppearancePartEditService.ArmorSelectionChanged(this, AppearanceArmor.Robe, value);
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

                AppearanceSwatchService.UpdateColorSheet(this, value);
            }
        }

        // Equipment slot tabs, in tab order. Helmets and cloaks are chosen on the armor body map.
        public const int ArmorEquipmentValue = 0;
        public const int MainHandEquipmentValue = 1;
        public const int OffHandEquipmentValue = 2;
        private const int MainHandItemType = 3;
        private const int OffHandItemType = 4;

        /// <summary>Equipment slot tab: armor (also while its helmet or cloak is edited) or a weapon hand.</summary>
        public int EquipmentTypeValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                var itemType = ToItemTypeIndex(value);
                // Armor already covers its helmet and cloak; only a weapon or a weapon-to-armor change loads a page.
                if (itemType == 0 && SelectedItemTypeIndex is 0 or 1 or 2)
                    return;
                if (SelectedItemTypeIndex != itemType)
                    SelectedItemTypeIndex = itemType;
            }
        }

        public static int ToItemTypeIndex(int equipmentValue) => equipmentValue switch
        {
            MainHandEquipmentValue => MainHandItemType,
            OffHandEquipmentValue => OffHandItemType,
            _ => 0
        };

        public static int ToEquipmentTypeValue(int itemTypeIndex) => itemTypeIndex switch
        {
            MainHandItemType => MainHandEquipmentValue,
            OffHandItemType => OffHandEquipmentValue,
            _ => ArmorEquipmentValue
        };

        public int SelectedItemTypeIndex
        {
            get => Get<int>();
            set
            {
                if (value is < 0 or > 4)
                {
                    Set(_session.LoadedItemTypeIndex);
                    return;
                }
                Set(value);
                this.SetIfChanged(nameof(EquipmentTypeValue), ToEquipmentTypeValue(value));
                // Client hydration updates the binding before calling its property setter.
                // Track the loaded page separately so an echo cannot reset a selected armor part.
                if (value == _session.LoadedItemTypeIndex)
                    return;
                _session.LoadedItemTypeIndex = value;
                AppearanceTintEditorService.SuspendArmorClientWatches(this);
                if (value is 0 or 1 or 2)
                {
                    // Armor, helmet and cloak all start on Base Leather 1, the first colour category.
                    _session.ColorTarget = AppearanceColorTarget.Global;
                    _session.SelectedColorChannel = AppearanceArmorColor.Leather1;
                }
                ToggleItemEquippedFlags();
                // Helmets, cloaks and weapons have no armor parts to colour.
                if (value != 0)
                    this.SetIfChanged(nameof(IsPartColorsVisible), false);
                AppearancePartListService.LoadColorCategoryOptions(this);
                AppearancePartListService.LoadPartCategoryOptions(this);
                LoadItemParts();
                if (IsEquipmentSelected && value == 0)
                    AppearanceSwatchService.UpdateTargetedColor(this);
                else if (IsEquipmentSelected && value is 1 or 2)
                    AppearanceSwatchService.UpdateColorSelection(this);
                LoadItemTypeEditor();
                AppearanceTintEditorService.LoadTintMapEditor(this);
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
        // Palette cell geometry shared with the definition; the outline overlay is drawn in these units.
        public const float PaletteHeaderWidth = 20f;
        public const float PaletteHeaderHeight = 16f;
        public const float PaletteCellWidth = 20f;
        public const float PaletteCellHeight = 16f;

        // ---------------- selected part bindings ----------------
        public string PartPreviewImage { get => Get<string>(); set => Set(value); }
        public string PartPreviewLabel { get => Get<string>(); set => Set(value); }
        /// <summary>The worn part's number, drawn in the corner of its picture.</summary>
        public string PartPreviewNumber { get => Get<string>(); set => Set(value); }
        public bool IsPartPreviewEnabled { get => Get<bool>(); set => Set(value); }
        public string PreviewViewText { get => Get<string>(); set => Set(value); }

        /// <summary>Weapon and shield parts ship only a front picture, so they cannot turn.</summary>
        public bool IsPreviewViewChoiceVisible { get => Get<bool>(); set => Set(value); }

        // ---------------- body map bindings ----------------
        public string MapHeadImage { get => Get<string>(); set => Set(value); }
        public string MapNeckImage { get => Get<string>(); set => Set(value); }
        public string MapChestImage { get => Get<string>(); set => Set(value); }
        public string MapBeltImage { get => Get<string>(); set => Set(value); }
        public string MapPelvisImage { get => Get<string>(); set => Set(value); }
        public string MapRobeImage { get => Get<string>(); set => Set(value); }
        public string MapCloakImage { get => Get<string>(); set => Set(value); }
        public string MapRightShoulderImage { get => Get<string>(); set => Set(value); }
        public string MapLeftShoulderImage { get => Get<string>(); set => Set(value); }
        public string MapRightBicepImage { get => Get<string>(); set => Set(value); }
        public string MapLeftBicepImage { get => Get<string>(); set => Set(value); }
        public string MapRightForearmImage { get => Get<string>(); set => Set(value); }
        public string MapLeftForearmImage { get => Get<string>(); set => Set(value); }
        public string MapRightHandImage { get => Get<string>(); set => Set(value); }
        public string MapLeftHandImage { get => Get<string>(); set => Set(value); }
        public string MapRightThighImage { get => Get<string>(); set => Set(value); }
        public string MapLeftThighImage { get => Get<string>(); set => Set(value); }
        public string MapRightShinImage { get => Get<string>(); set => Set(value); }
        public string MapLeftShinImage { get => Get<string>(); set => Set(value); }
        public string MapRightFootImage { get => Get<string>(); set => Set(value); }
        public string MapLeftFootImage { get => Get<string>(); set => Set(value); }
        public bool MapHeadSelected { get => Get<bool>(); set => Set(value); }
        public bool MapNeckSelected { get => Get<bool>(); set => Set(value); }
        public bool MapChestSelected { get => Get<bool>(); set => Set(value); }
        public bool MapBeltSelected { get => Get<bool>(); set => Set(value); }
        public bool MapPelvisSelected { get => Get<bool>(); set => Set(value); }
        public bool MapRobeSelected { get => Get<bool>(); set => Set(value); }
        public bool MapCloakSelected { get => Get<bool>(); set => Set(value); }
        public bool MapRightShoulderSelected { get => Get<bool>(); set => Set(value); }
        public bool MapLeftShoulderSelected { get => Get<bool>(); set => Set(value); }
        public bool MapRightBicepSelected { get => Get<bool>(); set => Set(value); }
        public bool MapLeftBicepSelected { get => Get<bool>(); set => Set(value); }
        public bool MapRightForearmSelected { get => Get<bool>(); set => Set(value); }
        public bool MapLeftForearmSelected { get => Get<bool>(); set => Set(value); }
        public bool MapRightHandSelected { get => Get<bool>(); set => Set(value); }
        public bool MapLeftHandSelected { get => Get<bool>(); set => Set(value); }
        public bool MapRightThighSelected { get => Get<bool>(); set => Set(value); }
        public bool MapLeftThighSelected { get => Get<bool>(); set => Set(value); }
        public bool MapRightShinSelected { get => Get<bool>(); set => Set(value); }
        public bool MapLeftShinSelected { get => Get<bool>(); set => Set(value); }
        public bool MapRightFootSelected { get => Get<bool>(); set => Set(value); }
        public bool MapLeftFootSelected { get => Get<bool>(); set => Set(value); }

        // ---------------- colour indicator bindings ----------------

        public GuiRectangle SelectedPartLeather1Region { get => Get<GuiRectangle>(); set => Set(value); }
        public GuiRectangle SelectedPartLeather2Region { get => Get<GuiRectangle>(); set => Set(value); }
        public GuiRectangle SelectedPartCloth1Region { get => Get<GuiRectangle>(); set => Set(value); }
        public GuiRectangle SelectedPartCloth2Region { get => Get<GuiRectangle>(); set => Set(value); }
        public GuiRectangle SelectedPartMetal1Region { get => Get<GuiRectangle>(); set => Set(value); }
        public GuiRectangle SelectedPartMetal2Region { get => Get<GuiRectangle>(); set => Set(value); }
        public GuiColor SelectedPartLeather1Tint { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor SelectedPartLeather2Tint { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor SelectedPartCloth1Tint { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor SelectedPartCloth2Tint { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor SelectedPartMetal1Tint { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor SelectedPartMetal2Tint { get => Get<GuiColor>(); set => Set(value); }
        public bool SelectedPartLeather1Custom { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartLeather2Custom { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartCloth1Custom { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartCloth2Custom { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartMetal1Custom { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartMetal2Custom { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartLeather1Selected { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartLeather2Selected { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartCloth1Selected { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartCloth2Selected { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartMetal1Selected { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartMetal2Selected { get => Get<bool>(); set => Set(value); }

        // Palette code ("P4") or "RGB" over each part swatch.
        public string SelectedPartLeather1Code { get => Get<string>(); set => Set(value); }
        public string SelectedPartLeather2Code { get => Get<string>(); set => Set(value); }
        public string SelectedPartCloth1Code { get => Get<string>(); set => Set(value); }
        public string SelectedPartCloth2Code { get => Get<string>(); set => Set(value); }
        public string SelectedPartMetal1Code { get => Get<string>(); set => Set(value); }
        public string SelectedPartMetal2Code { get => Get<string>(); set => Set(value); }
        public bool IsPartColorsVisible { get => Get<bool>(); set => Set(value); }

        public GuiRectangle BodySkinRegion { get => Get<GuiRectangle>(); set => Set(value); }
        public GuiRectangle BodyHairRegion { get => Get<GuiRectangle>(); set => Set(value); }
        public GuiRectangle BodyTattoo1Region { get => Get<GuiRectangle>(); set => Set(value); }
        public GuiRectangle BodyTattoo2Region { get => Get<GuiRectangle>(); set => Set(value); }
        public GuiColor BodySkinTint { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor BodyHairTint { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor BodyTattoo1Tint { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor BodyTattoo2Tint { get => Get<GuiColor>(); set => Set(value); }
        public bool BodySkinCustom { get => Get<bool>(); set => Set(value); }
        public bool BodyHairCustom { get => Get<bool>(); set => Set(value); }
        public bool BodyTattoo1Custom { get => Get<bool>(); set => Set(value); }
        public bool BodyTattoo2Custom { get => Get<bool>(); set => Set(value); }
        public bool BodySkinSelected { get => Get<bool>(); set => Set(value); }
        public bool BodyHairSelected { get => Get<bool>(); set => Set(value); }
        public bool BodyTattoo1Selected { get => Get<bool>(); set => Set(value); }
        public bool BodyTattoo2Selected { get => Get<bool>(); set => Set(value); }

        public GuiBindingList<float> PaletteSelectionPoints { get => Get<GuiBindingList<float>>(); set => Set(value); }
        public bool IsPaletteSelectionVisible { get => Get<bool>(); set => Set(value); }

        public const string OutfitsPartial = "OUTFITS_PARTIAL";

        public int OutfitsTabToggleValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                OutfitsToggles.HandleClientChange(value, SelectEditorTab);
            }
        }

        public bool IsOutfitsSelected { get => Get<bool>(); set => Set(value); }

        /// <summary>Outfits belong to the player, so they appear only while players edit themselves.</summary>
        public bool IsOutfitsVisible { get => Get<bool>(); set => Set(value); }

        public GuiBindingList<string> OutfitSlotNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> OutfitSlotToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public string WardrobeCapacityText { get => Get<string>(); set => Set(value); }
        public bool IsOutfitSelected { get => Get<bool>(); set => Set(value); }
        public string OutfitDetails { get => Get<string>(); set => Set(value); }
        public bool IsOutfitRenameEnabled { get => Get<bool>(); set => Set(value); }
        public bool IsOutfitWearEnabled { get => Get<bool>(); set => Set(value); }
        public bool IsUndoOutfitEnabled { get => Get<bool>(); set => Set(value); }

        public string OutfitName
        {
            get => Get<string>();
            set
            {
                Set(value);
                IsOutfitRenameEnabled = _session.SelectedOutfitIndex > -1;
            }
        }

        public GuiBindingList<GuiComboEntry> QuickOutfitOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int QuickOutfitValue { get => Get<int>(); set => Set(value); }
        public bool IsQuickWearEnabled { get => Get<bool>(); set => Set(value); }

        private bool IsPartEditorHidden => IsSettingsSelected || IsOutfitsSelected;

        private bool IsValidItem() => AppearancePartListService.IsValidItem(this);
        private void ToggleItemEquippedFlags()
        {
            var hasItemEquipped = IsValidItem();
            HasItemEquipped = hasItemEquipped;
            DoesNotHaveItemEquipped = !hasItemEquipped;
            IsNoEditableItemVisible = !hasItemEquipped && IsEquipmentSelected;
            // An empty slot has no parts: the browser must not keep showing the last item's.
            if (!hasItemEquipped)
                AppearancePartBrowserService.ClearBrowser(this);
        }

        /// <summary>The slot tabs' "Nothing to edit" note: on the Equipment tab with no editable item.</summary>
        public bool IsNoEditableItemVisible { get => Get<bool>(); set => Set(value); }
        protected override void Initialize(AppearanceEditorPayload initialPayload)
        {
            AppearanceTintEditorService.CancelTintPickerGesture(this);
            _session.PendingPickerColor = null;
            _session.PendingPickerApply = null;
            _session.PickerFlushScheduled = false;
            _session.HasTintComponentDraft = false;
            _session.TintComponentApply = null;
            _session.TintComponentCorrection = null;
            _session.TintEditGeneration++;
            _session.ArmorBindingGeneration++;
            _session.ArmorClientBindingsWatched = false;
            _session.TintControlBindingsWatched = false;
            _session.Target = Player;
            if (GetIsObjectValid(initialPayload.Target))
            {
                _session.Target = initialPayload.Target;
            }

            if (!RacialAppearanceRegistry.TryGet(GetAppearanceType(_session.Target), out _))
            {
                AppearanceSettingsService.CloseForUnsupportedAppearance(this);
                return;
            }

            _session.ColorTarget = AppearanceColorTarget.Global;
            _session.SelectedColorChannel = AppearanceArmorColor.Leather1;
            _session.SelectedTabId = AppearanceTabId;
            AppearancePartBrowserService.FitWindowToPlayerScreen(this);
            Set(0, nameof(EditorTabToggleValue));
            Set(-1, nameof(SettingsTabToggleValue));
            Set(0, nameof(SelectedItemTypeIndex));
            Set(ArmorEquipmentValue, nameof(EquipmentTypeValue));
            _session.LoadedItemTypeIndex = 0;
            RegisterColorMappings();
            _session.SelectedSlot = AppearanceEditorSlot.Head;
            AppearancePartBrowserService.InitializePartBrowser(this);
            AppearanceSwatchService.InitializeColorSelection(this);
            IsAppearanceSelected = true;
            IsEquipmentSelected = false;
            IsSettingsSelected = false;
            IsColorPickerVisible = true;
            IsCopyEnabled = true;
            IsCustomTintAvailable = false;
            ToggleItemEquippedFlags();
            AppearancePartListService.LoadColorCategoryOptions(this);
            AppearancePartListService.LoadPartCategoryOptions(this);
            SelectedColorCategoryIndex = 0;
            SelectedPartCategoryIndex = 0;
            SelectedPartIndex = 0;
            ColorCategorySelected[0] = true;
            PartCategorySelected[0] = true;
            LoadBodyParts();
            AppearanceTintEditorService.RefreshTintMapAvailability(this);
            AppearanceTintEditorService.LoadTintMapEditor(this);
            InitializeSettings();
            AppearanceOutfitService.InitializeOutfits(this);
            ColorTargetText = string.Empty;
            LoadItemTypeEditor();

            WatchOnClient(model => model.EditorTabToggleValue);
            WatchOnClient(model => model.SettingsTabToggleValue);
            WatchOnClient(model => model.OutfitsTabToggleValue);
            WatchOnClient(model => model.OutfitName);
            WatchOnClient(model => model.QuickOutfitValue);
            WatchOnClient(model => model.SelectedColorCategoryIndex);
            WatchOnClient(model => model.SelectedPartCategoryIndex);
            WatchOnClient(model => model.SelectedPartIndex);
            WatchOnClient(model => model.EquipmentTypeValue);
            WatchOnClient(model => model.SelectedTintColor);
            WatchOnClient(model => model.CustomTintRed);
            WatchOnClient(model => model.CustomTintGreen);
            WatchOnClient(model => model.CustomTintBlue);
            _session.TintControlBindingsWatched = true;
        }
        private void InitializeSettings()
        {
            AppearanceSettingsService.InitializeSettings(this);

            if (IsArmorDisplayVisible)
            {
                WatchOnClient(model => model.ShowHelmet);
                WatchOnClient(model => model.ShowCloak);
            }
        }

        public Action OnMouseDownTintPicker() => () =>
        {
            var payload = NuiGetEventPayload();
            var button = JsonGetInt(JsonObjectGet(payload, "mouse_btn"));
            AppearanceTintEditorService.BeginTintPickerGesture(this, (NuiMouseButton)button,
                (NuiMouseButton)button == NuiMouseButton.Left ? AppearanceTintEditorService.CaptureTintColorEdit(this) : null);
        };

        public Action OnMouseUpTintPicker() => () =>
        {
            var payload = NuiGetEventPayload();
            var button = JsonGetInt(JsonObjectGet(payload, "mouse_btn"));
            AppearanceTintEditorService.ReleaseTintPickerGesture(this, (NuiMouseButton)button);
        };
        protected override void OnClientPropertyUpdated(string propertyName)
        {
            if (_session.IsDMArmorDisplay && propertyName is nameof(ShowHelmet) or nameof(ShowCloak))
                AppearanceSettingsService.ApplyDMArmorDisplay(this);

            if (_session.TintComponentCorrection == propertyName)
            {
                _session.TintComponentCorrection = null;
                // Correct invalid input after SkipNotify ends. Valid input is already on
                // the client: echoing it (or the other fields) can reset an active edit.
                AppearanceTintEditorService.SynchronizeTintControlBindings(this, () => OnPropertyChanged(propertyName));
            }
        }
        private void LoadItemTypeEditor()
        {
            AppearanceTintEditorService.SuspendArmorClientWatches(this);
            var partialTabId = IsSettingsSelected
                ? SettingsTabId
                : IsOutfitsSelected
                ? OutfitsTabId
                : IsEquipmentSelected
                    ? SelectedItemTypeIndex == 0 ? EquipmentTabId
                        : SelectedItemTypeIndex is 3 or 4 ? WeaponEquipmentTabId : SimpleEquipmentTabId
                    : AppearanceTabId;
            EditorTabs.Select(this, MainPartialElement, partialTabId, OnEditorPartialApplied);
        }

        private void OnEditorPartialApplied()
        {
            AppearanceTintEditorService.SuspendArmorClientWatches(this);
            var isArmorEditor = IsEquipmentSelected && SelectedItemTypeIndex == 0;
            AppearanceTintEditorService.SynchronizeTintControlBindings(this, () =>
            {
                if (isArmorEditor)
                {
                    IsCopyEnabled = true;
                    AppearanceSwatchService.UpdatePaletteSheetForChannel(this);
                }
                // Tab replacement still needs current bindings after its nested palette.
                // Resizing does not replace controls or pause their watches.
                RepublishBindings();
            });
            AppearanceOutfitService.RepublishOutfitInputs(this);
            if (isArmorEditor)
                AppearanceTintEditorService.ResumeArmorClientWatches(this);
        }

        protected override void OnModalClosedRestore()
        {
            EditorToggles.SyncTo(_session.SelectedTabId, value => EditorTabToggleValue = value);
            SettingsToggles.SyncTo(_session.SelectedTabId, value => SettingsTabToggleValue = value);
            OutfitsToggles.SyncTo(_session.SelectedTabId, value => OutfitsTabToggleValue = value);
            LoadItemTypeEditor();
        }

        private void SelectEditorTab(int tabId)
        {
            if (tabId is not (AppearanceTabId or EquipmentTabId or SettingsTabId or OutfitsTabId) ||
                tabId == SettingsTabId && !IsSettingsVisible ||
                tabId == OutfitsTabId && !IsOutfitsVisible)
                return;
            AppearanceTintEditorService.CommitCustomTintComponents(this);
            _session.SelectedTabId = tabId;
            EditorToggles.SyncTo(tabId, value => EditorTabToggleValue = value);
            SettingsToggles.SyncTo(tabId, value => SettingsTabToggleValue = value);
            OutfitsToggles.SyncTo(tabId, value => OutfitsTabToggleValue = value);
            IsAppearanceSelected = tabId == AppearanceTabId;
            IsEquipmentSelected = tabId == EquipmentTabId;
            IsSettingsSelected = tabId == SettingsTabId;
            IsOutfitsSelected = tabId == OutfitsTabId;
            AppearanceTintEditorService.SuspendArmorClientWatches(this);

            if (IsPartEditorHidden)
            {
                IsNoEditableItemVisible = false;
                // Wardrobe and Settings edit no parts: drop the part list and close its browser.
                AppearancePartBrowserService.ClearBrowser(this);
                if (IsSettingsSelected)
                    AppearanceSettingsService.LoadSettings(this);
                else
                    AppearanceOutfitService.LoadOutfits(this);
                LoadItemTypeEditor();
                return;
            }

            ToggleItemEquippedFlags();
            AppearancePartListService.LoadColorCategoryOptions(this);
            AppearancePartListService.LoadPartCategoryOptions(this);
            if (IsAppearanceSelected)
                LoadBodyParts();
            else
            {
                _session.ColorTarget = AppearanceColorTarget.Global;
                _session.SelectedColorChannel = AppearanceArmorColor.Leather1;
                LoadItemParts();
                AppearanceSwatchService.UpdateTargetedColor(this);
            }
            LoadItemTypeEditor();
            AppearanceTintEditorService.LoadTintMapEditor(this);
        }
        private uint GetItem() => AppearancePartListService.GetItem(this);

        private uint GetOffHandAppearanceOwner(uint offHand) => AppearancePartListService.GetOffHandAppearanceOwner(this, offHand);

        private void LoadBodyParts() => AppearancePartListService.LoadBodyParts(this);

        private void LoadItemParts() => AppearancePartListService.LoadItemParts(this);

        private void LoadPart() => AppearancePartListService.LoadPart(this);

        private void UpdateEditedColorSwatches() => AppearanceSwatchService.UpdateEditedColorSwatches(this);

        private void RefreshBodySwatches() => AppearanceSwatchService.RefreshBodySwatches(this);

        private void RefreshItemBaseSwatches() => AppearanceSwatchService.RefreshItemBaseSwatches(this);

        private void ChangeColor(AppearanceColorTarget target, AppearanceArmorColor channel, int colorId) => AppearanceSwatchService.ChangeColor(this, target, channel, colorId);

        private void RegisterColorMappings()
        {
            foreach (var swatch in ArmorColorRegionCatalog.Entries)
                Set(ArmorColorRegionCatalog.NeutralRegion(), swatch.RegionName);
            _session.ColorMappingsRegistered = true;
        }

        AppearanceEditorSession IAppearanceEditorHost.Session => _session;
        uint IAppearanceEditorHost.Player => Player;
        int IAppearanceEditorHost.WindowToken => WindowToken;
        GuiWindowType IAppearanceEditorHost.WindowType => WindowType;
        T IAppearanceEditorHost.Get<T>(string name) => Get<T>(name);
        void IAppearanceEditorHost.Set<T>(T value, string name) => Set(value, name);
        (GuiBindingList<GuiComboEntry> Options, int Selection, Action<int> Select) IAppearanceEditorHost.GetArmorPartBinding(AppearanceArmor partType) => GetArmorPartBinding(partType);
        void IAppearanceEditorHost.WatchArmorSelections() => WatchArmorSelections();
        bool IAppearanceEditorHost.IsWindowOpen() => Gui.IsWindowOpen(Player, WindowType);
        bool IAppearanceEditorHost.IsPartEditorHidden => IsPartEditorHidden;
        void IAppearanceEditorHost.LoadBodyParts() => LoadBodyParts();
        void IAppearanceEditorHost.LoadPart() => LoadPart();
        void IAppearanceEditorHost.RefreshTintMapEditorAfterAppearanceChange(bool refreshItemParts) => RefreshTintMapEditorAfterAppearanceChange(refreshItemParts);
        void IAppearanceEditorHost.ShowModal(string prompt, Action confirmAction) => ShowModal(prompt, confirmAction);
        void IAppearanceEditorHost.ChangeColor(AppearanceColorTarget target, AppearanceArmorColor channel, int colorId) => ChangeColor(target, channel, colorId);
        uint IAppearanceEditorHost.GetItem() => GetItem();
        uint IAppearanceEditorHost.GetOffHandAppearanceOwner(uint offHand) => GetOffHandAppearanceOwner(offHand);
        bool IAppearanceEditorHost.IsValidItem() => IsValidItem();
        void IAppearanceEditorHost.LoadItemParts() => LoadItemParts();
        void IAppearanceEditorHost.RefreshBodySwatches() => RefreshBodySwatches();
        void IAppearanceEditorHost.RefreshItemBaseSwatches() => RefreshItemBaseSwatches();
        void IAppearanceEditorHost.ToggleItemEquippedFlags() => ToggleItemEquippedFlags();
        void IAppearanceEditorHost.UpdateEditedColorSwatches() => UpdateEditedColorSwatches();
        /// <summary>
        /// The current value of a binding by name. The colour swatch bindings (<c>ArmorColorRegionCatalog</c>) have no declared
        /// property.
        /// </summary>
        public T GetBindingValue<T>(string name) => Get<T>(name);

        public Action OnSelectAppearance() => () => SelectEditorTab(AppearanceTabId);

        public Action OnSelectEquipment() => () => SelectEditorTab(EquipmentTabId);

        public Action OnSelectSettings() => () => SelectEditorTab(SettingsTabId);
        public Action OnDecreaseAppearanceScale() => () => AppearanceSettingsService.DecreaseAppearanceScale(this);
        public Action OnIncreaseAppearanceScale() => () => AppearanceSettingsService.IncreaseAppearanceScale(this);
        public Action OnDecreaseHeadScale() => () => AppearanceSettingsService.DecreaseHeadScale(this);
        public Action OnIncreaseHeadScale() => () => AppearanceSettingsService.IncreaseHeadScale(this);

        public Action OnSelectPartCategory() => () => AppearancePartListService.SelectPartCategory(this);
        public Action OnPreviousPart() => () => AppearancePartListService.PreviousPart(this);
        public Action OnNextPart() => () => AppearancePartListService.NextPart(this);

        internal static bool TryParsePartNumber(string text, out int partId)
        {
            text = text?.Trim() ?? string.Empty;
            if (text.StartsWith("#"))
                text = text[1..];

            return int.TryParse(text, out partId);
        }
        public Action OnCloseWindow() => () =>
        {
            AppearancePartBrowserService.ClosePartBrowser(this);
            AppearanceTintEditorService.FlushPendingPickerColor(this);
            AppearanceTintEditorService.CancelTintPickerGesture(this);
            AppearanceTintEditorService.CommitCustomTintComponents(this);
            _session.TintEditGeneration++;
            AppearanceSettingsService.RestoreSavedScales(this);
        };

        public Action OnClickSaveSettings() => () => AppearanceSettingsService.SaveSettings(this);

        public Action OnClickColorPalette(int colorId) => () =>
        {
            if (AppearanceColorService.ApplySelectedPaletteColor(this, colorId))
                AppearanceColorService.SynchronizeCustomTintControlsToPaletteColor(this, colorId);
        };

        public Action OnClickClearColor(AppearanceColorTarget colorTarget, AppearanceArmorColor colorChannel) => () =>
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            if (colorTarget == AppearanceColorTarget.Invalid)
                return;

            // Right clicks only.
            var payload = NuiGetEventPayload();
            var button = JsonGetInt(JsonObjectGet(payload, "mouse_btn"));
            if (button != 2)
                return;

            AppearanceColorService.ResetArmorColorToInheritance(this, colorTarget, colorChannel);
        };
        public Action OnClickColorTarget(AppearanceColorTarget target, AppearanceArmorColor channel) => () => AppearanceSwatchService.ClickColorTarget(this, target, channel);

        public Action OnClickAdjustArmorPart(AppearanceArmor partType, int adjustBy) => () =>
        {
            if (!IsEquipmentSelected || SelectedItemTypeIndex != 0)
                return;

            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            AppearancePartEditService.AdjustArmorPart(this, partType, adjustBy);
        };

        private (GuiBindingList<GuiComboEntry> Options, int Selection, Action<int> Select) GetArmorPartBinding(AppearanceArmor partType)
        {
            return partType switch
            {
                AppearanceArmor.RightFoot => (RightFootOptions, RightFootSelection, value => RightFootSelection = value),
                AppearanceArmor.LeftFoot => (LeftFootOptions, LeftFootSelection, value => LeftFootSelection = value),
                AppearanceArmor.RightShin => (RightShinOptions, RightShinSelection, value => RightShinSelection = value),
                AppearanceArmor.LeftShin => (LeftShinOptions, LeftShinSelection, value => LeftShinSelection = value),
                AppearanceArmor.LeftThigh => (LeftThighOptions, LeftThighSelection, value => LeftThighSelection = value),
                AppearanceArmor.RightThigh => (RightThighOptions, RightThighSelection, value => RightThighSelection = value),
                AppearanceArmor.Pelvis => (PelvisOptions, PelvisSelection, value => PelvisSelection = value),
                AppearanceArmor.Torso => (ChestOptions, ChestSelection, value => ChestSelection = value),
                AppearanceArmor.Belt => (BeltOptions, BeltSelection, value => BeltSelection = value),
                AppearanceArmor.Neck => (NeckOptions, NeckSelection, value => NeckSelection = value),
                AppearanceArmor.RightForearm => (RightForearmOptions, RightForearmSelection, value => RightForearmSelection = value),
                AppearanceArmor.LeftForearm => (LeftForearmOptions, LeftForearmSelection, value => LeftForearmSelection = value),
                AppearanceArmor.RightBicep => (RightBicepOptions, RightBicepSelection, value => RightBicepSelection = value),
                AppearanceArmor.LeftBicep => (LeftBicepOptions, LeftBicepSelection, value => LeftBicepSelection = value),
                AppearanceArmor.RightShoulder => (RightShoulderOptions, RightShoulderSelection, value => RightShoulderSelection = value),
                AppearanceArmor.LeftShoulder => (LeftShoulderOptions, LeftShoulderSelection, value => LeftShoulderSelection = value),
                AppearanceArmor.RightHand => (RightHandOptions, RightHandSelection, value => RightHandSelection = value),
                AppearanceArmor.LeftHand => (LeftHandOptions, LeftHandSelection, value => LeftHandSelection = value),
                AppearanceArmor.Robe => (RobeOptions, RobeSelection, value => RobeSelection = value),
                _ => (null, 0, null)
            };
        }

        private void WatchArmorSelections()
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

        public Action OnClickCopyToRight() => () => AppearanceColorService.CopyArmorSide(this, copyToRight: true);

        public Action OnClickCopyToLeft() => () => AppearanceColorService.CopyArmorSide(this, copyToRight: false);

        public void Refresh(EquipItemRefreshEvent payload)
        {
            RefreshTintMapEditorAfterAppearanceChange(refreshItemParts: true);
        }

        public void Refresh(UnequipItemRefreshEvent payload)
        {
            RefreshTintMapEditorAfterAppearanceChange(refreshItemParts: true);
        }

        public void Refresh(AppearanceChangedRefreshEvent payload)
        {
            RefreshTintMapEditorAfterAppearanceChange();
        }

        /// <summary>
        /// /helm or /cloak changed the saved setting; show it so Save Settings keeps it.
        /// </summary>
        public void Refresh(ArmorDisplayChangedRefreshEvent payload)
        {
            if (!IsArmorDisplayVisible || payload.Creature != _session.Target)
                return;

            if (payload.Helmet)
                ShowHelmet = payload.Shown;
            else
                ShowCloak = payload.Shown;
        }

        private void RefreshTintMapEditorAfterAppearanceChange(bool refreshItemParts = false)
        {
            ToggleItemEquippedFlags();
            if (refreshItemParts)
                LoadItemParts();
            if (IsAppearanceSelected || IsEquipmentSelected)
                AppearanceTintEditorService.LoadTintMapEditor(this);
            else
                AppearanceTintEditorService.RefreshTintMapAvailability(this);
            if (IsEquipmentSelected && SelectedItemTypeIndex == 0 && HasItemEquipped)
            {
                AppearanceSwatchService.UpdateAllColors(this);
                AppearanceSwatchService.UpdateTargetedColor(this);
            }
        }

        public Action OnClickPreviewView() => () => AppearancePartBrowserService.ClickPreviewView(this);

        public Action OnClickPartPreview() => () => AppearancePartBrowserService.ClickPartPreview(this);

        public Action OnClickPreviousPart() => () => AppearancePartBrowserService.StepPart(this, -1);

        public Action OnClickNextPart() => () => AppearancePartBrowserService.StepPart(this, 1);

        /// <summary>Wears a part picked in the editor or the part browser window.</summary>
        internal void ApplyBrowserPart(int partId) => AppearancePartBrowserService.ApplyBrowserPart(this, partId);

        /// <summary>Shows every picture from one side: the editor's part and the browser's pages.</summary>
        internal void SelectPreviewView(AppearancePreviewView view) => AppearancePartBrowserService.SelectPreviewView(this, view);

        public Action OnClickMapSlot(AppearanceEditorSlot slot) => () => AppearancePartBrowserService.ClickMapSlot(this, slot);

        public Action OnClickBaseSwatch(AppearanceArmorColor channel) => () => AppearanceSwatchService.ClickBaseSwatch(this, channel);

        public Action OnClickPartSwatch(AppearanceArmorColor channel) => () =>
            OnClickColorTarget(AppearanceSwatchService.ToColorTarget(_session.SelectedSlot), channel)();

        public Action OnMouseDownPartSwatch(AppearanceArmorColor channel) => () =>
            OnClickClearColor(AppearanceSwatchService.ToColorTarget(_session.SelectedSlot), channel)();

        public Action OnClickBodySwatch(int index) => () => AppearanceSwatchService.ClickBodySwatch(this, index);

        public Action OnClickOutfitSlot() => () => AppearanceOutfitService.ClickOutfitSlot(this);

        public Action OnClickNewOutfit() => () => AppearanceOutfitService.ClickNewOutfit(this);

        public Action OnClickDeleteOutfit() => () => AppearanceOutfitService.ClickDeleteOutfit(this);

        public Action OnClickRenameOutfit() => () => AppearanceOutfitService.ClickRenameOutfit(this);

        public Action OnClickStoreOutfit() => () => AppearanceOutfitService.ClickStoreOutfit(this);

        public Action OnClickWearOutfit() => () => AppearanceOutfitService.ClickWearOutfit(this);

        public Action OnClickQuickWear() => () => AppearanceOutfitService.ClickQuickWear(this);

        public Action OnClickUndoOutfit() => () => AppearanceOutfitService.ClickUndoOutfit(this);
    }
}
