using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;

namespace SWLOR.Game.Server.Feature.AppearanceEditing
{
    /// <summary>The names of the view model bindings the editor services address.</summary>
    public static class AppearanceEditorBindings
    {
        public const string CustomTintBlue = nameof(AppearanceEditorViewModel.CustomTintBlue);
        public const string CustomTintGreen = nameof(AppearanceEditorViewModel.CustomTintGreen);
        public const string CustomTintRed = nameof(AppearanceEditorViewModel.CustomTintRed);
        public const string CustomTintTooltip = nameof(AppearanceEditorViewModel.CustomTintTooltip);
        public const string DoesNotHaveItemEquipped = nameof(AppearanceEditorViewModel.DoesNotHaveItemEquipped);
        public const string HasItemEquipped = nameof(AppearanceEditorViewModel.HasItemEquipped);
        public const string IsAppearanceSelected = nameof(AppearanceEditorViewModel.IsAppearanceSelected);
        public const string IsCopyEnabled = nameof(AppearanceEditorViewModel.IsCopyEnabled);
        public const string IsCustomTintAvailable = nameof(AppearanceEditorViewModel.IsCustomTintAvailable);
        public const string IsCustomTintEditable = nameof(AppearanceEditorViewModel.IsCustomTintEditable);
        public const string IsEquipmentSelected = nameof(AppearanceEditorViewModel.IsEquipmentSelected);
        public const string IsTintMapAvailable = nameof(AppearanceEditorViewModel.IsTintMapAvailable);
        public const string RobeSelection = nameof(AppearanceEditorViewModel.RobeSelection);
        public const string SelectedColorCategoryIndex = nameof(AppearanceEditorViewModel.SelectedColorCategoryIndex);
        public const string SelectedItemTypeIndex = nameof(AppearanceEditorViewModel.SelectedItemTypeIndex);
        public const string SelectedTintColor = nameof(AppearanceEditorViewModel.SelectedTintColor);
    }
}
