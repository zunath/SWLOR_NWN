using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Editors.TintMaps
{
    public sealed partial class TintMapColorRowViewModel : ObservableObject
    {
        private static readonly Color StandardPlaceholder = Color.FromRgb(128, 128, 128);

        private readonly VarTable _variables;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Action? _colorChanged;
        private bool _loading;

        public string MaterialName { get; }
        public TintMapLayerType Layer { get; }
        public string LayerName => TintMapMaterialRegistry.GetLayer(Layer).Name;
        public string Key => TintMapVariable.GetName(MaterialName, Layer);

        [ObservableProperty]
        private Color _color = StandardPlaceholder;

        [ObservableProperty]
        private bool _isCustom;

        [ObservableProperty]
        private bool _hasOverride;

        public string Status => IsCustom
            ? $"#{Color.R:X2}{Color.G:X2}{Color.B:X2}"
            : HasOverride
                ? "Legacy palette override"
                : "Standard NWN color";

        public TintMapColorRowViewModel(
            string materialName,
            TintMapLayerType layer,
            VarTable variables,
            Func<string, Action, bool> runEdit,
            Action? colorChanged)
        {
            MaterialName = materialName;
            Layer = layer;
            _variables = variables;
            _runEdit = runEdit;
            _colorChanged = colorChanged;
            Reload();
        }

        partial void OnColorChanged(Color value)
        {
            if (_loading)
                return;

            var tint = new TintMapColor(value.R, value.G, value.B);
            if (!_runEdit(
                    $"Set {MaterialName} {LayerName} tint to #{value.R:X2}{value.G:X2}{value.B:X2}",
                    () =>
                    {
                        _variables.SetInt(Key, tint.ToStoredValue());
                        RemoveGlobalSemanticIntent();
                    }))
            {
                Reload();
                return;
            }

            IsCustom = true;
            HasOverride = true;
            OnPropertyChanged(nameof(Status));
            _colorChanged?.Invoke();
        }

        [RelayCommand]
        private void Reset()
        {
            if (!HasOverride)
                return;

            if (!_runEdit(
                    $"Reset {MaterialName} {LayerName} tint",
                    () =>
                    {
                        _variables.Remove(Key);
                        RemoveGlobalSemanticIntent();
                    }))
            {
                Reload();
                return;
            }

            Reload();
            _colorChanged?.Invoke();
        }

        public void Reload()
        {
            _loading = true;
            try
            {
                var saved = TintMapOverrides.GetMaterialColor(
                    _variables,
                    MaterialName,
                    Layer);
                if (TintMapColor.TryFromStoredValue(saved, out var tint))
                {
                    Color = Color.FromRgb(tint.Red, tint.Green, tint.Blue);
                    IsCustom = true;
                    HasOverride = true;
                }
                else if (saved is > 0 and <= TintMapMaterialRegistry.PaletteColorCount)
                {
                    var paletteColor = TintMapPaletteColors.GetColor(Layer, saved - 1);
                    Color = Color.FromRgb(
                        paletteColor.Red,
                        paletteColor.Green,
                        paletteColor.Blue);
                    IsCustom = false;
                    HasOverride = true;
                }
                else
                {
                    Color = StandardPlaceholder;
                    IsCustom = false;
                    HasOverride = false;
                }
            }
            finally
            {
                _loading = false;
            }

            OnPropertyChanged(nameof(Status));
        }

        private void RemoveGlobalSemanticIntent()
        {
            if (TintMapVariable.IsCreatureColorLayer(Layer))
                _variables.Remove(TintMapVariable.GetCreatureColorStateName(Layer));
        }
    }
}
