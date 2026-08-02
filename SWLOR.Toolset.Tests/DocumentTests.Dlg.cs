using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Verifies DlgDocument reads real conversations correctly, against
    /// Module/dlg/dantherbs.dlg.json — a quest giver with two quests, a guarded opening chain and a
    /// link-back, which is the shape most authored conversations in the module take.
    /// </summary>
    public class DlgDocumentReadTests
    {
        private static string DantHerbsPath =>
            Path.Combine(CorpusLocator.ModuleDirectory, "dlg", "dantherbs.dlg.json");

        private static string BartenderPath =>
            Path.Combine(CorpusLocator.ModuleDirectory, "dlg", "bartender.dlg.json");

        [Test]
        public void DantHerbs_NodeCounts_MatchCorpus()
        {
            var document = DlgDocument.Load(DantHerbsPath);

            document.Entries.Should().HaveCount(13);
            document.Replies.Should().HaveCount(13);
            document.Openings.Should().HaveCount(8);
        }

        [Test]
        public void DantHerbs_ConversationProperties_MatchCorpus()
        {
            var document = DlgDocument.Load(DantHerbsPath);

            document.EndConversation.Should().Be("nw_walk_wp");
            document.EndConverAbort.Should().Be("nw_walk_wp");
            document.DelayEntry.Should().Be(0u);
            document.PreventZoomIn.Should().BeFalse();
        }

        [Test]
        public void DantHerbs_LastOpening_IsTheUnguardedGreeting()
        {
            var document = DlgDocument.Load(DantHerbsPath);
            var last = document.Openings[^1];

            last.Conditions.Should().BeEmpty("the fallback greeting has to answer everybody");
            last.Active.Should().BeEmpty();
            last.Target.Text.Should().StartWith("Greetings, traveler.");
        }

        [Test]
        public void DantHerbs_FirstOpening_IsGuardedByAQuestCondition()
        {
            var document = DlgDocument.Load(DantHerbsPath);
            var first = document.Openings[0];

            first.Active.Should().Be("condition");
            first.Conditions.Should().HaveCount(1);
            first.Conditions[0].Key.Should().Be("condition-completed-quest");
            first.Conditions[0].Value.Should().Be("field_tinctures");
            first.Conditions[0].IsNegated.Should().BeFalse();
            first.Conditions[0].Arguments.Should().Equal("field_tinctures");
        }

        [Test]
        public void DantHerbs_OfferOpening_CarriesThreeConditions_TwoOfThemNegated()
        {
            var document = DlgDocument.Load(DantHerbsPath);

            // The offer only appears once the first quest is done and the second is neither taken
            // nor finished. Snippet.ProcessConditions ANDs them, so all three must pass.
            var offer = document.Openings.Single(
                opening => opening.Target.Text.StartsWith("Hold a moment."));

            offer.Conditions.Select(condition => condition.Key).Should().Equal(
                "condition-completed-quest",
                "!condition-completed-quest",
                "!condition-has-quest");
            offer.Conditions[0].SnippetKey.Should().Be("condition-completed-quest");
            offer.Conditions[1].IsNegated.Should().BeTrue();
            offer.Conditions[1].SnippetKey.Should().Be("condition-completed-quest");
        }

        [Test]
        public void DantHerbs_AcceptReply_CarriesTheAcceptAction()
        {
            var document = DlgDocument.Load(DantHerbsPath);

            var accept = document.Replies.Single(
                reply => reply.Text == "I'll bring the innards and blood sample.");

            accept.Script.Should().Be("action");
            accept.Actions.Should().HaveCount(1);
            accept.Actions[0].Key.Should().Be("action-accept-quest");
            accept.Actions[0].Value.Should().Be("field_tinctures");
        }

        [Test]
        public void DantHerbs_TheDeclineReply_IsReachedFromTwoPlaces_NeitherMarkedAsALinkBack()
        {
            var document = DlgDocument.Load(DantHerbsPath);

            var decline = document.Replies.Single(
                reply => reply.Text == "Not today. The fields have had enough of me.");

            // Editing this line changes both routes, which is why sharing has to be surfaced at
            // all. IsChild does not help here: this file reaches the shared reply through two
            // ordinary links, so an editor that keys "is this a re-use?" off the flag would expand
            // the same subtree twice and offer to edit it as if it were two separate lines.
            // Reachability is the reliable test; the flag is only a rendering hint.
            var incoming = document.IncomingLinks(decline);
            incoming.Should().HaveCount(2);
            incoming.Should().OnlyContain(link => !link.IsChild);
        }

        [Test]
        public void DantHerbs_EntriesLeadToReplies_AndRepliesToEntries()
        {
            var document = DlgDocument.Load(DantHerbsPath);

            foreach (var entry in document.Entries)
            {
                foreach (var link in entry.Links)
                    link.TargetKind.Should().Be(DlgNodeKind.Reply);
            }

            foreach (var reply in document.Replies)
            {
                foreach (var link in reply.Links)
                    link.TargetKind.Should().Be(DlgNodeKind.Entry);
            }
        }

        [Test]
        public void DantHerbs_HasNoOrphansAndNoDanglingLinks()
        {
            var document = DlgDocument.Load(DantHerbsPath);

            document.FindOrphans().Should().BeEmpty();
            document.FindDanglingLinks().Should().BeEmpty();
        }

        [Test]
        public void Bartender_StoreReply_CarriesTheOpenStoreAction()
        {
            var document = DlgDocument.Load(BartenderPath);

            var shop = document.Replies.Single(reply => reply.Text.Contains("Show me what drinks"));
            shop.Actions.Should().ContainSingle()
                .Which.Key.Should().Be("action-open-store");
        }

        [Test]
        public void Bartender_WordCount_MatchesTheStoredNumWords()
        {
            // Confirms the counting rule Aurora uses: whitespace-separated tokens across every
            // entry and reply, punctuation and quote marks included.
            var document = DlgDocument.Load(BartenderPath);

            document.NumWords.Should().Be(26u);
            document.CountWords().Should().Be(26);
        }

        [Test]
        public void EveryNodeDefaultsToTheConversationDelay()
        {
            var document = DlgDocument.Load(DantHerbsPath);

            foreach (var node in document.Entries.Concat(document.Replies))
                node.Delay.Should().Be(DlgDocument.NoDelay);
        }
    }
}
