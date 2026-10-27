using System.Runtime.Loader;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// The manifest file format and the ordering rule, without reflection: what <see cref="ScriptHandlerManifest"/> accepts,
/// refuses and decides, and that the manifest the server embeds is the file in the repository.
/// </summary>
[TestFixture]
public class ScriptHandlerManifestFormatTests
{
    private static string Lines(params string[] lines) => string.Join("\n", lines) + "\n";

    [Test]
    public void ParseReadsScriptsKindsAndOrderAndIgnoresCommentsAndSectionHeadings()
    {
        var manifest = ScriptHandlerManifest.Parse(Lines(
            "# SWLOR script handler order",
            "# A comment",
            "",
            "== BOOT BUCKETS (in the order the server reaches them) ==",
            "",
            "mod_preload  # run by the engine before the module loads",
            "  action      A.B.Second",
            "  action      A.B.First",
            "",
            "== OTHER SCRIPTS ==",
            "",
            "crea_hb_aft",
            "  action      A.C.Beat",
            "  conditional A.C.Gate",
            "  conditional A.C.Check",
            "",
            "empty_script",
            "  (no handlers)"), "test");

        manifest.GetHandlers("mod_preload", false).Should().Equal("A.B.Second", "A.B.First");
        manifest.GetHandlers("mod_preload", true).Should().BeEmpty();
        manifest.GetHandlers("crea_hb_aft", false).Should().Equal("A.C.Beat");
        manifest.GetHandlers("crea_hb_aft", true).Should().Equal("A.C.Gate", "A.C.Check");
        manifest.GetHandlers("empty_script", false).Should().BeEmpty();
        manifest.GetHandlers("not_listed", false).Should().BeEmpty();
        manifest.ScriptNames.Should().BeEquivalentTo("mod_preload", "crea_hb_aft");
    }

    [Test]
    public void ParseAcceptsWindowsLineEndings()
    {
        var manifest = ScriptHandlerManifest.Parse(
            Lines("mod_preload", "  action      A.B.First", "  action      A.B.Second").Replace("\n", "\r\n"), "test");

        manifest.GetHandlers("mod_preload", false).Should().Equal("A.B.First", "A.B.Second");
    }

    [TestCase("  action      A.B.First\n", "line 1", "before any script name", TestName = "ParseRejectsAHandlerBeforeAnyScript")]
    [TestCase("mod_preload\n  routine     A.B.First\n", "line 2", "'routine' is not 'action' or 'conditional'", TestName = "ParseRejectsAnUnknownKind")]
    [TestCase("mod_preload\n  action\n", "line 2", "is not '<kind> <Namespace.Type.Method>'", TestName = "ParseRejectsAHandlerLineWithoutAName")]
    [TestCase("mod_preload\n  action      A.B.First extra\n", "line 2", "is not '<kind> <Namespace.Type.Method>'", TestName = "ParseRejectsAHandlerNameWithSpaces")]
    [TestCase("mod_preload\n  action      A.B.First\nmod_preload\n  action      A.B.Second\n", "line 3", "'mod_preload' is listed more than once", TestName = "ParseRejectsAScriptListedTwice")]
    [TestCase("mod_preload\n  action      A.B.First\nnot a script\n", "line 3", "is not a script name", TestName = "ParseRejectsALineThatIsNeitherAScriptNorAHandler")]
    public void ParseRefusesMalformedText(string text, string line, string problem)
    {
        var parse = () => ScriptHandlerManifest.Parse(text, "the-source");

        parse.Should().Throw<InvalidDataException>()
            .Which.Message.Should().Contain("the-source").And.Contain(line).And.Contain(problem);
    }

    [TestCase("", TestName = "ParseRefusesAnEmptyManifest")]
    [TestCase("# only a comment\n\nmod_preload\n  (no handlers)\n", TestName = "ParseRefusesAManifestThatListsNoHandlers")]
    public void ParseRefusesAManifestWithNoHandlers(string text)
    {
        var parse = () => ScriptHandlerManifest.Parse(text, "the-source");

        parse.Should().Throw<InvalidDataException>().Which.Message.Should().Contain("lists no handlers");
    }

    [Test]
    public void EmptyListsNothingSoEveryHandlerIsUnlistedInDiscoveryOrder()
    {
        var manifest = ScriptHandlerManifest.Empty;

        manifest.ScriptNames.Should().BeEmpty();
        var arranged = manifest.Arrange("mod_preload", false, new[] { "A.First", "A.Second", "A.Third" });
        arranged.Order.Should().Equal(0, 1, 2);
        arranged.Unlisted.Should().Equal(0, 1, 2);
        arranged.Stale.Should().BeEmpty();
    }

    [Test]
    public void ArrangeRunsListedHandlersInManifestOrderWhateverTheDiscoveryOrder()
    {
        var manifest = ScriptHandlerManifest.Parse(Lines("s", "  action      C", "  action      A", "  action      B"), "test");

        foreach (var discovered in new[] { new[] { "A", "B", "C" }, new[] { "C", "B", "A" }, new[] { "B", "C", "A" } })
        {
            var arranged = manifest.Arrange("s", false, discovered);

            arranged.Order.Select(i => discovered[i]).Should().Equal("C", "A", "B");
            arranged.Unlisted.Should().BeEmpty();
            arranged.Stale.Should().BeEmpty();
        }
    }

    [Test]
    public void ArrangeRunsUnlistedHandlersAfterTheListedOnesInDiscoveryOrder()
    {
        var manifest = ScriptHandlerManifest.Parse(Lines("s", "  action      B"), "test");

        var discovered = new[] { "A", "B", "C", "D" };
        var arranged = manifest.Arrange("s", false, discovered);

        arranged.Order.Select(i => discovered[i]).Should().Equal("B", "A", "C", "D");
        arranged.Unlisted.Select(i => discovered[i]).Should().Equal("A", "C", "D");

        var reversed = new[] { "D", "C", "B", "A" };
        var arrangedReversed = manifest.Arrange("s", false, reversed);
        arrangedReversed.Order.Select(i => reversed[i]).Should().Equal("B", "D", "C", "A");
    }

    [Test]
    public void ArrangeReportsListedHandlersThatWereNotFoundAsStaleAndIgnoresThem()
    {
        var manifest = ScriptHandlerManifest.Parse(Lines("s", "  action      Gone", "  action      A", "  action      AlsoGone"), "test");

        var arranged = manifest.Arrange("s", false, new[] { "A" });

        arranged.Order.Should().Equal(0);
        arranged.Unlisted.Should().BeEmpty();
        arranged.Stale.Should().Equal("Gone", "AlsoGone");
    }

    [Test]
    public void ArrangeMatchesANameListedTwiceToDiscoveredHandlersOneByOne()
    {
        // A method with the attribute twice for one script, or two overloads, appears twice under one name.
        var manifest = ScriptHandlerManifest.Parse(Lines("s", "  action      A", "  action      B", "  action      A"), "test");

        var discovered = new[] { "A", "A", "B" };
        var arranged = manifest.Arrange("s", false, discovered);

        arranged.Order.Should().Equal(0, 2, 1);
        arranged.Unlisted.Should().BeEmpty();
        arranged.Stale.Should().BeEmpty();

        var onlyOne = manifest.Arrange("s", false, new[] { "A", "B" });
        onlyOne.Order.Should().Equal(0, 1);
        onlyOne.Stale.Should().Equal("A");

        var three = manifest.Arrange("s", false, new[] { "A", "A", "A", "B" });
        three.Order.Should().Equal(0, 3, 1, 2);
        three.Unlisted.Should().Equal(2);
    }

    [Test]
    public void ArrangeKeepsActionAndConditionalHandlersOfAScriptApart()
    {
        var manifest = ScriptHandlerManifest.Parse(
            Lines("s", "  action      A", "  conditional B", "  conditional A"), "test");

        manifest.Arrange("s", false, new[] { "A", "B" }).Unlisted.Select(i => new[] { "A", "B" }[i]).Should().Equal("B");
        manifest.Arrange("s", true, new[] { "A", "B" }).Order.Should().Equal(1, 0);
        manifest.Arrange("other", false, new[] { "A" }).Unlisted.Should().Equal(0);
    }

    [Test]
    public void HandlerNameIsNamespaceTypeAndMethod()
    {
        ScriptHandlerManifest.HandlerName(typeof(ScriptRegistry).GetMethod(nameof(ScriptRegistry.HasScript))!)
            .Should().Be("SWLOR.Game.Server.Core.ScriptRegistry.HasScript");
    }

    [Test]
    public void TheShippedManifestIsEmbeddedInTheGameAssemblyAndKeepsDbLoadAheadOfOnModulePreload()
    {
        var manifest = ScriptHandlerManifest.LoadEmbedded(typeof(ScriptRegistry).Assembly);

        manifest.ScriptNames.Should().Contain(ScriptName.OnModulePreload);
        var preload = manifest.GetHandlers(ScriptName.OnModulePreload, false).ToList();
        preload.IndexOf("SWLOR.Game.Server.Service.DB.Load").Should().BeGreaterThanOrEqualTo(0);
        preload.IndexOf("SWLOR.Game.Server.Service.DB.Load")
            .Should().BeLessThan(preload.IndexOf("SWLOR.Game.Server.Feature.EventRegistration.OnModulePreload"),
                "DB.Load connects Redis, which OnModulePreload reads");
    }

    [Test]
    public void TheEmbeddedManifestIsTheFileInTheRepository()
    {
        // Catches a regenerated manifest that was not rebuilt into the assembly, and a manifest that stopped being embedded.
        var file = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), ScriptHandlerTestSupport.ManifestRepositoryPath.Replace('/', Path.DirectorySeparatorChar)));

        ScriptHandlerTestSupport.ShippedManifestText().Replace("\r\n", "\n")
            .Should().Be(file.Replace("\r\n", "\n"),
                "SWLOR.Game.Server embeds Core/ScriptHandlerOrder.txt; rebuild after regenerating or editing it");
        typeof(ScriptRegistry).Assembly.GetManifestResourceNames().Should().Contain(ScriptHandlerManifest.ResourceName);
    }

    [Test]
    public void TheManifestIsReadableFromTheShippedAssemblyFileLoadedIntoItsOwnLoadContext()
    {
        // The NWNX .NET host loads the deployed SWLOR.Game.Server.dll into a load context of its own, not the default one.
        var shipped = File.ReadAllBytes(typeof(ScriptRegistry).Assembly.Location);
        var context = new AssemblyLoadContext("script-handler-manifest-probe", isCollectible: true);
        try
        {
            var loaded = context.LoadFromStream(new MemoryStream(shipped));

            loaded.Should().NotBeSameAs(typeof(ScriptRegistry).Assembly);
            var manifest = ScriptHandlerManifest.LoadEmbedded(loaded);
            manifest.ScriptNames.Should().BeEquivalentTo(
                ScriptHandlerManifest.LoadEmbedded(typeof(ScriptRegistry).Assembly).ScriptNames);
            manifest.GetHandlers(ScriptName.OnModulePreload, false).Should().NotBeEmpty();
        }
        finally
        {
            context.Unload();
        }
    }

    [Test]
    public void LoadingFromAnAssemblyWithoutTheManifestFailsAndSaysWhere()
    {
        var load = () => ScriptHandlerManifest.LoadEmbedded(typeof(ScriptHandlerManifestFormatTests).Assembly);

        load.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain(ScriptHandlerManifest.ResourceName).And.Contain("not embedded");
    }

    [Test]
    public void LoadingARealManifestStillRefusesMalformedText()
    {
        // Parse is what LoadEmbedded runs on the resource, so a corrupted embedded file fails the same way.
        var parse = () => ScriptHandlerManifest.Parse("mod_preload\n  action      \n", typeof(ScriptRegistry).Assembly.GetName().Name + "!" + ScriptHandlerManifest.ResourceName);

        parse.Should().Throw<InvalidDataException>().Which.Message.Should().Contain(ScriptHandlerManifest.ResourceName);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
