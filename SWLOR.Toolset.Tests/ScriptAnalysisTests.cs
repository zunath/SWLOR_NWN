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
