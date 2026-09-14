using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Conversations;

namespace SWLOR.Toolset.Tests;

public sealed class ModuleConversationRouterTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "swlor-conversation-routing-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "Module", "git"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    [Test]
    public void RouteRepository_UpdatesOnlyMigratedConversationEventScripts()
    {
        var path = Path.Combine(_root, "Module", "git", "sample.git.json");
        File.WriteAllText(path, """
        {
          "Creature List": {
            "type": "list",
            "value": [
              {
                "Conversation": { "type": "resref", "value": "migrated" },
                "ScriptDialogue": { "type": "resref", "value": "x2_def_onconv" }
              },
              {
                "Conversation": { "type": "resref", "value": "migrated" },
                "ScriptDialogue": { "type": "resref", "value": "dialog_start" }
              },
              {
                "Conversation": { "type": "resref", "value": "legacy" },
                "ScriptDialogue": { "type": "resref", "value": "x2_def_onconv" }
              }
            ]
          },
          "Placeable List": {
            "type": "list",
            "value": [
              {
                "Conversation": { "type": "resref", "value": "migrated" },
                "OnUsed": { "type": "resref", "value": "nw_startconv" }
              }
            ]
          }
        }
        """);

        var result = ModuleConversationRouter.RouteRepository(
            _root,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "MIGRATED" });
        var updated = File.ReadAllText(path);

        result.ReferencesFound.Should().Be(3);
        result.ReferencesUpdated.Should().Be(2);
        result.ReferencesAlreadyRouted.Should().Be(1);
        result.Issues.Should().BeEmpty();
        var normalized = updated.Replace("\r\n", "\n", StringComparison.Ordinal);
        normalized.Should().Contain("\"ScriptDialogue\": { \"type\": \"resref\", \"value\": \"dialog_start\" }");
        normalized.Should().Contain("\"OnUsed\": { \"type\": \"resref\", \"value\": \"dialog_start\" }");
        normalized.Should().Contain("\"Conversation\": { \"type\": \"resref\", \"value\": \"legacy\" },\n        \"ScriptDialogue\": { \"type\": \"resref\", \"value\": \"x2_def_onconv\" }");

        var secondRun = ModuleConversationRouter.RouteRepository(
            _root,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "migrated" });
        secondRun.ReferencesFound.Should().Be(3);
        secondRun.ReferencesUpdated.Should().Be(0);
        secondRun.ReferencesAlreadyRouted.Should().Be(3);
        secondRun.Issues.Should().BeEmpty();
    }

    [Test]
    public void RouteRepository_PreservesWindows1252SourceText()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding(1252);
        var path = Path.Combine(_root, "Module", "git", "encoded.git.json");
        var source = """
        {
          "Creature List": {
            "type": "list",
            "value": [
              {
                "Description": { "type": "cexostring", "value": "Pilot’s terminal" },
                "Conversation": { "type": "resref", "value": "migrated" },
                "ScriptDialogue": { "type": "resref", "value": "" }
              }
            ]
          }
        }
        """;
        File.WriteAllBytes(path, encoding.GetBytes(source));

        var result = ModuleConversationRouter.RouteRepository(_root, new HashSet<string> { "migrated" });
        var bytes = File.ReadAllBytes(path);

        result.Issues.Should().BeEmpty();
        bytes.Should().Contain((byte)0x92, "the original Windows-1252 apostrophe must be preserved");
        encoding.GetString(bytes).Should().Contain("\"value\": \"dialog_start\"");
    }
}
