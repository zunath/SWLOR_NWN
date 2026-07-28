using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.GameCode;

namespace SWLOR.Toolset.Domain.Conversations
{
    /// <summary>Whether a situation has been written, is still blank, or can never be reached.</summary>
    public enum SituationState
    {
        /// <summary>Has a line, and something can reach it.</summary>
        Written,

        /// <summary>Reachable, but the NPC has nothing to say yet.</summary>
        Empty,

        /// <summary>
        /// An earlier opening answers everybody, so this one is never tested. The single mistake
        /// this conversation format makes easy: openings are checked in order and the first that
        /// fits wins, so appending a guarded opening below an unguarded one silently kills it.
        /// </summary>
        Unreachable
    }

    /// <summary>
    /// One circumstance a conversation answers — an opening, described the way a writer thinks about
    /// it rather than as a guarded index into a list.
    /// </summary>
    public sealed class Situation
    {
        /// <summary>Position in the openings list, which is the order the engine tests them in.</summary>
        public required int Order { get; init; }

        public required DlgLink Opening { get; init; }

        /// <summary>A short title, derived from the guards or from the line itself.</summary>
        public required string Title { get; init; }

        /// <summary>The guards spelled out, or "anyone else" when there are none.</summary>
        public required string When { get; init; }

        public required SituationState State { get; init; }

        /// <summary>How many lines and choices sit under this opening.</summary>
        public required int LineCount { get; init; }

        public required int ChoiceCount { get; init; }

        /// <summary>True when nothing guards this opening, so it answers every player.</summary>
        public required bool IsCatchAll { get; init; }

        public override string ToString() => $"{Order}. {Title}";
    }

    /// <summary>Whether a quest step has a line written for it in this conversation.</summary>
    public sealed record CoverageCell(string Label, bool IsCovered, int? State);

    /// <summary>One quest this conversation touches, and which of its steps are answered.</summary>
    public sealed class QuestCoverage
    {
        public required string QuestId { get; init; }

        /// <summary>The quest's display name, or its id when the game code cannot be read.</summary>
        public required string Name { get; init; }

        public required bool IsRepeatable { get; init; }

        /// <summary>Offer, one cell per step, then done.</summary>
        public required IReadOnlyList<CoverageCell> Cells { get; init; }

        public bool IsComplete => Cells.All(cell => cell.IsCovered);
    }

    /// <summary>
    /// Reads a conversation as the set of circumstances it answers, which is how the module is
    /// actually built and how the editor navigates. A walk shows one path at a time, so this is
    /// what answers "what have I not written yet?".
    /// </summary>
    public sealed class SituationModel
    {
        private readonly DlgDocument _document;
        private readonly ReachabilityEvaluator _evaluator;
        private readonly IGameCodeIndex? _gameCode;

        public SituationModel(DlgDocument document, ReachabilityEvaluator evaluator, IGameCodeIndex? gameCode = null)
        {
            _document = document;
            _evaluator = evaluator;
            _gameCode = gameCode;
        }

        /// <summary>The conversation's openings as situations, in the order the engine tests them.</summary>
        public IReadOnlyList<Situation> Situations()
        {
            var result = new List<Situation>();
            var catchAllSeen = false;

            var openings = _document.Openings;
            for (var i = 0; i < openings.Count; i++)
            {
                var opening = openings[i];
                var isCatchAll = opening.Conditions.Count == 0;
                var (lines, choices) = CountBeneath(opening);

                var state = SituationState.Written;
                if (catchAllSeen || !TryBuildPlayer(i + 1, opening, out _))
                    state = SituationState.Unreachable;
                else if (!HasSomethingToSay(opening, lines))
                    state = SituationState.Empty;

                result.Add(new Situation
                {
                    Order = i + 1,
                    Opening = opening,
                    Title = TitleFor(opening),
                    When = WhenFor(opening),
                    State = state,
                    LineCount = lines,
                    ChoiceCount = choices,
                    IsCatchAll = isCatchAll
                });

                if (isCatchAll)
                    catchAllSeen = true;
            }

            return result;
        }

        /// <summary>
        /// A player who would actually reach this situation, or null when no such player exists.
        /// Selecting a situation in the rail is what this produces.
        /// </summary>
        /// <remarks>
        /// Satisfying the situation's own guards is the easy half. The hard half is that every
        /// earlier opening must also FAIL, because the engine takes the first that fits — so this
        /// repeatedly breaks whichever earlier opening still catches the player, re-checking after
        /// each change since breaking one can open another. When no set of quest states satisfies
        /// all of that at once, the situation is unreachable and this returns null. That is not
        /// hypothetical: dantherbs ships one.
        /// </remarks>
        public PretendPlayer? PlayerFor(Situation situation) =>
            TryBuildPlayer(situation.Order, situation.Opening, out var player) ? player : null;

        private bool TryBuildPlayer(int order, DlgLink opening, out PretendPlayer player)
        {
            var candidate = new PretendPlayer();
            player = candidate;
            ApplyToSatisfy(opening, candidate);

            var protectedQuests = QuestsConstrainedBy(opening);
            var questStatePins = QuestStatePinsOf(opening);
            var budget = NonQuestConstraintsOf(opening);
            var earlier = _document.Openings.Take(order - 1).ToList();

            // Each pass breaks one blocking opening. Bounded by their number plus slack, since
            // breaking one can open an earlier one and each pass fixes at most one.
            for (var pass = 0; pass <= earlier.Count + 1; pass++)
            {
                var blocking = earlier.FirstOrDefault(link => _evaluator.Evaluate(link, candidate).IsOpen);
                if (blocking == null)
                    return _evaluator.Evaluate(opening, candidate).IsOpen;

                if (!TryBreak(blocking, protectedQuests, questStatePins, budget, candidate))
                    return false;
            }

            return false;
        }

        /// <summary>
        /// Makes an earlier opening stop matching, without disturbing any quest, key item, skill,
        /// faction threshold or tutorial flag the target situation depends on. Returns false when
        /// there is no such move — which is what "unreachable" means.
        /// </summary>
        /// <remarks>
        /// Every guard kind here is the mirror image of what <see cref="ApplyToSatisfy"/> does to
        /// make that same guard pass: a positive guard breaks by undoing that state, and a negated
        /// guard — passing today only because the pretend player starts from nothing — breaks by
        /// granting exactly what it says the player must not have.
        /// </remarks>
        private static bool TryBreak(
            DlgLink blocking,
            ISet<string> protectedQuests,
            IReadOnlyDictionary<string, int?> questStatePins,
            GuardBudget budget,
            PretendPlayer player)
        {
            foreach (var condition in blocking.Conditions)
            {
                var arguments = condition.Arguments;
                var satisfy = !condition.IsNegated;

                switch (condition.SnippetKey)
                {
                    case "condition-has-quest":
                    case "condition-completed-quest":
                    {
                        if (condition.IsNegated)
                        {
                            // A negated guard only fails once EVERY listed quest is in the stated
                            // condition - completing just the first of "!completed q1 q2" leaves
                            // the opening matching on the next pass and the solver spinning until
                            // its retry budget expires. All-or-nothing: if any argument is
                            // protected by the target's own constraints, this breaker is unusable.
                            if (arguments.Length == 0 ||
                                arguments.Any(q => string.IsNullOrEmpty(q) || protectedQuests.Contains(q)))
                                continue;

                            foreach (var negatedQuest in arguments)
                            {
                                player.WithQuest(negatedQuest, condition.SnippetKey == "condition-completed-quest"
                                    ? QuestProgress.Completed
                                    : QuestProgress.OnStep(1));
                            }

                            return true;
                        }

                        // A positive guard needs only one listed quest removed to fail.
                        var questId = arguments.FirstOrDefault(
                            q => !string.IsNullOrEmpty(q) && !protectedQuests.Contains(q));
                        if (questId == null)
                            continue;

                        player.WithQuest(questId, QuestProgress.None);
                        return true;
                    }

                    // Only the first argument is a quest id; the rest are step numbers, not more
                    // quests - treating them as quest ids (the bug this case fixes) creates bogus
                    // quests and leaves the real one unmoved.
                    case "condition-on-quest-state":
                    {
                        if (arguments.Length < 2)
                            continue;

                        var questId = arguments[0];
                        if (string.IsNullOrEmpty(questId))
                            continue;

                        var states = arguments.Skip(1)
                            .Select(argument => int.TryParse(argument, out var parsed) ? (int?)parsed : null)
                            .Where(parsed => parsed != null)
                            .Select(parsed => parsed!.Value)
                            .ToList();
                        if (states.Count == 0)
                            continue;

                        if (condition.IsNegated)
                        {
                            // Passes today because the quest sits outside every listed state;
                            // breaking it means landing on ANY one of them - "not on state 3 or 4"
                            // only needs one of the two to hold to fail.
                            if (protectedQuests.Contains(questId))
                                continue;

                            player.WithQuest(questId, QuestProgress.OnStep(states[0]));
                            return true;
                        }

                        // A positive guard needs the quest off every listed state to fail.
                        if (protectedQuests.Contains(questId))
                        {
                            // Only a quest the target merely needs "in progress" - no pinned step
                            // of its own from a condition-on-quest-state - can be nudged to a
                            // different step. Completed, not-yet-accepted, or a pinned step of its
                            // own means there is nowhere left to move it without breaking the
                            // target too; and if that pinned step already avoided this guard's
                            // list, this condition would never have blocked in the first place.
                            if (!questStatePins.TryGetValue(questId, out var pinnedState) || pinnedState != null)
                                continue;

                            var candidate = 1;
                            while (states.Contains(candidate))
                                candidate++;

                            player.WithQuest(questId, QuestProgress.OnStep(candidate));
                            return true;
                        }

                        player.WithQuest(questId, QuestProgress.None);
                        return true;
                    }

                    case "condition-can-accept-quest" when !condition.IsNegated:
                    {
                        var questId = arguments.FirstOrDefault();
                        if (string.IsNullOrEmpty(questId) || protectedQuests.Contains(questId))
                            continue;

                        // Being in progress makes CanAcceptQuest false for both repeatable and
                        // one-shot quests. Marking it merely Completed left a repeatable quest
                        // re-offerable on every retry pass, since both the runtime and
                        // ReachabilityEvaluator.CanAcceptQuest let a completed repeatable quest be
                        // accepted again.
                        player.WithQuest(questId, QuestProgress.OnStep(1));
                        return true;
                    }

                    case "condition-all-key-items" when arguments.Length > 0:
                        if (satisfy)
                        {
                            // Every listed key item is held; taking away one the target doesn't
                            // itself need makes the guard fail.
                            var removable = arguments.FirstOrDefault(keyItem => !budget.KeyItems.Contains(keyItem));
                            if (removable == null)
                                continue;

                            player.WithoutKeyItem(removable);
                            return true;
                        }
                        else
                        {
                            // Passes today because at least one is missing; breaking it means
                            // granting every one of them, so none can be an item the target needs
                            // absent.
                            if (arguments.Any(budget.KeyItems.Contains))
                                continue;

                            foreach (var keyItem in arguments)
                                player.WithKeyItem(keyItem);
                            return true;
                        }

                    case "condition-has-completed-tutorial":
                        if (budget.Tutorial)
                            continue;

                        player.HasCompletedTutorial = !satisfy;
                        return true;

                    case "condition-any-skill":
                    case "condition-all-skills":
                    {
                        if (arguments.Length < 2 || arguments.Length % 2 != 0)
                            continue;

                        var pairs = new List<(string Skill, int Rank)>();
                        var parsed = true;
                        for (var i = 0; i + 1 < arguments.Length; i += 2)
                        {
                            if (!int.TryParse(arguments[i + 1], out var rank))
                            {
                                parsed = false;
                                break;
                            }

                            pairs.Add((arguments[i], rank));
                        }

                        if (!parsed)
                            continue;

                        var wantsAll = condition.SnippetKey == "condition-all-skills";

                        // "all-skills" passing needs one unmet pair to break; "any-skill" passing
                        // needs every pair unmet. Negating either flips which of those breaks it.
                        var touchOne = satisfy == wantsAll;

                        if (touchOne)
                        {
                            var pick = pairs.FirstOrDefault(pair => !budget.Skills.Contains(pair.Skill));
                            if (pick.Skill == null)
                                continue;

                            player.WithSkill(pick.Skill, satisfy ? Math.Max(0, pick.Rank - 1) : pick.Rank);
                            return true;
                        }

                        if (pairs.Any(pair => budget.Skills.Contains(pair.Skill)))
                            continue;

                        foreach (var (skill, rank) in pairs)
                            player.WithSkill(skill, satisfy ? Math.Max(0, rank - 1) : rank);
                        return true;
                    }

                    case "condition-has-faction-standing" when arguments.Length > 1:
                    case "condition-has-faction-points" when arguments.Length > 1:
                    {
                        if (!int.TryParse(arguments[0], out var factionId)
                            || !int.TryParse(arguments[1], out var required))
                            continue;

                        var isStanding = condition.SnippetKey == "condition-has-faction-standing";

                        // Faction points floor at zero and standing is clamped to the runtime's
                        // range, the same as Faction.AdjustPlayerFactionStanding/Points - a
                        // breaker must not write a value the game could never produce.
                        var floor = isStanding ? Game.Server.Service.Faction.MinimumFaction : 0;
                        var ceiling = isStanding ? Game.Server.Service.Faction.MaximumFaction : int.MaxValue;

                        // The target's own guards on this same faction may still leave room to
                        // move within - identifier-only protection refused any move at all, even
                        // one that stays inside the interval the target still allows.
                        var ranges = isStanding ? budget.FactionStandingRanges : budget.FactionPointRanges;
                        var allowedMin = floor;
                        var allowedMax = ceiling;
                        if (ranges.TryGetValue(factionId, out var allowed))
                        {
                            allowedMin = Math.Max(allowedMin, allowed.Min);
                            allowedMax = Math.Min(allowedMax, allowed.Max);
                        }

                        if (allowedMin > allowedMax)
                            continue;

                        if (satisfy)
                        {
                            // Currently at or above the requirement; drop just below it, but no
                            // lower than the target's own guards on this faction still allow.
                            var value = Math.Min(required - 1, allowedMax);
                            if (value < allowedMin)
                                continue;

                            if (isStanding)
                                player.WithFactionStanding(factionId, value);
                            else
                                player.WithFactionPoints(factionId, value);
                            return true;
                        }
                        else
                        {
                            // Currently below the requirement; raise it to exactly meet it, but no
                            // higher than the target's own guards on this faction still allow.
                            var value = Math.Max(required, allowedMin);
                            if (value > allowedMax)
                                continue;

                            if (isStanding)
                                player.WithFactionStanding(factionId, value);
                            else
                                player.WithFactionPoints(factionId, value);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Everything besides quests that this opening's own guards pin down — key items it needs
        /// held or absent, skills, faction thresholds, and the tutorial flag — which a breaker must
        /// leave alone for the same reason <see cref="QuestsConstrainedBy"/> protects quest ids.
        /// </summary>
        private GuardBudget NonQuestConstraintsOf(DlgLink opening)
        {
            var keyItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var skills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var factionStandingRanges = new Dictionary<int, (int Min, int Max)>();
            var factionPointRanges = new Dictionary<int, (int Min, int Max)>();
            var tutorial = false;

            foreach (var condition in opening.Conditions)
            {
                var arguments = condition.Arguments;
                switch (condition.SnippetKey)
                {
                    case "condition-all-key-items":
                        foreach (var keyItem in arguments)
                            keyItems.Add(keyItem);
                        break;

                    case "condition-has-completed-tutorial":
                        tutorial = true;
                        break;

                    case "condition-any-skill":
                    case "condition-all-skills":
                        for (var i = 0; i + 1 < arguments.Length; i += 2)
                            skills.Add(arguments[i]);
                        break;

                    case "condition-has-faction-standing" when arguments.Length > 1:
                        if (int.TryParse(arguments[0], out var standingFaction)
                            && int.TryParse(arguments[1], out var standingRequired))
                            IntersectFactionRange(factionStandingRanges, standingFaction, standingRequired,
                                condition.IsNegated,
                                Game.Server.Service.Faction.MinimumFaction, Game.Server.Service.Faction.MaximumFaction);
                        break;

                    case "condition-has-faction-points" when arguments.Length > 1:
                        if (int.TryParse(arguments[0], out var pointsFaction)
                            && int.TryParse(arguments[1], out var pointsRequired))
                            IntersectFactionRange(factionPointRanges, pointsFaction, pointsRequired,
                                condition.IsNegated, 0, int.MaxValue);
                        break;

                    case "condition-can-accept-quest" when arguments.Length > 0:
                    {
                        // Reaching an offer means meeting its prerequisites too, the same reason
                        // QuestsConstrainedBy pins the prerequisite quest ids.
                        var quest = _gameCode?.FindQuest(arguments[0]);
                        foreach (var keyItem in quest?.PrerequisiteKeyItems ?? Array.Empty<string>())
                            keyItems.Add(keyItem);
                        foreach (var (skill, _) in quest?.PrerequisiteSkills ?? Array.Empty<(string, int)>())
                            skills.Add(skill);
                        break;
                    }
                }
            }

            return new GuardBudget
            {
                KeyItems = keyItems,
                Skills = skills,
                FactionStandingRanges = factionStandingRanges,
                FactionPointRanges = factionPointRanges,
                Tutorial = tutorial
            };
        }

        /// <summary>
        /// Narrows the allowed interval for one faction to what a single guard on it permits — at
        /// least <paramref name="required"/> for a positive guard, below it for a negated one — and
        /// intersects that with whatever this opening's other guards on the same faction already
        /// allow, since every guard on a link must pass at once.
        /// </summary>
        private static void IntersectFactionRange(
            Dictionary<int, (int Min, int Max)> ranges, int factionId, int required, bool isNegated,
            int floor, int ceiling)
        {
            var (min, max) = isNegated ? (floor, required - 1) : (required, ceiling);

            if (ranges.TryGetValue(factionId, out var existing))
            {
                min = Math.Max(min, existing.Min);
                max = Math.Min(max, existing.Max);
            }

            ranges[factionId] = (min, max);
        }

        /// <summary>The non-quest state one opening's own guards require, so <see cref="TryBreak"/> can avoid it.</summary>
        private sealed class GuardBudget
        {
            public required ISet<string> KeyItems { get; init; }
            public required ISet<string> Skills { get; init; }
            public required IReadOnlyDictionary<int, (int Min, int Max)> FactionStandingRanges { get; init; }
            public required IReadOnlyDictionary<int, (int Min, int Max)> FactionPointRanges { get; init; }
            public required bool Tutorial { get; init; }
        }

        /// <summary>
        /// For quests this opening's own guards need in progress, the exact step
        /// <see cref="ApplyToSatisfy"/> gave them — a specific step from a non-negated
        /// condition-on-quest-state, or null when only condition-has-quest asked for "in progress"
        /// and any step will do. Lets <see cref="TryBreak"/> nudge a protected quest's step to
        /// dodge an earlier guard's list instead of refusing to touch it at all.
        /// </summary>
        private static IReadOnlyDictionary<string, int?> QuestStatePinsOf(DlgLink opening)
        {
            var pins = new Dictionary<string, int?>(StringComparer.Ordinal);

            foreach (var condition in opening.Conditions)
            {
                if (condition.IsNegated || condition.SnippetKey != "condition-has-quest")
                    continue;

                var questId = condition.Arguments.FirstOrDefault();
                if (!string.IsNullOrEmpty(questId))
                    pins.TryAdd(questId, null);
            }

            // A pinned step is more specific than a bare "in progress", so it always wins,
            // regardless of which order the two conditions appear in.
            foreach (var condition in opening.Conditions)
            {
                if (condition.IsNegated || condition.SnippetKey != "condition-on-quest-state"
                    || condition.Arguments.Length < 2)
                    continue;

                var questId = condition.Arguments[0];
                if (!string.IsNullOrEmpty(questId) && int.TryParse(condition.Arguments[1], out var state))
                    pins[questId] = state;
            }

            return pins;
        }

        /// <summary>Quest ids this opening's own guards pin down, which a breaker must not touch.</summary>
        private ISet<string> QuestsConstrainedBy(DlgLink opening)
        {
            var quests = new HashSet<string>(StringComparer.Ordinal);
            foreach (var condition in opening.Conditions)
            {
                if (!IsQuestSnippet(condition.SnippetKey))
                    continue;

                if (condition.SnippetKey == "condition-completed-quest")
                {
                    foreach (var questId in condition.Arguments)
                        quests.Add(questId);
                    continue;
                }

                var first = condition.Arguments.FirstOrDefault();
                if (!string.IsNullOrEmpty(first))
                {
                    quests.Add(first);

                    // Reaching an offer means meeting its prerequisites, so those are pinned too.
                    if (condition.SnippetKey == "condition-can-accept-quest")
                    {
                        foreach (var prerequisite in _gameCode?.FindQuest(first)?.PrerequisiteQuestIds
                                                     ?? Array.Empty<string>())
                            quests.Add(prerequisite);
                    }
                }
            }

            return quests;
        }

        /// <summary>
        /// Every quest this conversation mentions, with a cell per step showing whether anything is
        /// written for a player in that position.
        /// </summary>
        public IReadOnlyList<QuestCoverage> Coverage()
        {
            var questIds = new List<string>();
            foreach (var questId in MentionedQuestIds())
            {
                if (!questIds.Contains(questId, StringComparer.Ordinal))
                    questIds.Add(questId);
            }

            var situations = Situations();
            var result = new List<QuestCoverage>();

            foreach (var questId in questIds)
            {
                var quest = _gameCode?.FindQuest(questId);
                var stateCount = quest?.StateCount ?? MaxStateMentioned(questId);
                var cells = new List<CoverageCell>
                {
                    new("OFFER", CoversOffer(situations, questId), null)
                };

                for (var state = 1; state <= stateCount; state++)
                {
                    cells.Add(new CoverageCell(
                        state.ToString(),
                        CoversState(situations, questId, state, isFinalState: state == stateCount),
                        state));
                }

                cells.Add(new CoverageCell("DONE", CoversCompleted(situations, questId), null));

                result.Add(new QuestCoverage
                {
                    QuestId = questId,
                    Name = quest?.Name ?? questId,
                    IsRepeatable = quest?.IsRepeatable ?? false,
                    Cells = cells
                });
            }

            return result;
        }

        /// <summary>Every quest id named by any condition or action in this conversation.</summary>
        public IEnumerable<string> MentionedQuestIds()
        {
            foreach (var link in _document.AllLinks())
            {
                foreach (var condition in link.Conditions)
                {
                    if (!IsQuestSnippet(condition.SnippetKey))
                        continue;

                    var count = condition.SnippetKey == "condition-completed-quest"
                        ? condition.Arguments.Length
                        : Math.Min(condition.Arguments.Length, 1);
                    for (var i = 0; i < count; i++)
                        yield return condition.Arguments[i];
                }
            }

            foreach (var node in _document.Entries.Concat(_document.Replies))
            {
                foreach (var action in node.Actions)
                {
                    if (IsQuestSnippet(action.SnippetKey) && action.Arguments.Length > 0)
                        yield return action.Arguments[0];
                }
            }
        }

        /// <summary>
        /// Every value this conversation names for one kind of argument — the key items it checks,
        /// the skills it gates on, and so forth.
        /// </summary>
        /// <remarks>
        /// This is what decides which controls the pretend-player row needs. Offering every key item
        /// in the game would be a list of hundreds; offering the two this NPC actually looks at is a
        /// row of two. A conversation guarded on something with no control is a conversation the
        /// writer cannot navigate, so the row is built from the conversation rather than fixed.
        /// </remarks>
        public IEnumerable<string> MentionedArguments(
            SnippetCatalog snippets,
            Game.Server.Service.SnippetService.SnippetArgumentType type)
        {
            foreach (var (param, _) in AllParams())
            {
                var snippet = snippets.Find(param.Key);
                if (snippet == null)
                    continue;

                var arguments = param.Arguments;
                for (var i = 0; i < arguments.Length; i++)
                {
                    if (snippet.ArgumentAt(i)?.Type == type)
                        yield return arguments[i];
                }
            }
        }

        /// <summary>True when any guard in this conversation reads the named snippet.</summary>
        public bool Uses(string snippetKey) =>
            AllParams().Any(entry => entry.Param.SnippetKey == snippetKey);

        private IEnumerable<(DlgParam Param, bool IsCondition)> AllParams()
        {
            foreach (var link in _document.AllLinks())
            {
                foreach (var condition in link.Conditions)
                    yield return (condition, true);
            }

            foreach (var node in _document.Entries.Concat(_document.Replies))
            {
                foreach (var action in node.Actions)
                    yield return (action, false);
            }
        }

        private static bool IsQuestSnippet(string key) =>
            key.Contains("quest", StringComparison.Ordinal);

        private bool CoversOffer(IReadOnlyList<Situation> situations, string questId)
        {
            // An offer is any reachable line that starts the quest.
            foreach (var situation in situations)
            {
                if (situation.State == SituationState.Unreachable)
                    continue;

                if (Beneath(situation.Opening).Any(node =>
                        node.Actions.Any(action =>
                            action.SnippetKey == "action-accept-quest"
                            && action.Arguments.FirstOrDefault() == questId)))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Whether a player sitting on this step of the quest has something written for them.
        /// </summary>
        /// <remarks>
        /// A generic "is doing this quest" opening is a perfectly good reminder and covers the
        /// middle steps. It deliberately does NOT cover the final step: that is the turn-in, which
        /// the content standards ask for as its own beat, and letting a reminder stand in for it
        /// would hide the single most common gap the strip exists to show.
        /// </remarks>
        private bool CoversState(IReadOnlyList<Situation> situations, string questId, int state, bool isFinalState)
        {
            foreach (var situation in situations)
            {
                if (situation.State != SituationState.Written)
                    continue;

                foreach (var condition in situation.Opening.Conditions)
                {
                    if (condition.IsNegated || condition.Arguments.FirstOrDefault() != questId)
                        continue;

                    if (condition.SnippetKey == "condition-has-quest" && !isFinalState)
                        return true;

                    if (condition.SnippetKey == "condition-on-quest-state"
                        && condition.Arguments.Skip(1).Any(argument => argument == state.ToString()))
                        return true;
                }
            }

            return false;
        }

        private bool CoversCompleted(IReadOnlyList<Situation> situations, string questId)
        {
            return situations.Any(situation =>
                situation.State == SituationState.Written
                && situation.Opening.Conditions.Any(condition =>
                    !condition.IsNegated
                    && condition.SnippetKey == "condition-completed-quest"
                    && condition.Arguments.Contains(questId)));
        }

        private int MaxStateMentioned(string questId)
        {
            var highest = 1;
            foreach (var link in _document.AllLinks())
            {
                foreach (var condition in link.Conditions)
                {
                    if (condition.SnippetKey != "condition-on-quest-state"
                        || condition.Arguments.FirstOrDefault() != questId)
                        continue;

                    foreach (var argument in condition.Arguments.Skip(1))
                    {
                        if (int.TryParse(argument, out var state) && state > highest)
                            highest = state;
                    }
                }
            }

            return highest;
        }

        private void ApplyToSatisfy(DlgLink opening, PretendPlayer player)
        {
            foreach (var condition in opening.Conditions)
            {
                var arguments = condition.Arguments;
                var satisfy = !condition.IsNegated;

                switch (condition.SnippetKey)
                {
                    case "condition-has-quest" when arguments.Length > 0:
                        player.WithQuest(arguments[0],
                            satisfy ? QuestProgress.OnStep(1) : QuestProgress.None);
                        break;

                    case "condition-completed-quest":
                        foreach (var questId in arguments)
                            player.WithQuest(questId, satisfy ? QuestProgress.Completed : QuestProgress.None);
                        break;

                    case "condition-on-quest-state" when arguments.Length > 1:
                        if (satisfy && int.TryParse(arguments[1], out var state))
                            player.WithQuest(arguments[0], QuestProgress.OnStep(state));
                        else if (!satisfy)
                            player.WithQuest(arguments[0], QuestProgress.None);
                        break;

                    case "condition-can-accept-quest" when arguments.Length > 0:
                        if (satisfy)
                        {
                            SatisfyPrerequisites(arguments[0], player);
                        }
                        else
                        {
                            // Being in progress makes CanAcceptQuest false for both repeatable and
                            // one-shot quests, without accidentally invalidating another opening's
                            // prerequisite checks.
                            player.WithQuest(arguments[0], QuestProgress.OnStep(1));
                        }
                        break;

                    case "condition-all-key-items":
                        foreach (var keyItem in arguments)
                        {
                            if (satisfy)
                                player.WithKeyItem(keyItem);
                        }

                        break;

                    case "condition-has-completed-tutorial":
                        if (satisfy)
                            player.WithTutorialCompleted();
                        break;

                    case "condition-any-skill":
                    case "condition-all-skills":
                        for (var i = 0; satisfy && i + 1 < arguments.Length; i += 2)
                        {
                            if (int.TryParse(arguments[i + 1], out var rank))
                                player.WithSkill(arguments[i], rank);
                        }

                        break;

                    case "condition-has-faction-standing" when arguments.Length > 1:
                        if (int.TryParse(arguments[0], out var standingFaction)
                            && int.TryParse(arguments[1], out var standing))
                        {
                            if (satisfy)
                                player.WithFactionStanding(standingFaction, standing);
                            else if (standing > Game.Server.Service.Faction.MinimumFaction)
                            {
                                // A negated standing guard is reachable at runtime with any value
                                // below the threshold; only a threshold at the runtime minimum is
                                // genuinely unsatisfiable.
                                player.WithFactionStanding(standingFaction, standing - 1);
                            }
                        }

                        break;

                    case "condition-has-faction-points" when arguments.Length > 1:
                        if (int.TryParse(arguments[0], out var pointsFaction)
                            && int.TryParse(arguments[1], out var points))
                        {
                            if (satisfy)
                                player.WithFactionPoints(pointsFaction, points);
                            else if (points > 0)
                                player.WithFactionPoints(pointsFaction, points - 1);
                        }

                        break;
                }
            }
        }

        private void SatisfyPrerequisites(string questId, PretendPlayer player)
        {
            var quest = _gameCode?.FindQuest(questId);
            if (quest == null)
                return;

            foreach (var prerequisite in quest.PrerequisiteQuestIds)
                player.WithQuest(prerequisite, QuestProgress.Completed);

            foreach (var keyItem in quest.PrerequisiteKeyItems)
                player.WithKeyItem(keyItem);

            foreach (var (skill, rank) in quest.PrerequisiteSkills)
                player.WithSkill(skill, rank);
        }

        private bool HasSomethingToSay(DlgLink opening, int lineCount)
        {
            if (lineCount == 0)
                return false;

            return !string.IsNullOrWhiteSpace(opening.Target.Text);
        }

        private (int Lines, int Choices) CountBeneath(DlgLink opening)
        {
            var lines = 0;
            var choices = 0;
            foreach (var node in Beneath(opening))
            {
                if (node.IsEntry)
                    lines++;
                else
                    choices++;
            }

            return (lines, choices);
        }

        /// <summary>
        /// Every node reachable from an opening. Visited by position so a conversation that loops
        /// back on itself — which most of them do — terminates.
        /// </summary>
        private IEnumerable<DlgNode> Beneath(DlgLink opening)
        {
            if (!_document.HasNode(DlgNodeKind.Entry, opening.TargetIndex))
                yield break;

            var seen = new HashSet<(DlgNodeKind, int)>();
            var pending = new Queue<(DlgNodeKind Kind, int Index)>();
            pending.Enqueue((DlgNodeKind.Entry, opening.TargetIndex));
            seen.Add((DlgNodeKind.Entry, opening.TargetIndex));

            while (pending.Count > 0)
            {
                var (kind, index) = pending.Dequeue();
                var node = _document.GetNode(kind, index);
                yield return node;

                foreach (var link in node.Links)
                {
                    if (!_document.HasNode(link.TargetKind, link.TargetIndex))
                        continue;

                    if (seen.Add((link.TargetKind, link.TargetIndex)))
                        pending.Enqueue((link.TargetKind, link.TargetIndex));
                }
            }
        }

        private string WhenFor(DlgLink opening)
        {
            if (opening.Conditions.Count == 0)
                return "anyone else";

            return string.Join(", and ", opening.Conditions.Select(condition => _evaluator.Describe(condition)));
        }

        /// <summary>
        /// A short label for the rail, because "Doing Field Tinctures" is what a writer scans for.
        /// </summary>
        /// <remarks>
        /// Negated guards are read first, and deliberately. The module writes an offer as "finished
        /// the previous quest, and has NOT taken or finished this one" — so the quest the situation
        /// is actually about is the negated one, while the positive guard names its prerequisite.
        /// Reading positives first labels every offer after its predecessor instead.
        /// </remarks>
        private string TitleFor(DlgLink opening)
        {
            if (opening.Conditions.Count == 0)
                return "First meeting";

            foreach (var condition in opening.Conditions)
            {
                var questId = condition.Arguments.FirstOrDefault();
                if (!condition.IsNegated || string.IsNullOrEmpty(questId) || !IsQuestSnippet(condition.SnippetKey))
                    continue;

                return $"Offering {QuestName(questId)}";
            }

            foreach (var condition in opening.Conditions)
            {
                var questId = condition.Arguments.FirstOrDefault();
                if (condition.IsNegated || string.IsNullOrEmpty(questId))
                    continue;

                switch (condition.SnippetKey)
                {
                    case "condition-can-accept-quest":
                        return $"Offering {QuestName(questId)}";
                    case "condition-on-quest-state":
                        return condition.Arguments.Length > 1
                            ? $"On step {condition.Arguments[1]} of {QuestName(questId)}"
                            : $"Partway through {QuestName(questId)}";
                    case "condition-has-quest":
                        return $"Doing {QuestName(questId)}";
                    case "condition-completed-quest":
                        return $"Finished {QuestName(questId)}";
                }
            }

            return Summarize(opening.Target.Text);
        }

        private string QuestName(string questId) => _gameCode?.FindQuest(questId)?.Name ?? questId;

        private static string Summarize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "(nothing written)";

            var trimmed = text.Trim();
            return trimmed.Length <= 40 ? trimmed : trimmed[..37].TrimEnd() + "…";
        }
    }
}
