using FluentAssertions;
using Nwn.Toolset.Avalonia.Graph;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors.Conversations;

namespace SWLOR.Toolset.Tests.Editors.Conversations;

public sealed class ConversationGraphAdapterTests
{
    [Test]
    public void BranchesReuseDisconnectedNodesAndMissingTargetsRemainVisibleWithoutChangingBytes()
    {
        var dialog = CreateDialog();
        var start = dialog.Entries[0];
        var first = dialog.AddReply("First choice");
        var second = dialog.AddReply("Second choice");
        var disconnected = dialog.AddEntry("Disconnected NPC");
        dialog.AddLink(start, first).AddCondition("HasItem", "test_item");
        dialog.AddLink(start, second);
        dialog.AddLink(first, start, isChild: true);
        dialog.AddOpening(disconnected);
        var broken = dialog.AddLink(second, disconnected);
        broken.Struct.SetUInt("Index", GffFieldType.Dword, 999);
        var bytes = dialog.ToBytes();

        var snapshot = ConversationGraphAdapter.Build(dialog);

        snapshot.Diagram.Nodes.Select(node => node.Id.Value).Should()
            .Equal("openings", "Entry/0", "Entry/1", "Reply/0", "Reply/1");
        snapshot.Diagram.Edges.Should().HaveCount(6);
        snapshot.Diagram.Edges.Single(edge => edge.Id.Value == "Entry/0/route/0")
            .Label.Should().Be("Route 1 · conditional");
        snapshot.Diagram.Edges.Single(edge => edge.Id.Value == "Reply/0/route/0")
            .Label.Should().Be("Reuse 1");
        snapshot.Diagram.Edges.Single(edge => edge.Id.Value == "Reply/1/route/0")
            .Target.Should().Be(new GraphNodeId("Entry/999"));
        snapshot.Diagram.Edges.Single(edge => edge.Id.Value == "Reply/1/route/0")
            .Label.Should().Contain("unresolved");
        snapshot.Diagram.Edges.Where(edge => edge.Source.Value == "openings")
            .Select(edge => edge.Target.Value).Should().Equal("Entry/0", "Entry/1");
        dialog.ToBytes().Should().Equal(bytes);
    }

    [Test]
    public void FullTextSurvivesTheBoundedCanvasSubtitleAndRefreshIncludesUnsavedChanges()
    {
        var dialog = CreateDialog();
        var text = "First line\r\n" + new string('x', 3000);
        dialog.Entries[0].Text = text;
        var bytes = dialog.ToBytes();
        var before = ConversationGraphAdapter.Build(dialog);

        before.FullText[new GraphNodeId("Entry/0")].Should().Be(text);
        before.Diagram.Nodes.Single(node => node.Id.Value == "Entry/0").Subtitle
            .Should().HaveLength(2048).And.NotContain("\n").And.NotContain("\r");
        dialog.ToBytes().Should().Equal(bytes);
        dialog.Entries[0].Text = "An unsaved edit";
        var after = ConversationGraphAdapter.Build(dialog);
        after.FullText[new GraphNodeId("Entry/0")].Should().Be("An unsaved edit");
        before.FullText[new GraphNodeId("Entry/0")].Should().Be(text);
    }

    private static DlgDocument CreateDialog() => DlgDocument.Parse(
        ModuleResourceTemplateFactory.CreateFileContent(ResourceType.Dlg, "graph_test", "Graph test"));
}
