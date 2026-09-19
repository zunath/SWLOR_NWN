using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Tests
{
    /// <summary>Lossless legacy DLG editing against frozen import examples and the native DMFI file.</summary>
    public class DlgCorpusTests
    {
        private static IEnumerable<string> AllDialogs() =>
            LegacyConversationFixtures.AllPaths();

        /// <summary>Representative imported DLG shapes, including the large native DMFI conversation.</summary>
        private static IEnumerable<string> SampleDialogs()
        {
            foreach (var name in new[]
            {
                "dantherbs", "bartender", "avixtatham", "dmfi_universal", "tk_omnidye"
            })
            {
                var path = LegacyConversationFixtures.PathFor(name);
                if (File.Exists(path))
                    yield return path;
            }
        }

        [Test]
        public void EveryConversationOpensAndEveryLinkResolves()
        {
            var broken = new List<string>();
            foreach (var path in AllDialogs())
            {
                var document = DlgDocument.Load(path);
                var dangling = document.FindDanglingLinks();
                if (dangling.Count > 0)
                    broken.Add($"{Path.GetFileName(path)}: {dangling.Count} link(s) point at a line that is not there");
            }

            broken.Should().BeEmpty();
        }

        [Test]
        public void EveryListNumbersItsElementsByPosition()
        {
            var offenders = new List<string>();
            foreach (var path in AllDialogs())
            {
                var document = DlgDocument.Load(path);
                CheckStructIds(document.Fields, Path.GetFileName(path), string.Empty, offenders);
            }

            offenders.Should().BeEmpty();
        }

        [Test]
        public void EveryConversationHasAtLeastOneOpening()
        {
            var silent = AllDialogs()
                .Where(path => DlgDocument.Load(path).Openings.Count == 0)
                .Select(Path.GetFileName)
                .ToList();

            silent.Should().BeEmpty("a conversation with no opening can never start");
        }

        [Test]
        public void TheTypedViewNeverDisturbsAFileItOnlyRead()
        {
            foreach (var path in AllDialogs())
            {
                var original = File.ReadAllBytes(path);
                var document = DlgDocument.Parse(original);

                // Touch everything the view exposes, including the derived collections.
                _ = document.Openings.Count;
                _ = document.CountWords();
                foreach (var link in document.AllLinks())
                {
                    _ = link.TargetIndex;
                    _ = link.IsChild;
                    _ = link.Conditions.Count;
                }

                foreach (var node in document.Entries.Concat(document.Replies))
                {
                    _ = node.Text;
                    _ = node.Actions.Count;
                    _ = node.Delay;
                }

                document.ToBytes().Should().Equal(original, $"reading {Path.GetFileName(path)} must not change it");
            }
        }

        // These frozen imports predate the editor's word-count repair on save.
        private static readonly string[] KnownStaleWordCounts = { "dt_barman_gen", "star_attend_lau" };

        [Test]
        public void StoredWordCountsAgreeWithTheCountingRule()
        {
            // Pins Aurora's rule — whitespace-separated tokens across every entry and reply — against
            // every file that records one, so RecomputeWordCount cannot drift from what the rest of
            // the corpus was written with. A new name appearing here means the rule changed.
            var disagreements = new List<string>();
            var counted = 0;
            foreach (var path in AllDialogs())
            {
                var document = DlgDocument.Load(path);
                if (document.NumWords == null)
                    continue;

                counted++;
                if (document.NumWords != (uint)document.CountWords())
                    disagreements.Add(Path.GetFileName(path).Replace(".dlg.json", string.Empty));
            }

            disagreements.Should().BeEquivalentTo(KnownStaleWordCounts);
            counted.Should().BeGreaterThan(0, "legacy imports must exercise stored word counts");
        }

        [TestCaseSource(nameof(SampleDialogs))]
        public void ChangingOneLine_RewritesExactlyOneLineOfTheFile(string path)
        {
            var original = File.ReadAllBytes(path);
            var document = DlgDocument.Parse(original);
            if (document.Entries.Count == 0)
                Assert.Ignore($"{Path.GetFileName(path)} has no NPC lines.");

            document.Entries[0].Text = "Edited by the toolset.";

            var originalLines = Encoding.UTF8.GetString(original).Split('\n');
            var writtenLines = Encoding.UTF8.GetString(document.ToBytes()).Split('\n');

            writtenLines.Should().HaveCount(originalLines.Length);
            var changed = Enumerable.Range(0, originalLines.Length)
                .Where(i => originalLines[i] != writtenLines[i])
                .ToList();

            changed.Should().ContainSingle(
                "editing one line of dialogue should rewrite one line of JSON. Changed: "
                + string.Join(" | ", changed.Select(i => $"{i}: {originalLines[i].Trim()} -> {writtenLines[i].Trim()}")));
        }

        [TestCaseSource(nameof(SampleDialogs))]
        public void AddingALine_LeavesEveryExistingLineOfTheFileUntouched(string path)
        {
            var original = File.ReadAllBytes(path);
            var document = DlgDocument.Parse(original);

            document.AddEntry("Added by the toolset.");

            var originalLines = Encoding.UTF8.GetString(original).Split('\n');
            var writtenLines = Encoding.UTF8.GetString(document.ToBytes()).Split('\n');

            // An append is a pure insertion: the original lines survive in order, with the new
            // struct's block dropped in. Nothing that was already there is rewritten, which is what
            // makes appending the right insert strategy for a format that indexes by position.
            writtenLines.Length.Should().BeGreaterThan(originalLines.Length);
            FirstLineNotPreserved(originalLines, writtenLines).Should().Be(-1,
                "every line of the original file should still be present, in order");
        }

        [TestCaseSource(nameof(SampleDialogs))]
        public void AddingThenRemovingALine_RestoresTheFileByteForByte(string path)
        {
            var original = File.ReadAllBytes(path);
            var document = DlgDocument.Parse(original);

            var added = document.AddReply("Temporary.");
            document.EstimateRemoveNode(added).IsLocal.Should().BeTrue();
            document.RemoveNode(added);

            document.ToBytes().Should().Equal(original);
        }

        private static void CheckStructIds(JsonGffStruct target, string file, string path, List<string> offenders)
        {
            foreach (var (name, field) in target.Entries)
            {
                var here = string.IsNullOrEmpty(path) ? name : $"{path}.{name}";
                if (field.Type == GffFieldType.Struct && field.Struct != null)
                {
                    CheckStructIds(field.Struct, file, here, offenders);
                    continue;
                }

                if (field.Type != GffFieldType.List || field.Elements == null)
                    continue;

                for (var i = 0; i < field.Elements.Count; i++)
                {
                    if (field.Elements[i].StructId != (uint)i)
                        offenders.Add($"{file}: {here}[{i}] has id {field.Elements[i].StructId}");

                    CheckStructIds(field.Elements[i], file, $"{here}[{i}]", offenders);
                }
            }
        }

        /// <summary>
        /// Index of the first original line that does not reappear, in order, in the written file —
        /// or -1 when the original survives intact as a subsequence.
        /// </summary>
        /// <remarks>
        /// A greedy two-pointer walk rather than a longest-common-subsequence table. Both answer the
        /// question, but the table is O(n×m) and dmfi_universal is 108,000 lines long, which made
        /// this one test most of the suite's runtime. Greedy is exact here because the insertion is
        /// a contiguous block: no original line can be matched "too early" by a later duplicate
        /// without a preceding original line failing to match at all.
        /// </remarks>
        private static int FirstLineNotPreserved(IReadOnlyList<string> original, IReadOnlyList<string> written)
        {
            var writtenIndex = 0;
            for (var i = 0; i < original.Count; i++)
            {
                while (writtenIndex < written.Count && written[writtenIndex] != original[i])
                    writtenIndex++;

                if (writtenIndex == written.Count)
                    return i;

                writtenIndex++;
            }

            return -1;
        }
    }
}
