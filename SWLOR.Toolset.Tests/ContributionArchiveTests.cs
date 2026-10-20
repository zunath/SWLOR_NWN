using System.IO.Compression;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Distribution;

namespace SWLOR.Toolset.Tests;

public class ContributionArchiveTests
{
    private string _root = null!;
    private string _workspace = null!;
    private string _target = null!;
    private string Archive => Path.Combine(_root, "contribution.zip");
    private const string Revision = "1111111111111111111111111111111111111111";

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "swlor_contribution_" + Guid.NewGuid().ToString("N"));
        _workspace = Path.Combine(_root, "workspace");
        _target = Path.Combine(_root, "review");
        Write(_workspace, "Module/utc/old.utc.json", "original\r\n");
        Write(_workspace, "Module/ncs/script.ncs", "binary\0\u0001");
        Write(_workspace, "Module/uti/deleted.uti.json", "deleted");
        Write(_workspace, "SWLOR.Game.Server/ConversationData/test.conversation.json", "conversation");
        Write(_workspace, "toolset/categories.json", "categories");
        var baseline = ContentPaths.Capture(_workspace);
        foreach (var path in baseline.Keys) Write(_target, path, File.ReadAllText(ContentPaths.Resolve(_workspace, path)));
        var manifest = new WorkspaceManifest { ContentRevision = Revision, AssetRevision = Revision, Baseline = baseline };
        File.WriteAllText(Path.Combine(_workspace, WorkspaceManifest.FileName), JsonSerializer.Serialize(manifest));
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_root, recursive: true);

    [Test]
    public void ExportAndImportPreserveBytesAndIncludeAdditionsDeletionsAndConversationChanges()
    {
        Write(_workspace, "Module/utc/old.utc.json", "modified\r\n");
        Write(_workspace, "Module/utc/new.utc.json", "new\n");
        Write(_workspace, "SWLOR.Game.Server/ConversationData/test.conversation.json", "edited conversation");
        File.Delete(ContentPaths.Resolve(_workspace, "Module/uti/deleted.uti.json"));
        ContributionArchive.Export(_workspace, Archive).Should().Be(4);
        ContributionArchive.Import(Archive, _target, apply: true).Should().Be(4);
        ContentPaths.Capture(_target).Should().BeEquivalentTo(ContentPaths.Capture(_workspace));
        File.ReadAllBytes(ContentPaths.Resolve(_target, "Module/utc/old.utc.json"))
            .Should().Equal(File.ReadAllBytes(ContentPaths.Resolve(_workspace, "Module/utc/old.utc.json")));
    }

    [Test]
    public void ConflictInAnyFilePreventsEveryWrite()
    {
        Write(_workspace, "Module/utc/new.utc.json", "new");
        Write(_workspace, "toolset/categories.json", "contributor category");
        ContributionArchive.Export(_workspace, Archive);
        Write(_target, "toolset/categories.json", "maintainer category");
        var before = ContentPaths.Capture(_target);
        Action import = () => ContributionArchive.Import(Archive, _target, apply: true);
        import.Should().Throw<IOException>().WithMessage("Conflict:*");
        ContentPaths.Capture(_target).Should().BeEquivalentTo(before);
    }

    [Test]
    public void PreviewChecksWithoutWriting()
    {
        Write(_workspace, "Module/utc/new.utc.json", "new");
        ContributionArchive.Export(_workspace, Archive);
        var before = ContentPaths.Capture(_target);
        ContributionArchive.Import(Archive, _target, apply: false).Should().Be(1);
        ContentPaths.Capture(_target).Should().BeEquivalentTo(before);
    }

    [Test]
    public void CorruptPayloadIsRefusedBeforeWriting()
    {
        Write(_workspace, "Module/utc/new.utc.json", "new");
        ContributionArchive.Export(_workspace, Archive);
        using (var zip = ZipFile.Open(Archive, ZipArchiveMode.Update))
        {
            zip.GetEntry("files/Module/utc/new.utc.json")!.Delete();
            using var writer = new StreamWriter(zip.CreateEntry("files/Module/utc/new.utc.json").Open());
            writer.Write("corrupt");
        }
        Action import = () => ContributionArchive.Import(Archive, _target, apply: true);
        import.Should().Throw<InvalidDataException>().WithMessage("*checksum*");
        File.Exists(Path.Combine(_target, "Module/utc/new.utc.json")).Should().BeFalse();
    }

    [TestCase("../outside.txt")]
    [TestCase("Module/utc/../../outside.txt")]
    [TestCase("SWLOR.Game.Server/Service/Combat.cs")]
    [TestCase("Module/utc/object.utc.json:stream")]
    [TestCase("Module\\utc\\object.utc.json")]
    public void ContributionCannotWriteOutsideAuthoredContent(string path)
    {
        var manifest = new ContributionManifest { ContentRevision = Revision, AssetRevision = Revision,
            Changes = new() { new ContributionChange(path, null, ContentPaths.Hash(new byte[] { 1 })) } };
        using (var zip = ZipFile.Open(Archive, ZipArchiveMode.Create))
        {
            using var writer = new StreamWriter(zip.CreateEntry(ContributionArchive.ManifestName).Open());
            writer.Write(JsonSerializer.Serialize(manifest));
        }
        Action import = () => ContributionArchive.Import(Archive, _target, apply: true);
        import.Should().Throw<InvalidDataException>();
    }

    [Test]
    public void DuplicateEntriesAndUnexpectedFilesAreRefused()
    {
        Write(_workspace, "Module/utc/new.utc.json", "new");
        ContributionArchive.Export(_workspace, Archive);
        using (var zip = ZipFile.Open(Archive, ZipArchiveMode.Update)) zip.CreateEntry("unexpected.exe");
        Action import = () => ContributionArchive.Import(Archive, _target, apply: true);
        import.Should().Throw<InvalidDataException>().WithMessage("*unexpected*");
    }

    [Test]
    public void ExportDoesNotOverwriteAnExistingContribution()
    {
        Write(_workspace, "Module/utc/new.utc.json", "new");
        File.WriteAllText(Archive, "keep");
        Action export = () => ContributionArchive.Export(_workspace, Archive);
        export.Should().Throw<IOException>();
        File.ReadAllText(Archive).Should().Be("keep");
    }

    private static void Write(string root, string relative, string text)
    {
        var path = ContentPaths.Resolve(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }
}
