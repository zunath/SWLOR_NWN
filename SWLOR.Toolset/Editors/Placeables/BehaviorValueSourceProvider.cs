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
    /// <para>
    /// Every source degrades to an empty list when its backing index is unavailable, and an empty
    /// list means the field falls back to free text holding whatever is stored. A missing index must
    /// never stop a builder editing a placeable, and it must never let the editor conclude that a
    /// real value is wrong - which is why <see cref="IsKnown"/> answers true when it cannot tell.
    /// </para>
    /// <para>
    /// One instance for the session rather than one per editor. These lists are large - five figures
    /// of object tags, 524 visual effects, every placeable and creature blueprint in the module -
    /// and building a private copy for each open editor cost both the scan and the memory to hold
    /// the result, multiplied by however many tabs a builder had open.
    /// </para>
    /// <para>
    /// Game-code sources never change while the toolset runs, so they are cached for the process.
    /// Module-backed ones are dropped by <see cref="InvalidateModuleSources"/> whenever the module's
    /// content does, which is what keeps a newly created blueprint from being reported as unknown.
    /// </para>
    /// </remarks>
    public sealed class BehaviorValueSourceProvider
    {
        /// <summary>The sources read out of the open module rather than out of the game code.</summary>
        private static readonly PlaceableValueSource[] ModuleBackedSources =
        {
            PlaceableValueSource.ObjectTags,
            PlaceableValueSource.PlaceableBlueprints,
            PlaceableValueSource.CreatureBlueprints
        };

        private readonly IGameCodeIndex? _gameCode;
        private readonly Func<ModuleTagIndex?> _tags;
        private readonly Func<ResourceType, IReadOnlyList<CatalogEntry>>? _blueprints;
        private readonly ThumbnailService? _thumbnails;
        private readonly VfxPreviewService? _vfxPreviews;
        private readonly Dictionary<PlaceableValueSource, IReadOnlyList<BehaviorChoiceOption>> _cache = new();
        private readonly object _cacheGate = new();

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

        /// <summary>Options for a source, built once and shared by every editor that asks.</summary>
        public IReadOnlyList<BehaviorChoiceOption> GetOptions(PlaceableValueSource source)
        {
            if (source == PlaceableValueSource.None)
                return Array.Empty<BehaviorChoiceOption>();

            lock (_cacheGate)
            {
                if (_cache.TryGetValue(source, out var cached))
                    return cached;
            }

            var options = Build(source);

            lock (_cacheGate)
                _cache[source] = options;

            return options;
        }

        /// <summary>
        /// Drops the cached options that came out of the module, so a blueprint created or renamed
        /// since the last build is offered rather than reported as unknown. Game-code sources are
        /// left alone: nothing can change them while the process runs.
        /// </summary>
        public void InvalidateModuleSources()
        {
            lock (_cacheGate)
            {
                foreach (var source in ModuleBackedSources)
                    _cache.Remove(source);
            }
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
                    PlaceableValueSource.LootTables => FromTableNames(_gameCode?.LootTableIds),
                    PlaceableValueSource.SpawnTables => FromTableNames(_gameCode?.SpawnTableIds),
                    PlaceableValueSource.Quests => FromNames(_gameCode?.QuestIds),
                    PlaceableValueSource.Dialogs => FromNames(_gameCode?.DialogNames),
                    PlaceableValueSource.ObjectTags => FromNames(
                        _tags()?.TagsFor(ResourceType.Utw)),
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

        private static IReadOnlyList<BehaviorChoiceOption> FromTableNames(IEnumerable<string>? names)
        {
            if (names == null)
                return Array.Empty<BehaviorChoiceOption>();

            return names
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => new BehaviorChoiceOption(
                    name,
                    TableChoiceDisplayName.FromIdentifier(name),
                    Details: name))
                .OrderBy(option => option.Display, StringComparer.OrdinalIgnoreCase)
                .ThenBy(option => option.Value, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IReadOnlyList<BehaviorChoiceOption> FromIds(
            IReadOnlyDictionary<int, string>? entries)
        {
            if (entries == null)
                return Array.Empty<BehaviorChoiceOption>();

            return entries
                .OrderBy(entry => entry.Key)
                .Select(entry => new BehaviorChoiceOption(
                    entry.Key.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    entry.Value))
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
