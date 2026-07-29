using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Editors.TintMaps
{
    /// <summary>RGB tint-map controls shared by item and creature blueprint editors.</summary>
    public sealed partial class TintMapEditorViewModel : ObservableObject
    {
        private readonly VarTable _variables;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly TintMapCatalog _catalog;
        private readonly Func<RenderModel?>? _resolveModel;
        private readonly Action? _colorChanged;

        public ObservableCollection<TintMapColorRowViewModel> Colors { get; } = new();

        public bool HasColors => Colors.Count > 0;

        public TintMapEditorViewModel(
            VarTable variables,
            Func<string, Action, bool> runEdit,
            TintMapCatalog catalog,
            Func<RenderModel?>? resolveModel = null,
            Action? colorChanged = null)
        {
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _resolveModel = resolveModel;
            _colorChanged = colorChanged;

            if (_resolveModel != null)
                Reload();
        }

        public void Reload()
        {
            Reload(_resolveModel?.Invoke());
        }

        public void Reload(RenderModel? model)
        {
            var wanted = _catalog.FindMaterials(model)
                .SelectMany(material => material.Layers.Select(layer => (material, layer)))
                .ToList();
            var currentKeys = Colors.Select(row => row.Key);
            var wantedKeys = wanted.Select(entry =>
                TintMapVariable.GetName(entry.material.Resref, entry.layer));

            if (currentKeys.SequenceEqual(wantedKeys, StringComparer.Ordinal))
            {
                foreach (var row in Colors)
                    row.Reload();
                return;
            }

            Colors.Clear();
            foreach (var (material, layer) in wanted)
            {
                Colors.Add(new TintMapColorRowViewModel(
                    material.Resref,
                    layer,
                    _variables,
                    _runEdit,
                    _colorChanged));
            }

            OnPropertyChanged(nameof(HasColors));
        }
    }

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

        public string Status => IsCustom ? $"#{Color.R:X2}{Color.G:X2}{Color.B:X2}" : "Standard NWN color";

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
                    () => _variables.SetInt(Key, tint.ToStoredValue())))
            {
                Reload();
                return;
            }

            IsCustom = true;
            OnPropertyChanged(nameof(Status));
            _colorChanged?.Invoke();
        }

        [RelayCommand]
        private void Reset()
        {
            if (!IsCustom)
                return;

            if (!_runEdit(
                    $"Reset {MaterialName} {LayerName} tint",
                    () => _variables.Remove(Key)))
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
                var saved = _variables.GetInt(Key) ?? 0;
                if (TintMapColor.TryFromStoredValue(saved, out var tint))
                {
                    Color = Color.FromRgb(tint.Red, tint.Green, tint.Blue);
                    IsCustom = true;
                }
                else
                {
                    Color = StandardPlaceholder;
                    IsCustom = false;
                }
            }
            finally
            {
                _loading = false;
            }

            OnPropertyChanged(nameof(Status));
        }
    }
}
