using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Validation;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Which conversations Preview can simulate exactly. The editor opens every one; this check
    /// controls whether it needs to explain that legacy NWScript visibility cannot be predicted.
    /// </summary>
    public class ConversationCompatibilityTests
    {
        private static IEnumerable<string> AuthoredConversations() =>
            Directory.EnumerateFiles(Path.Combine(CorpusLocator.ModuleDirectory, "dlg"), "*.json")
                .Where(path => !UnreferencedConversationRule.IsGeneratedShell(ResRefOf(path)))
                .OrderBy(path => path, StringComparer.Ordinal);

        private static string ResRefOf(string path) =>
            Path.GetFileName(path).Replace(".dlg.json", string.Empty, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Preview cannot simulate 9 of 346 hand-authored conversations, about 3%. Every one decides
        /// what to SHOW with its own NWScript rather than with snippets: the DMFI DM menus and a
        /// handful of imported dialogs. The editor could not predict a single branch of them, so
        /// the shell opens the editable legacy surface with a preview-fidelity notice.
        /// </summary>
        /// <remarks>
        /// An earlier version of the rule also refused a custom action script and turned away 28,
        /// including ordinary conversations like <c>train_terminal</c> and <c>capn_sluuk</c>. A
        /// custom action does not affect what a player can see, so those open and the choice reads
        /// "runs the script X" instead of pretending to be just talk.
        /// </remarks>
        private static readonly string[] KnownUnsupported =
        {
            "dmfi_universal", "dt_barman_gen", "dt_cntr_magasin", "dt_doc_velpo",
            "q1_nikka_larson", "quest_example", "red_journal_mand", "spawn_banner",
            "zomb_telconv"
        };

        [Test]
        public void PlayItOpensTheOverwhelmingMajorityOfAuthoredConversations()
        {
            var refused = new SortedSet<string>(StringComparer.Ordinal);
            var total = 0;

            foreach (var path in AuthoredConversations())
            {
                total++;
                if (!ConversationCompatibility.Check(DlgDocument.Load(path)).IsSupported)
                    refused.Add(ResRefOf(path));
            }

            refused.Should().BeEquivalentTo(KnownUnsupported);
            total.Should().Be(346, "the hand-authored conversations, generated shells excluded");
        }

        [Test]
        public void APreviewLimitationNamesTheScriptItCannotEvaluate()
        {
            var path = Path.Combine(CorpusLocator.ModuleDirectory, "dlg", "dmfi_universal.dlg.json");
            var support = ConversationCompatibility.Check(DlgDocument.Load(path));

            support.IsSupported.Should().BeFalse();
            support.Reason.Should().Contain("its own script");
            support.Reason.Should().Contain("saved unchanged");
        }

        [Test]
        public void AConversationThatCannotStartReportsThatReason()
        {
            var document = DlgDocument.Load(
                Path.Combine(CorpusLocator.ModuleDirectory, "dlg", "dantherbs.dlg.json"));

            foreach (var opening in document.Openings.ToList())
                document.RemoveLink(opening);

            var support = ConversationCompatibility.Check(document);

            support.IsSupported.Should().BeFalse();
            support.Reason.Should().Contain("no opening");
        }

        [Test]
        public void AnOrdinaryQuestGiverIsSupported()
        {
            var document = DlgDocument.Load(
                Path.Combine(CorpusLocator.ModuleDirectory, "dlg", "dantherbs.dlg.json"));

            ConversationCompatibility.Check(document).IsSupported.Should().BeTrue();
        }
    }
}
