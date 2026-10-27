using FluentAssertions;
using NUnit.Framework;
using SWLOR.EngineSeamGenerator;
using static SWLOR.NWN.API.Tests.Generator.PluginGeneratorFixtures;

namespace SWLOR.NWN.API.Tests.Generator;

/// <summary>
/// The generator on small in-memory NWNX plugin sources: one seam per plugin file, bodies moved verbatim, data-only nested
/// types left in the static class, a second run that changes nothing, and nothing written when any plugin does not round-trip.
/// </summary>
[TestFixture]
public class PluginSeamGeneratorTests
{
    private static string Lf(string text) => GeneratorFixtures.Lf(text);

    [Test]
    public void Discover_FindsNWScriptAndEveryTopLevelPluginFile()
    {
        var tree = Tree();
        tree.WriteAllText("SWLOR.NWN.API/NWNX/Enum/NotAPlugin.cs", "namespace X { }\n");

        var definitions = SeamDefinition.Discover(tree);

        definitions.Select(d => d.InterfaceName).Should().Equal("INWScript", "IOtherPlugin", "ISamplePlugin");
        definitions.Skip(1).Should().OnlyContain(d => d.IsSingleFile);
    }

    [Test]
    public void PluginDefinition_NamesTheGeneratedTypesAfterThePlugin()
    {
        var definition = Sample;

        definition.StaticClassName.Should().Be("SamplePlugin");
        definition.InterfaceName.Should().Be("ISamplePlugin");
        definition.PassThroughName.Should().Be("NwnCoreSamplePlugin");
        definition.RuntimeName.Should().Be("SamplePluginRuntime");
        definition.FakeName.Should().Be("FakeSamplePlugin");
        definition.InterfaceRoot.Should().Be("SWLOR.NWN.API/Generated/NWNX/SamplePlugin/ISamplePlugin.cs");
        definition.PassThroughRoot.Should().Be("SWLOR.NWN.API/Generated/NWNX/SamplePlugin/NwnCoreSamplePlugin.cs");
        definition.RuntimeFile.Should().Be("SWLOR.NWN.API/Generated/NWNX/SamplePlugin/SamplePluginRuntime.cs");
        definition.FakeRoot.Should().Be("SWLOR.NWN.API.Testing/Generated/NWNX/SamplePlugin/FakeSamplePlugin.cs");
    }

    [Test]
    public void FirstRun_WritesFourFilesPerPlugin_AndLeavesDataTypesAlone()
    {
        var tree = Tree();
        var dataType = tree.Files[DataTypePath];

        var output = GenerateAllAndApply(tree);

        output.Errors.Should().BeEmpty();
        foreach (var definition in new[] { Sample, Other })
        {
            tree.Files.Keys.Should().Contain(new[] { definition.InterfaceRoot, definition.PassThroughRoot, definition.RuntimeFile, definition.FakeRoot });
        }

        tree.Files[DataTypePath].Should().Be(dataType, "a type that is not a plugin wrapper is not touched");
        tree.Files.Keys.Where(k => k.Contains("/Generated/NWNX/")).Should().HaveCount(8);
    }

    [Test]
    public void FirstRun_MovesBodiesVerbatim_AndRewritesTheWrappersToForward()
    {
        var tree = Tree();

        var output = GenerateAllAndApply(tree);

        output.Errors.Should().BeEmpty();
        var core = output.Files[Sample.PassThroughRoot];
        core.Should().Contain("public sealed partial class NwnCoreSamplePlugin : ISamplePlugin");
        core.Should().Contain("return global::NWN.Core.NWNX.SamplePlugin.GetCount(oObject, nOffset);");
        core.Should().Contain("int result = global::NWN.Core.NWNX.SamplePlugin.GetChannel();");
        core.Should().Contain("return (ChatChannel)result;");
        core.Should().Contain("var core = global::NWN.Core.NWNX.SamplePlugin.GetTime();");
        core.Should().Contain("bAll ? 1 : 0,", "a multi-line body moves with its exact text");
        core.Should().NotContain("SamplePluginRuntime");

        var wrapper = tree.Files[SamplePath];
        wrapper.Should().Contain("return SamplePluginRuntime.Current.GetCount(oObject, nOffset);");
        wrapper.Should().Contain("return SamplePluginRuntime.Current.GetChannel();");
        wrapper.Should().Contain("SamplePluginRuntime.Current.Reset(oObject, bAll, oOther);");
        wrapper.Should().Contain("return SamplePluginRuntime.Current.GetTime();");
        wrapper.Should().Contain("public static void Reset(\n            uint oObject,\n            bool bAll = false,\n            uint oOther = OBJECT_INVALID)",
            "a multi-line signature is not touched");
        wrapper.Should().NotContain("global::NWN.Core");
        wrapper.Should().Contain("public struct SampleTime").And.Contain("public enum SampleKind");
        Lf(wrapper).Should().Contain("/// <summary>A sample plugin.</summary>");
    }

    [Test]
    public void Interface_CarriesSignaturesDefaultsAndNestedTypes()
    {
        var output = GenerateAllAndApply(Tree());

        var contract = output.Files[Sample.InterfaceRoot];
        contract.Should().Contain("public partial interface ISamplePlugin");
        contract.Should().Contain("int GetCount(uint oObject, int nOffset = 0);");
        contract.Should().Contain("ChatChannel GetChannel();");
        contract.Should().Contain("void Reset(uint oObject, bool bAll = false, uint oOther = OBJECT_INVALID);");
        contract.Should().Contain("SampleTime GetTime();");
        contract.Should().Contain("using static SWLOR.NWN.API.NWNX.SamplePlugin;", "the nested types resolve as they do inside the wrapper");
        contract.Should().Contain("using SWLOR.Game.Server.Core.NWNX.Enum;", "the part mirrors its wrapper file's usings");
        contract.Should().Contain("/// <param name=\"oObject\">The object.</param>");
    }

    [Test]
    public void Fake_RecordsEveryMember_AndSeesTheNestedTypes()
    {
        var output = GenerateAllAndApply(Tree());

        var fake = output.Files[Sample.FakeRoot];
        fake.Should().Contain("namespace SWLOR.NWN.API.Testing");
        fake.Should().Contain("public partial class FakeSamplePlugin : FakeEngine, ISamplePlugin");
        fake.Should().Contain("public int GetCount(uint oObject, int nOffset)");
        fake.Should().Contain("return InvokeCall<int>(\"GetCount\", new object?[] { oObject, nOffset });");
        fake.Should().Contain("public SampleTime GetTime()");
        fake.Should().Contain("RecordCall(\"Reset\", new object?[] { oObject, bAll, oOther });");
        fake.Should().Contain("using static SWLOR.NWN.API.NWNX.SamplePlugin;");
        output.Files[Other.FakeRoot].Should().NotContain("using static", "a plugin without nested types needs no static import");
    }

    [Test]
    public void Runtime_IsTheSameTemplateAsNWScript_WithThePluginsNames()
    {
        var output = GenerateAllAndApply(Tree());

        var runtime = output.Files[Sample.RuntimeFile];
        runtime.Should().Contain("public static class SamplePluginRuntime");
        runtime.Should().Contain("private static readonly NwnCoreSamplePlugin Default = new();");
        runtime.Should().Contain("public static ISamplePlugin Current");
        runtime.Should().Contain("public static IDisposable Override(ISamplePlugin engine)");
        runtime.Should().Contain("namespace SWLOR.NWN.API.NWNX");
    }

    [Test]
    public void Report_CountsMembersPerPlugin_AndListsWhatStaysStatic()
    {
        var output = GenerateAllAndApply(Tree());

        output.Report.Should().Contain(line => line.StartsWith("SamplePlugin: 4 members behind ISamplePlugin (4 methods, 0 properties);") &&
                                               line.Contains("bodies moved by this run 4") && line.EndsWith("left static: SampleTime, SampleKind."));
        output.Report.Should().Contain(line => line.StartsWith("OtherPlugin: 1 members behind IOtherPlugin") && line.EndsWith("left static: nothing."));
        output.Report.Should().Contain("NWNX plugins: 2 seams, 8 generated files (interface, pass-through, runtime and fake per plugin).");
    }

    [Test]
    public void SecondRun_ChangesNothing()
    {
        var tree = Tree();
        GenerateAllAndApply(tree);
        var snapshot = tree.Files.ToDictionary(f => f.Key, f => f.Value);

        var second = GenerateAllAndApply(tree);

        second.Errors.Should().BeEmpty();
        second.RewrittenWrappers.Should().BeEmpty();
        second.Report.Should().Contain(line => line.StartsWith("SamplePlugin:") && line.Contains("bodies moved by this run 0, already moved 4"));
        tree.Files.Should().BeEquivalentTo(snapshot);
        SeamGenerator.Compare(second, tree).Should().BeEmpty();
    }

    [Test]
    public void Output_IsDeterministic()
    {
        var first = GenerateAllAndApply(Tree());
        var second = GenerateAllAndApply(Tree());

        second.Files.Should().BeEquivalentTo(first.Files);
        second.Files.Keys.Should().Equal(first.Files.Keys);
    }

    [Test]
    public void NewBodyAddedAfterTheFirstRun_IsMoved_AndMovedBodiesStay()
    {
        var tree = Tree();
        GenerateAllAndApply(tree);
        tree.WriteAllText(SamplePath, tree.Files[SamplePath].Replace("public static ChatChannel GetChannel()", Lf("""
            public static int GetLevel(uint oObject)
                    {
                        return global::NWN.Core.NWNX.SamplePlugin.GetLevel(oObject);
                    }

                    public static ChatChannel GetChannel()
            """)));

        var output = GenerateAllAndApply(tree);

        output.Errors.Should().BeEmpty();
        output.Report.Should().Contain(line => line.StartsWith("SamplePlugin: 5 members") && line.Contains("bodies moved by this run 1, already moved 4"));
        output.Files[Sample.PassThroughRoot].Should().Contain("GetLevel(oObject);").And.Contain("return (ChatChannel)result;");
        output.Files[Sample.InterfaceRoot].Should().Contain("int GetLevel(uint oObject);");
        tree.Files[SamplePath].Should().Contain("return SamplePluginRuntime.Current.GetLevel(oObject);");
    }

    [Test]
    public void PluginFileRemoved_DropsItsGeneratedFiles_AndLeavesTheOtherPluginsAlone()
    {
        var tree = Tree();
        GenerateAllAndApply(tree);
        tree.Delete(OtherPath);

        var output = GenerateAllAndApply(tree);

        output.Errors.Should().BeEmpty();
        tree.Files.Keys.Should().NotContain(new[] { Other.InterfaceRoot, Other.PassThroughRoot, Other.RuntimeFile, Other.FakeRoot },
            "stale generated files of a removed plugin are deleted");
        tree.Files.Keys.Should().Contain(new[] { Sample.InterfaceRoot, Sample.PassThroughRoot, Sample.RuntimeFile, Sample.FakeRoot });
    }

    [Test]
    public void PluginFileAdded_GetsASeamOnTheNextRun()
    {
        var tree = Tree();
        tree.Delete(OtherPath);
        GenerateAllAndApply(tree);
        tree.WriteAllText(OtherPath, Lf(OtherPlugin) + "\n");

        var output = GenerateAllAndApply(tree);

        output.Errors.Should().BeEmpty();
        tree.Files.Keys.Should().Contain(new[] { Other.InterfaceRoot, Other.PassThroughRoot, Other.RuntimeFile, Other.FakeRoot });
        tree.Files[OtherPath].Should().Contain("OtherPluginRuntime.Current.GetName(oObject)");
    }

    [Test]
    public void StaleFileInAGeneratedFolder_IsReportedByCompare_AndDeletedByApply()
    {
        var tree = Tree();
        GenerateAllAndApply(tree);
        tree.WriteAllText("SWLOR.NWN.API.Testing/Generated/NWNX/GonePlugin/FakeGonePlugin.cs", "// left over\n");

        var output = SeamGenerator.GenerateAll(tree, tree);

        SeamGenerator.Compare(output, tree).Should().Equal("stale: SWLOR.NWN.API.Testing/Generated/NWNX/GonePlugin/FakeGonePlugin.cs");
        SeamGenerator.Apply(output, tree);
        tree.Files.Keys.Should().NotContain("SWLOR.NWN.API.Testing/Generated/NWNX/GonePlugin/FakeGonePlugin.cs");
    }

    [Test]
    public void UpstreamFileTakenOnASync_WinsOverTheBodyMovedEarlier()
    {
        var tree = Tree();
        GenerateAllAndApply(tree);
        tree.WriteAllText(OtherPath, Lf(OtherPlugin).Replace("GetName(oObject);", "GetName(oObject) + \"!\";") + "\n");

        var output = GenerateAllAndApply(tree);

        output.Errors.Should().BeEmpty();
        output.Files[Other.PassThroughRoot].Should().Contain("GetName(oObject) + \"!\";");
        tree.Files[OtherPath].Should().Contain("return OtherPluginRuntime.Current.GetName(oObject);");
    }

    [TestCase("public struct Bad { public int Total() { return 1; } }", "Bad")]
    [TestCase("public struct Bad { public int Total => 1; }", "Bad")]
    [TestCase("public struct Bad { public int Total { get { return 1; } } }", "Bad")]
    [TestCase("public struct Bad { public int Total = 5; }", "Bad")]
    [TestCase("public struct Bad { public Bad(int total) { Total = total; } public int Total; }", "Bad")]
    [TestCase("public class Bad<T> { public T Value; }", "Bad")]
    [TestCase("private struct Bad { public int Total; }", "Bad")]
    [TestCase("internal struct Bad { public int Total; }", "Bad")]
    [TestCase("[System.Obsolete] public struct Bad { public int Total; }", "Bad")]
    [TestCase("public static class Bad { public static int Total; }", "Bad")]
    public void NestedTypeThatHoldsCode_IsReported_AndNothingIsGenerated(string nested, string name)
    {
        var tree = Tree();
        tree.WriteAllText(SamplePath, Lf(SamplePlugin).Replace("public struct SampleTime", nested + "\n\n        public struct SampleTime") + "\n");

        var output = SeamGenerator.GenerateAll(tree, tree);

        output.Errors.Should().Contain(e => e.Contains(name));
        output.Report.Should().NotContain(line => line.StartsWith("SamplePlugin:"));
    }

    [TestCase("public static T Echo<T>(T value) { return value; }", "Echo")]
    [TestCase("public static void Out(out int value) { value = 0; }", "Out")]
    [TestCase("public static int Quick(int value) => value;", "Quick")]
    [TestCase("public static int Counter;", "Counter")]
    [TestCase("public static int Setter { get; set; }", "Setter")]
    [TestCase("private static void Hidden() { }", "Hidden")]
    [TestCase("public void Instance() { }", "Instance")]
    [TestCase("[System.Obsolete] public static void Old() { }", "Old")]
    public void UnsupportedMember_IsReported_AndNothingIsGenerated(string member, string name)
    {
        var tree = Tree();
        tree.WriteAllText(SamplePath, Lf(SamplePlugin).Replace("public static int GetCount", member + "\n\n        public static int GetCount") + "\n");

        var output = SeamGenerator.GenerateAll(tree, tree);

        output.Errors.Should().Contain(e => e.Contains(name));
        output.Report.Should().NotContain(line => line.StartsWith("SamplePlugin:"));
    }

    [TestCase("public static partial class SamplePlugin", "public static class SamplePlugin")]
    [TestCase("public sealed class SamplePlugin", "public static class SamplePlugin")]
    [TestCase("public static class OtherNamePlugin", "public static class SamplePlugin")]
    public void ClassThatIsNotThePluginsPublicStaticClass_IsReported(string replacement, string original)
    {
        var tree = Tree();
        tree.WriteAllText(SamplePath, Lf(SamplePlugin).Replace(original, replacement) + "\n");

        var output = SeamGenerator.GenerateAll(tree, tree);

        output.Errors.Should().Contain(e => e.Contains("public static class SamplePlugin"));
    }

    [Test]
    public void NestedTypesAreNotAcceptedInTheNWScriptWrappers()
    {
        var tree = Tree();
        tree.WriteAllText(GeneratorFixtures.FirstPath, GeneratorFixtures.Lf(GeneratorFixtures.First).Replace("public const int SampleConstant = 5;",
            "public struct Result { public int Value; }") + "\n");

        var output = SeamGenerator.GenerateAll(tree, tree);

        output.Errors.Should().Contain(e => e.Contains("Result"));
    }

    [Test]
    public void OneBadPlugin_StopsTheWholeRun_AndApplyWritesNothing()
    {
        var tree = Tree();
        tree.WriteAllText(OtherPath, Lf(OtherPlugin).Replace("public static string GetName", "public static T Echo<T>(T v) { return v; }\n\n        public static string GetName") + "\n");
        var before = tree.Files.ToDictionary(f => f.Key, f => f.Value);

        var output = SeamGenerator.GenerateAll(tree, tree);
        var apply = () => SeamGenerator.Apply(output, tree);

        output.Errors.Should().Contain(e => e.Contains("Echo"));
        apply.Should().Throw<InvalidOperationException>();
        tree.Files.Should().BeEquivalentTo(before);
    }

    [Test]
    public void MovedBodyThatDiffersFromTheOriginal_FailsTheRoundTrip()
    {
        var tree = Tree();

        var output = SeamGenerator.Generate(tree, tree, Sample, files =>
        {
            files[Sample.PassThroughRoot] = files[Sample.PassThroughRoot].Replace("SamplePlugin.GetCount(oObject, nOffset);", "SamplePlugin.GetCount(oObject, nOffset + 1);");
        });

        output.Errors.Should().Contain(e => e.Contains("GetCount(uint,int)") && e.Contains("not textually identical to the original"));
    }

    [Test]
    public void NestedTypeChangedByTheRewrite_FailsTheRoundTrip()
    {
        var tree = Tree();

        var output = SeamGenerator.Generate(tree, tree, Sample, files =>
        {
            files[SamplePath] = files[SamplePath].Replace("public int Day { get; set; }", "public long Day { get; set; }");
        });

        output.Errors.Should().NotBeEmpty();
    }

    [Test]
    public void ForwardingBodyWithoutAPreviousOutput_IsAnError()
    {
        var tree = Tree();
        GenerateAllAndApply(tree);
        tree.Delete(Sample.PassThroughRoot);

        var output = SeamGenerator.GenerateAll(tree, tree);

        output.Errors.Should().Contain(e => e.Contains("already forwards to SamplePluginRuntime") && e.Contains("has no body"));
    }

    [Test]
    public void Runner_AppliesAndChecksAllSeamsTogether()
    {
        var root = Path.Combine(Path.GetTempPath(), "seam-generator-" + Guid.NewGuid().ToString("N"));
        try
        {
            var files = new DiskFileSystem(root);
            foreach (var (path, content) in Tree().Files) files.WriteAllText(path, content);

            GeneratorRunner.Run(root, GeneratorMode.Check, null, new StringWriter()).Should().Be(GeneratorRunner.Differs);
            GeneratorRunner.Run(root, GeneratorMode.Apply, null, new StringWriter()).Should().Be(GeneratorRunner.Ok);
            GeneratorRunner.Run(root, GeneratorMode.Check, null, new StringWriter()).Should().Be(GeneratorRunner.Ok);

            files.WriteAllText(Sample.FakeRoot, "// edited by hand\n");
            var log = new StringWriter();
            GeneratorRunner.Run(root, GeneratorMode.Check, null, log).Should().Be(GeneratorRunner.Differs);
            log.ToString().Should().Contain("differs: " + Sample.FakeRoot);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Runner_WritesNothing_WhenAPluginIsUnsupported()
    {
        var root = Path.Combine(Path.GetTempPath(), "seam-generator-" + Guid.NewGuid().ToString("N"));
        try
        {
            var files = new DiskFileSystem(root);
            foreach (var (path, content) in Tree().Files) files.WriteAllText(path, content);
            files.WriteAllText(OtherPath, Lf(OtherPlugin).Replace("public static string GetName", "public static T Echo<T>(T v) { return v; }\n\n        public static string GetName") + "\n");
            var before = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).ToDictionary(f => f, File.ReadAllText);
            var log = new StringWriter();

            var exit = GeneratorRunner.Run(root, GeneratorMode.Apply, null, log);

            exit.Should().Be(GeneratorRunner.Failed);
            log.ToString().Should().Contain("nothing was written").And.Contain("Echo");
            Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).ToDictionary(f => f, File.ReadAllText).Should().BeEquivalentTo(before);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
