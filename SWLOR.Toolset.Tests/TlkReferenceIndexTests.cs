using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Tlk;
using SWLOR.Toolset.Domain.GameData.Tlk;

namespace SWLOR.Toolset.Tests
{
    public class TlkReferenceIndexTests
    {
        [Test]
        public void Build_UsesStructuredParserAndConservativeRawTextFallback()
        {
            var root = CreateTempDirectory();
            try
            {
                var maxValidStrRef = TlkService.CustomTlkBase + (uint)TlkFormatLimits.MaximumEntryId;
                var firstInvalidStrRef = maxValidStrRef + 1;
                File.WriteAllText(Path.Combine(root, "sample.2da"),
                    $"""
                    2DA V2.0

                        DisplayName       Description       OrdinaryNumber
                    alpha "Quoted value" 16777221          42
                    beta  Plain           16777222          16777215
                    gamma Plain           {maxValidStrRef}          42
                    delta Plain           {firstInvalidStrRef}          42
                    """);
                File.WriteAllText(Path.Combine(root, "scratch.2da"),
                    """
                    not a 2DA
                    orphan "quoted 16777223," and16777224suffix
                    ignored 24777216
                    """);

                var index = TlkReferenceIndex.Build(root);

                index.ReferencedEntryIds.Should().Equal(5, 6, 7, 8, TlkFormatLimits.MaximumEntryId);
                index.UsagesOf(5).Should().ContainSingle().Which.Should().Be(
                    new TlkReferenceUsage("sample.2da", 0, "alpha", "Description", 16777221, 5));
                index.UsageCountFor(6).Should().Be(1);
                index.IsReferenced(4).Should().BeFalse();
                index.IsReferenced(TlkFormatLimits.MaximumEntryId).Should().BeTrue();
                index.IsReferenced(TlkFormatLimits.MaximumEntryId + 1).Should().BeFalse(
                    "a number outside the writable TLK range is not a valid custom TLK reference");
                index.UsagesOf(7).Should().ContainSingle().Which.Should().Be(
                    new TlkReferenceUsage(
                        "scratch.2da",
                        1,
                        "orphan",
                        TlkReferenceIndex.FallbackColumnName,
                        16777223,
                        7));
                index.UsagesOf(8).Should().ContainSingle(usage =>
                    usage.ColumnName == TlkReferenceIndex.FallbackColumnName && usage.RowLabel == "orphan");
                index.UnscannableFiles.Should().BeEmpty(
                    "a readable malformed file is conservatively covered by raw-text fallback");
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Test]
        public void Corpus_RecognizesIntentionallyBlankBiographyRow80831()
        {
            var haks = Path.Combine(CorpusLocator.RepositoryRoot, "SWLOR_Haks");
            var document = TlkDocument.Load(Path.Combine(haks, "sw_tlk", "sw_tlk.tlk.json"));
            var index = TlkReferenceIndex.Build(Path.Combine(haks, "sw_2da"), CorpusLocator.RepositoryRoot);

            document.ContainsEntry(80831).Should().BeFalse("the biography slot is intentionally blank");
            index.IsReferenced(80831).Should().BeTrue();
            index.UsagesOf(80831).Should().Contain(usage =>
                usage.FileName.Equals("racialtypes.2da", StringComparison.OrdinalIgnoreCase) &&
                usage.RowLabel == "6" &&
                usage.ColumnName.Equals("Biography", StringComparison.OrdinalIgnoreCase) &&
                usage.StrRef == 16858047);
            index.UnscannableFiles.Should().BeEmpty(
                "the malformed legacy 2DA is still fully covered by raw-text fallback");

            var expectedFirstSafeGap = Enumerable.Range(0, document.MaxEntryId + 2)
                .First(id => !document.ContainsEntry(id) && !index.IsReferenced(id));
            var firstSafeGap = document.FindFirstAvailableBlank(index);
            TestContext.Out.WriteLine($"First corpus-safe custom TLK gap: {firstSafeGap}");
            firstSafeGap.Should().Be(expectedFirstSafeGap);
            firstSafeGap.Should().Be(6181, "the current corpus's first unpopulated and unreferenced row is stable");
        }

        [Test]
        public void Build_IndexesCustomStrRefsOutsideTwoDaFiles()
        {
            var root = CreateTempDirectory();
            try
            {
                var twoDaDirectory = Path.Combine(root, "SWLOR_Haks", "sw_2da");
                var moduleDirectory = Path.Combine(root, "Module", "itp");
                Directory.CreateDirectory(twoDaDirectory);
                Directory.CreateDirectory(moduleDirectory);
                File.WriteAllText(Path.Combine(moduleDirectory, "palette.itp.json"),
                    "{\n  \"value\": 16777261,\n  \"notAStrRef\": 0.16777262\n}\n");
                foreach (var ignoredDirectory in new[] { ".agents", ".claude", ".codex" })
                {
                    var path = Path.Combine(root, ignoredDirectory, "worktrees");
                    Directory.CreateDirectory(path);
                    File.WriteAllText(
                        Path.Combine(path, "agent-metadata.json"),
                        $"{{\"notRuntimeContent\":{TlkService.CustomTlkBase + 90}}}\n");
                }

                var index = TlkReferenceIndex.Build(twoDaDirectory, root);

                index.IsReferenced(45).Should().BeTrue();
                index.UsagesOf(45).Should().ContainSingle(usage =>
                    usage.FileName == "Module/itp/palette.itp.json" &&
                    usage.RowIndex == 2 &&
                    usage.ColumnName == TlkReferenceIndex.RepositoryTextColumnName);
                index.IsReferenced(46).Should().BeFalse(
                    "digits after a decimal point are not standalone StrRef tokens");
                index.IsReferenced(90).Should().BeFalse(
                    "agent metadata and nested agent worktrees are not runtime reference sources");
                index.UnscannableFiles.Should().BeEmpty();
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Test]
        public void Refresh_ReusesUnchangedSourcesAndRescansOnlyChangedFiles()
        {
            var root = CreateTempDirectory();
            try
            {
                var twoDaDirectory = Path.Combine(root, "SWLOR_Haks", "sw_2da");
                var moduleDirectory = Path.Combine(root, "Module");
                Directory.CreateDirectory(twoDaDirectory);
                Directory.CreateDirectory(moduleDirectory);
                var twoDaPath = Path.Combine(twoDaDirectory, "sample.2da");
                var jsonPath = Path.Combine(moduleDirectory, "sample.json");
                File.WriteAllText(twoDaPath,
                    $"2DA V2.0\n\nLABEL STRREF\n0 test {TlkService.CustomTlkBase + 1}\n");
                File.WriteAllText(jsonPath, $"{{\"strRef\":{TlkService.CustomTlkBase + 2}}}\n");

                var initial = TlkReferenceIndex.Build(twoDaDirectory, root);
                initial.LastRefreshScannedSourceCount.Should().Be(2);

                var unchanged = initial.Refresh(twoDaDirectory, root);
                unchanged.LastRefreshScannedSourceCount.Should().Be(0,
                    "an editor action with no repository changes must not reread source contents");

                File.WriteAllText(jsonPath,
                    $"{{\"strRef\":{TlkService.CustomTlkBase + 30},\"changed\":true}}\n");
                var changed = unchanged.Refresh(twoDaDirectory, root);

                changed.LastRefreshScannedSourceCount.Should().Be(1);
                changed.IsReferenced(1).Should().BeTrue("the unchanged 2DA result was retained");
                changed.IsReferenced(2).Should().BeFalse("the changed file's old result was replaced");
                changed.IsReferenced(30).Should().BeTrue();
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        private static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "SWLOR.Toolset.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
