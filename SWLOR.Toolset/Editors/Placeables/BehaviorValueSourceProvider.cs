using Avalonia.Media.Imaging;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Placeables;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Placeables
{
    /// <summary>
    /// Turns a <see cref="PlaceableValueSource"/> into the options a picker shows, reading the game
    /// code and the module rather than a list kept by hand.
    /// </summary>
    /// <remarks>
    /// Every source degrades to an empty list when its backing index is unavailable, and an empty
    /// list means the field falls back to free text holding whatever is stored. A missing index must
    /// never stop a builder editing a placeable, and it must never let the editor conclude that a
    /// real value is wrong - which is why <see cref="IsKnown"/> answers true when it cannot tell.
    /// </remarks>
    public sealed class BehaviorValueSourceProvider
    {
        private readonly IGameCodeIndex? _gameCode;
        private readonly Func<ModuleTagIndex?> _tags;
        private readonly Func<ResourceType, IReadOnlyList<CatalogEntry>>? _blueprints;
        private readonly ThumbnailService? _thumbnails;
        private readonly VfxPreviewService? _vfxPreviews;
        private readonly Dictionary<PlaceableValueSource, IReadOnlyList<BehaviorChoiceOption>> _cache = new();

        public BehaviorValueSourceProvider(
            IGameCodeIndex? gameCode,
            Func<ModuleTagIndex?> tags,
            Func<ResourceType, IReadOnlyList<CatalogEntry>>? blueprints = null,
            ThumbnailService? thumbnails = null,
            VfxPreviewService? vfxPreviews = null)
        {
            _gameCode = gameCode;
            _tags = tags;
            _blueprints = blueprints;
            _thumbnails = thumbnails;
            _vfxPreviews = vfxPreviews;
        }

        /// <summary>Options for a source, built once per editor session.</summary>
        public IReadOnlyList<BehaviorChoiceOption> GetOptions(PlaceableValueSource source)
        {
            if (source == PlaceableValueSource.None)
                return Array.Empty<BehaviorChoiceOption>();

            if (_cache.TryGetValue(source, out var cached))
                return cached;

            var options = Build(source);
            _cache[source] = options;
            return options;
        }

        /// <summary>
        /// Whether a stored value is one this source knows. True for an empty value (nothing to
        /// check) and true whenever the source produced no options at all, since an index that did
        /// not load cannot be evidence that a value is wrong.
        /// </summary>
        public bool IsKnown(PlaceableValueSource source, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;

            var options = GetOptions(source);
            if (options.Count == 0)
                return true;

            return options.Any(option => string.Equals(option.Value, value, StringComparison.OrdinalIgnoreCase));
        }

        public Bitmap? CachedPreview(PlaceableValueSource source, BehaviorChoiceOption option)
        {
            if (source == PlaceableValueSource.VisualEffects)
                return _vfxPreviews?.Cached(option.ImageUrl);

            var resourceType = BlueprintType(source);
            return resourceType == null ? null : _thumbnails?.Cached(resourceType.Value, option.Value);
        }

        public void RequestPreview(
            PlaceableValueSource source,
            BehaviorChoiceOption option,
            Action<Bitmap> onReady)
        {
            if (source == PlaceableValueSource.VisualEffects)
            {
                _vfxPreviews?.RequestAsync(option.ImageUrl, onReady);
                return;
            }

            var resourceType = BlueprintType(source);
            if (resourceType != null)
                _thumbnails?.RequestAsync(resourceType.Value, option.Value, onReady);
        }

        private IReadOnlyList<BehaviorChoiceOption> Build(PlaceableValueSource source)
        {
            try
            {
                return source switch
                {
                    PlaceableValueSource.LootTables => FromNames(_gameCode?.LootTableIds),
                    PlaceableValueSource.SpawnTables => FromNames(_gameCode?.SpawnTableIds),
                    PlaceableValueSource.Quests => FromNames(_gameCode?.QuestIds),
                    PlaceableValueSource.Dialogs => FromNames(_gameCode?.DialogNames),
                    PlaceableValueSource.ObjectTags => FromNames(_tags()?.Tags),
                    PlaceableValueSource.KeyItems => FromIds(_gameCode?.KeyItems),
                    PlaceableValueSource.SkillTypes => FromIds(_gameCode?.SkillTypes),
                    PlaceableValueSource.MarketRegions => FromIds(_gameCode?.MarketRegions),
                    PlaceableValueSource.VisualEffects => FromVisualEffects(),
                    PlaceableValueSource.PlaceableBlueprints => FromBlueprints(ResourceType.Utp),
                    PlaceableValueSource.CreatureBlueprints => FromBlueprints(ResourceType.Utc),
                    _ => Array.Empty<BehaviorChoiceOption>()
                };
            }
            catch (Exception)
            {
                return Array.Empty<BehaviorChoiceOption>();
            }
        }

        private static IReadOnlyList<BehaviorChoiceOption> FromNames(IEnumerable<string>? names)
        {
            if (names == null)
                return Array.Empty<BehaviorChoiceOption>();

            return names
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Select(name => new BehaviorChoiceOption(name, name))
                .ToList();
        }

        private static IReadOnlyList<BehaviorChoiceOption> FromIds(IReadOnlyDictionary<int, string>? entries)
        {
            if (entries == null)
                return Array.Empty<BehaviorChoiceOption>();

            return entries
                .OrderBy(entry => entry.Key)
                .Select(entry => new BehaviorChoiceOption(
                    entry.Key.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    $"{entry.Value} ({entry.Key})"))
                .ToList();
        }

        private IReadOnlyList<BehaviorChoiceOption> FromVisualEffects()
        {
            if (_gameCode == null)
                return Array.Empty<BehaviorChoiceOption>();

            return _gameCode.VisualEffects
                .Select(entry =>
                {
                    if (_gameCode.VisualEffectReferences.TryGetValue(entry.Key, out var reference))
                    {
                        return new BehaviorChoiceOption(
                            entry.Key.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            $"{HumanizeVfxName(entry.Value)} ({entry.Key})",
                            reference.Group,
                            VfxDetails(reference),
                            reference.ImageUrl);
                    }

                    return new BehaviorChoiceOption(
                        entry.Key.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        $"{HumanizeVfxName(entry.Value)} ({entry.Key})");
                })
                .OrderBy(option => option.Group ?? "ZZZ", StringComparer.Ordinal)
                .ThenBy(option => option.Display, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private IReadOnlyList<BehaviorChoiceOption> FromBlueprints(ResourceType resourceType)
        {
            var entries = _blueprints?.Invoke(resourceType);
            if (entries == null)
                return Array.Empty<BehaviorChoiceOption>();

            return entries
                .OrderBy(entry => entry.Name ?? entry.ResRef, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.ResRef, StringComparer.OrdinalIgnoreCase)
                .Select(entry => new BehaviorChoiceOption(
                    entry.ResRef,
                    string.IsNullOrWhiteSpace(entry.Name) ? entry.ResRef : entry.Name,
                    Details: entry.ResRef))
                .ToList();
        }

        private static ResourceType? BlueprintType(PlaceableValueSource source) => source switch
        {
            PlaceableValueSource.PlaceableBlueprints => ResourceType.Utp,
            PlaceableValueSource.CreatureBlueprints => ResourceType.Utc,
            _ => null
        };

        private static string HumanizeVfxName(string name)
        {
            var value = name.StartsWith("Vfx_", StringComparison.OrdinalIgnoreCase)
                ? name[4..]
                : name;
            return value.Replace('_', ' ');
        }

        private static string VfxDetails(VisualEffectReferenceInfo reference) =>
            string.Join(
                " · ",
                new[]
                {
                    reference.SelectionHint,
                    reference.VisualTags,
                    reference.Location,
                    reference.Colors
                }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
