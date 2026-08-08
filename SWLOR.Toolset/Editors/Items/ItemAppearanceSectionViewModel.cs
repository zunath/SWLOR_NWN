using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Editors.TintMaps;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// The item editor's Appearance tab. What it offers follows baseitems.2da's ModelType for the
    /// item's BaseItem: one gallery of picture tiles (ModelType 0/1), three model-color galleries for
    /// a composite weapon (ModelType 2), or a body-part/dye grid for armor (ModelType 3). An unknown
    /// or unresolved base item offers nothing.
    /// </summary>
    public sealed partial class ItemAppearanceSectionViewModel : ObservableObject
    {
        private const int ItemThumbnailWidth = 72;

        /// <summary>Legacy range checked for icon-only gallery parts.</summary>
        private const int MaxSimplePart = 255;

        /// <summary>Standard model/color range checked for icon-only composite parts.</summary>
        private const int MaxCompositePart = 259;

        private readonly ItemValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Func<int, BaseItemIconRow?> _baseItems;
        private readonly Func<string, bool> _textureExists;
        private readonly ChoicePreviewService? _previews;
        private readonly Action? _appearanceChanged;
        private readonly ArmorDyeSwatchService? _armorDyes;
        private readonly ArmorPartCatalog? _armorPartModels;

        [ObservableProperty]
        private ItemAppearanceKind _kind = ItemAppearanceKind.None;

        [ObservableProperty]
        private GalleryViewModel? _gallery;

        [ObservableProperty]
        private CompositePartViewModel? _bottom;

        [ObservableProperty]
        private CompositePartViewModel? _middle;

        [ObservableProperty]
        private CompositePartViewModel? _top;

        [ObservableProperty]
        private ArmorPartsViewModel? _armor;

        [ObservableProperty]
        private TintMapEditorViewModel? _tints;

        public void SetTintMapEditor(TintMapEditorViewModel? editor)
        {
            Tints = editor;
        }

        public ItemAppearanceSectionViewModel(
            ItemValueStore store,
            Func<string, Action, bool> runEdit,
            Func<int, BaseItemIconRow?> baseItems,
            Func<string, bool> textureExists,
            ChoicePreviewService? previews = null,
            Action? appearanceChanged = null,
            ArmorDyeSwatchService? armorDyes = null,
            ArmorPartCatalog? armorPartModels = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            _baseItems = baseItems ?? throw new ArgumentNullException(nameof(baseItems));
            _textureExists = textureExists ?? throw new ArgumentNullException(nameof(textureExists));
            _previews = previews;
            _appearanceChanged = appearanceChanged;
            _armorDyes = armorDyes;
            _armorPartModels = armorPartModels;

            Rebuild();
        }

        /// <summary>
        /// Rebuilds the whole tab for the item's current BaseItem: reclassifies ModelType and
        /// re-probes which parts have artwork. Callers re-run this after BaseItem itself changes - the
        /// same trigger the Stats tab already rebuilds on.
        /// </summary>
        public void Rebuild()
        {
            var baseItem = (int)(_store.GetInteger(BehaviorFieldStorage.Field, "BaseItem") ?? -1);
            var row = baseItem < 0 ? null : _baseItems(baseItem);

            Gallery = null;
            Bottom = null;
            Middle = null;
            Top = null;
            Armor = null;

            if (row == null)
            {
                Kind = ItemAppearanceKind.None;
                return;
            }

            switch (row.ModelType)
            {
                case 0:
                case 1:
                    Kind = ItemAppearanceKind.Gallery;
                    Gallery = BuildGallery(row);
                    break;

                case 2:
                    Kind = ItemAppearanceKind.Composite;
                    Bottom = BuildCompositePart(row, "Bottom", ItemAppearanceFieldNames.Bottom, "b");
                    Middle = BuildCompositePart(row, "Middle", ItemAppearanceFieldNames.Middle, "m");
                    Top = BuildCompositePart(row, "Top", ItemAppearanceFieldNames.Top, "t");
                    break;

                case 3:
                    Kind = ItemAppearanceKind.ArmorParts;
                    Armor = new ArmorPartsViewModel(
                        _store, _runEdit, _appearanceChanged, _armorDyes, _armorPartModels);
                    break;

                default:
                    Kind = ItemAppearanceKind.None;
                    break;
            }
        }

        /// <summary>
        /// Picks a default appearance for whatever is currently built, when nothing is selected yet
        /// and real options exist: the first offered tile for a Gallery, the first offered part for
        /// each of a Composite's three layers. A no-op for Armor (left alone) and for a build that
        /// already has a selection or has no options to offer.
        /// </summary>
        public void EnsureSelection()
        {
            switch (Kind)
            {
                case ItemAppearanceKind.Gallery:
                    EnsureFirstSelected(Gallery);
                    break;

                case ItemAppearanceKind.Composite:
                    EnsureCompositeSelection();
                    break;

                case ItemAppearanceKind.ArmorParts:
                    Armor?.EnsureDefaults();
                    break;
            }
        }

        private static void EnsureFirstSelected(GalleryViewModel? gallery)
        {
            if (gallery != null && gallery.Selected == null && gallery.Options.Count > 0)
                gallery.Selected = gallery.Options[0];
        }

        private void EnsureCompositeSelection()
        {
            var missing = new[]
                {
                    (Part: Bottom, Field: ItemAppearanceFieldNames.Bottom),
                    (Part: Middle, Field: ItemAppearanceFieldNames.Middle),
                    (Part: Top, Field: ItemAppearanceFieldNames.Top),
                }
                .Where(entry => entry.Part is { Selected: null, Options.Count: > 0 })
                .Select(entry => (entry.Part!, entry.Field))
                .ToList();

            if (missing.Count == 0)
                return;

            var applied = _runEdit("Set composite appearance", () =>
            {
                foreach (var (part, field) in missing)
                    ItemAppearanceValues.Write(_store, field, checked((int)part.Options[0].Value));
            });

            if (!applied)
                return;

            foreach (var (part, _) in missing)
                part.Reload();

            _appearanceChanged?.Invoke();
        }

        /// <summary>Re-reads whatever is currently built, without re-probing for artwork.</summary>
        public void ReloadFromDocument()
        {
            switch (Kind)
            {
                case ItemAppearanceKind.Gallery:
                    Gallery?.Reload();
                    break;
                case ItemAppearanceKind.Composite:
                    Bottom?.Reload();
                    Middle?.Reload();
                    Top?.Reload();
                    break;
                case ItemAppearanceKind.ArmorParts:
                    Armor?.ReloadFromDocument();
                    break;
            }
        }

        /// <summary>
        /// ModelType 0 tries the simple icon and its unseparated spelling; ModelType 1 tries the part
        /// icon and its sized spelling - the same two candidates and order
        /// <see cref="Domain.Render.Icons.ItemIconResolver"/> tries for each.
        /// </summary>
        private GalleryViewModel BuildGallery(BaseItemIconRow row)
        {
            var itemClass = row.ItemClass ?? string.Empty;
            var options = new List<BehaviorChoiceViewModel>();
            var modelPrefix = itemClass + "_";
            var modelParts = _armorPartModels?.NumbersForModelPrefix(modelPrefix) ?? Array.Empty<int>();
            var modelPartSet = modelParts.ToHashSet();
            var candidateParts = Enumerable.Range(0, MaxSimplePart + 1)
                .Concat(modelParts)
                .Distinct()
                .OrderBy(part => part);

            foreach (var part in candidateParts)
            {
                var resolved = row.ModelType == 1
                    ? Probe($"i{itemClass}_{part:D3}") ?? Probe($"i{itemClass}_m_{part:D3}")
                    : Probe($"i{itemClass}_{part:D3}") ?? Probe($"i{itemClass}{part:D3}");
                var model = modelPartSet.Contains(part) ? modelPrefix + part.ToString("D3") : null;

                if (resolved == null && model == null)
                    continue;

                options.Add(new BehaviorChoiceViewModel(
                    new BehaviorChoice(
                        part,
                        part.ToString("D3", CultureInfo.InvariantCulture),
                        imageResRef: resolved,
                        modelResRef: resolved == null ? model : null)));
            }

            RequestThumbnails(options);
            return new GalleryViewModel(_store, _runEdit, options, _appearanceChanged);
        }

        private CompositePartViewModel BuildCompositePart(
            BaseItemIconRow row, string label, string fieldName, string layerInfix)
        {
            var itemClass = row.ItemClass ?? string.Empty;
            var options = new List<BehaviorChoiceViewModel>();
            var modelPrefix = $"{itemClass}_{layerInfix}_";
            var modelParts = _armorPartModels?.NumbersForModelPrefix(modelPrefix) ?? Array.Empty<int>();
            var modelPartSet = modelParts.ToHashSet();
            var candidateParts = Enumerable.Range(0, MaxCompositePart + 1)
                .Concat(modelParts)
                .Distinct()
                .OrderBy(part => part);

            foreach (var part in candidateParts)
            {
                var resRef = $"i{itemClass}_{layerInfix}_{part:D3}";
                var image = _textureExists(resRef) ? resRef : null;
                var model = modelPartSet.Contains(part) ? modelPrefix + part.ToString("D3") : null;
                if (image == null && model == null)
                    continue;

                // A part below 10 has no color digit to split off; everything else is read as
                // model*10 + color, matching how ItemIconResolver names the composite layer.
                var caption = part >= 10
                    ? $"{part / 10}-{part % 10}"
                    : part.ToString(CultureInfo.InvariantCulture);

                options.Add(new BehaviorChoiceViewModel(
                    new BehaviorChoice(
                        part,
                        caption,
                        imageResRef: image,
                        modelResRef: image == null ? model : null)));
            }

            RequestThumbnails(options, cropTransparentCanvas: true);
            return new CompositePartViewModel(label, fieldName, _store, _runEdit, options, _appearanceChanged);
        }

        private string? Probe(string resRef) => _textureExists(resRef) ? resRef : null;

        private void RequestThumbnails(
            IReadOnlyList<BehaviorChoiceViewModel> options,
            bool cropTransparentCanvas = false)
        {
            if (_previews == null)
                return;

            foreach (var option in options)
            {
                if (!option.HasArtwork || option.Thumbnail != null)
                    continue;

                if (_previews.Cached(
                        option.Choice,
                        ItemThumbnailWidth,
                        cropTransparentCanvas) is { } cached)
                {
                    option.Thumbnail = cached;
                    continue;
                }

                _ = _previews.RequestAsync(
                    option.Choice,
                    ItemThumbnailWidth,
                    bitmap => option.Thumbnail = bitmap,
                    cropTransparentCanvas);
            }
        }
    }
}
