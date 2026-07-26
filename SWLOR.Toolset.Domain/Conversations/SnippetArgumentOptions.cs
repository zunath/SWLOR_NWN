using SWLOR.Game.Server.Service.SnippetService;
using SWLOR.Toolset.Domain.GameData.GameCode;

namespace SWLOR.Toolset.Domain.Conversations
{
    /// <summary>One value a writer can pick for a snippet argument: what is stored, and what is shown.</summary>
    public sealed record ArgumentOption(string Value, string Label)
    {
        public override string ToString() => Label;
    }

    /// <summary>
    /// The values a snippet argument can take, so an editor offers a list instead of a text box.
    /// </summary>
    /// <remarks>
    /// Everything here comes from the game code or the module — never from a table restated in the
    /// toolset. That is the whole point: a quest id a writer picks from this list cannot be a quest
    /// id the game does not have.
    /// </remarks>
    public sealed class SnippetArgumentOptions
    {
        private readonly IGameCodeIndex? _gameCode;
        private readonly Func<string, IReadOnlyList<string>>? _tagsFor;

        /// <param name="gameCode">Quests, key items, factions and skills.</param>
        /// <param name="tagsFor">
        /// Resolves placed-object tags by resource extension ("utm" for stores, "utw" for
        /// waypoints). Optional — without it those arguments fall back to free text, which is what
        /// they were before.
        /// </param>
        public SnippetArgumentOptions(
            IGameCodeIndex? gameCode,
            Func<string, IReadOnlyList<string>>? tagsFor = null)
        {
            _gameCode = gameCode;
            _tagsFor = tagsFor;
        }

        /// <summary>
        /// The options for one argument, or an empty list when the value is free text. A caller
        /// showing an empty list should fall back to a plain text box rather than an empty dropdown.
        /// </summary>
        public IReadOnlyList<ArgumentOption> For(SnippetArgument argument, IReadOnlyList<string> siblingValues)
        {
            switch (argument.Type)
            {
                case SnippetArgumentType.QuestId:
                    return _gameCode == null
                        ? Array.Empty<ArgumentOption>()
                        : _gameCode.Quests.Values
                            .OrderBy(quest => quest.Name, StringComparer.OrdinalIgnoreCase)
                            .Select(quest => new ArgumentOption(quest.Id, quest.Name))
                            .ToList();

                case SnippetArgumentType.QuestState:
                    return QuestStates(siblingValues);

                case SnippetArgumentType.KeyItemId:
                    return FromEnum(_gameCode?.KeyItems);

                case SnippetArgumentType.FactionId:
                    return FromEnum(_gameCode?.Factions);

                case SnippetArgumentType.SkillId:
                    // Stored by name in the corpus, which is also what SkillType.TryParse reads.
                    return _gameCode == null
                        ? Array.Empty<ArgumentOption>()
                        : _gameCode.SkillEnumNames
                            .Where(pair => pair.Value != "Invalid")
                            .OrderBy(pair => Display(_gameCode.Skills, pair.Key, pair.Value), StringComparer.OrdinalIgnoreCase)
                            .Select(pair => new ArgumentOption(pair.Value, Display(_gameCode.Skills, pair.Key, pair.Value)))
                            .ToList();

                case SnippetArgumentType.StoreTag:
                    return Tags("utm");

                case SnippetArgumentType.WaypointTag:
                    return Tags("utw");

                default:
                    return Array.Empty<ArgumentOption>();
            }
        }

        /// <summary>
        /// The steps of whichever quest this argument sits beside. A state number means nothing on
        /// its own, so the list is bounded by the quest already chosen in the same snippet — which
        /// is what stops a guard being written for step 7 of a two-step quest.
        /// </summary>
        private IReadOnlyList<ArgumentOption> QuestStates(IReadOnlyList<string> siblingValues)
        {
            var questId = siblingValues.FirstOrDefault();
            var quest = questId == null ? null : _gameCode?.FindQuest(questId);
            if (quest == null)
                return Array.Empty<ArgumentOption>();

            return Enumerable.Range(1, quest.StateCount)
                .Select(state => new ArgumentOption(
                    state.ToString(),
                    quest.JournalTextByState.TryGetValue(state, out var journal)
                        ? $"Step {state} — {Trim(journal)}"
                        : $"Step {state}"))
                .ToList();
        }

        private IReadOnlyList<ArgumentOption> Tags(string extension)
        {
            var tags = _tagsFor?.Invoke(extension);
            return tags == null
                ? Array.Empty<ArgumentOption>()
                : tags.OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                    .Select(tag => new ArgumentOption(tag, tag))
                    .ToList();
        }

        private static IReadOnlyList<ArgumentOption> FromEnum(IReadOnlyDictionary<int, string>? values)
        {
            if (values == null)
                return Array.Empty<ArgumentOption>();

            return values
                .Where(pair => !string.Equals(pair.Value, "Invalid", StringComparison.OrdinalIgnoreCase))
                .OrderBy(pair => pair.Value, StringComparer.OrdinalIgnoreCase)
                .Select(pair => new ArgumentOption(pair.Key.ToString(), pair.Value))
                .ToList();
        }

        private static string Display(IReadOnlyDictionary<int, string> names, int id, string fallback) =>
            names.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name) ? name : fallback;

        private static string Trim(string text) =>
            text.Length <= 48 ? text : text[..45].TrimEnd() + "…";
    }
}
