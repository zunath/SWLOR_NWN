using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Script;
using SWLOR.Toolset.Domain.Script.Symbols;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Completion ranking, tested as "caret position + source → expected ordered items". Ranking is
    /// the easiest thing in the language service to get subtly wrong and the hardest to notice, which
    /// is why it lives in Domain rather than behind the editor control.
    /// </summary>
    public class ScriptCompletionEngineTests
    {
        private static string HeaderPath => Path.Combine(
            CorpusLocator.RepositoryRoot, "SWLOR.NWN.API", "NWN", "nwscript-8193.37.nss");

        private static EngineSymbolDatabase? _db;
        private static EngineSymbolDatabase Db => _db ??= EngineSymbolDatabase.Load(HeaderPath);

        private static ScriptCompletionEngine Engine => new(Db);

        /// <summary>Source with "$" marking the caret, which keeps the tests readable.</summary>
        private static (string Source, int Caret) At(string marked)
        {
            var caret = marked.IndexOf('$');
            return (marked.Remove(caret, 1), caret);
        }

        [OneTimeSetUp]
        public void RequireHeader()
        {
            if (!File.Exists(HeaderPath))
                Assert.Ignore("engine header not present");
        }

        // ---- context detection ----

        [Test]
        public void CaretInsideACall_IsAnArgumentContext()
        {
            var (src, caret) = At("void main() { object o = GetNearestCreature($); }");
            var ctx = ScriptCompletionEngine.DescribeContext(src, caret);

            ctx.Kind.Should().Be(CompletionContextKind.Argument);
            ctx.FunctionName.Should().Be("GetNearestCreature");
            ctx.ArgumentIndex.Should().Be(0);
        }

        [Test]
        public void CommasAdvanceTheArgumentIndex()
        {
            var (src, caret) = At("void main() { GetNearestCreature(CREATURE_TYPE_PLAYER_CHAR, $); }");
            var ctx = ScriptCompletionEngine.DescribeContext(src, caret);

            ctx.ArgumentIndex.Should().Be(1);
        }

        [Test]
        public void NestedCallsResolveToTheInnermost()
        {
            var (src, caret) = At("void main() { SetLocalInt(GetModule(), \"x\", Random($)); }");
            var ctx = ScriptCompletionEngine.DescribeContext(src, caret);

            ctx.FunctionName.Should().Be("Random");
            ctx.ArgumentIndex.Should().Be(0);
        }

        [Test]
        public void ACompletedCallIsNotAnArgumentContext()
        {
            var (src, caret) = At("void main() { int n = Random(10); $ }");
            var ctx = ScriptCompletionEngine.DescribeContext(src, caret);

            ctx.Kind.Should().Be(CompletionContextKind.General);
        }

        [Test]
        public void AnIfConditionIsNotACall()
        {
            var (src, caret) = At("void main() { if ($) return; }");
            var ctx = ScriptCompletionEngine.DescribeContext(src, caret);

            // "if" is a keyword token, not an identifier, so this must not read as calling if().
            ctx.Kind.Should().Be(CompletionContextKind.General);
        }

        [Test]
        public void CaretInsideAnIncludeString_IsAnIncludeContext()
        {
            var (src, caret) = At("#include \"dmfi_$\"\nvoid main() {}");
            var ctx = ScriptCompletionEngine.DescribeContext(src, caret);

            ctx.Kind.Should().Be(CompletionContextKind.IncludePath);
            ctx.Prefix.Should().Be("dmfi_");
        }

        [Test]
        public void CaretInsideAnOrdinaryString_OffersNothing()
        {
            var (src, caret) = At("void main() { SetLocalInt(GetModule(), \"some te$xt\", 1); }");
            var items = Engine.GetCompletions(src, caret);

            items.Should().BeEmpty("an identifier list while typing prose is pure noise");
        }

        // ---- the headline feature ----

        [Test]
        public void ArgumentWithADocumentedFamily_OffersThatFamilyFirst()
        {
            var (src, caret) = At("void main() { object o = GetNearestCreature($); }");
            var items = Engine.GetCompletions(src, caret);

            var familySize = Db.ConstantsInFamily("CREATURE_TYPE_*").Count;
            familySize.Should().BeGreaterThan(5).And.BeLessThan(50,
                "the whole point is that this is a short list, not all 6,201 constants");

            items.Should().NotBeEmpty();
            items[0].Kind.Should().Be(CompletionItemKind.Constant);
            items.Take(familySize).Should().OnlyContain(i => i.Text.StartsWith("CREATURE_TYPE_"),
                "argument 1 documents CREATURE_TYPE_*, so the whole family leads");
        }

        [Test]
        public void TypingAPrefixInsideThatArgument_KeepsTheFamilyOnTop()
        {
            var (src, caret) = At("void main() { object o = GetNearestCreature(CREATURE_TYPE_P$); }");
            var items = Engine.GetCompletions(src, caret);

            items[0].Text.Should().Be("CREATURE_TYPE_PLAYER_CHAR");
        }

        [Test]
        public void LocalsOutrankEngineConstants()
        {
            var (src, caret) = At("void main() { int nCount = 0; int x = nCou$; }");
            var items = Engine.GetCompletions(src, caret);

            items[0].Text.Should().Be("nCount");
            items[0].Kind.Should().Be(CompletionItemKind.Variable);
        }

        [Test]
        public void ParametersAreInScopeInTheBody()
        {
            var (src, caret) = At("void dmw_CleanUp(object oMySpeaker) { DeleteLocalObject(oMySp$); }");
            var items = Engine.GetCompletions(src, caret);

            items.Should().Contain(i => i.Text == "oMySpeaker" && i.Kind == CompletionItemKind.Variable);
        }

        [Test]
        public void FunctionsDeclaredInTheFileAreOffered()
        {
            var (src, caret) = At("void dmw_CleanUp(object o) {}\nvoid main() { dmw_$ }");
            var items = Engine.GetCompletions(src, caret);

            items.Should().Contain(i => i.Text == "dmw_CleanUp" && i.Kind == CompletionItemKind.LocalFunction);
        }

        // ---- matching ----

        [Test]
        public void SubsequenceMatching_FindsLongNamesFromInitials()
        {
            var (src, caret) = At("void main() { object o = gnc$; }");
            var items = Engine.GetCompletions(src, caret);

            items.Should().Contain(i => i.Text == "GetNearestCreature",
                "NWScript names are long; typing them out defeats the purpose");
        }

        [Test]
        public void PrefixMatchesOutrankSubstringMatches()
        {
            var (src, caret) = At("void main() { GetNearest$ }");
            var items = Engine.GetCompletions(src, caret);

            items[0].Text.Should().StartWith("GetNearest");
        }

        [Test]
        public void IncludeContext_OffersResrefsAndPutsIncludeHeadersFirst()
        {
            var engine = Engine;
            engine.AvailableIncludes = new[] { "zep_monsterspawn", "dmfi_init_inc", "colors_inc" };

            var (src, caret) = At("#include \"$\"\nvoid main() {}");
            var items = engine.GetCompletions(src, caret);

            items.Should().HaveCount(3);
            items[0].Text.Should().EndWith("_inc", "an #include almost always wants a header");
        }

        [Test]
        public void HalfTypedSourceNeverThrows()
        {
            const string full = "void main()\n{\n    object o = GetNearestCreature(CREATURE_TYPE_PLAYER_CHAR,\n";
            for (var cut = 0; cut <= full.Length; cut++)
            {
                var src = full[..cut];
                var act = () => Engine.GetCompletions(src, src.Length);
                act.Should().NotThrow("completion runs against broken code constantly (cut {0})", cut);
            }
        }
    }

    /// <summary>Signature help resolution for a caret inside a call.</summary>
    public class ScriptSignatureHelpTests
    {
        private static string HeaderPath => Path.Combine(
            CorpusLocator.RepositoryRoot, "SWLOR.NWN.API", "NWN", "nwscript-8193.37.nss");

        private static EngineSymbolDatabase? _db;
        private static EngineSymbolDatabase Db => _db ??= EngineSymbolDatabase.Load(HeaderPath);

        private static (string Source, int Caret) At(string marked)
        {
            var caret = marked.IndexOf('$');
            return (marked.Remove(caret, 1), caret);
        }

        [OneTimeSetUp]
        public void RequireHeader()
        {
            if (!File.Exists(HeaderPath))
                Assert.Ignore("engine header not present");
        }

        [Test]
        public void ResolvesTheFunctionAndActiveParameter()
        {
            var (src, caret) = At("void main() { GetNearestCreature(CREATURE_TYPE_PLAYER_CHAR, $); }");
            var help = new ScriptSignatureHelpEngine(Db).GetSignatureHelp(src, caret);

            help.Should().NotBeNull();
            help!.Function.Name.Should().Be("GetNearestCreature");
            help.ActiveParameter.Should().Be(1);
            help.Active!.Name.Should().Be("nFirstCriteriaValue");
            help.PositionLabel.Should().Be("argument 2 of 8");
        }

        [Test]
        public void OutsideACall_ReturnsNull()
        {
            var (src, caret) = At("void main() { int n = 1; $ }");
            new ScriptSignatureHelpEngine(Db).GetSignatureHelp(src, caret).Should().BeNull();
        }

        [Test]
        public void UnknownFunction_ReturnsNull()
        {
            var (src, caret) = At("void main() { NotARealEngineFunction($); }");
            new ScriptSignatureHelpEngine(Db).GetSignatureHelp(src, caret).Should().BeNull();
        }
    }
}
