using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Placeables;
using SWLOR.Toolset.Domain.Workspace;

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
        private readonly Dictionary<PlaceableValueSource, IReadOnlyList<BehaviorChoiceOption>> _cache = new();

        public BehaviorValueSourceProvider(IGameCodeIndex? gameCode, Func<ModuleTagIndex?> tags)
        {
            _gameCode = gameCode;
            _tags = tags;
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
                    PlaceableValueSource.VisualEffects => FromIds(_gameCode?.VisualEffects),
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
    }
}
