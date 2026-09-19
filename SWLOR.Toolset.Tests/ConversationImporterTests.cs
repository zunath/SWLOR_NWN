using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Tests;

public sealed class ConversationImporterTests
{
    private string _directory = null!;

    [SetUp]
    public void SetUp() => _directory = Directory.CreateTempSubdirectory("swlor-conversation-import-").FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(_directory, recursive: true);

    [Test]
    public void Import_CreatesOneGraphWithoutModifyingOtherGraphsOrTheSource()
    {
        var source = LegacyConversationFixtures.PathFor("dantherbs");
        var original = File.ReadAllBytes(source);
        var destination = Path.Combine(_directory, "dantherbs.conversation.json");
        var other = Path.Combine(_directory, "independent.conversation.json");
        File.WriteAllText(other, "authored separately");

        var result = ConversationImporter.Import(source, destination);

        result.CanRunInNui.Should().BeTrue();
        var graph = JsonConvert.DeserializeObject<ConversationGraph>(File.ReadAllText(destination))!;
        graph.Id.Should().Be("dantherbs");
        ConversationGraphValidator.Validate(graph).Should().BeEmpty();
        File.ReadAllBytes(source).Should().Equal(original);
        File.ReadAllText(other).Should().Be("authored separately");
        Directory.GetFiles(_directory).Should().HaveCount(2);
    }

    [Test]
    public void Import_RefusesToOverwriteAnAuthoredGraph()
    {
        var destination = Path.Combine(_directory, "dantherbs.conversation.json");
        File.WriteAllText(destination, "new author edits");

        var import = () => ConversationImporter.Import(LegacyConversationFixtures.PathFor("dantherbs"), destination);

        import.Should().Throw<IOException>().WithMessage("*cannot overwrite*");
        File.ReadAllText(destination).Should().Be("new author edits");
    }

    [Test]
    public void Import_RejectsUnsupportedActionsWithoutCreatingAFile()
    {
        var source = Path.Combine(_directory, "dantherbs.dlg.json");
        var document = DlgDocument.Load(LegacyConversationFixtures.PathFor("dantherbs"));
        document.Entries[0].Script = "unknown_script";
        File.WriteAllBytes(source, document.ToBytes());
        var destination = Path.Combine(_directory, "dantherbs.conversation.json");

        var import = () => ConversationImporter.Import(source, destination);

        import.Should().Throw<InvalidDataException>().WithMessage("*unknown_script*");
        File.Exists(destination).Should().BeFalse();
    }

    [Test]
    public void Import_LeavesDmfiOnItsNativePath()
    {
        var destination = Path.Combine(_directory, "dmfi_universal.conversation.json");
        var import = () => ConversationImporter.Import(LegacyConversationFixtures.PathFor("dmfi_universal"), destination);

        import.Should().Throw<InvalidDataException>().WithMessage("*DMFI*");
        File.Exists(destination).Should().BeFalse();
    }
}
