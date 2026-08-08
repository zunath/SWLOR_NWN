using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
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

}
