using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.GameCode;

namespace SWLOR.Toolset.Domain.Conversations
{
    /// <summary>One situation the scaffold will lay out, described the way the wizard lists it.</summary>
    public sealed record ScaffoldBeat(string Title, string Explanation);

    /// <summary>
    /// Lays out the situations a quest-giver needs, wired correctly and in the right order, with a
    /// blank line in each for the writer to fill.
    /// </summary>
    /// <remarks>
    /// A new conversation never starts empty, because typing a quest giver into a walk-shaped editor
    /// one line at a time would be miserable and the structure is the part that is easy to get
    /// wrong. Everything here is derived from the quest definition — step count, prerequisites,
    /// repeatability — so the writer finds a correct shape rather than assembling one.
    /// <para>
    /// The order matters more than it looks. Openings are checked top-down and the first that fits
    /// wins, so the most specific situation goes first and the catch-all greeting last. Getting that
    /// backwards is the mistake this whole design exists to prevent, so the scaffold never produces
    /// it.
    /// </para>
    /// </remarks>
    public sealed class QuestConversationScaffold
    {
        /// <summary>
        /// The marker left in every line the writer still has to write. Plain angle brackets rather
        /// than typographic ones, and deliberately: dialogue is stored in NWN's single-byte string
        /// encoding, which cannot represent them — and matching the "&lt;Enter dialogue here&gt;"
        /// convention <see cref="ModuleResourceTemplateFactory"/> already uses keeps one look for
        /// "not written yet" across the toolset.
        /// </summary>
        public const string Placeholder = "<write this>";

        private readonly IGameCodeIndex _gameCode;

        public QuestConversationScaffold(IGameCodeIndex gameCode)
        {
            _gameCode = gameCode;
        }

        /// <summary>
        /// What <see cref="Apply"/> would lay out, for the wizard to show before committing.
        /// Returns an empty list when the quest is not one the game code declares.
        /// </summary>
        /// <param name="questId">The quest to build a conversation around.</param>
        /// <param name="document">
        /// The conversation being extended, when there is one. Passing it keeps the preview honest:
        /// a conversation that already greets everybody does not get a second greeting.
        /// </param>
        public IReadOnlyList<ScaffoldBeat> Preview(string questId, DlgDocument? document = null)
        {
            var quest = _gameCode.FindQuest(questId);
            if (quest == null)
                return Array.Empty<ScaffoldBeat>();

            var beats = new List<ScaffoldBeat>();

            if (!quest.IsRepeatable)
            {
                beats.Add(new ScaffoldBeat(
                    $"Finished {quest.Name}",
                    "Every time they talk afterwards. Not repeatable, so this is permanent."));
            }

            for (var state = quest.StateCount; state >= 1; state--)
            {
                if (quest.CollectItemObjectiveStates.Contains(state))
                {
                    beats.Add(new ScaffoldBeat(
                        $"Ready to hand in {quest.Name} (step {state})",
                        $"Step {state} — opens the item collector, then ends while it processes the hand-in."));
                }
                else if (state == quest.StateCount)
                {
                    beats.Add(new ScaffoldBeat(
                        $"Ready to complete {quest.Name}",
                        $"Step {state} — pays out after the final non-item objectives are complete."));
                }
                else
                {
                    beats.Add(new ScaffoldBeat(
                        $"On step {state} of {quest.Name}",
                        Journal(quest, state) ?? $"Step {state} — remind them what is still outstanding."));
                }
            }

            beats.Add(new ScaffoldBeat(
                $"Offering {quest.Name}",
                "The offer, with an optional \"what's involved?\" branch, and yes / no."));

            if (!HasCatchAll(document))
                beats.Add(new ScaffoldBeat("First meeting", CatchAllExplanation(quest)));

            return beats;
        }

        /// <summary>
        /// What the trailing situation is for. When the quest is gated, this is also where a player
        /// who has not met the gate ends up — and it is deliberately NOT given a guard of its own.
        /// </summary>
        /// <remarks>
        /// An earlier draft emitted a separate "not ready yet" opening guarded on the prerequisite
        /// being unmet. That guard is the exact complement of the offer's, so between them the two
        /// answered everybody and whichever came last could never fire. One unguarded line at the
        /// bottom does the same job with nothing dead behind it.
        /// </remarks>
        private string CatchAllExplanation(QuestDefinitionInfo quest)
        {
            if (quest.PrerequisiteQuestIds.Count == 0)
                return "Anyone else. Stays last so it cannot swallow the others.";

            var names = quest.PrerequisiteQuestIds.Select(id => _gameCode.FindQuest(id)?.Name ?? id);
            return $"Anyone else — including a player who has not finished {string.Join(" or ", names)} "
                   + "and so cannot be offered this yet.";
        }

        /// <summary>
        /// Whether the conversation already answers everybody somewhere. A second unguarded opening
        /// could never fire, and shipping one is a mistake four conversations in the module already
        /// make — so the scaffold declines to add a fifth.
        /// </summary>
        private static bool HasCatchAll(DlgDocument? document) =>
            document != null && document.Openings.Any(opening => opening.Conditions.Count == 0);

        /// <summary>
        /// Lays the situations out in <paramref name="document"/>, above anything already there.
        /// </summary>
        /// <remarks>
        /// Position is not cosmetic. New openings are appended by the document model — which is what
        /// keeps the diff small — and would therefore sit BELOW an existing catch-all greeting,
        /// where none of them could ever fire. So the last thing this does is lift them to the top,
        /// in the order it built them. Anything already in the conversation keeps working; if the
        /// new openings now subsume an old one, the analyzer reports it rather than this method
        /// quietly deleting it.
        /// </remarks>
        /// <returns>The openings created, most specific first.</returns>
        public IReadOnlyList<DlgLink> Apply(DlgDocument document, string questId)
        {
            var existingOpenings = document.Openings.Count;
            var needsCatchAll = !HasCatchAll(document);
            var quest = _gameCode.FindQuest(questId)
                        ?? throw new ArgumentException($"No quest called '{questId}'.", nameof(questId));

            var created = new List<DlgLink>();

            // Finished. A repeatable quest deliberately gets no finished opening: a completed
            // player still satisfies condition-can-accept-quest, and openings are first-match-wins,
            // so this placeholder would shadow the offer below and lock them out of restarting.
            if (!quest.IsRepeatable)
            {
                created.Add(Opening(document, Placeholder, opening =>
                    opening.AddCondition("condition-completed-quest", questId)));
            }

            // Quest states, latest first. Ordinary progress steps share one reminder line by
            // default. A writer who needs step-specific wording can split one incoming route with
            // "Make a separate copy"; until then there is only one reminder to maintain.
            DlgNode? sharedProgress = null;

            // Collection states open the collector and deliberately end:
            // it advances the quest after the last item and starts a fresh conversation with the NPC.
            for (var state = quest.StateCount; state >= 1; state--)
            {
                DlgLink onState;
                if (!quest.CollectItemObjectiveStates.Contains(state) && state != quest.StateCount)
                {
                    if (sharedProgress == null)
                    {
                        sharedProgress = document.AddEntry(Placeholder);
                        var leave = document.AddReply("Goodbye.");
                        document.AddLink(sharedProgress, leave);
                    }

                    onState = document.AddOpening(sharedProgress);
                    onState.AddCondition("condition-on-quest-state", $"{questId} {state}");
                }
                else
                {
                    onState = Opening(document, Placeholder, opening =>
                        opening.AddCondition("condition-on-quest-state", $"{questId} {state}"));
                }

                if (quest.CollectItemObjectiveStates.Contains(state))
                {
                    var handOver = document.AddReply(Placeholder);
                    handOver.AddAction("action-request-quest-items", questId);
                    document.AddLink(onState.Target, handOver);
                }
                else if (state == quest.StateCount)
                {
                    var complete = document.AddReply(Placeholder);
                    document.AddLink(onState.Target, complete);

                    var reward = document.AddEntry(Placeholder);
                    reward.AddAction("action-advance-quest", questId);
                    document.AddLink(complete, reward);
                }
                created.Add(onState);
            }

            // The offer.
            var offer = Opening(document, Placeholder, opening =>
                opening.AddCondition("condition-can-accept-quest", questId));
            BuildOffer(document, offer.Target, questId);
            created.Add(offer);

            // No separate "not ready yet" opening - see CatchAllExplanation for why the unguarded
            // line at the bottom covers that case without leaving a dead opening behind.

            // The catch-all, last, because it answers everybody - unless the conversation has one.
            if (needsCatchAll)
                created.Add(Opening(document, Placeholder, _ => { }));

            // Lift the new openings above whatever was already there, keeping their relative order.
            for (var i = 0; i < created.Count; i++)
                document.MoveOpening(existingOpenings + i, i);

            return created;
        }

        /// <summary>
        /// The offer branch: a question that reveals what the job involves, then accept and decline.
        /// The accept is the only reply carrying an action — the content standards ask for lore and
        /// questions to sit on branches a player can explore without committing to anything.
        /// </summary>
        private static void BuildOffer(DlgDocument document, DlgNode offer, string questId)
        {
            var ask = document.AddReply(Placeholder);
            document.AddLink(offer, ask);

            var explain = document.AddEntry(Placeholder);
            document.AddLink(ask, explain);

            var accept = document.AddReply(Placeholder);
            accept.AddAction("action-accept-quest", questId);
            document.AddLink(explain, accept);

            var confirm = document.AddEntry(Placeholder);
            document.AddLink(accept, confirm);

            var decline = document.AddReply(Placeholder);
            document.AddLink(explain, decline);
            document.AddLink(offer, decline);

            var declined = document.AddEntry(Placeholder);
            document.AddLink(decline, declined);
        }

        private static DlgLink Opening(DlgDocument document, string text, Action<DlgLink> guard)
        {
            var entry = document.AddEntry(text);
            var opening = document.AddOpening(entry);
            guard(opening);
            return opening;
        }

        private static string? Journal(QuestDefinitionInfo quest, int state) =>
            quest.JournalTextByState.TryGetValue(state, out var text) ? text : null;
    }
}
