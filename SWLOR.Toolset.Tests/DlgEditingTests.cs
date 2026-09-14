using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Covers the editing half of DlgDocument: that inserts are local, that removals renumber
    /// everything they have to, and that the snippet dispatcher resrefs are maintained rather than
    /// typed. The last of those is the point — params with no dispatcher beside them are a silent
    /// no-op at runtime, and the whole reason the toolset writes those fields itself.
    /// </summary>
    public class DlgEditingTests
    {
        private static string DantHerbsPath =>
            Path.Combine(CorpusLocator.ModuleDirectory, "dlg", "dantherbs.dlg.json");

        private static DlgDocument LoadDantHerbs() =>
            DlgDocument.Parse(File.ReadAllBytes(DantHerbsPath));

        // ---------- inserts are appends ----------

        [Test]
        public void AddingALine_AppendsIt_AndMovesNothingElse()
        {
            var document = LoadDantHerbs();
            var indicesBefore = document.AllLinks().Select(link => link.TargetIndex).ToList();
            var entriesBefore = document.Entries.Count;

            var added = document.AddEntry("A new line.");

            added.Index.Should().Be(entriesBefore, "a new line goes on the end so nothing renumbers");
            document.Entries.Should().HaveCount(entriesBefore + 1);
            document.AllLinks().Select(link => link.TargetIndex).Should().Equal(indicesBefore);
        }

        [Test]
        public void AddingALine_NumbersTheNewElementByItsPosition()
        {
            var document = LoadDantHerbs();
            var added = document.AddReply("A new choice.");

            added.Struct.StructId.Should().Be((uint)added.Index);
        }

        [Test]
        public void AddingALine_WritesTheSameFieldSetAuthoredContentUses()
        {
            var document = LoadDantHerbs();
            var existing = document.Entries[0];
            var added = document.AddEntry("A new line.");

            added.Struct.Entries.Select(field => field.Key).Should()
                .Equal(existing.Struct.Entries.Select(field => field.Key));
        }

        [Test]
        public void AddingALine_ThenSerializing_ReadsBackWithTheSameText()
        {
            var document = LoadDantHerbs();
            document.AddEntry("A new line.");

            var reparsed = DlgDocument.Parse(document.ToBytes());
            reparsed.Entries[^1].Text.Should().Be("A new line.");
            reparsed.Entries[^1].Delay.Should().Be(DlgDocument.NoDelay);
            reparsed.Entries[^1].AnimLoop.Should().BeTrue();
        }

        [Test]
        public void AddingALineAndItsRoute_LeavesEveryExistingLinkAlone()
        {
            var document = LoadDantHerbs();
            // Keyed by the link's own struct rather than by position: AddLink appends to the
            // parent's list, which sits in the middle of the document's flattened link order, so a
            // positional snapshot would report the insertion as though everything after it moved.
            var targetsBefore = document.AllLinks()
                .ToDictionary(link => link.Struct, link => link.TargetIndex);

            var reply = document.AddReply("Tell me more.");
            document.AddLink(document.Entries[0], reply);

            var after = document.AllLinks().ToList();
            after.Should().HaveCount(targetsBefore.Count + 1);
            foreach (var link in after.Where(link => targetsBefore.ContainsKey(link.Struct)))
                link.TargetIndex.Should().Be(targetsBefore[link.Struct]);
        }

        [Test]
        public void MovingAChoiceReordersOnlyItsParentsLinks()
        {
            var document = LoadDantHerbs();
            var parent = document.Entries.First(entry => entry.Links.Count >= 2);
            var before = parent.Links.Select(link => link.Target.Text).ToList();
            var moved = parent.Links[0];

            document.MoveLink(moved, parent.Links.Count - 1);

            parent.Links.Select(link => link.Target.Text).Should()
                .Equal(before.Skip(1).Append(before[0]));
            parent.Links.Select(link => link.Struct.StructId).Should()
                .Equal(Enumerable.Range(0, before.Count).Select(index => (uint?)index));
        }

        [Test]
        public void ALineCanOnlyLeadToTheOtherKindOfLine()
        {
            var document = LoadDantHerbs();
            var entry = document.Entries[0];
            var otherEntry = document.Entries[1];

            var act = () => document.AddLink(entry, otherEntry);

            act.Should().Throw<ArgumentException>()
                .WithMessage("A Entry leads to a Reply, not to a Entry.*");
        }

        [Test]
        public void AddingAnOpening_AppendsIt_WhereItWillNotFireBehindTheFallback()
        {
            var document = LoadDantHerbs();
            var openingsBefore = document.Openings.Count;

            var entry = document.AddEntry("A new situation.");
            var opening = document.AddOpening(entry);

            document.Openings.Should().HaveCount(openingsBefore + 1);
            document.Openings[^1].Struct.Should().BeSameAs(opening.Struct);
            opening.Struct.Contains("IsChild").Should()
                .BeFalse("no StartingList element in the corpus carries an IsChild field");
        }

        // ---------- snippets own their dispatcher resrefs ----------

        [Test]
        public void AddingTheFirstAction_WiresTheDispatcherScript()
        {
            var document = LoadDantHerbs();
            var reply = document.AddReply("I'll do it.");

            reply.Script.Should().BeEmpty();
            reply.AddAction("action-accept-quest", "field_tinctures");

            reply.Script.Should().Be(DlgDocument.ActionDispatcher);
            reply.Actions.Should().ContainSingle()
                .Which.Value.Should().Be("field_tinctures");
        }

        [Test]
        public void AddingASnippetActionCannotReplaceACustomScript()
        {
            var document = LoadDantHerbs();
            var reply = document.AddReply("Custom.");
            reply.Script = "my_custom_script";

            var act = () => reply.AddAction("action-accept-quest", "field_tinctures");

            act.Should().Throw<InvalidOperationException>();
            reply.Script.Should().Be("my_custom_script");
            reply.Actions.Should().BeEmpty();
        }

        [Test]
        public void DuplicatingAHybridNodePreservesItsCustomScriptAndSnippetParameters()
        {
            var document = LoadDantHerbs();
            var reply = document.AddReply("Hybrid.");
            reply.AddAction("action-accept-quest", "field_tinctures");
            reply.Script = "my_custom_script";

            var copy = document.DuplicateNode(reply);

            copy.Script.Should().Be("my_custom_script");
            copy.Actions.Should().ContainSingle()
                .Which.Value.Should().Be("field_tinctures");
        }

        [Test]
        public void DuplicatingANodeDropsObsoleteOncePerPlayerMetadata()
        {
            var document = LoadDantHerbs();
            var reply = document.AddReply("A legacy reward.");
            reply.AddAction("action-give-faction-points", "1 50");
            reply.AddAction("once-action-give-faction-points", "dant_herbs:stable-marker");

            var copy = document.DuplicateNode(reply);

            copy.Actions.Should().ContainSingle();
            copy.Actions.Should().NotContain(candidate => candidate.IsOncePerPlayerMarker);
            copy.Actions.Single().Value.Should().Be("1 50");
        }

        [Test]
        public void DuplicatingANodePreservesEveryLocalizedStringIndependently()
        {
            var document = LoadDantHerbs();
            var reply = document.AddReply("Default language.");
            reply.TextLocString.SetText("2", "Alternate language.");

            var copy = document.DuplicateNode(reply);

            copy.TextLocString.GetText("0").Should().Be("Default language.");
            copy.TextLocString.GetText("2").Should().Be("Alternate language.");

            copy.TextLocString.SetText("2", "Changed copy.");
            reply.TextLocString.GetText("2").Should().Be("Alternate language.",
                "the copied localized entries must not share mutable storage with the original");
        }

        [Test]
        public void RemovingTheLastAction_ClearsTheDispatcherScript()
        {
            var document = LoadDantHerbs();
            var reply = document.AddReply("I'll do it.");
            var action = reply.AddAction("action-accept-quest", "field_tinctures");

            reply.RemoveAction(action);

            reply.Actions.Should().BeEmpty();
            reply.Script.Should().BeEmpty("params left without a dispatcher never run");
        }

        [Test]
        public void AddingTheFirstCondition_WiresTheDispatcherScript()
        {
            var document = LoadDantHerbs();
            var entry = document.AddEntry("Only sometimes.");
            var opening = document.AddOpening(entry);

            opening.AddCondition("condition-has-quest", "field_tinctures");

            opening.Active.Should().Be(DlgDocument.ConditionDispatcher);
            opening.Conditions.Should().ContainSingle();
        }

        [Test]
        public void RemovingTheLastCondition_ClearsTheDispatcherScript()
        {
            var document = LoadDantHerbs();
            var entry = document.AddEntry("Only sometimes.");
            var opening = document.AddOpening(entry);
            var condition = opening.AddCondition("condition-has-quest", "field_tinctures");

            opening.RemoveCondition(condition);

            opening.Conditions.Should().BeEmpty();
            opening.Active.Should().BeEmpty();
        }

        [Test]
        public void RemovingOneOfTwoConditions_KeepsTheDispatcherAndRenumbersTheRest()
        {
            var document = LoadDantHerbs();
            var entry = document.AddEntry("Only sometimes.");
            var opening = document.AddOpening(entry);
            var first = opening.AddCondition("condition-has-quest", "field_tinctures");
            opening.AddCondition("!condition-completed-quest", "harvest_herbs");

            opening.RemoveCondition(first);

            opening.Conditions.Should().ContainSingle()
                .Which.Key.Should().Be("!condition-completed-quest");
            opening.Active.Should().Be(DlgDocument.ConditionDispatcher);
            opening.Conditions[0].Struct.StructId.Should().Be(0u);
        }

        [Test]
        public void RemovingTheLastConditionClearsTheOlderDispatcherSpellingToo()
        {
            // 725 links in the module use "appears" where new content writes "condition". Both name
            // the same handler, so the editor has to recognise the one it did not write — otherwise
            // emptying an existing link leaves a dispatcher behind on a link with nothing to run.
            var document = LoadDantHerbs();
            var opening = document.Openings[0];
            opening.Active = "appears";

            foreach (var condition in opening.Conditions)
                opening.RemoveCondition(condition);

            opening.Conditions.Should().BeEmpty();
            opening.Active.Should().BeEmpty();
        }

        [Test]
        public void ClearingASnippetLeavesTheGeneratedShellScriptsAlone()
        {
            // dialog_appears_* belongs to the C# Dialog service's 255 generated shells, not to the
            // snippet system. Stripping it would break a conversation the toolset does not own.
            var document = LoadDantHerbs();
            var opening = document.Openings[0];
            opening.Active = "dialog_appears_h";

            foreach (var condition in opening.Conditions)
                opening.RemoveCondition(condition);

            opening.Active.Should().Be("dialog_appears_h");
        }

        [Test]
        public void ANegatedConditionReportsItsUnderlyingSnippet()
        {
            var document = LoadDantHerbs();
            var entry = document.AddEntry("Only sometimes.");
            var opening = document.AddOpening(entry);

            var condition = opening.AddCondition("!condition-has-quest", "field_tinctures");

            condition.IsNegated.Should().BeTrue();
            condition.SnippetKey.Should().Be("condition-has-quest");
        }

        // ---------- removal renumbers everything it has to ----------

        [Test]
        public void RemovingALineFromTheMiddle_RepointsEveryLinkThatPointedPastIt()
        {
            var document = LoadDantHerbs();
            var textByRemainingReply = document.Replies
                .Select(reply => reply.Text)
                .ToList();

            var removed = document.Replies[3];
            textByRemainingReply.RemoveAt(3);
            document.RemoveNode(removed);

            document.Replies.Select(reply => reply.Text).Should().Equal(textByRemainingReply);

            // The real test: every surviving link still resolves to the line it named before.
            document.FindDanglingLinks().Should().BeEmpty();
        }

        [Test]
        public void RemovingALine_KeepsEverySurvivingLinkPointingAtTheSameText()
        {
            var document = LoadDantHerbs();
            var before = DescribeRoutes(document);
            var removed = document.Replies[3];
            var removedText = removed.Text;

            document.RemoveNode(removed);

            // Both the routes TO the removed line and the ones FROM it disappear; every other route
            // still names the same pair of lines it named before, which is the whole point of
            // renumbering.
            var expected = before
                .Where(route => !route.Contains($"-> Reply:{removedText}"))
                .Where(route => !route.StartsWith($"Reply:{removedText} ->"))
                .ToList();

            var after = DescribeRoutes(document);
            after.Should().Equal(expected);
        }

        [Test]
        public void RemovingALine_TakesTheRoutesToItAndFromItWithIt()
        {
            var document = LoadDantHerbs();
            var removed = document.Replies[3];
            var incoming = document.IncomingLinks(removed).Count;
            var outgoing = removed.Links.Count;
            var linksBefore = document.AllLinks().Count();

            incoming.Should().BeGreaterThan(0);
            outgoing.Should().BeGreaterThan(0);

            document.RemoveNode(removed);

            document.AllLinks().Should().HaveCount(linksBefore - incoming - outgoing,
                "the routes pointing at the line are removed explicitly, and its own onward links "
                + "leave with the node struct that held them");
        }

        [Test]
        public void RemovingALine_RenumbersTheElementsAfterIt()
        {
            var document = LoadDantHerbs();
            document.RemoveNode(document.Replies[3]);

            for (var i = 0; i < document.Replies.Count; i++)
                document.Replies[i].Struct.StructId.Should().Be((uint)i);
        }

        [Test]
        public void RemovingTheLineJustAdded_CostsNothingElse()
        {
            var document = LoadDantHerbs();
            var added = document.AddEntry("A new line.");

            var cost = document.EstimateRemoveNode(added);

            cost.IsLocal.Should().BeTrue();
            cost.RoutesRemoved.Should().Be(0);
        }

        [Test]
        public void RemovingALineFromTheMiddle_ReportsWhatItWillDisturb()
        {
            var document = LoadDantHerbs();
            var target = document.Replies[3];

            var cost = document.EstimateRemoveNode(target);

            cost.IsLocal.Should().BeFalse();
            cost.NodesRenumbered.Should().Be(document.Replies.Count - 4);
            cost.RoutesRemoved.Should().Be(document.IncomingLinks(target).Count);
            cost.LinksRewritten.Should().BeGreaterThan(0);
        }

        [Test]
        public void UnlinkingALine_RenumbersNothing_ButCanStrandIt()
        {
            var document = LoadDantHerbs();
            var entry = document.Entries.Single(node => node.Text.StartsWith("Hold a moment."));
            var link = entry.Links[0];
            var target = link.Target;
            var replyCountBefore = document.Replies.Count;

            document.RemoveLink(link);

            document.Replies.Should().HaveCount(replyCountBefore, "unlinking keeps the line itself");
            document.IncomingLinks(target).Should().BeEmpty();
            document.FindOrphans().Should().Contain(orphan => orphan.Struct == target.Struct);
        }

        // ---------- orphans ----------

        [Test]
        public void ALineNothingLeadsTo_IsReportedAsAnOrphan()
        {
            var document = LoadDantHerbs();
            document.FindOrphans().Should().BeEmpty();

            var stranded = document.AddEntry("Nothing leads here.");

            document.FindOrphans().Should().ContainSingle()
                .Which.Struct.Should().BeSameAs(stranded.Struct);
        }

        [Test]
        public void ALineReachedOnlyThroughAnotherOrphan_IsAlsoAnOrphan()
        {
            var document = LoadDantHerbs();
            var stranded = document.AddEntry("Nothing leads here.");
            var behindIt = document.AddReply("Nor here.");
            document.AddLink(stranded, behindIt);

            document.FindOrphans().Should().HaveCount(2);
        }

        // ---------- word count ----------

        [Test]
        public void ChangingText_ThenRecounting_UpdatesNumWords()
        {
            var document = LoadDantHerbs();
            var before = document.NumWords;

            document.AddEntry("One two three four five.");
            document.RecomputeWordCount();

            document.NumWords.Should().Be(before + 5);
        }

        [Test]
        public void RecountingWithoutChangingText_LeavesNumWordsAlone()
        {
            var original = File.ReadAllBytes(DantHerbsPath);
            var document = DlgDocument.Parse(original);

            document.RecomputeWordCount().Should().BeFalse();
            document.ToBytes().Should().Equal(original, "an unchanged conversation must not gain a diff");
        }

        // ---------- undo ----------

        [Test]
        public void UndoingAnInsert_RestoresTheFileByteForByte()
        {
            var original = File.ReadAllBytes(DantHerbsPath);
            var document = DlgDocument.Parse(original);
            var stack = new UndoStack();

            using (var transaction = stack.Begin("Add a line"))
            {
                var reply = document.AddReply("Something new.");
                document.AddLink(document.Entries[0], reply);
                transaction.Commit();
            }

            document.ToBytes().Should().NotEqual(original);
            stack.Undo();
            document.ToBytes().Should().Equal(original);
        }

        [Test]
        public void UndoingARemoval_RestoresTheFileByteForByte()
        {
            var original = File.ReadAllBytes(DantHerbsPath);
            var document = DlgDocument.Parse(original);
            var stack = new UndoStack();

            using (var transaction = stack.Begin("Remove a line"))
            {
                document.RemoveNode(document.Replies[3]);
                transaction.Commit();
            }

            document.ToBytes().Should().NotEqual(original);
            stack.Undo();
            document.ToBytes().Should().Equal(original,
                "every index and element id the removal rewrote has to come back");
        }

        [Test]
        public void UndoingASnippetEdit_RestoresTheFileByteForByte()
        {
            var original = File.ReadAllBytes(DantHerbsPath);
            var document = DlgDocument.Parse(original);
            var stack = new UndoStack();

            using (var transaction = stack.Begin("Add an action"))
            {
                document.Replies[0].AddAction("action-open-store");
                transaction.Commit();
            }

            stack.Undo();
            document.ToBytes().Should().Equal(original);
        }

        // ---------- diff locality ----------

        [Test]
        public void ChangingOneLine_RewritesOnlyThatLinesText()
        {
            var original = File.ReadAllBytes(DantHerbsPath);
            var document = DlgDocument.Parse(original);

            document.Entries[0].Text = "Changed.";

            var originalLines = Encoding.UTF8.GetString(original).Split('\n');
            var writtenLines = Encoding.UTF8.GetString(document.ToBytes()).Split('\n');

            writtenLines.Should().HaveCount(originalLines.Length);
            var changed = Enumerable.Range(0, originalLines.Length)
                .Where(i => originalLines[i] != writtenLines[i])
                .ToList();

            changed.Should().ContainSingle();
            writtenLines[changed[0]].Should().Contain("Changed.");
        }

        /// <summary>
        /// Every route in the conversation described by the TEXT either end names rather than by
        /// index, so it survives renumbering — which is exactly what a removal has to preserve.
        /// </summary>
        private static List<string> DescribeRoutes(DlgDocument document)
        {
            var routes = new List<string>();
            foreach (var opening in document.Openings)
                routes.Add($"start -> Entry:{opening.Target.Text}");

            foreach (var node in document.Entries.Concat(document.Replies))
            {
                foreach (var link in node.Links)
                    routes.Add($"{node.Kind}:{node.Text} -> {link.TargetKind}:{link.Target.Text}");
            }

            return routes;
        }
    }
}
