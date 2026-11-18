using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceEditing;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.Species;
using SWLOR.Game.Server.Feature.AppearanceDefinition.Preview;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public partial class AppearanceEditorViewModel :
        GuiViewModelBase<AppearanceEditorViewModel, AppearanceEditorPayload>,
        IGuiRefreshable<EquipItemRefreshEvent>,
        IGuiRefreshable<UnequipItemRefreshEvent>,
        IGuiRefreshable<AppearanceChangedRefreshEvent>,
        IGuiRefreshable<ArmorDisplayChangedRefreshEvent>,
        IAppearanceEditorHost
    {
        private AppearanceColorService AppearanceColorService { get; }
        private AppearancePartEditService AppearancePartEditService { get; }
        private AppearanceTintEditorService AppearanceTintEditorService { get; }
        private IDBService DB { get; }
        private IDMPresenceService DMPresence { get; }
        private IGuiService Gui { get; }
        private IPowerGloveEquipmentService PowerGloveEquipment { get; }
        private ITintMapCreatureColorService TintMapCreatureColor { get; }
        private ITwinPistolEquipmentService TwinPistolEquipment { get; }

        public AppearanceEditorViewModel(AppearanceColorService appearanceColorService, AppearancePartEditService appearancePartEditService, AppearanceTintEditorService appearanceTintEditorService, IDBService db, IDMPresenceService dmPresence, IGuiService gui, IPowerGloveEquipmentService powerGloveEquipment, ITintMapCreatureColorService tintMapCreatureColor, ITwinPistolEquipmentService twinPistolEquipment)
        {
            AppearanceColorService = appearanceColorService;
            AppearancePartEditService = appearancePartEditService;
            AppearanceTintEditorService = appearanceTintEditorService;
            DB = db;
            DMPresence = dmPresence;
            Gui = gui;
            PowerGloveEquipment = powerGloveEquipment;
            TintMapCreatureColor = tintMapCreatureColor;
            TwinPistolEquipment = twinPistolEquipment;
        }

        public const string MainPartialElement = "MAIN_PARTIAL_VIEW";

        public const string EditorMainPartial = "APPEARANCE_EDITOR_MAIN_PARTIAL";
        public const string EditorWeaponPartial = "APPEARANCE_EDITOR_WEAPON_PARTIAL";
        public const string EditorArmorPartial = "APPEARANCE_EDITOR_ARMOR_PARTIAL";
        public const string SettingsPartial = "SETTINGS_PARTIAL";

        private readonly AppearanceEditorSession _session = new();
        private static readonly IReadOnlyDictionary<AppearanceType, BaseArmorAppearanceDefinition> _armorAppearances =
            ArmorAppearanceCatalog.Definitions;



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
            WeaponAppearanceCatalog.EnsureLoaded();
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
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShoulder))
                    AppearancePartEditService.AdjustArmorPart(this, AppearanceArmor.LeftShoulder, 0);
            }
        }

        public int LeftBicepSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftBicep))
                    AppearancePartEditService.AdjustArmorPart(this, AppearanceArmor.LeftBicep, 0);
            }
        }
        public int LeftForearmSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftForearm))
                    AppearancePartEditService.AdjustArmorPart(this, AppearanceArmor.LeftForearm, 0);
            }
        }
        public int LeftHandSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand))
                    AppearancePartEditService.AdjustArmorPart(this, AppearanceArmor.LeftHand, 0);
            }
        }
        public int LeftThighSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftThigh))
                    AppearancePartEditService.AdjustArmorPart(this, AppearanceArmor.LeftThigh, 0);
            }
        }
        public int LeftShinSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShin))
                    AppearancePartEditService.AdjustArmorPart(this, AppearanceArmor.LeftShin, 0);
            }
        }
        public int LeftFootSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftFoot))
                    AppearancePartEditService.AdjustArmorPart(this, AppearanceArmor.LeftFoot, 0);
            }
        }
        public int RightShoulderSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShoulder))
                    AppearancePartEditService.AdjustArmorPart(this, AppearanceArmor.RightShoulder, 0);
            }
        }
        public int RightBicepSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightBicep))
                    AppearancePartEditService.AdjustArmorPart(this, AppearanceArmor.RightBicep, 0);
            }
        }
        public int RightForearmSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightForearm))
                    AppearancePartEditService.AdjustArmorPart(this, AppearanceArmor.RightForearm, 0);
            }
        }
        public int RightHandSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand))
                    AppearancePartEditService.AdjustArmorPart(this, AppearanceArmor.RightHand, 0);
            }
        }
        public int RightThighSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightThigh))
                    AppearancePartEditService.AdjustArmorPart(this, AppearanceArmor.RightThigh, 0);
            }
        }
        public int RightShinSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShin))
                    AppearancePartEditService.AdjustArmorPart(this, AppearanceArmor.RightShin, 0);
            }
        }
        public int RightFootSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightFoot))
                    AppearancePartEditService.AdjustArmorPart(this, AppearanceArmor.RightFoot, 0);
            }
        }
        public int NeckSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck))
                    AppearancePartEditService.AdjustArmorPart(this, AppearanceArmor.Neck, 0);
            }
        }
        public int ChestSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso))
                    AppearancePartEditService.AdjustArmorPart(this, AppearanceArmor.Torso, 0);
            }
        }
        public int BeltSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Belt))
                    AppearancePartEditService.AdjustArmorPart(this, AppearanceArmor.Belt, 0);
            }
        }
        public int PelvisSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis))
                    AppearancePartEditService.AdjustArmorPart(this, AppearanceArmor.Pelvis, 0);
            }
        }
        public int RobeSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe))
                    AppearancePartEditService.AdjustArmorPart(this, AppearanceArmor.Robe, 0);
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
                SetIfChanged(nameof(EquipmentTypeValue), ToEquipmentTypeValue(value));
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
                    SetIfChanged(nameof(IsPartColorsVisible), false);
                LoadColorCategoryOptions();
                LoadPartCategoryOptions();
                LoadItemParts();
                if (IsEquipmentSelected && value == 0)
                    UpdateTargetedColor();
                else if (IsEquipmentSelected && value is 1 or 2)
                    UpdateColorSelection();
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

        private void LoadTintMapEditor() => AppearanceTintEditorService.LoadTintMapEditor(this);

        private void AdjustArmorPart(AppearanceArmor partType, int adjustBy) => AppearancePartEditService.AdjustArmorPart(this, partType, adjustBy);

        private bool IsValidItem() =>
            // Treated as a valid item if we're not in the item customization page.
            IsAppearanceSelected || AppearanceItemEligibility.IsValid(_session.Target, GetItem(), SelectedItemTypeIndex);

        private void ToggleItemEquippedFlags()
        {
            var hasItemEquipped = IsValidItem();
            HasItemEquipped = hasItemEquipped;
            DoesNotHaveItemEquipped = !hasItemEquipped;
            IsNoEditableItemVisible = !hasItemEquipped && IsEquipmentSelected;
            // An empty slot has no parts: the browser must not keep showing the last item's.
            if (!hasItemEquipped)
                ClearBrowser();
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
                CloseForUnsupportedAppearance();
                return;
            }

            _session.ColorTarget = AppearanceColorTarget.Global;
            _session.SelectedColorChannel = AppearanceArmorColor.Leather1;
            _session.SelectedTabId = AppearanceTabId;
            FitWindowToPlayerScreen();
            Set(0, nameof(EditorTabToggleValue));
            Set(-1, nameof(SettingsTabToggleValue));
            Set(0, nameof(SelectedItemTypeIndex));
            Set(ArmorEquipmentValue, nameof(EquipmentTypeValue));
            _session.LoadedItemTypeIndex = 0;
            RegisterColorMappings();
            _session.SelectedSlot = AppearanceEditorSlot.Head;
            InitializePartBrowser();
            InitializeColorSelection();
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
            AppearanceTintEditorService.RefreshTintMapAvailability(this);
            AppearanceTintEditorService.LoadTintMapEditor(this);
            InitializeSettings();
            InitializeOutfits();
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
            IsPlayerSettingsVisible = GetIsPC(_session.Target) && !GetIsDM(_session.Target) && !GetIsDMPossessed(_session.Target);
            IsSettingsVisible = IsPlayerSettingsVisible || GetIsDM(Player) || GetIsDMPossessed(Player);
            // A DM shows or hides their avatar's or possessed creature's helmet and cloak at once, with no Save.
            _session.IsDMArmorDisplay = GetIsDM(_session.Target) || GetIsDMPossessed(_session.Target);
            IsArmorDisplayVisible = IsPlayerSettingsVisible || _session.IsDMArmorDisplay;
            ShowHelmet = true;
            ShowCloak = true;
            LoadSettings();

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
                ApplyDMArmorDisplay();

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
                    UpdatePaletteSheetForChannel();
                }
                // Tab replacement still needs current bindings after its nested palette.
                // Resizing does not replace controls or pause their watches.
                RepublishBindings();
            });
            RepublishOutfitInputs();
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
                ClearBrowser();
                if (IsSettingsSelected)
                    LoadSettings();
                else
                    LoadOutfits();
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
                _session.ColorTarget = AppearanceColorTarget.Global;
                _session.SelectedColorChannel = AppearanceArmorColor.Leather1;
                LoadItemParts();
                UpdateTargetedColor();
            }
            LoadItemTypeEditor();
            AppearanceTintEditorService.LoadTintMapEditor(this);
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
                    var partAppearance = WeaponAppearanceCatalog.Definitions[type];

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

        private (GuiBindingList<string>, GuiBindingList<bool>) GetPartLists(int[] partIds, Func<int, string> getPartName = null)
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

            _session.PartIdToIndex = partIdToIndex;
            _session.PartIds = partIds.ToList();
            return (partNames, partSelected);
        }

        private uint GetItem()
        {
            if (SelectedItemTypeIndex == 0) // 0 = Armor
            {
                return GetItemInSlot(InventorySlot.Chest, _session.Target);
            }
            else if (SelectedItemTypeIndex == 1) // 1 = Helmet
            {
                return GetItemInSlot(InventorySlot.Head, _session.Target);
            }
            else if (SelectedItemTypeIndex == 2) // 2 = Cloak
            {
                return GetItemInSlot(InventorySlot.Cloak, _session.Target);
            }
            else if (SelectedItemTypeIndex == 3) // 3 = Weapon (Main Hand)
            {
                return GetItemInSlot(InventorySlot.RightHand, _session.Target);
            }
            else if (SelectedItemTypeIndex == 4) // 4 = Weapon (Off Hand)
            {
                return GetOffHandAppearanceOwner(GetItemInSlot(InventorySlot.LeftHand, _session.Target));
            }

            return OBJECT_INVALID;
        }

        // A generated off hand mirrors its owned main-hand item, so its edits apply to that item.
        private uint GetOffHandAppearanceOwner(uint offHand) =>
            TwinPistolEquipment.ResolveAppearanceOwner(_session.Target,
                PowerGloveEquipment.ResolveAppearanceOwner(_session.Target, offHand));

        private void CloseForUnsupportedAppearance()
        {
            // Bind uses the possessed creature as Player, but the DM owns the window cache.
            var owner = GetIsDMPossessed(Player) ? GetMaster(Player) : Player;
            SendMessageToPC(owner, "This creature's appearance cannot be customized.");
            Gui.ClosePlayerWindow(owner, GuiWindowType.AppearanceEditor, Player);
        }

        private void LoadBodyParts()
        {
            var appearanceType = GetAppearanceType(_session.Target);
            var gender = GetGender(_session.Target);

            if (!RacialAppearanceRegistry.TryGet(appearanceType, out var appearance))
            {
                CloseForUnsupportedAppearance();
                return;
            }

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

                    selectedPartId = GetCreatureBodyPart(CreaturePart.Head, _session.Target);
                    break;
                case 1: // Torso
                    partIds = appearance.Torsos;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.Torso, _session.Target);
                    break;
                case 2: // Pelvis
                    partIds = appearance.Pelvis;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.Pelvis, _session.Target);
                    break;
                case 3: // Right Bicep
                    partIds = appearance.RightBicep;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightBicep, _session.Target);
                    break;
                case 4: // Right Forearm
                    partIds = appearance.RightForearm;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightForearm, _session.Target);
                    break;
                case 5: // Right Hand
                    partIds = appearance.RightHand;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightHand, _session.Target);
                    break;
                case 6: // Right Thigh
                    partIds = appearance.RightThigh;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightThigh, _session.Target);
                    break;
                case 7: // Right Shin
                    partIds = appearance.RightShin;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightShin, _session.Target);
                    break;
                case 8: // Right Foot
                    partIds = appearance.RightFoot;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightFoot, _session.Target);
                    break;
                case 9: // Left Bicep
                    partIds = appearance.LeftBicep;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftBicep, _session.Target);
                    break;
                case 10: // Left Forearm
                    partIds = appearance.LeftForearm;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftForearm, _session.Target);
                    break;
                case 11: // Left Hand
                    partIds = appearance.LeftHand;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftHand, _session.Target);
                    break;
                case 12: // Left Thigh
                    partIds = appearance.LeftThigh;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftThigh, _session.Target);
                    break;
                case 13: // Left Shin
                    partIds = appearance.LeftShin;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftShin, _session.Target);
                    break;
                case 14: // Left Foot
                    partIds = appearance.LeftFoot;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftFoot, _session.Target);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(SelectedPartIndex));
            }

            var (partNames, partSelected) = GetPartLists(partIds);

            PartOptions = partNames;
            PartSelected = partSelected;
            SelectedPartIndex = _session.PartIdToIndex.TryGetValue(selectedPartId, out var selectedIndex) ? selectedIndex : -1;
            if (SelectedPartIndex >= 0)
                PartSelected[SelectedPartIndex] = true;
            RefreshPartBrowser();
        }

        private void LoadSettings()
        {
            if (_session.IsDMArmorDisplay)
            {
                LoadDMArmorDisplay();
                return;
            }

            if (!GetIsPC(_session.Target) || GetIsDM(_session.Target) || GetIsDMPossessed(_session.Target))
                return;

            var playerId = GetObjectUUID(_session.Target);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            ShowHelmet = dbPlayer.Settings.ShowHelmet;
            ShowCloak = dbPlayer.Settings.ShowCloak;
        }

        private void LoadDMArmorDisplay()
        {
            var (showHelmet, showCloak) = ArmorDisplay.GetDMArmorDisplay(_session.Target);
            ShowHelmet = showHelmet;
            ShowCloak = showCloak;
        }

        private void ApplyDMArmorDisplay()
        {
            if (!ArmorDisplay.SetDMArmorDisplay(_session.Target, ShowHelmet, ShowCloak))
                DMPresence.SendMessage(Player, ColorToken.Red("Your helmet and cloak setting could not be saved."));
        }

        private void LoadItemParts()
        {
            if (!IsEquipmentSelected)
                return;
            AppearanceTintEditorService.SuspendArmorClientWatches(this);
            var wasSkipping = _session.SkipAdjustArmorPart;
            _session.SkipAdjustArmorPart = true;
            try
            {
                PopulateItemParts();
            }
            finally
            {
                _session.SkipAdjustArmorPart = wasSkipping;
            }
            if (SelectedItemTypeIndex == 0 && HasItemEquipped)
                AppearanceTintEditorService.ResumeArmorClientWatches(this);
        }

        private void PopulateItemParts()
        {
            if (DoesNotHaveItemEquipped)
                return;

            var item = GetItem();
            int[] partIds;
            int selectedPartId;
            var appearanceType = GetAppearanceType(_session.Target);
            var type = GetBaseItemType(item);

            if (SelectedItemTypeIndex == 0) // 0 = Armor
            {
                GuiBindingList<GuiComboEntry> Available(AppearanceArmor part, IEnumerable<GuiComboEntry> options) =>
                    ArmorPartAppearance.GetAvailableOptions(_session.Target, part, options);
                var neckOptions = new GuiBindingList<GuiComboEntry>();
                foreach (var style in NeckAppearance.GetAvailableStyles(_session.Target, _armorAppearances[appearanceType].Neck.Prepend(0)))
                    neckOptions.Add(new GuiComboEntry(style.ToString(), style));
                NeckOptions = neckOptions;
                ChestOptions = Available(AppearanceArmor.Torso, _armorAppearances[appearanceType].TorsoOptions);
                BeltOptions = Available(AppearanceArmor.Belt, _armorAppearances[appearanceType].BeltOptions);
                PelvisOptions = Available(AppearanceArmor.Pelvis, _armorAppearances[appearanceType].PelvisOptions);
                var robeOptions = new GuiBindingList<GuiComboEntry>();
                foreach (var style in RobeAppearance.GetAvailableStyles(_session.Target, _armorAppearances[appearanceType].Robe))
                    robeOptions.Add(new GuiComboEntry(style.ToString(), style));
                RobeOptions = robeOptions;
                var repairedNeck = NeckAppearance.RemoveUnavailableNeck(_session.Target, item);
                var repairedParts = ArmorPartAppearance.RemoveUnavailableParts(_session.Target, item);
                if (RobeAppearance.RemoveUnavailableRobe(_session.Target, item) || repairedNeck || repairedParts)
                    EquippedItemAppearance.Refresh(_session.Target, item);

                LeftShoulderOptions = Available(AppearanceArmor.LeftShoulder, _armorAppearances[appearanceType].ShoulderOptions);
                LeftBicepOptions = Available(AppearanceArmor.LeftBicep, _armorAppearances[appearanceType].BicepOptions);
                LeftForearmOptions = Available(AppearanceArmor.LeftForearm, _armorAppearances[appearanceType].ForearmOptions);
                LeftHandOptions = Available(AppearanceArmor.LeftHand, _armorAppearances[appearanceType].HandOptions);
                LeftThighOptions = Available(AppearanceArmor.LeftThigh, _armorAppearances[appearanceType].ThighOptions);
                LeftShinOptions = Available(AppearanceArmor.LeftShin, _armorAppearances[appearanceType].ShinOptions);
                LeftFootOptions = Available(AppearanceArmor.LeftFoot, _armorAppearances[appearanceType].FootOptions);

                RightShoulderOptions = Available(AppearanceArmor.RightShoulder, _armorAppearances[appearanceType].ShoulderOptions);
                RightBicepOptions = Available(AppearanceArmor.RightBicep, _armorAppearances[appearanceType].BicepOptions);
                RightForearmOptions = Available(AppearanceArmor.RightForearm, _armorAppearances[appearanceType].ForearmOptions);
                RightHandOptions = Available(AppearanceArmor.RightHand, _armorAppearances[appearanceType].HandOptions);
                RightThighOptions = Available(AppearanceArmor.RightThigh, _armorAppearances[appearanceType].ThighOptions);
                RightShinOptions = Available(AppearanceArmor.RightShin, _armorAppearances[appearanceType].ShinOptions);
                RightFootOptions = Available(AppearanceArmor.RightFoot, _armorAppearances[appearanceType].FootOptions);

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
                SyncSelectedPartSwatches();
                RefreshPartBrowser();

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
                if (WeaponAppearanceCatalog.Definitions[type].IsSimple)
                {
                    partIds = WeaponAppearanceCatalog.Definitions[type].SimpleParts;
                    selectedPartId = GetItemAppearance(item, ItemAppearanceType.SimpleModel, -1);
                }
                else
                {
                    var weaponPart = SelectedPartCategoryIndex switch
                    {
                        0 => AppearanceWeapon.Top,
                        1 => AppearanceWeapon.Middle,
                        2 => AppearanceWeapon.Bottom,
                        _ => throw new ArgumentOutOfRangeException(nameof(SelectedPartIndex))
                    };
                    partIds = AppearanceItemEligibility.GetWeaponPartOptions(item, WeaponAppearanceCatalog.Definitions[type], weaponPart);
                    selectedPartId = AppearanceItemEligibility.GetWeaponPartId(item, weaponPart);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(SelectedItemTypeIndex));
            }

            if (selectedPartId <= -1)
                return;

            Func<int, string> getPartName = null;
            if (SelectedItemTypeIndex is 3 or 4)
            {
                var part = SelectedPartCategoryIndex switch
                {
                    0 => AppearanceWeapon.Top,
                    1 => AppearanceWeapon.Middle,
                    2 => AppearanceWeapon.Bottom,
                    _ => AppearanceWeapon.Invalid
                };
                getPartName = partId => WeaponAppearanceCatalog.Definitions[type].GetPartName(part, partId);
            }
            var (partNames, partSelected) = GetPartLists(partIds, getPartName);

            PartOptions = partNames;
            PartSelected = partSelected;
            SelectedPartIndex = _session.PartIdToIndex.TryGetValue(selectedPartId, out var selectedIndex) ? selectedIndex : -1;
            if (SelectedPartIndex >= 0)
                PartSelected[SelectedPartIndex] = true;
            RefreshItemBaseSwatches();
            RefreshPartBrowser();
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
        string IAppearanceEditorHost.CustomTintBlue { get => CustomTintBlue; set => CustomTintBlue = value; }
        string IAppearanceEditorHost.CustomTintGreen { get => CustomTintGreen; set => CustomTintGreen = value; }
        string IAppearanceEditorHost.CustomTintRed { get => CustomTintRed; set => CustomTintRed = value; }
        string IAppearanceEditorHost.CustomTintTooltip { get => CustomTintTooltip; set => CustomTintTooltip = value; }
        bool IAppearanceEditorHost.DoesNotHaveItemEquipped { get => DoesNotHaveItemEquipped; set => DoesNotHaveItemEquipped = value; }
        bool IAppearanceEditorHost.HasItemEquipped { get => HasItemEquipped; set => HasItemEquipped = value; }
        bool IAppearanceEditorHost.IsAppearanceSelected { get => IsAppearanceSelected; set => IsAppearanceSelected = value; }
        bool IAppearanceEditorHost.IsCopyEnabled { get => IsCopyEnabled; set => IsCopyEnabled = value; }
        bool IAppearanceEditorHost.IsCustomTintAvailable { get => IsCustomTintAvailable; set => IsCustomTintAvailable = value; }
        bool IAppearanceEditorHost.IsCustomTintEditable { get => IsCustomTintEditable; set => IsCustomTintEditable = value; }
        bool IAppearanceEditorHost.IsEquipmentSelected { get => IsEquipmentSelected; set => IsEquipmentSelected = value; }
        bool IAppearanceEditorHost.IsTintMapAvailable { get => IsTintMapAvailable; set => IsTintMapAvailable = value; }
        int IAppearanceEditorHost.RobeSelection { get => RobeSelection; set => RobeSelection = value; }
        int IAppearanceEditorHost.SelectedColorCategoryIndex { get => SelectedColorCategoryIndex; set => SelectedColorCategoryIndex = value; }
        int IAppearanceEditorHost.SelectedItemTypeIndex { get => SelectedItemTypeIndex; set => SelectedItemTypeIndex = value; }
        GuiColor IAppearanceEditorHost.SelectedTintColor { get => SelectedTintColor; set => SelectedTintColor = value; }
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

        public Action OnDecreaseAppearanceScale() => () =>
        {
            var appearanceType = GetAppearanceType(_session.Target);
            if (!RacialAppearanceRegistry.TryGet(appearanceType, out var appearance))
            {
                CloseForUnsupportedAppearance();
                return;
            }

            var scale = GetObjectVisualTransform(_session.Target, ObjectVisualTransform.Scale);
            const float Increment = 0.01f;

            if (scale - Increment < appearance.MinimumScale)
            {
                SendScaleMessage("You cannot decrease your height any further.");
            }
            else
            {
                SetObjectVisualTransform(_session.Target, ObjectVisualTransform.Scale, scale - Increment);
                SendScaleMessage($"Height: {GetObjectVisualTransform(_session.Target, ObjectVisualTransform.Scale)}");
            }
        };
        public Action OnIncreaseAppearanceScale() => () =>
        {
            var appearanceType = GetAppearanceType(_session.Target);
            if (!RacialAppearanceRegistry.TryGet(appearanceType, out var appearance))
            {
                CloseForUnsupportedAppearance();
                return;
            }

            var scale = GetObjectVisualTransform(_session.Target, ObjectVisualTransform.Scale);
            const float Increment = 0.01f;

            if (scale + Increment > appearance.MaximumScale)
            {
                SendScaleMessage("You cannot increase your height any further.");
            }
            else
            {
                SetObjectVisualTransform(_session.Target, ObjectVisualTransform.Scale, scale + Increment);
                SendScaleMessage($"Height: {GetObjectVisualTransform(_session.Target, ObjectVisualTransform.Scale)}");
            }
        };

        public Action OnDecreaseHeadScale() => () =>
        {
            var appearanceType = GetAppearanceType(_session.Target);
            if (!RacialAppearanceRegistry.TryGet(appearanceType, out var appearance))
            {
                CloseForUnsupportedAppearance();
                return;
            }

            var scale = HelmetModelRenderer.GetHeadScale(_session.Target);
            if (scale <= 0f)
                scale = 1.0f;

            const float Increment = 0.01f;

            if (scale - Increment < appearance.MinimumHeadScale)
            {
                SendScaleMessage("You cannot decrease your head size any further.");
            }
            else
            {
                HelmetModelRenderer.SetHeadScale(_session.Target, scale - Increment);
                SendScaleMessage($"Head Size: {HelmetModelRenderer.GetHeadScale(_session.Target)}");
            }
        };

        public Action OnIncreaseHeadScale() => () =>
        {
            var appearanceType = GetAppearanceType(_session.Target);
            if (!RacialAppearanceRegistry.TryGet(appearanceType, out var appearance))
            {
                CloseForUnsupportedAppearance();
                return;
            }

            var scale = HelmetModelRenderer.GetHeadScale(_session.Target);
            if (scale <= 0f)
                scale = 1.0f;

            const float Increment = 0.01f;

            if (scale + Increment > appearance.MaximumHeadScale)
            {
                SendScaleMessage("You cannot increase your head size any further.");
            }
            else
            {
                HelmetModelRenderer.SetHeadScale(_session.Target, scale + Increment);
                SendScaleMessage($"Head Size: {HelmetModelRenderer.GetHeadScale(_session.Target)}");
            }
        };

        private void SendScaleMessage(string message)
        {
            // Messages to a possessing DM's avatar are dropped, so they go to the creature the DM controls.
            DMPresence.SendMessage(Player, message);
        }


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










        private void LoadBodyPart()
        {
            var appearanceType = GetAppearanceType(_session.Target);
            var gender = GetGender(_session.Target);
            if (!RacialAppearanceRegistry.TryGet(appearanceType, out var appearance))
                return;

            switch (SelectedPartCategoryIndex)
            {
                case 0: // Head
                    switch (gender)
                    {
                        case Gender.Male:
                            SetCreatureBodyPart(CreaturePart.Head, appearance.MaleHeads[SelectedPartIndex], _session.Target);
                            break;
                        default:
                            SetCreatureBodyPart(CreaturePart.Head, appearance.FemaleHeads[SelectedPartIndex], _session.Target);
                            break;
                    }
                    break;
                case 1: // Torso
                    SetCreatureBodyPart(CreaturePart.Torso, appearance.Torsos[SelectedPartIndex], _session.Target);
                    break;
                case 2: // Pelvis
                    SetCreatureBodyPart(CreaturePart.Pelvis, appearance.Pelvis[SelectedPartIndex], _session.Target);
                    break;
                case 3: // Right Bicep
                    BodyPartAppearance.Set(CreaturePart.RightBicep, appearance.RightBicep[SelectedPartIndex], _session.Target);
                    break;
                case 4: // Right Forearm
                    SetCreatureBodyPart(CreaturePart.RightForearm, appearance.RightForearm[SelectedPartIndex], _session.Target);
                    break;
                case 5: // Right Hand
                    BodyPartAppearance.Set(CreaturePart.RightHand, appearance.RightHand[SelectedPartIndex], _session.Target);
                    break;
                case 6: // Right Thigh
                    SetCreatureBodyPart(CreaturePart.RightThigh, appearance.RightThigh[SelectedPartIndex], _session.Target);
                    break;
                case 7: // Right Shin
                    SetCreatureBodyPart(CreaturePart.RightShin, appearance.RightShin[SelectedPartIndex], _session.Target);
                    break;
                case 8: // Right Foot
                    SetCreatureBodyPart(CreaturePart.RightFoot, appearance.RightFoot[SelectedPartIndex], _session.Target);
                    break;
                case 9: // Left Bicep
                    BodyPartAppearance.Set(CreaturePart.LeftBicep, appearance.LeftBicep[SelectedPartIndex], _session.Target);
                    break;
                case 10: // Left Forearm
                    SetCreatureBodyPart(CreaturePart.LeftForearm, appearance.LeftForearm[SelectedPartIndex], _session.Target);
                    break;
                case 11: // Left Hand
                    BodyPartAppearance.Set(CreaturePart.LeftHand, appearance.LeftHand[SelectedPartIndex], _session.Target);
                    break;
                case 12: // Left Thigh
                    SetCreatureBodyPart(CreaturePart.LeftThigh, appearance.LeftThigh[SelectedPartIndex], _session.Target);
                    break;
                case 13: // Left Shin
                    SetCreatureBodyPart(CreaturePart.LeftShin, appearance.LeftShin[SelectedPartIndex], _session.Target);
                    break;
                case 14: // Left Foot
                    SetCreatureBodyPart(CreaturePart.LeftFoot, appearance.LeftFoot[SelectedPartIndex], _session.Target);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(SelectedPartIndex));
            }

            TintMapCreatureColor.CarryStoredCreatureCustomColors(_session.Target);
            ExecuteScript(ScriptName.OnAppearanceEdit, _session.Target);
            AppearanceTintEditorService.LoadTintMapEditor(this);
        }

        private void LoadArmorPart()
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;
            var item = GetItem();
            var itemType = GetBaseItemType(item);
            var appearanceType = GetAppearanceType(_session.Target);

            if (SelectedItemTypeIndex == 1) // 1 = Helmet
            {
                AppearancePartEditService.ModifyItemPart(this, (int)AppearanceArmor.Invalid, _armorAppearances[appearanceType].Helmet[SelectedPartIndex]);
            }
            else if (SelectedItemTypeIndex == 2) // 2 = Cloak
            {
                AppearancePartEditService.ModifyItemPart(this, (int)AppearanceArmor.Invalid, _armorAppearances[appearanceType].Cloak[SelectedPartIndex]);
            }
            else if (SelectedItemTypeIndex == 3 || SelectedItemTypeIndex == 4) // 3 = Weapon (Main Hand), 4 = Weapon (Off Hand)
            {
                if (WeaponAppearanceCatalog.Definitions[itemType].IsSimple)
                {
                    AppearancePartEditService.ModifyItemPart(this, (int)ItemAppearanceType.SimpleModel, WeaponAppearanceCatalog.Definitions[itemType].SimpleParts[SelectedPartIndex]);
                }
                else
                {
                    var weaponPart = SelectedPartCategoryIndex switch
                    {
                        0 => AppearanceWeapon.Top,
                        1 => AppearanceWeapon.Middle,
                        2 => AppearanceWeapon.Bottom,
                        _ => throw new ArgumentOutOfRangeException(nameof(SelectedPartIndex))
                    };
                    var selected = AppearanceItemEligibility.GetWeaponPartOptions(item, WeaponAppearanceCatalog.Definitions[itemType], weaponPart)[SelectedPartIndex];
                    AppearancePartEditService.ModifyItemPart(this, (int)weaponPart, selected % 100, selected / 100);
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
            if (newPartIndex > _session.PartIdToIndex.Count - 1)
                newPartIndex = _session.PartIdToIndex.Count - 1;

            PartSelected[SelectedPartIndex] = false;
            SelectedPartIndex = newPartIndex;
            PartSelected[SelectedPartIndex] = true;
            LoadPart();
        };


        internal static bool TryParsePartNumber(string text, out int partId)
        {
            text = text?.Trim() ?? string.Empty;
            if (text.StartsWith("#"))
                text = text[1..];

            return int.TryParse(text, out partId);
        }

        public Action OnCloseWindow() => () =>
        {
            ClosePartBrowser();
            AppearanceTintEditorService.FlushPendingPickerColor(this);
            AppearanceTintEditorService.CancelTintPickerGesture(this);
            AppearanceTintEditorService.CommitCustomTintComponents(this);
            _session.TintEditGeneration++;
            if (GetIsDM(_session.Target) || GetIsDMPossessed(_session.Target) || !GetIsPC(_session.Target))
                return;

            var playerId = GetObjectUUID(_session.Target);
            var dbPlayer = DB.Get<Player>(playerId);
            var headScale = dbPlayer.HeadAppearanceScale <= 0f ? 1.0f : dbPlayer.HeadAppearanceScale;

            SetObjectVisualTransform(_session.Target, ObjectVisualTransform.Scale, dbPlayer.AppearanceScale);
            HelmetModelRenderer.SetHeadScale(_session.Target, headScale);
        };

        public Action OnClickSaveSettings() => () =>
        {
            if (!GetIsPC(_session.Target) || GetIsDM(_session.Target) || GetIsDMPossessed(_session.Target))
                return;

            var playerId = GetObjectUUID(_session.Target);
            var dbPlayer = DB.Get<Player>(playerId);

            dbPlayer.Settings.ShowCloak = ShowCloak;
            dbPlayer.Settings.ShowHelmet = ShowHelmet;

            var newHeight = GetObjectVisualTransform(_session.Target, ObjectVisualTransform.Scale);
            dbPlayer.AppearanceScale = newHeight;

            var newHeadScale = HelmetModelRenderer.GetHeadScale(_session.Target);
            if (newHeadScale <= 0f)
                newHeadScale = 1.0f;
            dbPlayer.HeadAppearanceScale = newHeadScale;

            DB.Set(dbPlayer);
            SendMessageToPC(_session.Target, ColorToken.Green("Appearance settings saved successfully."));

            UpdateArmorDisplay();
        };

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









        public Action OnClickColorTarget(AppearanceColorTarget target, AppearanceArmorColor channel) => () =>
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            _session.ColorTarget = target;
            _session.SelectedColorChannel = channel;

            UpdateTargetedColor();
            UpdatePaletteSheetForChannel();
            AppearanceTintEditorService.LoadTintMapEditor(this);
        };

        private GuiRectangle BuildColorRegion(AppearanceColorTarget target, AppearanceArmorColor colorChannel)
        {
            var item = GetItem();
            int colorId;

            if (target == AppearanceColorTarget.Global)
            {
                colorId = GetItemAppearance(item, ItemAppearanceType.ArmorColor, (int)colorChannel);
            }
            else
            {
                var armorModel = AppearancePartEditService.GetArmorModelType(this, target);
                colorId = AppearanceColorService.GetArmorSwatchColor(this, item, armorModel, colorChannel);
            }
            return ArmorColorRegionCatalog.BuildPaletteRegion(colorId);
        }

        private void UpdateAllColors()
        {
            AppearanceTintEditorService.RefreshTintMapAvailability(this);
            foreach (var swatch in ArmorColorRegionCatalog.Entries)
            {
                Set(BuildColorRegion(swatch.Target, swatch.Channel), swatch.RegionName);
                UpdateColorSwatch(swatch.Target, swatch.Channel, swatch.RegionName);
            }
        }

        private void UpdateEditedColorSwatches()
        {
            // Only this material channel changed. Inherited parts remain empty;
            // legacy global overrides may have been normalized by the service.
            foreach (var target in ArmorColorRegionCatalog.Targets)
            {
                if (_session.ColorTarget != AppearanceColorTarget.Global && target != _session.ColorTarget)
                    continue;
                var swatch = ArmorColorRegionCatalog.Get(target, _session.SelectedColorChannel);
                var region = BuildColorRegion(target, _session.SelectedColorChannel);
                var previous = Get<GuiRectangle>(swatch.RegionName);
                if (previous == null || previous.X != region.X || previous.Y != region.Y ||
                    previous.Width != region.Width || previous.Height != region.Height)
                    Set(region, swatch.RegionName);
                UpdateColorSwatch(target, _session.SelectedColorChannel, swatch.RegionName);
            }
        }

        private void UpdateTargetedColor()
        {
            UpdateColorSelection();
            string targetName;
            string channelName;

            switch (_session.ColorTarget)
            {
                case AppearanceColorTarget.Global:
                    targetName = "Global";
                    break;
                case AppearanceColorTarget.LeftShoulder:
                    targetName = "Left Shoulder";
                    break;
                case AppearanceColorTarget.LeftBicep:
                    targetName = "Left Bicep";
                    break;
                case AppearanceColorTarget.LeftForearm:
                    targetName = "Left Forearm";
                    break;
                case AppearanceColorTarget.LeftHand:
                    targetName = "Left Hand";
                    break;
                case AppearanceColorTarget.LeftThigh:
                    targetName = "Left Thigh";
                    break;
                case AppearanceColorTarget.LeftShin:
                    targetName = "Left Shin";
                    break;
                case AppearanceColorTarget.LeftFoot:
                    targetName = "Left Foot";
                    break;
                case AppearanceColorTarget.RightShoulder:
                    targetName = "Right Shoulder";
                    break;
                case AppearanceColorTarget.RightBicep:
                    targetName = "Right Bicep";
                    break;
                case AppearanceColorTarget.RightForearm:
                    targetName = "Right Forearm";
                    break;
                case AppearanceColorTarget.RightHand:
                    targetName = "Right Hand";
                    break;
                case AppearanceColorTarget.RightThigh:
                    targetName = "Right Thigh";
                    break;
                case AppearanceColorTarget.RightShin:
                    targetName = "Right Shin";
                    break;
                case AppearanceColorTarget.RightFoot:
                    targetName = "Right Foot";
                    break;
                case AppearanceColorTarget.Neck:
                    targetName = "Neck";
                    break;
                case AppearanceColorTarget.Chest:
                    targetName = "Chest";
                    break;
                case AppearanceColorTarget.Belt:
                    targetName = "Belt";
                    break;
                case AppearanceColorTarget.Pelvis:
                    targetName = "Pelvis";
                    break;
                case AppearanceColorTarget.Robe:
                    targetName = "Robe";
                    break;
                default:
                    targetName = "Unknown";
                    break;
            }

            switch (_session.SelectedColorChannel)
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


        private void UpdateArmorDisplay()
        {
            ArmorDisplay.Apply(_session.Target, ShowHelmet, ShowCloak);
        }

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
                UpdateAllColors();
                UpdateTargetedColor();
            }
        }
    }
}
