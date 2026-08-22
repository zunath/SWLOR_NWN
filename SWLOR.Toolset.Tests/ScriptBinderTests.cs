using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Script;
using SWLOR.Toolset.Domain.Script.Symbols;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Unknown-identifier reporting, and the guard that makes it safe. The binder must stay silent
    /// whenever it cannot see the whole include set — a false error on valid legacy code is a worse
    /// outcome than no error at all.
    /// </summary>
    public class ScriptBinderTests
    {
        private static string HeaderPath => Path.Combine(
            CorpusLocator.RepositoryRoot, "SWLOR.NWN.API", "NWN", "nwscript-8193.37.nss");

        private static EngineSymbolDatabase? _db;
        private static EngineSymbolDatabase Db => _db ??=
            File.Exists(HeaderPath) ? EngineSymbolDatabase.Load(HeaderPath) : EngineSymbolDatabase.Empty;

        [OneTimeSetUp]
        public void RequireHeader()
        {
            if (!File.Exists(HeaderPath))
                Assert.Ignore("engine header not present");
        }

        private static ScriptBinder Binder(params (string Name, string Source)[] includes)
        {
            var map = includes.ToDictionary(i => i.Name, i => i.Source, StringComparer.OrdinalIgnoreCase);
            return new ScriptBinder(Db, name => map.TryGetValue(name, out var text) ? text : null);
        }

        [Test]
        public void ScopeGathersNamesFromTheWholeIncludeChain()
        {
            var binder = Binder(
                ("mid_inc", "#include \"base_inc\"\nint Mid() { return 1; }"),
                ("base_inc", "int Base() { return 1; }"));

            var scope = binder.BuildScope("#include \"mid_inc\"\nvoid main() {}");

            scope.Complete.Should().BeTrue();
            scope.Functions.Should().Contain(new[] { "Mid", "Base", "main" });
        }

        [Test]
        public void AnUnresolvedIncludeMakesTheScopeIncomplete()
        {
            var binder = Binder();
            var scope = binder.BuildScope("#include \"nw_i0_generic\"\nvoid main() {}");

            scope.Complete.Should().BeFalse();
            scope.MissingIncludes.Should().Contain("nw_i0_generic");
        }

        /// <summary>
        /// The guard that matters most. Sixteen of this module's scripts include base-game headers
        /// that live only in an NWN install, so an incomplete scope is the normal case for a builder
        /// without one. Reporting there would produce hundreds of false errors.
        /// </summary>
        [Test]
        public void NothingIsReportedWhenAnIncludeCannotBeResolved()
        {
            var binder = Binder();

            var diagnostics = binder.FindUnknownIdentifiers(
                "#include \"nw_i0_generic\"\nvoid main() { TotallyUnknownThing(); }");

            diagnostics.Should().BeEmpty("an unresolved include means the name might be perfectly valid");
        }

        [Test]
        public void AnUnknownNameIsReportedWhenTheScopeIsComplete()
        {
            var binder = Binder(("helper_inc", "void Known() {}"));

            var diagnostics = binder.FindUnknownIdentifiers(
                "#include \"helper_inc\"\nvoid main() { Known(); NotDefinedAnywhere(); }");

            diagnostics.Should().ContainSingle()
                .Which.Message.Should().Contain("NotDefinedAnywhere");
        }

        [Test]
        public void EngineFunctionsAndConstantsResolve()
        {
            var binder = Binder();

            binder.FindUnknownIdentifiers(
                    "void main() { object o = GetNearestCreature(CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_IS_PC); }")
                .Should().BeEmpty();
        }

        [Test]
        public void LocalsAndParametersResolve()
        {
            var binder = Binder();

            binder.FindUnknownIdentifiers(
                    "void Helper(object oPC) { int nCount = 1; SetLocalInt(oPC, \"x\", nCount); }\nvoid main() { Helper(OBJECT_SELF); }")
                .Should().BeEmpty();
        }

        [Test]
        public void ANameIsReportedOnceHoweverOftenItIsUsed()
        {
            var binder = Binder();

            var diagnostics = binder.FindUnknownIdentifiers(
                "void main() { Missing(); Missing(); Missing(); }");

            // A variable used ten times is one mistake, not ten.
            diagnostics.Should().ContainSingle();
        }

        [Test]
        public void UnknownScreamingCaseIsNotReported()
        {
            var binder = Binder();

            // Module includes define their own constants; casing is the only signal available, and
            // guessing wrong here would be a false positive on real code.
            binder.FindUnknownIdentifiers("void main() { int n = SOME_MODULE_CONSTANT; }")
                .Should().BeEmpty();
        }

        [Test]
        public void StructMemberAccessIsNotReported()
        {
            var binder = Binder();

            // Member resolution needs a type model this pass does not have.
            binder.FindUnknownIdentifiers("void main() { vector v = GetPosition(OBJECT_SELF); float f = v.x; }")
                .Should().BeEmpty();
        }

        /// <summary>
        /// Found by the corpus gate, not by reasoning: <c>int i, iBegin, iEnd;</c> in
        /// dmfi_arrays_inc.nss recorded only the first name, so the other two looked undefined.
        /// Multi-declarator statements are common in this module's legacy scripts.
        /// </summary>
        [Test]
        public void MultiVariableDeclarationsIntroduceEveryName()
        {
            var binder = Binder();

            binder.FindUnknownIdentifiers(
                    "void main() { int i, iBegin, iEnd; iBegin = 0; iEnd = 1; i = iBegin + iEnd; }")
                .Should().BeEmpty();
        }

        [Test]
        public void MultiDeclaratorsWithInitialisersAlsoResolve()
        {
            var binder = Binder();

            binder.FindUnknownIdentifiers(
                    "void main() { int nFirst = Random(3), nSecond = 2, nThird; nThird = nFirst + nSecond; }")
                .Should().BeEmpty();
        }

        [Test]
        public void CyclicIncludesTerminate()
        {
            var binder = Binder(
                ("a_inc", "#include \"b_inc\"\nint A() { return 1; }"),
                ("b_inc", "#include \"a_inc\"\nint B() { return 1; }"));

            var scope = binder.BuildScope("#include \"a_inc\"\nvoid main() {}");

            scope.Functions.Should().Contain(new[] { "A", "B" });
        }

        /// <summary>
        /// The real gate. Every module script must produce no unknown-identifier finding — either
        /// because everything resolves, or because an include does not and the binder stays quiet.
        /// Either way a builder must never see a false error on shipped code.
        /// </summary>
        [Test]
        public void NoModuleScriptProducesAFalseUnknownIdentifier()
        {
            var nss = Path.Combine(CorpusLocator.ModuleDirectory, "nss");
            var sources = Directory.EnumerateFiles(nss, "*.nss")
                .ToDictionary(
                    Path.GetFileNameWithoutExtension,
                    p => ScriptTextDocument.Load(p).Text,
                    StringComparer.OrdinalIgnoreCase);

            var binder = new ScriptBinder(Db, name => sources.TryGetValue(name, out var text) ? text : null);
            var offenders = new List<string>();

            foreach (var (resRef, source) in sources)
            {
                foreach (var diagnostic in binder.FindUnknownIdentifiers(source))
                    offenders.Add($"{resRef}.nss({diagnostic.Line}): {diagnostic.Message}");
            }

            offenders.Should().BeEmpty(
                "these are shipped, working scripts - any finding here is the binder's bug");
        }
    }
}
