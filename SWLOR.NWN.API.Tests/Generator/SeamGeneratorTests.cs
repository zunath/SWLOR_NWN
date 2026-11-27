using FluentAssertions;
using NUnit.Framework;
using SWLOR.EngineSeamGenerator;
using static SWLOR.NWN.API.Tests.Generator.GeneratorFixtures;

namespace SWLOR.NWN.API.Tests.Generator;

/// <summary>
/// The generator on small in-memory sources: it moves bodies verbatim, rewrites only the bodies of the wrappers, is a fixed
/// point on a second run, picks up upstream changes, and refuses to write anything that does not round-trip.
/// </summary>
[TestFixture]
public class SeamGeneratorTests
{
    private static string Part(GeneratorOutput output, string root, string sourcePath) => output.Files[Definition.PartPath(root, sourcePath)];

    [Test]
    public void FirstRun_MovesBodiesVerbatim_AndRewritesTheWrappersToForward()
    {
        var tree = Tree();

        var output = GenerateAndApply(tree);

        output.Errors.Should().BeEmpty();
        var core = Part(output, Definition.PassThroughRoot, FirstPath);
        core.Should().Contain("public string GetTag(uint oObject)");
        core.Should().Contain("return global::NWN.Core.NWScript.GetTag(oObject);");
        var original = Lf(First);
        var start = original.IndexOf("if (oObject == OBJECT_INVALID)", StringComparison.Ordinal);
        var end = original.IndexOf("(nClearCombatState ? 1 : 0, oObject);", StringComparison.Ordinal) + "(nClearCombatState ? 1 : 0, oObject);".Length;
        core.Should().Contain(original[start..end], "the body moves with its exact text, indentation included");
        core.Should().Contain("public uint OBJECT_SELF => global::NWN.Core.NWScript.OBJECT_SELF;");
        core.Should().Contain("var tag = GetTag(oObject);", "bodies with logic move unchanged");
        Part(output, Definition.PassThroughRoot, SecondPath).Should().Contain("ExecuteScript(\"thing_destroyed\", oThing);");

        var wrapper = tree.Files[FirstPath];
        wrapper.Should().Contain("return NWScriptRuntime.Current.GetTag(oObject);");
        wrapper.Should().Contain("NWScriptRuntime.Current.ClearAllActions(nClearCombatState, oObject);");
        wrapper.Should().Contain("return NWScriptRuntime.Current.HasTag(oObject, tags);");
        wrapper.Should().Contain("public static uint OBJECT_SELF => NWScriptRuntime.Current.OBJECT_SELF;");
        wrapper.Should().Contain("public const int SampleConstant = 5;");
        wrapper.Should().Contain("public static void ClearAllActions(bool nClearCombatState = false, uint oObject = OBJECT_INVALID)");
        wrapper.Should().NotContain("global::NWN.Core");
    }

    [Test]
    public void Interface_CarriesSignaturesDefaultsAndDocumentation()
    {
        var output = GenerateAndApply(Tree());

        var contract = Part(output, Definition.InterfaceRoot, FirstPath);
        contract.Should().Contain("string GetTag(uint oObject);");
        contract.Should().Contain("void ClearAllActions(bool nClearCombatState = false, uint oObject = OBJECT_INVALID);");
        contract.Should().Contain("bool HasTag(uint oObject, params string[] tags);");
        contract.Should().Contain("uint OBJECT_SELF { get; }");
        contract.Should().Contain("/// <param name=\"oObject\">The object.</param>");
        contract.Should().Contain("using Alignment = SWLOR.NWN.API.NWScript.Enum.Alignment;", "the part mirrors its wrapper file's usings");
        contract.Should().NotContain("SampleConstant", "constants cannot sit behind an interface");
    }

    [Test]
    public void Fake_RecordsEveryMemberAndOmitsDefaults()
    {
        var output = GenerateAndApply(Tree());

        var fake = Part(output, Definition.FakeRoot, FirstPath);
        fake.Should().Contain("public string GetTag(uint oObject)");
        fake.Should().Contain("return InvokeCall<string>(\"GetTag\", new object?[] { oObject });");
        fake.Should().Contain("public void ClearAllActions(bool nClearCombatState, uint oObject)");
        fake.Should().Contain("RecordCall(\"ClearAllActions\", new object?[] { nClearCombatState, oObject });");
        fake.Should().Contain("public bool HasTag(uint oObject, params string[] tags)");
        fake.Should().Contain("public uint OBJECT_SELF => InvokeCall<uint>(\"OBJECT_SELF\", Array.Empty<object?>());");
    }

    [Test]
    public void SecondRun_ChangesNothing()
    {
        var tree = Tree();
        GenerateAndApply(tree);
        var snapshot = tree.Files.ToDictionary(f => f.Key, f => f.Value);

        var second = GenerateAndApply(tree);

        second.Errors.Should().BeEmpty();
        second.RewrittenWrappers.Should().BeEmpty();
        second.Report.Should().Contain(line => line.StartsWith("Bodies moved by this run: 0;"));
        tree.Files.Should().BeEquivalentTo(snapshot);
        SeamGenerator.Compare(second, tree).Should().BeEmpty();
    }

    [Test]
    public void Output_IsDeterministic()
    {
        var first = GenerateAndApply(Tree());
        var second = GenerateAndApply(Tree());

        second.Files.Should().BeEquivalentTo(first.Files);
        second.Files.Keys.Should().Equal(first.Files.Keys);
    }

    [Test]
    public void CrlfWrappers_KeepTheirLineEndings_AndGeneratedFilesUseLf()
    {
        var tree = Tree(crlf: true);

        var output = GenerateAndApply(tree);

        output.Errors.Should().BeEmpty();
        var wrapper = tree.Files[FirstPath];
        wrapper.Replace("\r\n", string.Empty).Should().NotContain("\n", "every line ending of a CRLF file stays CRLF");
        wrapper.Should().Contain("NWScriptRuntime.Current.GetTag(oObject);");
        Part(output, Definition.PassThroughRoot, FirstPath).Should().NotContain("\r");
        GenerateAndApply(tree).RewrittenWrappers.Should().BeEmpty("a second run on CRLF input is a fixed point");
    }

    [Test]
    public void NewWrapperAfterTheFirstRun_IsMoved_AndMovedBodiesStay()
    {
        var tree = Tree();
        GenerateAndApply(tree);
        var wrapper = tree.Files[FirstPath];
        tree.WriteAllText(FirstPath, wrapper.Replace("public static bool HasTag", Lf("""
            public static int GetLevel(uint oObject)
                    {
                        return global::NWN.Core.NWScript.GetLevel(oObject);
                    }

                    public static bool HasTag
            """)));

        var output = GenerateAndApply(tree);

        output.Errors.Should().BeEmpty();
        output.Report.Should().Contain(line => line.StartsWith("Bodies moved by this run: 1;"));
        var core = Part(output, Definition.PassThroughRoot, FirstPath);
        core.Should().Contain("return global::NWN.Core.NWScript.GetLevel(oObject);");
        core.Should().Contain("return global::NWN.Core.NWScript.GetTag(oObject);", "bodies moved earlier are kept");
        Part(output, Definition.InterfaceRoot, FirstPath).Should().Contain("int GetLevel(uint oObject);");
        tree.Files[FirstPath].Should().Contain("return NWScriptRuntime.Current.GetLevel(oObject);");
    }

    [Test]
    public void UpstreamFileTakenOnASync_WinsOverTheBodyMovedEarlier()
    {
        var tree = Tree();
        GenerateAndApply(tree);
        var upstream = Lf(First).Replace("return global::NWN.Core.NWScript.GetTag(oObject);", "return global::NWN.Core.NWScript.GetTag(oObject) + \"!\";") + "\n";
        tree.WriteAllText(FirstPath, upstream);

        var output = GenerateAndApply(tree);

        output.Errors.Should().BeEmpty();
        var core = Part(output, Definition.PassThroughRoot, FirstPath);
        core.Should().Contain("return global::NWN.Core.NWScript.GetTag(oObject) + \"!\";");
        core.Should().NotContain("return global::NWN.Core.NWScript.GetTag(oObject);");
        tree.Files[FirstPath].Should().Contain("return NWScriptRuntime.Current.GetTag(oObject);");
    }

    [Test]
    public void WrapperRemovedUpstream_IsDroppedFromEveryGeneratedPart()
    {
        var tree = Tree();
        GenerateAndApply(tree);
        tree.WriteAllText(SecondPath, Lf("""
            namespace SWLOR.NWN.API.NWScript
            {
                public partial class NWScript
                {
                }
            }
            """) + "\n");

        var output = GenerateAndApply(tree);

        output.Errors.Should().BeEmpty();
        output.Files.Keys.Should().NotContain(Definition.PartPath(Definition.PassThroughRoot, SecondPath));
        tree.Files.Keys.Should().NotContain(Definition.PartPath(Definition.PassThroughRoot, SecondPath), "stale generated parts are deleted");
        tree.Files.Keys.Should().NotContain(Definition.PartPath(Definition.InterfaceRoot, SecondPath));
        tree.Files.Keys.Should().NotContain(Definition.PartPath(Definition.FakeRoot, SecondPath));
        output.Report.Should().Contain(line => line.Contains("DestroyThing(uint,float)"));
    }

    [Test]
    public void Report_ListsTheMembersLeftStatic()
    {
        var output = GenerateAndApply(Tree());

        output.Report.Should().Contain(line => line.StartsWith("Left static: 1 public constants") && line.Contains("SampleConstant"));
        output.Report.Should().Contain("Members behind INWScript: 5 (4 methods, 1 properties).");
    }

    [Test]
    public void ForwardingBodyWithoutAPreviousOutput_IsAnError()
    {
        var tree = Tree();
        GenerateAndApply(tree);
        foreach (var path in tree.ListFiles(Definition.PassThroughRoot, recursive: true)) tree.Delete(path);

        var output = SeamGenerator.Generate(tree, tree, Definition);

        output.Errors.Should().Contain(e => e.Contains("already forwards to NWScriptRuntime") && e.Contains("has no body"));
    }

    [Test]
    public void BodyThatMixesOriginalCodeWithTheRuntime_IsRejected()
    {
        var tree = Tree();
        tree.WriteAllText(FirstPath, Lf(First).Replace("return global::NWN.Core.NWScript.GetTag(oObject);", "return NWScriptRuntime.Current.GetTag(oObject) + \"x\";") + "\n");

        var output = SeamGenerator.Generate(tree, tree, Definition);

        output.Errors.Should().Contain(e => e.Contains("GetTag(uint)") && e.Contains("call itself"));
    }

    [TestCase("public static T Echo<T>(T value) { return value; }", "Echo")]
    [TestCase("public static void Out(out int value) { value = 0; }", "Out")]
    [TestCase("public static int Quick(int value) => value;", "Quick")]
    [TestCase("public static int Counter;", "Counter")]
    [TestCase("public static int Setter { get; set; }", "Setter")]
    [TestCase("private static void Hidden() { }", "Hidden")]
    [TestCase("public void Instance() { }", "Instance")]
    [TestCase("[System.Obsolete] public static void Old() { }", "Old")]
    [TestCase("public class Nested { }", "Nested")]
    public void UnsupportedMember_IsReported_AndNothingIsGenerated(string member, string name)
    {
        var tree = Tree();
        var text = Lf(First).Replace("public static string GetTag(uint oObject)", member + "\n\n        public static string GetTag(uint oObject)");
        tree.WriteAllText(FirstPath, text + "\n");

        var output = SeamGenerator.Generate(tree, tree, Definition);

        output.Errors.Should().NotBeEmpty();
        output.Errors.Should().Contain(e => e.Contains(name));
        output.Report.Should().BeEmpty();
    }

    [Test]
    public void Apply_RefusesToWrite_WhenTheOutputHasErrors()
    {
        var tree = Tree();
        tree.WriteAllText(FirstPath, Lf(First).Replace("public static string GetTag", "public static T Echo<T>(T v) { return v; }\n\n        public static string GetTag") + "\n");
        var before = tree.Files.ToDictionary(f => f.Key, f => f.Value);
        var output = SeamGenerator.Generate(tree, tree, Definition);

        var apply = () => SeamGenerator.Apply(output, tree);

        apply.Should().Throw<InvalidOperationException>();
        tree.Files.Should().BeEquivalentTo(before);
    }

    [Test]
    public void Runner_WritesNothing_WhenTheSourcesAreUnsupported()
    {
        var root = Path.Combine(Path.GetTempPath(), "seam-generator-" + Guid.NewGuid().ToString("N"));
        try
        {
            var files = new DiskFileSystem(root);
            files.WriteAllText(FirstPath, Lf(First).Replace("public static string GetTag", "public static T Echo<T>(T v) { return v; }\n\n        public static string GetTag") + "\n");
            files.WriteAllText(SecondPath, Lf(Second) + "\n");
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

    [Test]
    public void Runner_CheckMode_ReportsOutOfDateOutput_AndPassesOnceApplied()
    {
        var root = Path.Combine(Path.GetTempPath(), "seam-generator-" + Guid.NewGuid().ToString("N"));
        try
        {
            var files = new DiskFileSystem(root);
            files.WriteAllText(FirstPath, Lf(First) + "\n");
            files.WriteAllText(SecondPath, Lf(Second) + "\n");

            GeneratorRunner.Run(root, GeneratorMode.Check, null, new StringWriter()).Should().Be(GeneratorRunner.Differs);
            GeneratorRunner.Run(root, GeneratorMode.Apply, null, new StringWriter()).Should().Be(GeneratorRunner.Ok);
            GeneratorRunner.Run(root, GeneratorMode.Check, null, new StringWriter()).Should().Be(GeneratorRunner.Ok);

            files.WriteAllText(files.ListFiles(Definition.InterfaceRoot, recursive: true)[0], "// edited by hand\n");
            GeneratorRunner.Run(root, GeneratorMode.Check, null, new StringWriter()).Should().Be(GeneratorRunner.Differs);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void MovedBodyThatDiffersFromTheOriginal_FailsTheRoundTrip()
    {
        var tree = Tree();

        var output = SeamGenerator.Generate(tree, tree, Definition, files =>
        {
            var path = Definition.PartPath(Definition.PassThroughRoot, FirstPath);
            files[path] = files[path].Replace("return global::NWN.Core.NWScript.GetTag(oObject);", "return global::NWN.Core.NWScript.GetTag(oObject) ;");
        });

        output.Errors.Should().Contain(e => e.Contains("GetTag(uint)") && e.Contains("not textually identical to the original"));
    }

    [Test]
    public void InterfaceSignatureThatDiffersFromTheWrapper_FailsTheRoundTrip()
    {
        var tree = Tree();

        var output = SeamGenerator.Generate(tree, tree, Definition, files =>
        {
            var path = Definition.PartPath(Definition.InterfaceRoot, FirstPath);
            files[path] = files[path].Replace("uint oObject = OBJECT_INVALID", "uint oObject");
        });

        output.Errors.Should().Contain(e => e.Contains("ClearAllActions") && e.Contains("signature"));
    }

    [Test]
    public void MemberMissingFromTheFake_FailsTheRoundTrip()
    {
        var tree = Tree();

        var output = SeamGenerator.Generate(tree, tree, Definition, files =>
        {
            var path = Definition.PartPath(Definition.FakeRoot, SecondPath);
            files[path] = files[path].Replace("public void DestroyThing(uint oThing, float fDelay)", "public void DestroyThingElsewhere(uint oThing, float fDelay)");
        });

        output.Errors.Should().Contain(e => e.Contains("DestroyThing"));
    }

    [Test]
    public void RewriteThatTouchesTextOutsideBodies_FailsTheRoundTrip()
    {
        var tree = Tree();

        var output = SeamGenerator.Generate(tree, tree, Definition, files =>
        {
            files[FirstPath] = files[FirstPath].Replace("/// <param name=\"oObject\">The object.</param>", "/// <param name=\"oObject\">Changed.</param>");
        });

        output.Errors.Should().Contain(e => e.Contains("outside the method bodies") || e.Contains("signature or documentation"));
    }

    [Test]
    public void RewrittenBodyThatIsNotTheForwardingBody_FailsTheRoundTrip()
    {
        var tree = Tree();

        var output = SeamGenerator.Generate(tree, tree, Definition, files =>
        {
            files[FirstPath] = files[FirstPath].Replace("NWScriptRuntime.Current.GetTag(oObject)", "NWScriptRuntime.Current.GetTag(oObject, 1)");
        });

        output.Errors.Should().Contain(e => e.Contains("does not hold the forwarding body"));
    }

    [Test]
    public void Apply_RefusesToWrite_WhenTheRoundTripFails()
    {
        var tree = Tree();
        var before = tree.Files.ToDictionary(f => f.Key, f => f.Value);
        var output = SeamGenerator.Generate(tree, tree, Definition, files =>
        {
            var path = Definition.PartPath(Definition.PassThroughRoot, FirstPath);
            files[path] = files[path].Replace("GetTag(oObject);", "GetTag(oObject + 1);");
        });

        var apply = () => SeamGenerator.Apply(output, tree);

        output.Errors.Should().NotBeEmpty();
        apply.Should().Throw<InvalidOperationException>();
        tree.Files.Should().BeEquivalentTo(before);
    }
}
