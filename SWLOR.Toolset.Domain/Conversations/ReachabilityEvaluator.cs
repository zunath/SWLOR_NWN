using SWLOR.Game.Server.Service.SnippetService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.GameCode;

namespace SWLOR.Toolset.Domain.Conversations
{
    /// <summary>Whether a guard passes, fails, or is something the editor cannot model.</summary>
    public enum GuardOutcome
    {
        Passes,
        Fails,

        /// <summary>
        /// The condition is real but its meaning is not modelled here. Drawn as "not simulated"
        /// rather than guessed either way — a prediction that quietly invents an answer is worse
        /// than one that admits its limit.
        /// </summary>
        NotSimulated
    }

    /// <summary>One condition's verdict, with the sentence that explains it.</summary>
    public sealed class GuardResult
    {
        public required GuardOutcome Outcome { get; init; }

        /// <summary>The condition in plain English, negation already applied.</summary>
        public required string Sentence { get; init; }

        /// <summary>The raw key, for the advanced view and for reporting unknown snippets.</summary>
        public required string Key { get; init; }
    }

    /// <summary>Whether a whole link is open to a player, and why.</summary>
    public sealed class LinkReachability
    {
        public required IReadOnlyList<GuardResult> Guards { get; init; }

        /// <summary>
        /// True when nothing blocks this route. Every condition on a link must pass — there is no
        /// OR — and an unmodelled condition counts as open, because refusing to show a line the
        /// editor merely cannot reason about would hide real content.
        /// </summary>
        public bool IsOpen => Guards.All(guard => guard.Outcome != GuardOutcome.Fails);

        /// <summary>True when some guard could not be modelled, so <see cref="IsOpen"/> is a guess.</summary>
        public bool IsUncertain => Guards.Any(guard => guard.Outcome == GuardOutcome.NotSimulated);
    }

    /// <summary>
    /// Predicts what a hypothetical player would see. This is the navigation model of the whole
    /// editor, not a debugging aid: which opening wins, which choices are hidden, and which coverage
    /// cells are filled are all this question asked with different inputs.
    /// </summary>
    /// <remarks>
    /// It predicts from the snippets' declared meanings; it does not run C#. The quest, key item,
    /// skill, faction and tutorial conditions are modelled, which is every condition the module
    /// actually uses. Anything else returns <see cref="GuardOutcome.NotSimulated"/> and the surface
    /// says so.
    /// </remarks>
    public sealed class ReachabilityEvaluator
    {
        private readonly SnippetCatalog _snippets;
        private readonly IGameCodeIndex? _gameCode;

        public ReachabilityEvaluator(SnippetCatalog snippets, IGameCodeIndex? gameCode = null)
        {
            _snippets = snippets;
            _gameCode = gameCode;
        }

        /// <summary>Whether this route is open to the given player, and what each guard decided.</summary>
        /// <remarks>
        /// <c>Snippet.ProcessConditions</c> keys its condition dictionary by the bare snippet name, so
        /// a node carrying both <c>key</c> and <c>!key</c> is only ever looked at once per key: it
        /// checks whether <c>!key</c> is set first and, if so, evaluates only that param — the plain
        /// <c>key</c> param is never read at all. dantherbs ships exactly this on its Field Tinctures
        /// offer (<c>condition-completed-quest harvest_herbs</c> beside
        /// <c>!condition-completed-quest field_tinctures</c>); ANDing both, as a naive per-condition
        /// loop would, claims the offer needs the harvest quest when the live server does not check it
        /// at all. The positive form is reported as <see cref="GuardOutcome.NotSimulated"/> instead —
        /// it does not block <see cref="LinkReachability.IsOpen"/>, but the sentence says why it is
        /// listed without mattering, rather than silently vanishing from the guard list.
        /// </remarks>
        public LinkReachability Evaluate(DlgLink link, PretendPlayer player)
        {
            var negatedKeys = new HashSet<string>(
                link.Conditions.Where(condition => condition.IsNegated).Select(condition => condition.SnippetKey),
                StringComparer.Ordinal);

            var guards = new List<GuardResult>();
            foreach (var condition in link.Conditions)
            {
                var snippet = _snippets.Find(condition.Key);
                var sentence = Describe(condition, snippet);

                if (!condition.IsNegated && negatedKeys.Contains(condition.SnippetKey))
                {
                    guards.Add(new GuardResult
                    {
                        Outcome = GuardOutcome.NotSimulated,
                        Sentence = $"{sentence} (the game never reads this: "
                            + $"!{condition.SnippetKey} is also set on this node, and that takes precedence)",
                        Key = condition.Key
                    });
                    continue;
                }

                if (snippet == null)
                {
                    // A key the game no longer defines is skipped at runtime, so the route is open -
                    // but the writer should be told, which is what the sentence carries.
                    guards.Add(new GuardResult
                    {
                        Outcome = GuardOutcome.NotSimulated,
                        Sentence = sentence,
                        Key = condition.Key
                    });
                    continue;
                }

                // A known runtime condition with malformed arguments fails before negation. The
                // server refuses these guards rather than treating them as unknown; allowing the
                // editor's "not simulated" fallback to keep the route open (or inverting a failure
                // into a pass for !condition-*) would preview a line the game will never select.
                if (HasMalformedKnownArguments(snippet, condition.Arguments))
                {
                    guards.Add(new GuardResult
                    {
                        Outcome = GuardOutcome.Fails,
                        Sentence = sentence + " (invalid arguments)",
                        Key = condition.Key
                    });
                    continue;
                }

                var outcome = EvaluateSnippet(snippet, condition.Arguments, player);
                if (outcome != GuardOutcome.NotSimulated && condition.IsNegated)
                    outcome = outcome == GuardOutcome.Passes ? GuardOutcome.Fails : GuardOutcome.Passes;

                guards.Add(new GuardResult { Outcome = outcome, Sentence = sentence, Key = condition.Key });
            }

            return new LinkReachability { Guards = guards };
        }

        private static bool HasMalformedKnownArguments(
            SnippetDescriptor snippet,
            IReadOnlyList<string> arguments)
        {
            var isSimulated = snippet.Key is
                "condition-has-quest" or
                "condition-completed-quest" or
                "condition-on-quest-state" or
                "condition-can-accept-quest" or
                "condition-all-key-items" or
                "condition-has-completed-tutorial" or
                "condition-any-skill" or
                "condition-all-skills" or
                "condition-has-faction-standing" or
                "condition-has-faction-points";
            if (!isSimulated)
                return false;

            if (snippet.Arguments.Count > 0 && !snippet.HasEnoughArguments(arguments.Count))
                return true;

            if (snippet.RepeatGroupSize > 0 && !snippet.IsValidArgumentCount(arguments.Count))
                return true;

            return snippet.Key switch
            {
                "condition-on-quest-state" =>
                    arguments.Skip(1).Any(argument => !int.TryParse(argument, out _)),
                "condition-any-skill" or "condition-all-skills" =>
                    arguments.Where((_, index) => index % 2 == 0).Any(skill =>
                        !Enum.TryParse<SkillType>(skill, ignoreCase: true, out var parsed) ||
                        parsed == SkillType.Invalid ||
                        !Enum.IsDefined(parsed)) ||
                    arguments.Where((_, index) => index % 2 == 1).Any(rank =>
                        !int.TryParse(rank, out _)),
                "condition-has-faction-standing" or "condition-has-faction-points" =>
                    arguments.Take(2).Any(argument => !int.TryParse(argument, out _)),
                _ => false
            };
        }

        /// <summary>
        /// The opening this player actually gets: the first whose guards all pass. Null when none
        /// of them fit, which means the conversation cannot start for that player at all.
        /// </summary>
        public DlgLink? ResolveOpening(DlgDocument document, PretendPlayer player)
        {
            foreach (var opening in document.Openings)
            {
                if (Evaluate(opening, player).IsOpen)
                    return opening;
            }

            return null;
        }

        /// <summary>The choices this player would see under an NPC line, in order.</summary>
        public IReadOnlyList<DlgLink> VisibleChoices(DlgNode entry, PretendPlayer player)
        {
            if (!entry.IsEntry)
                throw new ArgumentException(
                    "Choices are what follows an NPC line. To continue past a player choice, use ResolveNextLine.",
                    nameof(entry));

            return entry.Links.Where(link => Evaluate(link, player).IsOpen).ToList();
        }

        /// <summary>
        /// The NPC line that follows a player choice, or null when the choice ends the conversation.
        /// </summary>
        /// <remarks>
        /// A conversation alternates, so a reply's links point at entries rather than at more
        /// choices — and the engine takes the first whose guards pass, exactly as it does for an
        /// opening. Getting this wrong is easy from the outside, which is why it is a named method
        /// rather than a second call to <see cref="VisibleChoices"/>.
        /// </remarks>
        public DlgLink? ResolveNextLine(DlgNode reply, PretendPlayer player)
        {
            if (reply.IsEntry)
                throw new ArgumentException(
                    "This continues past a player choice. For the choices under an NPC line, use VisibleChoices.",
                    nameof(reply));

            foreach (var link in reply.Links)
            {
                if (Evaluate(link, player).IsOpen)
                    return link;
            }

            return null;
        }

        /// <summary>
        /// Applies a line's actions to a player, so a walk can carry state forward the way the game
        /// would. Only the quest and key-item actions move anything the evaluator reads; the rest
        /// change the world rather than the player's answer to a guard.
        /// </summary>
        public PretendPlayer ApplyActions(DlgNode node, PretendPlayer player)
        {
            var result = player.Clone();

            foreach (var action in node.Actions.Where(action => !action.IsOncePerPlayerMarker))
            {
                var arguments = action.Arguments;
                switch (action.SnippetKey)
                {
                    case "action-accept-quest" when arguments.Length > 0:
                        TryAcceptQuest(result, arguments[0]);
                        break;

                    case "action-advance-quest" when arguments.Length > 0:
                        TryAdvanceQuest(result, arguments[0]);
                        break;

                    case "action-give-key-items":
                        foreach (var keyItem in arguments)
                            result.WithKeyItem(keyItem);
                        break;

                    case "action-give-faction-standing":
                    case "action-take-faction-standing":
                        ApplyFactionChange(
                            arguments,
                            action.SnippetKey.StartsWith("action-give-", StringComparison.Ordinal),
                            result.GetFactionStanding,
                            result.WithFactionStanding,
                            ClampStanding);
                        break;

                    case "action-give-faction-points":
                    case "action-take-faction-points":
                        ApplyFactionChange(
                            arguments,
                            action.SnippetKey.StartsWith("action-give-", StringComparison.Ordinal),
                            result.GetFactionPoints,
                            result.WithFactionPoints,
                            ClampPoints);
                        break;
                }

            }

            return result;
        }

        /// <summary>
        /// Mirrors the clamp <c>Faction.AdjustPlayerFactionStanding/Points</c> applies at write time,
        /// so a simulated give/take can never land on a value the runtime could never produce -
        /// taking more points than the player owns, or pushing standing past the game's range.
        /// </summary>
        private static void ApplyFactionChange(
            string[] arguments,
            bool give,
            Func<int, int> read,
            Func<int, int, PretendPlayer> write,
            Func<int, int> clamp)
        {
            if (arguments.Length < 2
                || !int.TryParse(arguments[0], out var factionId)
                || !int.TryParse(arguments[1], out var parsedAmount))
                return;

            var amount = Math.Abs(parsedAmount);
            var updated = read(factionId) + (give ? amount : -amount);
            write(factionId, clamp(updated));
        }

        /// <summary>Standing is clamped to the runtime's range, same as <c>Faction.AdjustPlayerFactionStanding</c>.</summary>
        private static int ClampStanding(int value) =>
            Math.Clamp(value, Game.Server.Service.Faction.MinimumFaction, Game.Server.Service.Faction.MaximumFaction);

        /// <summary>Points only floor at zero, same as <c>Faction.AdjustPlayerFactionPoints</c> - there is no ceiling.</summary>
        private static int ClampPoints(int value) => Math.Max(0, value);

        /// <summary>The condition as a sentence, with ids resolved to names where they are known.</summary>
        public string Describe(DlgParam condition, SnippetDescriptor? snippet = null)
        {
            snippet ??= _snippets.Find(condition.Key);
            if (snippet == null)
                return $"an unknown rule ({condition.SnippetKey})";

            return snippet.ToSentence(condition.Arguments, condition.IsNegated, DisplayValue);
        }

        /// <summary>An action as a sentence, for the consequence shown beside a choice.</summary>
        public string DescribeAction(DlgParam action)
        {
            var snippet = _snippets.Find(action.Key);
            return snippet == null
                ? $"an unknown effect ({action.SnippetKey})"
                : snippet.ToSentence(action.Arguments, negated: false, display: DisplayValue);
        }

        /// <summary>
        /// Mirrors <c>QuestDetail.Accept</c>, which silently refuses under <c>CanAccept</c>: already
        /// on the quest, already finished it and not repeatable, or missing a prerequisite quest.
        /// Unlike a guard, an unprovable gate here must not be guessed open - firing anyway would
        /// rewind or restart a quest the game would have left alone, so the pretend player's state is
        /// left exactly as it was.
        /// </summary>
        private void TryAcceptQuest(PretendPlayer player, string questId)
        {
            var progress = player.GetQuest(questId);

            // Accept refuses outright once a quest is already under way - this needs nothing from
            // the game code, so it always applies.
            if (progress.IsInProgress)
                return;

            var quest = _gameCode?.FindQuest(questId);

            // Already finished: refused unless repeatable. An unknown quest cannot prove
            // repeatability, so it is treated the same as non-repeatable rather than guessed open.
            if (progress.IsCompleted && (quest == null || !quest.IsRepeatable))
                return;

            if (quest != null)
            {
                foreach (var prerequisite in quest.PrerequisiteQuestIds)
                {
                    if (!player.GetQuest(prerequisite).IsCompleted)
                        return;
                }

                // The same key-item and skill gates CanAcceptQuest enforces: runtime
                // QuestDetail.CanAccept refuses when any is missing, so the simulated accept
                // must too (a rank-50 capstone gate must not open in preview for a rank-0 player).
                foreach (var keyItem in quest.PrerequisiteKeyItems)
                {
                    if (!player.HasKeyItem(keyItem))
                        return;
                }

                foreach (var (skill, rank) in quest.PrerequisiteSkills)
                {
                    if (player.GetSkillRank(skill) < rank)
                        return;
                }
            }

            player.WithQuest(questId, QuestProgress.OnStep(1));
        }

        /// <summary>
        /// Mirrors <c>QuestDetail.Advance</c>, which silently refuses when the player has not
        /// accepted the quest (<c>CurrentState &lt;= 0</c>) or has already finished it. Whether the
        /// current step's objectives are complete is not modelled at all - the pretend player carries
        /// no objective progress - so that gate is neither enforced nor guessed; reaching this action
        /// is taken as the writer's word that the step is done.
        /// </summary>
        private void TryAdvanceQuest(PretendPlayer player, string questId)
        {
            var progress = player.GetQuest(questId);
            if (!progress.IsInProgress)
                return;

            player.WithQuest(questId, Advance(progress, questId));
        }

        private QuestProgress Advance(QuestProgress current, string questId)
        {
            var quest = _gameCode?.FindQuest(questId);
            var next = (current.CurrentState ?? 0) + 1;

            // Advancing past the last step is what completes a quest and pays it out. Without a
            // known state count the step still moves, which keeps a walk usable when the source
            // scan is unavailable.
            if (quest != null && next > quest.StateCount)
            {
                // QuestDetail.Advance only completes the quest immediately when reward selection is
                // disabled. When the quest calls .HasRewardSelection(), reaching the final state
                // instead opens QuestRewardSelectionDialog and leaves the quest on its final state -
                // not completed - until the player actually chooses a reward, which this walk does not
                // simulate. Reporting Completed here would let condition-completed-quest pass, and
                // post-completion routes appear reachable, before the runtime has actually finished
                // the quest.
                if (quest.HasRewardSelection)
                    return QuestProgress.OnStep(quest.StateCount);

                return quest.IsRepeatable
                    ? new QuestProgress { IsCompleted = true }
                    : QuestProgress.Completed;
            }

            return QuestProgress.OnStep(next);
        }

        private GuardOutcome EvaluateSnippet(SnippetDescriptor snippet, string[] arguments, PretendPlayer player)
        {
            switch (snippet.Key)
            {
                case "condition-has-quest":
                    return arguments.Length < 1
                        ? GuardOutcome.NotSimulated
                        : Verdict(player.GetQuest(arguments[0]).IsInProgress);

                case "condition-completed-quest":
                    if (arguments.Length < 1)
                        return GuardOutcome.NotSimulated;

                    return Verdict(arguments.All(questId => player.GetQuest(questId).IsCompleted));

                case "condition-on-quest-state":
                {
                    if (arguments.Length < 2)
                        return GuardOutcome.NotSimulated;

                    var progress = player.GetQuest(arguments[0]);
                    if (progress.IsCompleted || progress.CurrentState == null)
                        return GuardOutcome.Fails;

                    for (var i = 1; i < arguments.Length; i++)
                    {
                        if (int.TryParse(arguments[i], out var state) && progress.CurrentState == state)
                            return GuardOutcome.Passes;
                    }

                    return GuardOutcome.Fails;
                }

                case "condition-can-accept-quest":
                {
                    if (arguments.Length < 1)
                        return GuardOutcome.NotSimulated;

                    return CanAcceptQuest(arguments[0], player);
                }

                case "condition-all-key-items":
                    if (arguments.Length < 1)
                        return GuardOutcome.NotSimulated;

                    return Verdict(arguments.All(player.HasKeyItem));

                case "condition-has-completed-tutorial":
                    return Verdict(player.HasCompletedTutorial);

                case "condition-any-skill":
                case "condition-all-skills":
                {
                    if (arguments.Length < 2 || arguments.Length % 2 != 0)
                        return GuardOutcome.NotSimulated;

                    var wantsAll = snippet.Key == "condition-all-skills";
                    var anyMet = false;
                    for (var i = 0; i < arguments.Length; i += 2)
                    {
                        if (!int.TryParse(arguments[i + 1], out var requiredRank))
                            return GuardOutcome.NotSimulated;

                        var met = player.GetSkillRank(arguments[i]) >= requiredRank;
                        if (wantsAll && !met)
                            return GuardOutcome.Fails;

                        anyMet |= met;
                    }

                    return Verdict(wantsAll || anyMet);
                }

                case "condition-has-faction-standing":
                    return CompareFaction(arguments, player.GetFactionStanding);

                case "condition-has-faction-points":
                    return CompareFaction(arguments, player.GetFactionPoints);

                default:
                    return GuardOutcome.NotSimulated;
            }
        }

        /// <summary>
        /// Mirrors <c>Quest.CanAcceptQuest</c> as far as the declared prerequisites reach: the quest
        /// must exist, must not already be taken or (unless repeatable) finished, and its
        /// prerequisite quests must be complete. Skill and key-item gates are checked only when the
        /// pretend player carries that information.
        /// </summary>
        private GuardOutcome CanAcceptQuest(string questId, PretendPlayer player)
        {
            var quest = _gameCode?.FindQuest(questId);
            if (quest == null)
                return GuardOutcome.NotSimulated;

            var progress = player.GetQuest(questId);
            if (progress.IsInProgress)
                return GuardOutcome.Fails;

            if (progress.IsCompleted && !quest.IsRepeatable)
                return GuardOutcome.Fails;

            foreach (var prerequisite in quest.PrerequisiteQuestIds)
            {
                if (!player.GetQuest(prerequisite).IsCompleted)
                    return GuardOutcome.Fails;
            }

            foreach (var keyItem in quest.PrerequisiteKeyItems)
            {
                if (!player.HasKeyItem(keyItem))
                    return GuardOutcome.Fails;
            }

            foreach (var (skill, rank) in quest.PrerequisiteSkills)
            {
                if (player.GetSkillRank(skill) < rank)
                    return GuardOutcome.Fails;
            }

            return GuardOutcome.Passes;
        }

        private static GuardOutcome CompareFaction(string[] arguments, Func<int, int> read)
        {
            if (arguments.Length < 2
                || !int.TryParse(arguments[0], out var factionId)
                || !int.TryParse(arguments[1], out var required))
                return GuardOutcome.NotSimulated;

            return Verdict(read(factionId) >= required);
        }

        private static GuardOutcome Verdict(bool passed) =>
            passed ? GuardOutcome.Passes : GuardOutcome.Fails;

        /// <summary>
        /// Turns a raw argument into something readable, where the game code knows a name for it.
        /// Exposed so every surface that renders a snippet reads the same way.
        /// </summary>
        public string? DisplayValue(SnippetArgument argument, string value)
        {
            if (_gameCode == null)
                return null;

            switch (argument.Type)
            {
                case SnippetArgumentType.QuestId:
                    return _gameCode.FindQuest(value)?.Name;

                case SnippetArgumentType.KeyItemId:
                    return int.TryParse(value, out var keyItemId) && _gameCode.KeyItems.TryGetValue(keyItemId, out var keyItem)
                        ? keyItem
                        : null;

                case SnippetArgumentType.FactionId:
                    return int.TryParse(value, out var factionId) && _gameCode.Factions.TryGetValue(factionId, out var faction)
                        ? faction
                        : null;

                case SnippetArgumentType.SkillId:
                    return int.TryParse(value, out var skillId) && _gameCode.Skills.TryGetValue(skillId, out var skill)
                        ? skill
                        : null;

                default:
                    return null;
            }
        }
    }
}
