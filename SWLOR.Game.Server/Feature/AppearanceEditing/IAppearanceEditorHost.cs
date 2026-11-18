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
    /// <summary>
    /// What the appearance editor services need from the view model that hosts them: its per-window session, the window it is
    /// bound to, the bindings by name, and the few view model members the moved code reads or writes. The view model implements
    /// it explicitly by delegating to the members it already has, so a property setter keeps its own logic.
    /// </summary>
    public interface IAppearanceEditorHost
    {
        AppearanceEditorSession Session { get; }
        uint Player { get; }
        int WindowToken { get; }
        GuiWindowType WindowType { get; }
        T Get<T>(string name);
        void Set<T>(T value, string name);
        (GuiBindingList<GuiComboEntry> Options, int Selection, Action<int> Select) GetArmorPartBinding(AppearanceArmor partType);
        void WatchArmorSelections();

        /// <summary>Whether the window this editor is bound to is still open for its player.</summary>
        bool IsWindowOpen();

        GuiBindingList<GuiComboEntry> BeltOptions { get; set; }
        int BeltSelection { get; set; }
        GuiBindingList<GuiComboEntry> ChestOptions { get; set; }
        int ChestSelection { get; set; }
        GuiBindingList<string> ColorCategoryOptions { get; set; }
        GuiBindingList<bool> ColorCategorySelected { get; set; }
        string ColorSheetResref { get; set; }
        string ColorTargetText { get; set; }
        GuiRectangle Geometry { get; set; }
        bool IsArmorDisplayVisible { get; set; }
        bool IsColorPickerVisible { get; set; }
        bool IsNoEditableItemVisible { get; set; }
        bool IsOutfitRenameEnabled { get; set; }
        bool IsOutfitSelected { get; set; }
        bool IsOutfitWearEnabled { get; set; }
        bool IsOutfitsSelected { get; set; }
        bool IsOutfitsVisible { get; set; }
        bool IsPaletteSelectionVisible { get; set; }
        bool IsPartColorsVisible { get; set; }
        bool IsPartPreviewEnabled { get; set; }
        bool IsPlayerSettingsVisible { get; set; }
        bool IsPreviewViewChoiceVisible { get; set; }
        bool IsQuickWearEnabled { get; set; }
        bool IsSettingsSelected { get; set; }
        bool IsSettingsVisible { get; set; }
        bool IsUndoOutfitEnabled { get; set; }
        GuiBindingList<GuiComboEntry> LeftBicepOptions { get; set; }
        int LeftBicepSelection { get; set; }
        GuiBindingList<GuiComboEntry> LeftFootOptions { get; set; }
        int LeftFootSelection { get; set; }
        GuiBindingList<GuiComboEntry> LeftForearmOptions { get; set; }
        int LeftForearmSelection { get; set; }
        GuiBindingList<GuiComboEntry> LeftHandOptions { get; set; }
        int LeftHandSelection { get; set; }
        GuiBindingList<GuiComboEntry> LeftShinOptions { get; set; }
        int LeftShinSelection { get; set; }
        GuiBindingList<GuiComboEntry> LeftShoulderOptions { get; set; }
        int LeftShoulderSelection { get; set; }
        GuiBindingList<GuiComboEntry> LeftThighOptions { get; set; }
        int LeftThighSelection { get; set; }
        GuiBindingList<GuiComboEntry> NeckOptions { get; set; }
        int NeckSelection { get; set; }
        string OutfitDetails { get; set; }
        string OutfitName { get; set; }
        GuiBindingList<string> OutfitSlotNames { get; set; }
        GuiBindingList<bool> OutfitSlotToggles { get; set; }
        int OutfitsTabToggleValue { get; set; }
        GuiBindingList<float> PaletteSelectionPoints { get; set; }
        GuiBindingList<string> PartCategoryOptions { get; set; }
        GuiBindingList<bool> PartCategorySelected { get; set; }
        GuiBindingList<string> PartOptions { get; set; }
        string PartPreviewImage { get; set; }
        string PartPreviewLabel { get; set; }
        string PartPreviewNumber { get; set; }
        GuiBindingList<bool> PartSelected { get; set; }
        GuiBindingList<GuiComboEntry> PelvisOptions { get; set; }
        int PelvisSelection { get; set; }
        string PreviewViewText { get; set; }
        GuiBindingList<GuiComboEntry> QuickOutfitOptions { get; set; }
        int QuickOutfitValue { get; set; }
        GuiBindingList<GuiComboEntry> RightBicepOptions { get; set; }
        int RightBicepSelection { get; set; }
        GuiBindingList<GuiComboEntry> RightFootOptions { get; set; }
        int RightFootSelection { get; set; }
        GuiBindingList<GuiComboEntry> RightForearmOptions { get; set; }
        int RightForearmSelection { get; set; }
        GuiBindingList<GuiComboEntry> RightHandOptions { get; set; }
        int RightHandSelection { get; set; }
        GuiBindingList<GuiComboEntry> RightShinOptions { get; set; }
        int RightShinSelection { get; set; }
        GuiBindingList<GuiComboEntry> RightShoulderOptions { get; set; }
        int RightShoulderSelection { get; set; }
        GuiBindingList<GuiComboEntry> RightThighOptions { get; set; }
        int RightThighSelection { get; set; }
        GuiBindingList<GuiComboEntry> RobeOptions { get; set; }
        int SelectedPartCategoryIndex { get; set; }
        int SelectedPartIndex { get; set; }
        bool ShowCloak { get; set; }
        bool ShowHelmet { get; set; }
        string WardrobeCapacityText { get; set; }
        bool IsPartEditorHidden { get; }

        void LoadBodyParts();
        void LoadPart();
        void RefreshTintMapEditorAfterAppearanceChange(bool refreshItemParts = false);
        void ShowModal(string prompt, Action confirmAction);
        string CustomTintBlue { get; set; }
        string CustomTintGreen { get; set; }
        string CustomTintRed { get; set; }
        string CustomTintTooltip { get; set; }
        bool DoesNotHaveItemEquipped { get; set; }
        bool HasItemEquipped { get; set; }
        bool IsAppearanceSelected { get; set; }
        bool IsCopyEnabled { get; set; }
        bool IsCustomTintAvailable { get; set; }
        bool IsCustomTintEditable { get; set; }
        bool IsEquipmentSelected { get; set; }
        bool IsTintMapAvailable { get; set; }
        int RobeSelection { get; set; }
        int SelectedColorCategoryIndex { get; set; }
        int SelectedItemTypeIndex { get; set; }
        GuiColor SelectedTintColor { get; set; }

        void ChangeColor(AppearanceColorTarget target, AppearanceArmorColor channel, int colorId);
        uint GetItem();
        uint GetOffHandAppearanceOwner(uint offHand);
        bool IsValidItem();
        void LoadItemParts();
        void RefreshBodySwatches();
        void RefreshItemBaseSwatches();
        void ToggleItemEquippedFlags();
        void UpdateEditedColorSwatches();
    }
}
