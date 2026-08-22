using SWLOR.Game.Server.Service.SnippetService;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.GameCode;

namespace SWLOR.Toolset.Domain.Conversations
{
    /// <summary>How much a finding matters, in the terms a writer would use.</summary>
    public enum ProblemSeverity
    {
        /// <summary>Something is broken: a player will hit it, or never will when they should.</summary>
        Broken,

        /// <summary>It works, but not the way it reads. Worth a look.</summary>
        Untidy,

        /// <summary>House style. Advisory, and never blocks anything.</summary>
        Hint
    }

    /// <summary>What part of the conversation a finding is about, so the editor can point at it.</summary>
    public enum ProblemAnchor
    {
        Conversation,
        Situation,
        Line,
        Choice
    }

    /// <summary>
    /// One finding, phrased as a consequence rather than as a rule name. "This can never happen,
    /// because the greeting above it answers everybody" is something a writer can act on;
    /// "UnreachableOpeningRule" is not.
    /// </summary>
    public sealed class ConversationProblem
    {
        public required string RuleId { get; init; }

        public required ProblemSeverity Severity { get; init; }

        public required string Message { get; init; }

        public required ProblemAnchor Anchor { get; init; }

        /// <summary>The situation this is about, when the finding belongs to an opening.</summary>
        public Situation? Situation { get; init; }

        /// <summary>The line this is about, when the finding belongs to a node.</summary>
        public DlgNode? Node { get; init; }

        /// <summary>The route this is about, when the finding belongs to a link.</summary>
        public DlgLink? Link { get; init; }

        public override string ToString() => $"[{Severity}] {Message}";
    }

    /// <summary>
    /// Reads one conversation and reports what is wrong with it, anchored to the situation, line or
    /// choice that carries the problem.
    /// </summary>
    public sealed class ConversationAnalyzer
    {
        /// <summary>
        /// Scaffolding the content standards ask writers to cut. Matched case-insensitively as
        /// whole phrases; deliberately short, because a long list of banned words turns into noise
        /// that gets ignored wholesale.
        /// </summary>
        private static readonly string[] BannedScaffolding =
        {
            "traveler", "adventurer", "I need you to", "Can you help me",
            "return when it is done", "thank you for your help"
        };

        private static readonly string[] GreetingOpeners =
        {
            "greetings", "hello", "welcome", "good day", "hail", "well met"
        };

        private readonly SnippetCatalog _snippets;
        private readonly IGameCodeIndex? _gameCode;
        private readonly ReachabilityEvaluator _evaluator;

        public ConversationAnalyzer(
            SnippetCatalog snippets,
            ReachabilityEvaluator evaluator,
            IGameCodeIndex? gameCode = null)
        {
            _snippets = snippets;
            _evaluator = evaluator;
            _gameCode = gameCode;
        }

        public IReadOnlyList<ConversationProblem> Analyze(DlgDocument document)
        {
            var model = new SituationModel(document, _evaluator, _gameCode);
            var situations = model.Situations();
            var problems = new List<ConversationProblem>();

            AddUnreachableOpenings(situations, problems);
            AddEmptySituations(situations, problems);
            AddDanglingLinks(document, problems);
            AddUnwrittenNodes(document, problems);
            AddConditionalChoiceFallbacks(document, problems);
            AddAutomaticContinuationLoops(document, problems);
            AddDuplicateSnippetKeys(document, problems);
            AddDispatcherProblems(document, problems);
            AddSnippetProblems(document, problems);
            AddOrphans(document, problems);
            AddCoverageGaps(model, problems);
            AddStrayLanguages(document, problems);
            AddHouseStyle(document, situations, problems);

            return problems;
        }

        private static void AddUnreachableOpenings(
            IReadOnlyList<Situation> situations,
            List<ConversationProblem> problems)
        {
            for (var i = 0; i < situations.Count; i++)
            {
                if (situations[i].State != SituationState.Unreachable)
                    continue;

                var catchAll = situations.Take(i).FirstOrDefault(earlier => earlier.IsCatchAll);
                var because = catchAll != null
                    ? $"“{Snippet(catchAll.Opening.Target.Text)}” answers everybody, and it comes first."
                    : "every situation above it already covers each player who could reach it.";

                problems.Add(new ConversationProblem
                {
                    RuleId = "unreachable-opening",
                    Severity = ProblemSeverity.Broken,
                    Message = $"“{situations[i].Title}” can never happen. {because}",
                    Anchor = ProblemAnchor.Situation,
                    Situation = situations[i]
                });
            }
        }

        private static void AddEmptySituations(
            IReadOnlyList<Situation> situations,
            List<ConversationProblem> problems)
        {
            foreach (var situation in situations.Where(s => s.State == SituationState.Empty))
            {
                problems.Add(new ConversationProblem
                {
                    RuleId = "empty-situation",
                    Severity = ProblemSeverity.Broken,
                    Message = $"Nothing is written for “{situation.Title}” — a player who fits it hears silence.",
                    Anchor = ProblemAnchor.Situation,
                    Situation = situation
                });
            }
        }

        /// <summary>
        /// New dialogue and quest scaffolds carry an internal marker until the writer supplies real
        /// copy. The UI renders that marker as an empty field; saving it would ship unfinished text.
        /// </summary>
        private static void AddUnwrittenNodes(DlgDocument document, List<ConversationProblem> problems)
        {
            foreach (var node in document.Entries.Concat(document.Replies))
            {
                var isMarker = node.Text is QuestConversationScaffold.Placeholder
                    or ModuleResourceTemplateFactory.PlaceholderEntryText;
                var isSilentNpc = node.IsEntry && string.IsNullOrWhiteSpace(node.Text);
                if (!isMarker && !isSilentNpc)
                    continue;

                problems.Add(new ConversationProblem
                {
                    RuleId = "required-text-missing",
                    Severity = ProblemSeverity.Broken,
                    Message = node.IsEntry
                        ? "This NPC line still needs text before the dialogue can be saved."
                        : "This player choice still needs text before the dialogue can be saved.",
                    Anchor = node.IsEntry ? ProblemAnchor.Line : ProblemAnchor.Choice,
                    Node = node
                });
            }
        }

        private static void AddDanglingLinks(DlgDocument document, List<ConversationProblem> problems)
        {
            foreach (var link in document.FindDanglingLinks())
            {
                problems.Add(new ConversationProblem
                {
                    RuleId = "dangling-route",
                    Severity = ProblemSeverity.Broken,
                    Message = "This route points to a line that no longer exists.",
                    Anchor = link.Parent?.IsEntry == true ? ProblemAnchor.Choice : ProblemAnchor.Conversation,
                    Link = link,
                    Node = link.Parent
                });
            }
        }

        /// <summary>
        /// NWN shows every reply whose check passes. With no unconditional fallback, a player can
        /// reach the NPC line and be left with no response at all.
        /// </summary>
        private static void AddConditionalChoiceFallbacks(
            DlgDocument document,
            List<ConversationProblem> problems)
        {
            foreach (var entry in document.Entries)
            {
                var choices = entry.Links;
                if (choices.Count == 0)
                    continue;

                var hasConditionalChoice = choices.Any(link =>
                    link.Conditions.Count > 0 || !string.IsNullOrWhiteSpace(link.Active));
                var hasFallback = choices.Any(link =>
                    link.Conditions.Count == 0 && string.IsNullOrWhiteSpace(link.Active));
                if (!hasConditionalChoice || hasFallback)
                    continue;

                problems.Add(new ConversationProblem
                {
                    RuleId = "conditional-choice-no-fallback",
                    Severity = ProblemSeverity.Broken,
                    Message = "Every player choice here has a check. Some players can be left with no response.",
                    Anchor = ProblemAnchor.Line,
                    Node = entry
                });
            }
        }

        /// <summary>
        /// Empty reply nodes are how NWN represents an automatic continuation. A cycle made only
        /// from those replies can advance forever without returning control to the player.
        /// </summary>
        private static void AddAutomaticContinuationLoops(
            DlgDocument document,
            List<ConversationProblem> problems)
        {
            var entriesByStruct = document.Entries.ToDictionary(entry => entry.Struct);
            var edges = new Dictionary<DlgNode, List<DlgNode>>();
            foreach (var entry in document.Entries)
            {
                var next = new List<DlgNode>();
                foreach (var replyLink in entry.Links)
                {
                    if (!document.HasNode(replyLink.TargetKind, replyLink.TargetIndex))
                        continue;

                    var reply = replyLink.Target;
                    if (!string.IsNullOrWhiteSpace(reply.Text))
                        continue;

                    foreach (var entryLink in reply.Links)
                    {
                        if (!document.HasNode(entryLink.TargetKind, entryLink.TargetIndex))
                            continue;

                        var target = entryLink.Target;
                        if (entriesByStruct.TryGetValue(target.Struct, out var canonical))
                            next.Add(canonical);
                    }
                }

                edges[entry] = next;
            }

            var states = new Dictionary<DlgNode, int>();
            var reported = new HashSet<DlgNode>();

            void Visit(DlgNode node)
            {
                states[node] = 1;
                foreach (var next in edges[node])
                {
                    states.TryGetValue(next, out var state);
                    if (state == 0)
                    {
                        Visit(next);
                    }
                    else if (state == 1 && reported.Add(next))
                    {
                        problems.Add(new ConversationProblem
                        {
                            RuleId = "automatic-continuation-loop",
                            Severity = ProblemSeverity.Broken,
                            Message = "This path loops using only automatic continuations, so the player never regains control.",
                            Anchor = ProblemAnchor.Line,
                            Node = next
                        });
                    }
                }

                states[node] = 2;
            }

            foreach (var entry in document.Entries)
            {
                if (!states.ContainsKey(entry))
                    Visit(entry);
            }
        }

        /// <summary>
        /// NWN script parameters are addressed by key, not list position. Repeating the same key
        /// on one route or node does not create two independent checks/actions; one value wins.
        /// </summary>
        private static void AddDuplicateSnippetKeys(DlgDocument document, List<ConversationProblem> problems)
        {
            foreach (var link in document.AllLinks())
            {
                foreach (var duplicate in link.Conditions
                             .GroupBy(param => param.SnippetKey, StringComparer.OrdinalIgnoreCase)
                             .Where(group => group.Count() > 1))
                {
                    problems.Add(new ConversationProblem
                    {
                        RuleId = "duplicate-condition",
                        Severity = ProblemSeverity.Broken,
                        Message = $"This route repeats the same check ({duplicate.Key}). NWN reads one value per check name, so one of them is ignored.",
                        Anchor = ProblemAnchor.Choice,
                        Link = link,
                        Node = link.Parent
                    });
                }
            }

            foreach (var node in document.Entries.Concat(document.Replies))
            {
                foreach (var duplicate in node.Actions
                             .GroupBy(param => param.SnippetKey, StringComparer.OrdinalIgnoreCase)
                             .Where(group => group.Count() > 1))
                {
                    problems.Add(new ConversationProblem
                    {
                        RuleId = "duplicate-action",
                        Severity = ProblemSeverity.Broken,
                        Message = $"This line repeats the same outcome ({duplicate.Key}). NWN reads one value per outcome name, so one of them is ignored.",
                        Anchor = node.IsEntry ? ProblemAnchor.Line : ProblemAnchor.Choice,
                        Node = node
                    });
                }
            }
        }

        /// <summary>
        /// Params with no dispatcher never run, and a dispatcher with no params does nothing. The
        /// editor writes both together, so anything found here predates it or came from Aurora.
        /// </summary>
        private void AddDispatcherProblems(DlgDocument document, List<ConversationProblem> problems)
        {
            foreach (var link in document.AllLinks())
            {
                if (link.Conditions.Count > 0 && string.IsNullOrEmpty(link.Active))
                {
                    problems.Add(new ConversationProblem
                    {
                        RuleId = "condition-never-runs",
                        Severity = ProblemSeverity.Broken,
                        Message = "This route has conditions on it, but nothing set up to check them, "
                                  + "so it always shows.",
                        Anchor = ProblemAnchor.Choice,
                        Link = link
                    });
                }
            }

            foreach (var node in document.Entries.Concat(document.Replies))
            {
                if (node.Actions.Count > 0 && string.IsNullOrEmpty(node.Script))
                {
                    problems.Add(new ConversationProblem
                    {
                        RuleId = "action-never-runs",
                        Severity = ProblemSeverity.Broken,
                        Message = "This line is meant to do something, but nothing is set up to run it, "
                                  + "so nothing happens.",
                        Anchor = ProblemAnchor.Line,
                        Node = node
                    });
                }
            }
        }

        private void AddSnippetProblems(DlgDocument document, List<ConversationProblem> problems)
        {
            foreach (var link in document.AllLinks())
            {
                foreach (var condition in link.Conditions)
                    CheckParam(condition, ProblemAnchor.Choice, link, null, problems);
            }

            foreach (var node in document.Entries.Concat(document.Replies))
            {
                foreach (var action in node.Actions)
                {
                    if (action.IsOncePerPlayerMarker)
                        continue;

                    CheckParam(action, ProblemAnchor.Line, null, node, problems);
                }
            }
        }

        private void CheckParam(
            DlgParam param,
            ProblemAnchor anchor,
            DlgLink? link,
            DlgNode? node,
            List<ConversationProblem> problems)
        {
            var snippet = _snippets.Find(param.Key);
            if (snippet == null)
            {
                problems.Add(Make("unknown-rule", ProblemSeverity.Broken,
                    $"“{param.SnippetKey}” is not something the game knows about any more, so it is skipped.",
                    anchor, link, node));
                return;
            }

            var arguments = param.Arguments;
            if (!snippet.HasEnoughArguments(arguments.Length))
            {
                problems.Add(Make("missing-detail", ProblemSeverity.Broken,
                    $"“{snippet.ToSentence(arguments, param.IsNegated)}” is missing something it needs.",
                    anchor, link, node));
                return;
            }

            if (!snippet.IsValidArgumentCount(arguments.Length))
            {
                if (snippet.RepeatGroupSize > 0)
                {
                    problems.Add(Make("incomplete-repeat-group", ProblemSeverity.Broken,
                        $"“{snippet.ToSentence(arguments, param.IsNegated)}” has an incomplete "
                        + "repeating set of details, so the game cannot use it.",
                        anchor, link, node));
                }
                else
                {
                    problems.Add(Make("extra-detail", ProblemSeverity.Untidy,
                        $"“{snippet.ToSentence(arguments, param.IsNegated)}” was given more than it reads, "
                        + "so the extra is ignored.",
                        anchor, link, node));
                }
            }

            CheckArgumentValues(snippet, arguments, anchor, link, node, problems);
        }

        private void CheckArgumentValues(
            SnippetDescriptor snippet,
            string[] arguments,
            ProblemAnchor anchor,
            DlgLink? link,
            DlgNode? node,
            List<ConversationProblem> problems)
        {
            QuestDefinitionInfo? quest = null;
            for (var i = 0; i < arguments.Length; i++)
            {
                var argument = snippet.ArgumentAt(i);
                if (argument == null)
                    continue;

                var value = arguments[i];
                switch (argument.Type)
                {
                    // Quests and quest steps come from the source scan, so they need it available;
                    // key items, factions and skills are read via reflection and stay populated even
                    // when the scan is not. Purely numeric arguments (quest steps, ranks, amounts)
                    // are parse-checked with no game code at all - the runtime int-parses them and
                    // fails the guard on garbage, so a non-numeric value is Broken either way.
                    case SnippetArgumentType.QuestId:
                        if (_gameCode == null || !_gameCode.IsSourceScanAvailable)
                            break;

                        quest = _gameCode.FindQuest(value);
                        if (quest == null)
                        {
                            problems.Add(Make("unknown-quest", ProblemSeverity.Broken,
                                $"There is no quest called “{value}”, so this can never match.",
                                anchor, link, node));
                        }

                        break;

                    case SnippetArgumentType.QuestState:
                        if (!int.TryParse(value, out var state))
                        {
                            problems.Add(Make("not-a-number", ProblemSeverity.Broken,
                                $"“{value}” is not a number, so this step can never match.",
                                anchor, link, node));
                            break;
                        }

                        if (_gameCode == null || !_gameCode.IsSourceScanAvailable || quest == null)
                            break;

                        if (state < 1 || state > quest.StateCount)
                        {
                            problems.Add(Make("impossible-step", ProblemSeverity.Broken,
                                $"{quest.Name} has {quest.StateCount} step(s), so step {state} "
                                + "will never match.",
                                anchor, link, node));
                        }

                        break;

                    case SnippetArgumentType.SkillRank:
                    case SnippetArgumentType.Amount:
                        if (!int.TryParse(value, out _))
                        {
                            problems.Add(Make("not-a-number", ProblemSeverity.Broken,
                                $"“{value}” is not a number, so this can never match.",
                                anchor, link, node));
                        }

                        break;

                    // Key items are stored as their integer enum value or their KeyItemType member
                    // name - the runtime accepts both (KeyItem.GetKeyItemTypeByName resolves names
                    // case-sensitively), so validation must too.
                    case SnippetArgumentType.KeyItemId:
                        if (_gameCode == null)
                            break;

                        var isKnownKeyItem =
                            (int.TryParse(value, out var keyItemId) && _gameCode.KeyItems.ContainsKey(keyItemId))
                            || (Enum.TryParse<Game.Server.Service.KeyItemService.KeyItemType>(value, out var keyItemByName)
                                && keyItemByName != Game.Server.Service.KeyItemService.KeyItemType.Invalid
                                && _gameCode.KeyItems.ContainsKey((int)keyItemByName));
                        if (!isKnownKeyItem)
                        {
                            problems.Add(Make("unknown-key-item", ProblemSeverity.Broken,
                                $"There is no key item called “{value}”, so this can never match.",
                                anchor, link, node));
                        }

                        break;

                    case SnippetArgumentType.FactionId:
                        if (_gameCode == null)
                            break;

                        if (!(int.TryParse(value, out var factionId) && _gameCode.Factions.ContainsKey(factionId)))
                        {
                            problems.Add(Make("unknown-faction", ProblemSeverity.Broken,
                                $"There is no faction called “{value}”, so this can never match.",
                                anchor, link, node));
                        }

                        break;

                    // Skills are stored either as their integer enum value or by their C# member
                    // name - both are what SkillType.TryParse accepts at runtime.
                    case SnippetArgumentType.SkillId:
                        if (_gameCode == null)
                            break;

                        var isKnownSkill = (int.TryParse(value, out var skillId) && _gameCode.Skills.ContainsKey(skillId))
                            || _gameCode.SkillEnumNames.Values.Any(name =>
                                string.Equals(name, value, StringComparison.OrdinalIgnoreCase));

                        if (!isKnownSkill)
                        {
                            problems.Add(Make("unknown-skill", ProblemSeverity.Broken,
                                $"There is no skill called “{value}”, so this can never match.",
                                anchor, link, node));
                        }

                        break;
                }
            }
        }

        private static void AddOrphans(DlgDocument document, List<ConversationProblem> problems)
        {
            foreach (var orphan in document.FindOrphans())
            {
                var what = orphan.IsEntry ? "line" : "choice";
                problems.Add(new ConversationProblem
                {
                    RuleId = "nothing-leads-here",
                    Severity = ProblemSeverity.Untidy,
                    Message = $"Nothing leads to this {what}, so it is never heard: “{Snippet(orphan.Text)}”",
                    Anchor = orphan.IsEntry ? ProblemAnchor.Line : ProblemAnchor.Choice,
                    Node = orphan
                });
            }
        }

        private static void AddCoverageGaps(SituationModel model, List<ConversationProblem> problems)
        {
            foreach (var quest in model.Coverage())
            {
                foreach (var cell in quest.Cells.Where(cell => !cell.IsCovered))
                {
                    var what = cell.Label switch
                    {
                        "OFFER" => $"no way to start {quest.Name}",
                        "DONE" => $"nothing to say once {quest.Name} is finished",
                        _ => $"nothing for a player on step {cell.Label} of {quest.Name}"
                    };

                    problems.Add(new ConversationProblem
                    {
                        RuleId = "quest-beat-missing",
                        Severity = ProblemSeverity.Untidy,
                        Message = $"This conversation has {what}.",
                        Anchor = ProblemAnchor.Conversation
                    });
                }
            }
        }

        private static void AddStrayLanguages(DlgDocument document, List<ConversationProblem> problems)
        {
            foreach (var node in document.Entries.Concat(document.Replies))
            {
                var text = node.Struct.GetOrNull(DlgNode.TextField);
                if (text?.LocStringEntries == null)
                    continue;

                foreach (var entry in text.LocStringEntries)
                {
                    if (entry.LanguageKey == "0")
                        continue;

                    problems.Add(new ConversationProblem
                    {
                        RuleId = "stray-translation",
                        Severity = ProblemSeverity.Untidy,
                        Message = "This line carries text in another language that nothing reads.",
                        Anchor = node.IsEntry ? ProblemAnchor.Line : ProblemAnchor.Choice,
                        Node = node
                    });
                    break;
                }
            }
        }

        private static void AddHouseStyle(
            DlgDocument document,
            IReadOnlyList<Situation> situations,
            List<ConversationProblem> problems)
        {
            foreach (var node in document.Entries.Concat(document.Replies))
            {
                var text = node.Text;
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                foreach (var phrase in BannedScaffolding)
                {
                    if (text.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                    {
                        problems.Add(new ConversationProblem
                        {
                            RuleId = "house-style",
                            Severity = ProblemSeverity.Hint,
                            Message = $"“{phrase}” is on the banned list in the content standards.",
                            Anchor = node.IsEntry ? ProblemAnchor.Line : ProblemAnchor.Choice,
                            Node = node
                        });
                    }
                }
            }

            var greetings = situations
                .Where(situation => situation.State != SituationState.Unreachable)
                .Count(situation => StartsWithGreeting(situation.Opening.Target.Text));

            if (greetings >= 3)
            {
                problems.Add(new ConversationProblem
                {
                    RuleId = "house-style",
                    Severity = ProblemSeverity.Hint,
                    Message = $"{greetings} of this NPC's openings start with a greeting. "
                              + "The standards ask for more variety than that.",
                    Anchor = ProblemAnchor.Conversation
                });
            }
        }

        private static bool StartsWithGreeting(string text)
        {
            var trimmed = text.TrimStart();
            return GreetingOpeners.Any(opener => trimmed.StartsWith(opener, StringComparison.OrdinalIgnoreCase));
        }

        private static ConversationProblem Make(
            string ruleId,
            ProblemSeverity severity,
            string message,
            ProblemAnchor anchor,
            DlgLink? link,
            DlgNode? node) =>
            new()
            {
                RuleId = ruleId,
                Severity = severity,
                Message = message,
                Anchor = anchor,
                Link = link,
                Node = node
            };

        private static string Snippet(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "(nothing written)";

            var trimmed = text.Trim();
            return trimmed.Length <= 45 ? trimmed : trimmed[..42].TrimEnd() + "…";
        }
    }
}
