using System.Text;
using NUnit.Framework;
using FluentAssertions;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Editors;

namespace SWLOR.Toolset.Tests;

[TestFixture]
public sealed class SwlorVarTablePolicyTests
{
    [Test]
    public void SharedVariableSectionReceivesKnownKeysAndSwlorGameCodeHint()
    {
        var document = JsonGffDocument.Parse(Encoding.UTF8.GetBytes("""{"__data_type":"GIT "}"""));
        var table = new VarTable(document.Root);
        var descriptions = new List<string>();
        var section = SwlorVarTablePolicy.Create((description, mutate) =>
        {
            descriptions.Add(description);
            mutate();
            return true;
        }, table, new GameCodeIndex(null));

        section.KnownKeys.Should().Contain("CREATURE_SPAWN_TABLE_ID");
        section.NewName = "QUEST_NPC_GROUP_ID";
        section.NewType = "int";
        section.NewValue = "999";
        section.SetVariableCommand.Execute(null);

        descriptions.Should().ContainSingle().Which.Should().Be("Set local QUEST_NPC_GROUP_ID");
        section.ValidationHint.Should().Be("Warning: 999 is not a known NPCGroupType value.");
        table.GetInt("QUEST_NPC_GROUP_ID").Should().Be(999);
    }
}
