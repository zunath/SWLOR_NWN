using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Script;
using SWLOR.Toolset.Domain.Script.Symbols;
using SWLOR.Toolset.Domain.Script.Syntax;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Tier-1 analysis. The gate that matters is the absence of false positives: a squiggle on valid
    /// legacy code is what makes an editor annoying enough to abandon, and the compiler - not this
    /// pass - is the authority on validity.
    /// </summary>
    public class ScriptAnalyzerTests
    {
        private static string HeaderPath => Path.Combine(
            CorpusLocator.RepositoryRoot, "SWLOR.NWN.API", "NWN", "nwscript-8193.37.nss");

        private static EngineSymbolDatabase? _db;
        private static EngineSymbolDatabase Db => _db ??=
            File.Exists(HeaderPath) ? EngineSymbolDatabase.Load(HeaderPath) : EngineSymbolDatabase.Empty;

        private static ScriptAnalyzer Analyzer => new(Db);

        private static ScriptAnalyzer AnalyzerWithBinder(Func<string, string?>? readInclude = null) =>
            new(Db, readInclude ?? (_ => null));

        [Test]
        public void EveryModuleScript_ProducesNoDiagnostics()
        {
            var directory = Path.Combine(CorpusLocator.ModuleDirectory, "nss");
            var offenders = new List<string>();

            foreach (var path in Directory.EnumerateFiles(directory, "*.nss"))
            {
                var analysis = Analyzer.Analyze(ScriptTextDocument.Load(path).Text);
                foreach (var d in analysis.Diagnostics)
                    offenders.Add($"{Path.GetFileName(path)}({d.Line}): {d.Message}");
            }

            offenders.Should().BeEmpty(
                "these 87 files are known-good, so any finding is this analyzer's bug, not the code's");
        }

        [Test]
        public void EveryModuleScript_WithResolvedIncludes_ProducesNoDiagnostics()
        {
            if (Db.Functions.Count == 0)
                Assert.Ignore("engine header not present");

            var directory = Path.Combine(CorpusLocator.ModuleDirectory, "nss");
            var sources = Directory.EnumerateFiles(directory, "*.nss")
                .ToDictionary(
                    p => Path.GetFileNameWithoutExtension(p)!,
                    p => ScriptTextDocument.Load(p).Text,
                    StringComparer.OrdinalIgnoreCase);

            var analyzer = AnalyzerWithBinder(name => sources.TryGetValue(name, out var text) ? text : null);
            var offenders = new List<string>();

            foreach (var (resRef, source) in sources)
            {
                var analysis = analyzer.Analyze(source);
                foreach (var d in analysis.Diagnostics)
                    offenders.Add($"{resRef}.nss({d.Line}): {d.Message}");
            }

            offenders.Should().BeEmpty(
                "literal type checking must never squiggle shipped module scripts");
        }

        [Test]
        public void UnclosedBrace_IsReported()
        {
            var analysis = Analyzer.Analyze("void main()\n{\n    int n = 1;\n");

            analysis.Diagnostics.Should().ContainSingle()
                .Which.Message.Should().Contain("Unclosed");
        }

        [Test]
        public void MismatchedBracket_IsReported()
        {
            var analysis = Analyzer.Analyze("void main()\n{\n    int n = (1;\n}\n");

            analysis.Diagnostics.Should().NotBeEmpty();
            analysis.Diagnostics[0].Severity.Should().Be(ScriptDiagnosticSeverity.Error);
        }

        [Test]
        public void UnterminatedString_IsAnError()
        {
            var analysis = Analyzer.Analyze("void main() { string s = \"oops\n}");

            analysis.Diagnostics.Should().Contain(d => d.Message.Contains("Unterminated string"));
        }

        [Test]
        public void UnterminatedBlockComment_IsOnlyAWarning()
        {
            var analysis = Analyzer.Analyze("void main() {}\n/* still typing");

            analysis.Diagnostics.Should().ContainSingle()
                .Which.Severity.Should().Be(ScriptDiagnosticSeverity.Warning);
        }

        [Test]
        public void DuplicateDefinition_IsReported()
        {
            var analysis = Analyzer.Analyze("void Foo() {}\nvoid Foo() {}\nvoid main() {}");

            analysis.Diagnostics.Should().ContainSingle()
                .Which.Message.Should().Contain("already defined");
        }

        [Test]
        public void ForwardDeclarationPlusDefinition_IsNotADuplicate()
        {
            var analysis = Analyzer.Analyze("void Foo();\nvoid Foo() {}\nvoid main() {}");

            analysis.Diagnostics.Should().BeEmpty("declaring then defining is the normal pattern");
        }

        [Test]
        public void TooManyArgumentsToAnEngineFunction_IsReported()
        {
            if (Db.Functions.Count == 0)
                Assert.Ignore("engine header not present");

            var analysis = Analyzer.Analyze("void main() { int n = Random(1, 2, 3); }");

            analysis.Diagnostics.Should().Contain(d => d.Message.Contains("at most 1"));
        }

        [Test]
        public void TooFewArgumentsIsNotReported()
        {
            if (Db.Functions.Count == 0)
                Assert.Ignore("engine header not present");

            // A short call is usually a half-typed one; squiggling it would fight the author's cursor.
            Analyzer.Analyze("void main() { int n = Random(); }")
                .Diagnostics.Should().BeEmpty();
        }

        [Test]
        public void StringLiteralToIntParameter_IsReported()
        {
            if (Db.Functions.Count == 0)
                Assert.Ignore("engine header not present");

            var diagnostics = AnalyzerWithBinder()
                .Analyze("void main() { int n = Random(\"bad\"); }")
                .Diagnostics;

            diagnostics.Should().ContainSingle(d =>
                d.Message.Contains("string literal") &&
                d.Message.Contains("int parameter") &&
                d.Message.Contains("Random"));
        }

        [Test]
        public void FloatLiteralToObjectParameter_IsReported()
        {
            if (Db.Functions.Count == 0)
                Assert.Ignore("engine header not present");

            var diagnostics = AnalyzerWithBinder()
                .Analyze("void main() { float f = GetDistanceBetween(1.0, OBJECT_SELF); }")
                .Diagnostics;

            diagnostics.Should().ContainSingle(d =>
                d.Message.Contains("float literal") &&
                d.Message.Contains("object parameter") &&
                d.Message.Contains("GetDistanceBetween"));
        }

        [Test]
        public void IntAndFloatLiteralArguments_AreImplicitlyCompatible()
        {
            if (Db.Functions.Count == 0)
                Assert.Ignore("engine header not present");

            AnalyzerWithBinder()
                .Analyze("void main() { int n = Random(1.0); string s = FloatToString(1); }")
                .Diagnostics.Should().BeEmpty();
        }

        [Test]
        public void LiteralTypeChecking_IsSilentWhenAnIncludeCannotBeResolved()
        {
            if (Db.Functions.Count == 0)
                Assert.Ignore("engine header not present");

            AnalyzerWithBinder()
                .Analyze("#include \"missing_inc\"\nvoid main() { int n = Random(\"bad\"); }")
                .Diagnostics.Should().BeEmpty("an unresolved include makes the scope incomplete");
        }

        [Test]
        public void UnknownIdentifiers_AreNeverReported()
        {
            if (Db.Functions.Count == 0)
                Assert.Ignore("engine header not present");

            // An identifier can come from any include, and this pass does not resolve include
            // contents. Flagging them would light up every legacy file in the module.
            Analyzer.Analyze("void main() { SomethingFromAnInclude(1); }")
                .Diagnostics.Should().BeEmpty();
        }

        [Test]
        public void HalfTypedSource_NeverThrows()
        {
            const string full = "void main()\n{\n    object o = GetNearestCreature(CREATURE_TYPE_PLAYER_CHAR,\n}";
            for (var cut = 0; cut <= full.Length; cut++)
            {
                var src = full[..cut];
                var act = () => Analyzer.Analyze(src);
                act.Should().NotThrow("analysis runs on every keystroke (cut {0})", cut);
            }
        }
    }

    /// <summary>Include graph, in both directions.</summary>
    public class ScriptIncludeGraphTests
    {
        private static ScriptIncludeGraph Graph(params (string Name, string Source)[] files) =>
            ScriptIncludeGraph.BuildFrom(files.ToDictionary(f => f.Name, f => f.Source, StringComparer.OrdinalIgnoreCase));

        [Test]
        public void ResolvesDirectAndTransitiveIncludes()
        {
            var graph = Graph(
                ("a", "#include \"b\"\nvoid main() {}"),
                ("b", "#include \"c\"\nint Helper() { return 1; }"),
                ("c", "int Deep() { return 2; }"));

            graph.DirectIncludes("a").Should().Equal("b");
            graph.TransitiveIncludes("a").Should().BeEquivalentTo(new[] { "b", "c" });
        }

        [Test]
        public void ReverseEdgesFindEveryDependent()
        {
            var graph = Graph(
                ("inc", "int Helper() { return 1; }"),
                ("one", "#include \"inc\"\nvoid main() {}"),
                ("two", "#include \"inc\"\nvoid main() {}"),
                ("three", "#include \"one\"\nvoid main() {}"));

            graph.DirectDependents("inc").Should().BeEquivalentTo(new[] { "one", "two" });
            graph.TransitiveDependents("inc").Should().BeEquivalentTo(new[] { "one", "two", "three" });
        }

        [Test]
        public void CyclesTerminateInsteadOfHanging()
        {
            var graph = Graph(
                ("a", "#include \"b\"\nvoid main() {}"),
                ("b", "#include \"a\"\nint Helper() { return 1; }"));

            graph.TransitiveIncludes("a").Should().Contain("b");
            graph.HasCycle("a").Should().BeTrue();
        }

        [Test]
        public void RealModuleCorpus_BuildsAndFindsTheDmfiChain()
        {
            var graph = ScriptIncludeGraph.Build(Path.Combine(CorpusLocator.ModuleDirectory, "nss"));

            graph.ResRefs.Should().NotBeEmpty();
            graph.TransitiveDependents("dmfi_init_inc").Should().NotBeEmpty(
                "editing that header must be known to invalidate the scripts that include it");
        }

        [Test]
        public void UnknownResRef_HasNoEdges()
        {
            Graph(("a", "void main() {}")).DirectIncludes("nope").Should().BeEmpty();
        }
    }

    /// <summary>Staleness detection, against a throwaway temp module.</summary>
    public class ScriptStalenessScannerTests
    {
        private string _root = null!;
        private string _nss = null!;
        private string _ncs = null!;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "swlor_stale_" + Guid.NewGuid().ToString("N"));
            _nss = Path.Combine(_root, "nss");
            _ncs = Path.Combine(_root, "ncs");
            Directory.CreateDirectory(_nss);
            Directory.CreateDirectory(_ncs);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        private void Source(string name, string text, DateTime? when = null)
        {
            var path = Path.Combine(_nss, name + ".nss");
            File.WriteAllText(path, text);
            if (when != null)
                File.SetLastWriteTimeUtc(path, when.Value);
        }

        private void Compiled(string name, DateTime when)
        {
            var path = Path.Combine(_ncs, name + ".ncs");
            File.WriteAllBytes(path, new byte[] { 0x4E, 0x43, 0x53, 0x20 });
            File.SetLastWriteTimeUtc(path, when);
        }

        private IReadOnlyList<StaleScript> Scan() => new ScriptStalenessScanner(_nss, _ncs).Scan();

        [Test]
        public void UpToDateScript_IsNotStale()
        {
            var old = DateTime.UtcNow.AddHours(-2);
            Source("a", "void main() {}", old);
            Compiled("a", DateTime.UtcNow);

            Scan().Should().BeEmpty();
        }

        [Test]
        public void SourceNewerThanArtifact_IsStale()
        {
            Compiled("a", DateTime.UtcNow.AddHours(-2));
            Source("a", "void main() {}", DateTime.UtcNow);

            Scan().Should().ContainSingle().Which.Reason.Should().Be(StaleReason.SourceNewer);
        }

        [Test]
        public void SourceSwappedUnderAPreservedMTime_IsStaleOnceAFingerprintExists()
        {
            var sharedTime = DateTime.UtcNow.AddHours(-2);
            Source("a", "void main() { NoOp(); }", sharedTime);
            Compiled("a", DateTime.UtcNow);

            // First scan: timestamps alone say fresh, and there is no cache yet (first sight), so
            // this scan also records the fingerprint baseline rather than reporting anything.
            Scan().Should().BeEmpty();

            // Replace the source's content but force its mtime back to the original value - the
            // same symptom as an external tool preserving mtimes, or two writes landing in the same
            // coarse filesystem timestamp bucket. The .ncs is untouched, so timestamps alone would
            // still say fresh.
            Source("a", "void main() { DifferentBody(); }", sharedTime);

            Scan().Should().ContainSingle().Which.Reason.Should().Be(StaleReason.SourceReplaced);
        }

        [Test]
        public void SuccessfulCompileFingerprintClearsSameTimestampSourceReplacement()
        {
            var sourceTime = DateTime.UtcNow.AddHours(-2);
            var compiledTime = DateTime.UtcNow.AddHours(-1);
            Source("a", "void main() { Before(); }", sourceTime);
            Compiled("a", compiledTime);
            Scan().Should().BeEmpty();

            Source("a", "void main() { After(); }", sourceTime);
            Compiled("a", compiledTime);
            Scan().Should().ContainSingle().Which.Reason.Should().Be(StaleReason.SourceReplaced);

            var scanner = new ScriptStalenessScanner(_nss, _ncs);
            scanner.RecordSuccessfulCompile("a").Should().BeTrue();

            Scan().Should().BeEmpty(
                "installing bytecode refreshes the persisted source/include hash even when mtimes are unchanged");
        }

        [Test]
        public void IncludeSwappedUnderAPreservedMTime_MarksTheDependentStale()
        {
            var sharedTime = DateTime.UtcNow.AddHours(-2);
            Source("a", "#include \"swap_inc\"\nvoid main() { Helper(); }", sharedTime);
            Source("swap_inc", "int Helper() { return 1; }", sharedTime);
            Compiled("a", DateTime.UtcNow);

            // First sight records the baseline over the entry point AND its include set.
            Scan().Should().BeEmpty();

            // The include's content changes but its mtime is forced back - the dependent's own
            // source and artifact are untouched, so every timestamp still says fresh, yet the
            // shipped bytecode was compiled against the old include.
            Source("swap_inc", "int Helper() { return 2; }", sharedTime);

            var finding = Scan().Should().ContainSingle().Which;
            finding.Reason.Should().Be(StaleReason.SourceReplaced);
            finding.ResRef.Should().Be("a");
        }

        [Test]
        public void DeletedSourceWithSurvivingArtifact_IsStaleOnceFingerprinted()
        {
            var old = DateTime.UtcNow.AddHours(-2);
            Source("a", "void main() {}", old);
            Compiled("a", DateTime.UtcNow);

            // First sight records the fingerprint - the memory that "a" WAS a sourced entry point.
            Scan().Should().BeEmpty();

            // The source is deleted but the bytecode remains; without the fingerprint no timestamp
            // could flag this, and the packer would ship removed behavior verbatim.
            File.Delete(Path.Combine(_nss, "a.nss"));

            var finding = Scan().Should().ContainSingle().Which;
            finding.Reason.Should().Be(StaleReason.SourceDeleted);
            finding.ResRef.Should().Be("a");
        }

        [Test]
        public void ACompiledOnlyArtifactThatNeverHadASourceIsNotReported()
        {
            Compiled("vendored_only", DateTime.UtcNow);

            Scan().Should().BeEmpty(
                "an artifact never fingerprinted never had a source here - intentional compiled-only content");
        }

        [Test]
        public void PurgeOrphanedArtifacts_RemovesTheOrphanAndForgetsIt()
        {
            var old = DateTime.UtcNow.AddHours(-2);
            Source("a", "void main() {}", old);
            Compiled("a", DateTime.UtcNow);
            Scan();

            File.Delete(Path.Combine(_nss, "a.nss"));

            new ScriptStalenessScanner(_nss, _ncs).PurgeOrphanedArtifacts().Should().Equal("a");
            File.Exists(Path.Combine(_ncs, "a.ncs")).Should().BeFalse();
            Scan().Should().BeEmpty("the orphan is gone and its fingerprint forgotten");
        }

        [Test]
        public void FreshCheckoutWithNoCache_DoesNotReportUntouchedScriptsStale()
        {
            // No prior Scan() has run in this test, so ScriptFingerprintStore has nothing persisted -
            // exactly a fresh checkout. The scan must still trust the plain timestamp comparison.
            var old = DateTime.UtcNow.AddHours(-2);
            Source("a", "void main() {}", old);
            Compiled("a", DateTime.UtcNow);

            Scan().Should().BeEmpty();
        }

        [Test]
        public void NeverCompiledEntryPoint_IsStale()
        {
            Source("a", "void main() {}");

            Scan().Should().ContainSingle().Which.Reason.Should().Be(StaleReason.NeverCompiled);
        }

        [Test]
        public void AnIncludeWithNoMain_IsNotExpectedToHaveAnArtifact()
        {
            Source("helper_inc", "int Helper() { return 1; }");

            Scan().Should().BeEmpty("an include produces no .ncs by design");
        }

        [Test]
        public void AnIncludeWithALeftoverArtifact_IsStale()
        {
            // The dmfi_dmw_inc case: a source that lost its entry point but kept its old bytecode.
            // Timestamps cannot see it, and the packer ships every .ncs verbatim, so the scan must.
            Source("helper_inc", "int Helper() { return 1; }");
            Compiled("helper_inc", DateTime.UtcNow);

            var finding = Scan().Should().ContainSingle().Which;
            finding.Reason.Should().Be(StaleReason.ObsoleteIncludeArtifact);
            finding.ResRef.Should().Be("helper_inc");
        }

        /// <summary>The case nothing else in the pipeline can see.</summary>
        [Test]
        public void EditingAnInclude_MarksEveryTransitiveDependentStale()
        {
            var old = DateTime.UtcNow.AddHours(-2);
            Source("one", "#include \"base_inc\"\nvoid main() {}", old);
            Source("two", "#include \"mid_inc\"\nvoid main() {}", old);
            Source("mid_inc", "#include \"base_inc\"\nint Mid() { return 1; }", old);
            Compiled("one", DateTime.UtcNow.AddHours(-1));
            Compiled("two", DateTime.UtcNow.AddHours(-1));

            // Touch the deepest header last.
            Source("base_inc", "int Base() { return 1; }", DateTime.UtcNow);

            var stale = Scan();

            stale.Should().HaveCount(2);
            stale.Should().OnlyContain(s => s.Reason == StaleReason.IncludeNewer);
            stale.Select(s => s.ResRef).Should().BeEquivalentTo(new[] { "one", "two" });
        }

        [Test]
        public void MissingDirectAndTransitiveIncludesBlockPacking()
        {
            var old = DateTime.UtcNow.AddHours(-2);
            Source("direct", "#include \"missing_direct\"\nvoid main() {}", old);
            Source("transitive", "#include \"middle_inc\"\nvoid main() {}", old);
            Source("middle_inc", "#include \"missing_deep\"\nint Middle() { return 1; }", old);
            Compiled("direct", DateTime.UtcNow);
            Compiled("transitive", DateTime.UtcNow);

            var stale = Scan();

            stale.Select(finding => finding.ResRef)
                .Should().BeEquivalentTo("direct", "transitive");
            stale.Should().OnlyContain(finding => finding.Reason == StaleReason.MissingInclude);
            ScriptPackReadiness.Evaluate(stale).Should().NotBeNull();
        }

        [Test]
        public void IncludesResolvedByTheCompilerGameLayersDoNotBlockPacking()
        {
            var old = DateTime.UtcNow.AddHours(-2);
            Source(
                "entry",
                "#include \"local_inc\"\nvoid main() { BaseHelper(); }",
                old);
            Source(
                "local_inc",
                "#include \"nw_i0_generic\"\nint BaseHelper() { return 1; }",
                old);
            Compiled("entry", DateTime.UtcNow);

            var stale = new ScriptStalenessScanner(
                _nss,
                _ncs,
                include => include.Equals(
                    "nw_i0_generic",
                    StringComparison.OrdinalIgnoreCase)).Scan();

            stale.Should().BeEmpty(
                "the compiler resolves engine headers through its staged and KEY/BIF resource layers");
            ScriptPackReadiness.Evaluate(stale).Should().BeNull();
        }

        [Test]
        public void StartingConditional_CountsAsAnEntryPoint()
        {
            Source("cond", "int StartingConditional() { return TRUE; }");

            Scan().Should().ContainSingle().Which.ResRef.Should().Be("cond");
        }

        [Test]
        public void PackReadiness_IsCleanWhenNoScriptsAreStale()
        {
            var old = DateTime.UtcNow.AddHours(-2);
            Source("a", "void main() {}", old);
            Compiled("a", DateTime.UtcNow);

            ScriptPackReadiness.Evaluate(Scan()).Should().BeNull();
        }

        [Test]
        public void PackReadiness_RejectsFailedBuildEvenWhenFollowUpScanIsClean()
        {
            ScriptPackReadiness.CanPackAfterBuild(
                    failed: 1,
                    remaining: Array.Empty<StaleScript>())
                .Should().BeFalse(
                    "surviving old bytecode can make the stale scan look clean after a compiler failure");

            ScriptPackReadiness.CanPackAfterBuild(
                    failed: 0,
                    remaining: Array.Empty<StaleScript>())
                .Should().BeTrue();
        }

        [Test]
        public void PackReadiness_NamesEveryStaleScriptAndOffersBuildThenPack()
        {
            Compiled("a", DateTime.UtcNow.AddHours(-2));
            Source("a", "void main() {}", DateTime.UtcNow);

            var warning = ScriptPackReadiness.Evaluate(Scan());

            warning.Should().NotBeNull();
            warning!.Headline.Should().Contain("1 stale compiled script");
            warning.ConfirmLabel.Should().Be("Build then Pack");
            warning.OutputLines.Should().ContainSingle()
                .Which.Should().Be("a.ncs is older than a.nss");
            warning.Message.Should().Contain("Build all scripts now");
        }

        [Test]
        public void IncludeRebuildOfferMatchesTransitiveDependentsAndRebuildClearsStaleness()
        {
            var old = DateTime.UtcNow.AddHours(-2);
            Source("one", "#include \"base_inc\"\nvoid main() {}", old);
            Source("two", "#include \"mid_inc\"\nvoid main() {}", old);
            Source("mid_inc", "#include \"base_inc\"\nint Mid() { return 1; }", old);
            Source("helper_inc", "#include \"base_inc\"\nint Helper() { return 1; }", old);
            Compiled("one", DateTime.UtcNow.AddHours(-1));
            Compiled("two", DateTime.UtcNow.AddHours(-1));

            Source("base_inc", "int Base() { return 1; }", DateTime.UtcNow);

            var graph = ScriptIncludeGraph.Build(_nss);
            var plan = ScriptIncludeRebuildPlanner.Create(_nss, "base_inc");

            plan.Dependents.Should().BeEquivalentTo(graph.TransitiveDependents("base_inc"));

            var stale = Scan();
            stale.Select(s => s.ResRef).Should().BeEquivalentTo(new[] { "one", "two" });
            stale.Select(s => s.ResRef).Should().OnlyContain(resRef => plan.Dependents.Contains(resRef));

            foreach (var entry in stale)
                Compiled(entry.ResRef, DateTime.UtcNow.AddMinutes(1));

            Scan().Should().BeEmpty();
        }
    }

    /// <summary>Project-wide script search.</summary>
    public class ScriptWorkspaceSearchTests
    {
        [Test]
        public void RealModuleCorpus_FindsKnownIdentifierWhereItIsUsed()
        {
            var search = new ScriptWorkspaceSearch(Path.Combine(CorpusLocator.ModuleDirectory, "nss"));

            var results = search.Search("GetIsPC", ScriptSearchMode.Identifier);

            results.Should().Contain(r => r.ResRef == "dmfi_activate" && r.Line == 106);
            results.Should().Contain(r => r.ResRef == "dmfi_execute");
            results.Should().OnlyContain(r => !r.LineText.TrimStart().StartsWith("//", StringComparison.Ordinal),
                "identifier mode searches code tokens, not comments");
        }

        [Test]
        public void IdentifierMode_ExcludesStringsAndComments()
        {
            using var corpus = new TempScriptCorpus();
            corpus.Write("one", """
                void main()
                {
                    string s = "Needle";
                    // Needle
                    int Needle = 1;
                }
                """);

            var results = new ScriptWorkspaceSearch(corpus.NssDirectory)
                .Search("Needle", ScriptSearchMode.Identifier);

            results.Should().ContainSingle()
                .Which.LineText.Should().Contain("int Needle");
        }

        [Test]
        public void SubstringMode_IncludesStrings()
        {
            using var corpus = new TempScriptCorpus();
            corpus.Write("one", """
                void main()
                {
                    string s = "Needle";
                }
                """);

            var results = new ScriptWorkspaceSearch(corpus.NssDirectory)
                .Search("Needle", ScriptSearchMode.Substring);

            results.Should().ContainSingle()
                .Which.LineText.Should().Contain("\"Needle\"");
        }

        [Test]
        public void OpenBufferOverlayReplacesDiskMatchesAndLineNumbers()
        {
            using var corpus = new TempScriptCorpus();
            corpus.Write("one", "int DiskNeedle = 1;\n");
            var openBuffer = """
                void main()
                {
                    int BufferNeedle = 2;
                }
                """;
            var search = new ScriptWorkspaceSearch(
                corpus.NssDirectory,
                resRef => resRef == "one" ? openBuffer : null);

            var bufferResults = search.Search("BufferNeedle", ScriptSearchMode.Identifier);
            var diskResults = search.Search("DiskNeedle", ScriptSearchMode.Identifier);

            bufferResults.Should().ContainSingle()
                .Which.Should().Be(new ScriptSearchResult("one", 3, "    int BufferNeedle = 2;"));
            diskResults.Should().BeEmpty(
                "an open editor buffer replaces that script's stale on-disk source");
        }

        private sealed class TempScriptCorpus : IDisposable
        {
            private readonly string _root = Path.Combine(
                Path.GetTempPath(), "swlor_script_search_" + Guid.NewGuid().ToString("N"));

            public TempScriptCorpus()
            {
                NssDirectory = Path.Combine(_root, "nss");
                Directory.CreateDirectory(NssDirectory);
            }

            public string NssDirectory { get; }

            public void Write(string resRef, string source) =>
                File.WriteAllText(Path.Combine(NssDirectory, resRef + ".nss"), source);

            public void Dispose()
            {
                if (Directory.Exists(_root))
                    Directory.Delete(_root, recursive: true);
            }
        }
    }

    /// <summary>Go-to-definition, find-references and rename.</summary>
    public class ScriptNavigationTests
    {
        private static string HeaderPath => Path.Combine(
            CorpusLocator.RepositoryRoot, "SWLOR.NWN.API", "NWN", "nwscript-8193.37.nss");

        private static EngineSymbolDatabase? _db;
        private static EngineSymbolDatabase Db => _db ??=
            File.Exists(HeaderPath) ? EngineSymbolDatabase.Load(HeaderPath) : EngineSymbolDatabase.Empty;

        private static (string Source, int Caret) At(string marked)
        {
            var caret = marked.IndexOf('$');
            return (marked.Remove(caret, 1), caret);
        }

        [Test]
        public void FindsADefinitionInTheSameFile()
        {
            var (src, caret) = At("void Helper() {}\nvoid main() { Hel$per(); }");
            var def = ScriptNavigation.FindDefinition(src, caret, Db);

            def.Should().NotBeNull();
            def!.ResRef.Should().BeNull("it is in this file");
            src.Substring(def.Offset, 6).Should().Be("Helper");
        }

        [Test]
        public void FollowsAnIncludeToItsDefinition()
        {
            var (src, caret) = At("#include \"helper_inc\"\nvoid main() { Do$Thing(); }");

            var def = ScriptNavigation.FindDefinition(src, caret, Db,
                name => name == "helper_inc" ? "void DoThing() {}" : null);

            def.Should().NotBeNull();
            def!.ResRef.Should().Be("helper_inc");
        }

        [Test]
        public void RecognisesAnEngineSymbol()
        {
            if (Db.Functions.Count == 0)
                Assert.Ignore("engine header not present");

            var (src, caret) = At("void main() { int n = Ran$dom(10); }");
            var def = ScriptNavigation.FindDefinition(src, caret, Db);

            def.Should().NotBeNull();
            def!.IsEngineSymbol.Should().BeTrue();
        }

        [Test]
        public void CaretOnNonIdentifier_ResolvesToNothing()
        {
            var (src, caret) = At("void main() { int n = 1; $}");
            ScriptNavigation.FindDefinition(src, caret, Db).Should().BeNull();
        }

        [Test]
        public void ReferencesIgnoreStringsAndComments()
        {
            const string src = """
                void main()
                {
                    int nCount = 1;
                    SetLocalInt(GetModule(), "nCount", nCount);
                    // nCount is mentioned here too
                }
                """;

            var refs = ScriptNavigation.FindReferences(src, "nCount");

            // Two real uses: the declaration and the argument. The string key and the comment
            // mention are not references.
            refs.Should().HaveCount(2);
        }

        [Test]
        public void RenameLeavesStringsAndCommentsAlone()
        {
            const string src = """
                void main()
                {
                    int nCount = 1;
                    SetLocalInt(GetModule(), "nCount", nCount);
                    // nCount stays
                }
                """;

            var renamed = ScriptNavigation.Rename(src, "nCount", "nTotal");

            renamed.Should().Contain("int nTotal = 1;");
            renamed.Should().Contain("\"nCount\"", "the string key is data, not a reference");
            renamed.Should().Contain("// nCount stays");
            renamed.Should().Contain(", nTotal)");
        }

        [Test]
        public void RenameOfAnAbsentNameIsANoOp()
        {
            const string src = "void main() {}";
            ScriptNavigation.Rename(src, "nope", "other").Should().Be(src);
        }

        [TestCase("nCount", true)]
        [TestCase("_x", true)]
        [TestCase("n1", true)]
        [TestCase("1n", false)]
        [TestCase("", false)]
        [TestCase("has space", false)]
        public void ValidatesIdentifiers(string name, bool expected) =>
            ScriptNavigation.IsValidIdentifier(name).Should().Be(expected);
    }
}
