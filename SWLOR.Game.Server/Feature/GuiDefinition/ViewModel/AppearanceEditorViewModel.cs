using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
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
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public partial class AppearanceEditorViewModel :
        GuiViewModelBase<AppearanceEditorViewModel, AppearanceEditorPayload>,
        IGuiRefreshable<EquipItemRefreshEvent>,
        IGuiRefreshable<UnequipItemRefreshEvent>,
        IGuiRefreshable<AppearanceChangedRefreshEvent>,
        IGuiRefreshable<ArmorDisplayChangedRefreshEvent>
    {
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

        [NWNEventHandler(ScriptName.OnDMPossessBefore)]
        [NWNEventHandler(ScriptName.OnDMPossessFullPowerBefore)]
        public static void CloseAppearanceWindowOnPossessionBefore()
        {
            var dm = OBJECT_SELF;
            var isUnpossess = StringToObject(EventsPlugin.GetEventData("TARGET")) == OBJECT_INVALID;

            if (isUnpossess)
            {
                var uiTarget = GetMaster(dm);

                Gui.CloseWindow(dm, GuiWindowType.AppearancePartBrowser, uiTarget);
                Gui.CloseWindow(dm, GuiWindowType.AppearanceEditor, uiTarget);
            }
            else
            {
                if (Gui.IsWindowOpen(dm, GuiWindowType.AppearanceEditor))
                    Gui.TogglePlayerWindow(dm, GuiWindowType.AppearanceEditor);
                Gui.ClosePlayerWindow(dm, GuiWindowType.AppearancePartBrowser);
            }
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
                    SynchronizeCustomTintComponents(value);
                    return;
                }

                if (!QueueTintPickerColor(value))
                {
                    DelayCommand(0.1f, StageTintPickerColor(value));
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
                        FlushPendingPickerColor();
                });
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
                    AdjustArmorPart(AppearanceArmor.LeftShoulder, 0);
            }
        }

        public int LeftBicepSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftBicep))
                    AdjustArmorPart(AppearanceArmor.LeftBicep, 0);
            }
        }
        public int LeftForearmSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftForearm))
                    AdjustArmorPart(AppearanceArmor.LeftForearm, 0);
            }
        }
        public int LeftHandSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand))
                    AdjustArmorPart(AppearanceArmor.LeftHand, 0);
            }
        }
        public int LeftThighSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftThigh))
                    AdjustArmorPart(AppearanceArmor.LeftThigh, 0);
            }
        }
        public int LeftShinSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShin))
                    AdjustArmorPart(AppearanceArmor.LeftShin, 0);
            }
        }
        public int LeftFootSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftFoot))
                    AdjustArmorPart(AppearanceArmor.LeftFoot, 0);
            }
        }
        public int RightShoulderSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShoulder))
                    AdjustArmorPart(AppearanceArmor.RightShoulder, 0);
            }
        }
        public int RightBicepSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightBicep))
                    AdjustArmorPart(AppearanceArmor.RightBicep, 0);
            }
        }
        public int RightForearmSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightForearm))
                    AdjustArmorPart(AppearanceArmor.RightForearm, 0);
            }
        }
        public int RightHandSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand))
                    AdjustArmorPart(AppearanceArmor.RightHand, 0);
            }
        }
        public int RightThighSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightThigh))
                    AdjustArmorPart(AppearanceArmor.RightThigh, 0);
            }
        }
        public int RightShinSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShin))
                    AdjustArmorPart(AppearanceArmor.RightShin, 0);
            }
        }
        public int RightFootSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightFoot))
                    AdjustArmorPart(AppearanceArmor.RightFoot, 0);
            }
        }
        public int NeckSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck))
                    AdjustArmorPart(AppearanceArmor.Neck, 0);
            }
        }
        public int ChestSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso))
                    AdjustArmorPart(AppearanceArmor.Torso, 0);
            }
        }
        public int BeltSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Belt))
                    AdjustArmorPart(AppearanceArmor.Belt, 0);
            }
        }
        public int PelvisSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis))
                    AdjustArmorPart(AppearanceArmor.Pelvis, 0);
            }
        }
        public int RobeSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe))
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
                SuspendArmorClientWatches();
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
                LoadTintMapEditor();
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
            CancelTintPickerGesture();
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
            RefreshTintMapAvailability();
            LoadTintMapEditor();
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

        private IReadOnlyList<TintMapMaterialSelection> GetCurrentTintMapSelections()
        {
            return TintMapModelResolver.GetCurrentSelections(_session.Target);
        }

        private void RefreshTintMapAvailability()
        {
            _session.TintMapSelections = GetCurrentTintMapSelections();
            IsTintMapAvailable = _session.TintMapSelections.Count > 0;
        }

        private void LoadTintMapEditor()
        {
            // Resetting legacy overrides is part of applying a color. Do not replace the
            // pending input with the old color halfway through that operation.
            if (_session.ApplyingTintColor)
                return;
            FlushPendingPickerColor();
            CommitCustomTintComponents();
            _session.TintEditGeneration++;
            _session.TintComponentCorrection = null;
            _session.HasTintComponentDraft = false;
            RefreshTintMapAvailability();
            RefreshBodySwatches();
            RefreshItemBaseSwatches();
            if (!TryGetEditableTintSelections(out var selections, out var layerType, out _))
            {
                IsCustomTintAvailable = false;
                IsCustomTintEditable = false;
                SetSelectedTintColor(GuiColor.Grey);
                return;
            }

            IsCustomTintAvailable = true;
            IsCustomTintEditable = selections.Count > 0 &&
                                   selections.All(SupportsRgb);
            CustomTintTooltip = IsCustomTintEditable ? "Apply an RGB color."
                : selections.Any(selection => selection.ArmorPart == AppearanceArmor.Robe)
                    ? "This body and robe combination supports preset colors only. Select a color from the palette above."
                    : selections.Any(selection => selection.IsWornHelmet)
                        ? "This helmet supports preset colors only on this species. Select a color from the palette above."
                        : "This part has no visible material for this color.";
            if (TryGetSelectedCustomColor(selections, layerType, out var customColor))
            {
                SetSelectedTintColor(new GuiColor(customColor.Red, customColor.Green, customColor.Blue));
                return;
            }
            if (IsEquipmentSelected && SelectedItemTypeIndex == 0)
            {
                // Native armor colors remain editable even when the current model does not
                // expose that channel. Empty part swatches inherit the global default.
                var paletteId = _session.ColorTarget == AppearanceColorTarget.Global
                    ? 255
                    : GetArmorSwatchColor(GetItem(), GetArmorModelType(_session.ColorTarget), _session.SelectedColorChannel);
                if (paletteId == 255)
                    paletteId = GetItemAppearance(GetItem(), ItemAppearanceType.ArmorColor, (int)_session.SelectedColorChannel);
                var globalColor = TintMapPaletteColors.GetColor(layerType, paletteId);
                SetLoadedTintColor(globalColor, layerType, paletteId);
                return;
            }
            if (selections.Count == 0)
            {
                var paletteId = IsAppearanceSelected
                    ? GetColor(_session.Target, (ColorChannel)SelectedColorCategoryIndex)
                    : GetItemAppearance(GetItem(), ItemAppearanceType.ArmorColor, SelectedColorCategoryIndex);
                var fallbackColor = TintMapPaletteColors.GetColor(layerType, paletteId);
                SetLoadedTintColor(fallbackColor, layerType, paletteId);
                return;
            }
            var effectiveColors = selections
                .Select(selection => TintMapColorResolver.GetEffectiveDisplayColor(
                    _session.Target,
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
                    ? GetColor(_session.Target, (ColorChannel)SelectedColorCategoryIndex)
                    : GetItemAppearance(GetItem(), ItemAppearanceType.ArmorColor, SelectedColorCategoryIndex);
                SetLoadedTintColor(color, layerType, paletteId);
                return;
            }

            SetSelectedTintColor(GuiColor.Grey);
        }

        private void SetLoadedTintColor(TintMapColor color, TintMapLayerType layerType, int paletteId)
        {
            SetSelectedTintColor(new GuiColor(color.Red, color.Green, color.Blue));
        }

        private static bool SupportsRgb(TintMapMaterialSelection selection) =>
            RobeModelRenderer.SupportsRgb(selection) && HelmetModelRenderer.SupportsRgb(selection);

        private bool TryGetSelectedCustomColor(IReadOnlyList<TintMapMaterialSelection> selections,
            TintMapLayerType layer, out TintMapColor color)
        {
            if (IsAppearanceSelected)
                return TintMapColor.TryFromStoredValue(
                    GetLocalInt(_session.Target, TintMapVariable.GetCreatureColorStateName(layer)), out color);
            if (SelectedItemTypeIndex != 0 || _session.ColorTarget == AppearanceColorTarget.Global)
                return TintMapColor.TryFromStoredValue(
                    GetLocalInt(GetItem(), TintMapVariable.GetItemGlobalColorStateName(layer)), out color);
            var customColors = selections
                .Where(selection => TintMapColorResolver.TryGetCustomColor(selection, layer, out _))
                .Select(selection => TintMapColorResolver.GetEffectiveDisplayColor(_session.Target, selection, layer))
                .Distinct().ToList();
            color = customColors.Count == 1 ? customColors[0] : default;
            return customColors.Count == 1;
        }

        private void SetSelectedTintColor(
            GuiColor color,
            bool synchronizeComponents = true)
        {
            CancelTintPickerGesture();
            SynchronizeTintControlBindings(() =>
            {
                _session.LoadingTintColor = true;
                try
                {
                    if (synchronizeComponents)
                        SelectedTintColor = color;
                    else
                        Set(color, nameof(SelectedTintColor));
                }
                finally
                {
                    _session.LoadingTintColor = false;
                }
            });
        }

        private void ApplyCustomTintColor(
            GuiColor value,
            bool synchronizeComponents,
            bool synchronizePicker = true,
            Action<TintMapColor> capturedEdit = null)
        {
            if (_session.LoadingTintColor)
            {
                if (synchronizeComponents)
                    SynchronizeCustomTintComponents(value);
                return;
            }
            var apply = capturedEdit ?? CaptureTintColorEdit();
            if (apply == null)
            {
                LoadTintMapEditor();
                return;
            }

            var requestedColor = new TintMapColor(value.R, value.G, value.B);
            _session.ApplyingTintColor = true;
            try
            {
                apply(requestedColor);
                if (IsEquipmentSelected && SelectedItemTypeIndex == 0)
                    UpdateEditedColorSwatches();
            }
            finally
            {
                _session.ApplyingTintColor = false;
            }

            // Persist the actual RGB on the creature/item. Never change a native preset,
            // replace equipment, or echo normalized text into fields while the user types.
            if (synchronizePicker)
                SetSelectedTintColor(value, synchronizeComponents);
            else if (synchronizeComponents)
                SynchronizeCustomTintComponents(value);
        }

        private Action<TintMapColor> CaptureTintColorEdit()
        {
            if (!TryGetEditableTintSelections(out var selections, out var layerType, out _) ||
                selections.Count == 0 || !selections.All(SupportsRgb))
                return null;

            // NUI hydrates selection binds before their setters run. A pending text edit
            // must retain its original layer and owner when the next selection loads.
            var target = _session.Target;
            var appearance = IsAppearanceSelected;
            var globalItem = SelectedItemTypeIndex != 0 || _session.ColorTarget == AppearanceColorTarget.Global;
            var item = appearance ? OBJECT_INVALID : GetItem();
            return color =>
            {
                if (!GetIsObjectValid(target) || !appearance && !GetIsObjectValid(item))
                    return;
                if (appearance)
                    TintMapCreatureColor.SetCreatureCustomColor(target, selections, layerType, color);
                else if (globalItem)
                    TintMapItemColor.SetGlobalItemCustomColor(target, selections, layerType, color, item);
                else
                    foreach (var selection in selections)
                        TintMapItemColor.SetColor(target, selection, layerType, color);
            };
        }

        public Action OnMouseDownTintPicker() => () =>
        {
            var payload = NuiGetEventPayload();
            var button = JsonGetInt(JsonObjectGet(payload, "mouse_btn"));
            BeginTintPickerGesture((NuiMouseButton)button,
                (NuiMouseButton)button == NuiMouseButton.Left ? CaptureTintColorEdit() : null);
        };

        private void BeginTintPickerGesture(NuiMouseButton button, Action<TintMapColor> apply)
        {
            var color = _session.UnclaimedPickerColor;
            _session.UnclaimedPickerColor = null;
            _session.UnclaimedPickerGeneration++;
            if (button == NuiMouseButton.Left)
            {
                _session.TintPickerActive = true;
                _session.TintPickerApply = apply;
                if (color != null)
                {
                    QueueTintPickerColor(color);
                    FlushPendingPickerColor();
                }
            }
        }

        private Action StageTintPickerColor(GuiColor color)
        {
            // The native picker sends a click's color watch before mousedown.
            // Hold it briefly for that press, without applying unsolicited hydration.
            _session.UnclaimedPickerColor = color;
            var generation = ++_session.UnclaimedPickerGeneration;
            var token = WindowToken;
            return () =>
            {
                if (generation == _session.UnclaimedPickerGeneration && token == WindowToken)
                    _session.UnclaimedPickerColor = null;
            };
        }

        private bool QueueTintPickerColor(GuiColor value)
        {
            // A newly created NUI picker can report its default black value. Only
            // an explicit pointer gesture may turn a watched value into a tint edit.
            if (!_session.TintPickerActive)
                return false;

            _session.TintEditGeneration++;
            _session.TintComponentCorrection = null;
            _session.HasTintComponentDraft = false;
            _session.PendingPickerColor = value;
            _session.PendingPickerApply = _session.TintPickerApply;
            return true;
        }

        public Action OnMouseUpTintPicker() => () =>
        {
            var payload = NuiGetEventPayload();
            var button = JsonGetInt(JsonObjectGet(payload, "mouse_btn"));
            ReleaseTintPickerGesture((NuiMouseButton)button);
        };

        private void ReleaseTintPickerGesture(NuiMouseButton button)
        {
            if (button != NuiMouseButton.Left || !_session.TintPickerActive)
                return;

            FlushPendingPickerColor();
            CancelTintPickerGesture();
        }

        private void CancelTintPickerGesture()
        {
            _session.UnclaimedPickerColor = null;
            _session.UnclaimedPickerGeneration++;
            _session.TintPickerActive = false;
            _session.TintPickerApply = null;
            _session.PendingPickerColor = null;
            _session.PendingPickerApply = null;
        }

        private void FlushPendingPickerColor()
        {
            var color = _session.PendingPickerColor;
            var apply = _session.PendingPickerApply;
            _session.PendingPickerColor = null;
            _session.PendingPickerApply = null;
            if (color != null && apply != null)
                ApplyCustomTintColor(color, synchronizeComponents: true, synchronizePicker: false, capturedEdit: apply);
        }

        private void SynchronizeCustomTintComponents(GuiColor color)
        {
            SynchronizeTintControlBindings(() =>
            {
                var wasLoading = _session.LoadingTintColor;
                _session.LoadingTintColor = true;
                try
                {
                    Set(color.R.ToString(), nameof(CustomTintRed));
                    Set(color.G.ToString(), nameof(CustomTintGreen));
                    Set(color.B.ToString(), nameof(CustomTintBlue));
                }
                finally
                {
                    _session.LoadingTintColor = wasLoading;
                }
            });
        }

        private void SynchronizeTintControlBindings(Action update)
        {
            var restoreWatches = _session.TintControlBindingsWatched;
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
            _session.TintControlBindingsWatched = watched;
        }

        private void SetCustomTintComponent(string value, string propertyName)
        {
            _session.PendingPickerColor = null;
            var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
            var normalized = int.TryParse(digits, out var component)
                ? component <= byte.MaxValue ? digits : byte.MaxValue.ToString()
                : string.Empty;
            Set(normalized, propertyName);

            if (_session.LoadingTintColor)
                return;

            _session.TintComponentCorrection = normalized == value ? null : propertyName;
            if (!_session.HasTintComponentDraft)
                _session.TintComponentApply = CaptureTintColorEdit();
            _session.HasTintComponentDraft = true;

            // Keep the three inputs as a draft while typing; applying each digit used to
            // reload the old color and overwrite the other two channels mid-edit.
            var generation = ++_session.TintEditGeneration;
            var token = WindowToken;
            DelayCommand(0.4f, () =>
            {
                if (generation == _session.TintEditGeneration && token == WindowToken &&
                    Gui.IsWindowOpen(Player, WindowType))
                    CommitCustomTintComponents();
            });
        }

        private void CommitCustomTintComponents()
        {
            if (!_session.HasTintComponentDraft || !byte.TryParse(CustomTintRed, out var red) ||
                !byte.TryParse(CustomTintGreen, out var green) ||
                !byte.TryParse(CustomTintBlue, out var blue))
                return;
            _session.HasTintComponentDraft = false;
            _session.TintEditGeneration++;
            var apply = _session.TintComponentApply;
            _session.TintComponentApply = null;
            if (apply != null)
                ApplyCustomTintColor(new GuiColor(red, green, blue), synchronizeComponents: false, capturedEdit: apply);
        }

        protected override void OnClientPropertyUpdated(string propertyName)
        {
            if (_session.IsDMArmorDisplay && propertyName is nameof(ShowHelmet) or nameof(ShowCloak))
                ApplyDMArmorDisplay();

            if (_session.TintComponentCorrection == propertyName)
            {
                _session.TintComponentCorrection = null;
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

            var paletteSource = IsAppearanceSelected ? _session.Target : GetItem();
            if (!GetIsObjectValid(paletteSource))
                return false;

            var restrictToArmorPart = IsEquipmentSelected &&
                                      SelectedItemTypeIndex == 0 &&
                                      _session.ColorTarget != AppearanceColorTarget.Global;
            var armorPart = restrictToArmorPart
                ? GetArmorModelType(_session.ColorTarget)
                : AppearanceArmor.Invalid;
            var selectedLayerType = layerType;
            selections = _session.TintMapSelections
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
                if (_session.ColorTarget == AppearanceColorTarget.Invalid)
                    return false;

                layerType = _session.SelectedColorChannel switch
                {
                    AppearanceArmorColor.Leather1 => TintMapLayerType.Leather1,
                    AppearanceArmorColor.Leather2 => TintMapLayerType.Leather2,
                    AppearanceArmorColor.Cloth1 => TintMapLayerType.Cloth1,
                    AppearanceArmorColor.Cloth2 => TintMapLayerType.Cloth2,
                    AppearanceArmorColor.Metal1 => TintMapLayerType.Metal1,
                    AppearanceArmorColor.Metal2 => TintMapLayerType.Metal2,
                    _ => default
                };
                return _session.SelectedColorChannel is >= AppearanceArmorColor.Leather1 and <= AppearanceArmorColor.Metal2;
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
            _session.ArmorClientBindingsWatched = true;
        }

        private void SuspendArmorClientWatches()
        {
            _session.ArmorBindingGeneration++;
            if (!_session.ArmorClientBindingsWatched)
                return;
            foreach (var target in ArmorColorRegionCatalog.Targets.Where(target => target != AppearanceColorTarget.Global))
                NuiSetBindWatch(Player, WindowToken, target + "Selection", false);
            _session.ArmorClientBindingsWatched = false;
        }

        private void ResumeArmorClientWatches()
        {
            var generation = _session.ArmorBindingGeneration;
            // Option-list replacement can transiently select the first client combo entry.
            // Keep that hydration outside the watched editing path, including repeated tab visits.
            DelayCommand(3f, () =>
            {
                if (generation == _session.ArmorBindingGeneration)
                    StartArmorClientWatches();
            });
        }

        private void LoadItemTypeEditor()
        {
            SuspendArmorClientWatches();
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
            SuspendArmorClientWatches();
            var isArmorEditor = IsEquipmentSelected && SelectedItemTypeIndex == 0;
            SynchronizeTintControlBindings(() =>
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
                ResumeArmorClientWatches();
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
            CommitCustomTintComponents();
            _session.SelectedTabId = tabId;
            EditorToggles.SyncTo(tabId, value => EditorTabToggleValue = value);
            SettingsToggles.SyncTo(tabId, value => SettingsTabToggleValue = value);
            OutfitsToggles.SyncTo(tabId, value => OutfitsTabToggleValue = value);
            IsAppearanceSelected = tabId == AppearanceTabId;
            IsEquipmentSelected = tabId == EquipmentTabId;
            IsSettingsSelected = tabId == SettingsTabId;
            IsOutfitsSelected = tabId == OutfitsTabId;
            SuspendArmorClientWatches();

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
            SuspendArmorClientWatches();
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
                ResumeArmorClientWatches();
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
                    var offHand = GetItemInSlot(InventorySlot.LeftHand, _session.Target);
                    slot = GetOffHandAppearanceOwner(offHand) != offHand
                        ? InventorySlot.RightHand : InventorySlot.LeftHand;
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
            FlushPendingPickerColor();
            CommitCustomTintComponents();
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
            var tintCarry = TintMapCarry.CaptureItemCustomColors(item, previousTintSelections);
            if (colorId > -1)
            {
                EquippedItemAppearance.Set(item, ItemAppearanceType.WeaponColor, type, colorId);
                partId %= 100;
            }

            if (WeaponAppearanceCatalog.Definitions.TryGetValue(itemType, out var weapon) && weapon.IsSimple)
                modelType = ItemAppearanceType.SimpleModel;

            EquippedItemAppearance.Set(item, modelType, type, partId);
            // The original item stays equipped. Its new material keys are available now,
            // so carry custom tints before another model/preset click can arrive.
            TintMapCarry.QueueItemCustomColorCarry(
                _session.Target, item, item, Player, slot, armorPart, tintCarry, applyImmediately: true);
            EquippedItemAppearance.Refresh(_session.Target, item);
        }

        private void SynchronizeCustomTintControlsToPaletteColor(int colorId)
        {
            if (!TryGetSelectedTintLayer(out var layerType))
                return;

            var color = TintMapPaletteColors.GetColor(layerType, colorId);
            SetSelectedTintColor(new GuiColor(color.Red, color.Green, color.Blue));
        }

        private bool ApplySelectedPaletteColor(int colorId, bool reloadEditor = true)
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return false;

            if (colorId < 0 || colorId >= TintMapMaterialRegistry.PaletteColorCount)
                return false;

            // A newer preset selection replaces any uncommitted text for this target.
            _session.HasTintComponentDraft = false;
            _session.TintComponentApply = null;
            _session.PendingPickerColor = null;
            _session.PendingPickerApply = null;

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
                        SetColor(_session.Target, ColorChannel.Skin, colorId);
                        break;
                    case 1: //  1 = Hair
                        SetColor(_session.Target, ColorChannel.Hair, colorId);
                        break;
                    case 2: // 2 = Tattoo 1
                        SetColor(_session.Target, ColorChannel.Tattoo1, colorId);
                        break;
                    case 3: // 3 = Tattoo 2
                        SetColor(_session.Target, ColorChannel.Tattoo2, colorId);
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

            TintMapApply.RefreshAfterColorChange(_session.Target);
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
                    TintMapCreatureColor.ResetCreatureCustomColor(_session.Target, selectedLayerType);
                    LoadTintMapEditor();
                }
                else if (IsEquipmentSelected && SelectedItemTypeIndex is >= 0 and <= 2)
                {
                    var item = GetItem();
                    if (GetIsObjectValid(item))
                    {
                        TintMapItemColor.ResetInactiveItemCustomColor(
                            _session.Target,
                            item,
                            selectedLayerType,
                            SelectedItemTypeIndex != 0 || _session.ColorTarget == AppearanceColorTarget.Global
                                ? AppearanceArmor.Invalid
                                : GetArmorModelType(_session.ColorTarget));
                        LoadTintMapEditor();
                    }
                }

                return;
            }

            if (IsEquipmentSelected &&
                (SelectedItemTypeIndex != 0 || _session.ColorTarget == AppearanceColorTarget.Global))
            {
                if (selections.Count == 0)
                    TintMapItemColor.ResetInactiveItemCustomColor(_session.Target, GetItem(), layerType, AppearanceArmor.Invalid);
                else
                    TintMapItemColor.ResetGlobalItemCustomColor(_session.Target, selections, layerType);
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
                TintMapCreatureColor.ResetCreatureCustomColor(_session.Target, layerType);
            }
            else
            {
                foreach (var selection in selections)
                {
                    TintMapItemColor.ResetColor(_session.Target, selection, layerType, paletteColor);
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

            EquippedItemAppearance.Set(item, ItemAppearanceType.ArmorColor, colorIndex, colorId);
            EquippedItemAppearance.Refresh(_session.Target, item, resetShaderOverrides: false);
        }

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
            LoadTintMapEditor();
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
                ModifyItemPart((int)AppearanceArmor.Invalid, _armorAppearances[appearanceType].Helmet[SelectedPartIndex]);
            }
            else if (SelectedItemTypeIndex == 2) // 2 = Cloak
            {
                ModifyItemPart((int)AppearanceArmor.Invalid, _armorAppearances[appearanceType].Cloak[SelectedPartIndex]);
            }
            else if (SelectedItemTypeIndex == 3 || SelectedItemTypeIndex == 4) // 3 = Weapon (Main Hand), 4 = Weapon (Off Hand)
            {
                if (WeaponAppearanceCatalog.Definitions[itemType].IsSimple)
                {
                    ModifyItemPart((int)ItemAppearanceType.SimpleModel, WeaponAppearanceCatalog.Definitions[itemType].SimpleParts[SelectedPartIndex]);
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
                    ModifyItemPart((int)weaponPart, selected % 100, selected / 100);
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
            FlushPendingPickerColor();
            CancelTintPickerGesture();
            CommitCustomTintComponents();
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
            if (ApplySelectedPaletteColor(colorId))
                SynchronizeCustomTintControlsToPaletteColor(colorId);
        };


        private bool ApplyArmorPaletteColor(int colorId)
        {
            if (_session.ColorTarget == AppearanceColorTarget.Invalid)
                return false;

            ResetCurrentCustomTintOverrides(colorId);
            var item = GetItem();
            if (!GetIsObjectValid(item))
                return false;

            int colorIndex;

            if (_session.ColorTarget == AppearanceColorTarget.Global)
            {
                colorIndex = (int)_session.SelectedColorChannel;
            }
            else
            {
                var armorModel = GetArmorModelType(_session.ColorTarget);
                colorIndex = ArmorColorIndexCalculator.CalculatePerPart(armorModel, _session.SelectedColorChannel);
                SetLocalInt(
                    item,
                    ArmorColorIndexCalculator.GetPerPartOverrideVariableName(
                        armorModel,
                        _session.SelectedColorChannel),
                    1);
            }

            SetItemColorInPlace(item, colorIndex, colorId);
            ChangeColor(_session.ColorTarget, _session.SelectedColorChannel, colorId);
            return true;
        }

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

            ResetArmorColorToInheritance(colorTarget, colorChannel);
        };

        private void ResetArmorColorToInheritance(AppearanceColorTarget colorTarget, AppearanceArmorColor colorChannel)
        {
            // Settle the previous edit before clearing this part. Reloading afterward
            // must not replay a draft over the user's newer inheritance reset.
            FlushPendingPickerColor();
            CommitCustomTintComponents();
            ResetCustomTintOverrides(colorTarget, colorChannel);

            var item = GetItem();
            var armorModel = GetArmorModelType(colorTarget);
            var index = ArmorColorIndexCalculator.CalculatePerPart(armorModel, colorChannel);
            DeleteLocalInt(
                item,
                ArmorColorIndexCalculator.GetPerPartOverrideVariableName(armorModel, colorChannel));

            SetItemColorInPlace(item, index, 255);
            ChangeColor(colorTarget, colorChannel, 255);
            if (_session.ColorTarget == colorTarget && _session.SelectedColorChannel == colorChannel)
                LoadTintMapEditor();
        }

        private void ResetCustomTintOverrides(
            AppearanceColorTarget colorTarget,
            AppearanceArmorColor colorChannel)
        {
            if (!TryGetArmorTintLayer(colorChannel, out var layerType))
                return;

            var item = GetItem();
            if (!GetIsObjectValid(item))
                return;


            var armorPart = colorTarget == AppearanceColorTarget.Global
                ? AppearanceArmor.Invalid
                : GetArmorModelType(colorTarget);
            var selections = _session.TintMapSelections
                .Where(selection =>
                    selection.GetPaletteSource(layerType) == item &&
                    selection.Material.Layers.Contains(layerType) &&
                    (armorPart == AppearanceArmor.Invalid || selection.ArmorPart == armorPart))
                .ToList();
            if (selections.Count == 0)
            {
                TintMapItemColor.ResetInactiveItemCustomColor(
                    _session.Target,
                    item,
                    layerType,
                    armorPart);
                return;
            }

            foreach (var selection in selections)
            {
                TintMapItemColor.ResetColorToInheritance(_session.Target, selection, layerType);
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
                foreach (var selection in _session.TintMapSelections.Where(selection =>
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

        private AppearanceArmor GetArmorModelType(AppearanceColorTarget colorTarget)
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

        public Action OnClickColorTarget(AppearanceColorTarget target, AppearanceArmorColor channel) => () =>
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            _session.ColorTarget = target;
            _session.SelectedColorChannel = channel;

            UpdateTargetedColor();
            UpdatePaletteSheetForChannel();
            LoadTintMapEditor();
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
                var armorModel = GetArmorModelType(target);
                colorId = GetArmorSwatchColor(item, armorModel, colorChannel);
            }
            return ArmorColorRegionCatalog.BuildPaletteRegion(colorId);
        }

        private void UpdateAllColors()
        {
            RefreshTintMapAvailability();
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

        private static int? ResolveAdjustedArmorValue(GuiBindingList<GuiComboEntry> options, int value, int adjustBy)
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

        private void AdjustArmorPart(AppearanceArmor partType, int adjustBy)
        {
            if (!IsEquipmentSelected || SelectedItemTypeIndex != 0 || !HasItemEquipped)
                return;

            if (partType == AppearanceArmor.Robe)
            {
                // A filtered option's value is the model ID; its index no longer
                // corresponds to the shared, unfiltered armor definition.
                var styles = RobeAppearance.GetAvailableStyles(_session.Target, _armorAppearances[GetAppearanceType(_session.Target)].Robe);
                var requested = RobeSelection;
                var index = styles.ToList().IndexOf(requested);
                if (index < 0 || styles.Count == 0)
                {
                    LoadItemParts();
                    return;
                }
                var selected = styles[System.Math.Clamp(index + adjustBy, 0, styles.Count - 1)];
                var wasSkipping = _session.SkipAdjustArmorPart;
                _session.SkipAdjustArmorPart = true;
                try
                {
                    RobeSelection = selected;
                    ModifyItemPart((int)partType, selected);
                }
                finally
                {
                    _session.SkipAdjustArmorPart = wasSkipping;
                }
                return;
            }
            void Adjust(GuiBindingList<GuiComboEntry> options, int selection, Action<int> setSelection)
            {
                var selected = ResolveAdjustedArmorValue(options, selection, adjustBy);
                if (!selected.HasValue)
                    return;

                setSelection(selected.Value);
                ModifyItemPart((int)partType, selected.Value);
            }

            var wasSkippingAdjustment = _session.SkipAdjustArmorPart;
            _session.SkipAdjustArmorPart = true;
            try
            {
                switch (partType)
                {
                    case AppearanceArmor.RightFoot:
                        Adjust(RightFootOptions, RightFootSelection, value => RightFootSelection = value);
                        break;
                    case AppearanceArmor.LeftFoot:
                        Adjust(LeftFootOptions, LeftFootSelection, value => LeftFootSelection = value);
                        break;
                    case AppearanceArmor.RightShin:
                        Adjust(RightShinOptions, RightShinSelection, value => RightShinSelection = value);
                        break;
                    case AppearanceArmor.LeftShin:
                        Adjust(LeftShinOptions, LeftShinSelection, value => LeftShinSelection = value);
                        break;
                    case AppearanceArmor.LeftThigh:
                        Adjust(LeftThighOptions, LeftThighSelection, value => LeftThighSelection = value);
                        break;
                    case AppearanceArmor.RightThigh:
                        Adjust(RightThighOptions, RightThighSelection, value => RightThighSelection = value);
                        break;
                    case AppearanceArmor.Pelvis:
                        Adjust(PelvisOptions, PelvisSelection, value => PelvisSelection = value);
                        break;
                    case AppearanceArmor.Torso:
                        Adjust(ChestOptions, ChestSelection, value => ChestSelection = value);
                        break;
                    case AppearanceArmor.Belt:
                        Adjust(BeltOptions, BeltSelection, value => BeltSelection = value);
                        break;
                    case AppearanceArmor.Neck:
                        Adjust(NeckOptions, NeckSelection, value => NeckSelection = value);
                        break;
                    case AppearanceArmor.RightForearm:
                        Adjust(RightForearmOptions, RightForearmSelection, value => RightForearmSelection = value);
                        break;
                    case AppearanceArmor.LeftForearm:
                        Adjust(LeftForearmOptions, LeftForearmSelection, value => LeftForearmSelection = value);
                        break;
                    case AppearanceArmor.RightBicep:
                        Adjust(RightBicepOptions, RightBicepSelection, value => RightBicepSelection = value);
                        break;
                    case AppearanceArmor.LeftBicep:
                        Adjust(LeftBicepOptions, LeftBicepSelection, value => LeftBicepSelection = value);
                        break;
                    case AppearanceArmor.RightShoulder:
                        Adjust(RightShoulderOptions, RightShoulderSelection, value => RightShoulderSelection = value);
                        break;
                    case AppearanceArmor.LeftShoulder:
                        Adjust(LeftShoulderOptions, LeftShoulderSelection, value => LeftShoulderSelection = value);
                        break;
                    case AppearanceArmor.RightHand:
                        Adjust(RightHandOptions, RightHandSelection, value => RightHandSelection = value);
                        break;
                    case AppearanceArmor.LeftHand:
                        Adjust(LeftHandOptions, LeftHandSelection, value => LeftHandSelection = value);
                        break;
                }
            }
            finally
            {
                _session.SkipAdjustArmorPart = wasSkippingAdjustment;
            }
        }

        public Action OnClickAdjustArmorPart(AppearanceArmor partType, int adjustBy) => () =>
        {
            if (!IsEquipmentSelected || SelectedItemTypeIndex != 0)
                return;

            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            AdjustArmorPart(partType, adjustBy);
        };


        private (GuiBindingList<GuiComboEntry> Options, Action<int> Select) GetArmorPartBinding(AppearanceArmor partType)
        {
            return partType switch
            {
                AppearanceArmor.RightFoot => (RightFootOptions, value => RightFootSelection = value),
                AppearanceArmor.LeftFoot => (LeftFootOptions, value => LeftFootSelection = value),
                AppearanceArmor.RightShin => (RightShinOptions, value => RightShinSelection = value),
                AppearanceArmor.LeftShin => (LeftShinOptions, value => LeftShinSelection = value),
                AppearanceArmor.LeftThigh => (LeftThighOptions, value => LeftThighSelection = value),
                AppearanceArmor.RightThigh => (RightThighOptions, value => RightThighSelection = value),
                AppearanceArmor.Pelvis => (PelvisOptions, value => PelvisSelection = value),
                AppearanceArmor.Torso => (ChestOptions, value => ChestSelection = value),
                AppearanceArmor.Belt => (BeltOptions, value => BeltSelection = value),
                AppearanceArmor.Neck => (NeckOptions, value => NeckSelection = value),
                AppearanceArmor.RightForearm => (RightForearmOptions, value => RightForearmSelection = value),
                AppearanceArmor.LeftForearm => (LeftForearmOptions, value => LeftForearmSelection = value),
                AppearanceArmor.RightBicep => (RightBicepOptions, value => RightBicepSelection = value),
                AppearanceArmor.LeftBicep => (LeftBicepOptions, value => LeftBicepSelection = value),
                AppearanceArmor.RightShoulder => (RightShoulderOptions, value => RightShoulderSelection = value),
                AppearanceArmor.LeftShoulder => (LeftShoulderOptions, value => LeftShoulderSelection = value),
                AppearanceArmor.RightHand => (RightHandOptions, value => RightHandSelection = value),
                AppearanceArmor.LeftHand => (LeftHandOptions, value => LeftHandSelection = value),
                AppearanceArmor.Robe => (RobeOptions, value => RobeSelection = value),
                _ => (null, null)
            };
        }

        private void CopyColors(uint item, AppearanceColorTarget copyToTarget, AppearanceColorTarget copyFromTarget)
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
                CopyColor(item, copyToTarget, copyFrom, copyTo, colorChannel);
            }

            TintMapModelResolver.CopyArmorPartTintOverrides(
                _session.Target,
                item,
                copyFrom,
                copyTo);
        }

        private void CopyColor(
            uint item,
            AppearanceColorTarget copyToTarget,
            AppearanceArmor copyFrom,
            AppearanceArmor copyTo,
            AppearanceArmorColor colorChannel)
        {
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
            EquippedItemAppearance.Set(item, ItemAppearanceType.ArmorColor,
                copyToIndex, sourceUsesPerPartColor ? sourceColor : 255);
            if (sourceUsesPerPartColor)
                MarkPerPartColorOverride(item, copyTo, colorChannel);
            else
                ClearPerPartColorOverride(item, copyTo, colorChannel);
        }

        public Action OnClickCopyToRight() => () => CopyArmorSide(copyToRight: true);

        public Action OnClickCopyToLeft() => () => CopyArmorSide(copyToRight: false);

        private void CopyArmorSide(bool copyToRight)
        {
            FlushPendingPickerColor();
            CommitCustomTintComponents();
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped || SelectedItemTypeIndex != 0)
                return;

            var item = GetItem();
            _session.SkipAdjustArmorPart = true;
            IsCopyEnabled = false;
            try
            {
                foreach (var (left, right) in new[]
                         {
                             (AppearanceColorTarget.LeftShoulder, AppearanceColorTarget.RightShoulder),
                             (AppearanceColorTarget.LeftBicep, AppearanceColorTarget.RightBicep),
                             (AppearanceColorTarget.LeftForearm, AppearanceColorTarget.RightForearm),
                             (AppearanceColorTarget.LeftHand, AppearanceColorTarget.RightHand),
                             (AppearanceColorTarget.LeftThigh, AppearanceColorTarget.RightThigh),
                             (AppearanceColorTarget.LeftShin, AppearanceColorTarget.RightShin),
                             (AppearanceColorTarget.LeftFoot, AppearanceColorTarget.RightFoot)
                         })
                {
                    var source = copyToRight ? left : right;
                    var destination = copyToRight ? right : left;
                    CopyColors(item, destination, source);
                    EquippedItemAppearance.Set(item, ItemAppearanceType.ArmorModel,
                        (int)GetArmorModelType(destination),
                        GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)GetArmorModelType(source)));
                }

                EquippedItemAppearance.Refresh(_session.Target, item);
                LoadItemParts();
            }
            finally
            {
                _session.SkipAdjustArmorPart = false;
                IsCopyEnabled = true;
            }
        }

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
