using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FluentAssertions;
using Nwn.Toolset.Avalonia.Graph;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using Nwn.Authoring.Documents.Native;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors.Conversations;

namespace SWLOR.Toolset.Tests.Editors.Conversations;

public sealed class ConversationGraphWindowTests
{
    [AvaloniaTest]
    public void SelectionShowsFullTextAndRefreshUsesTheCurrentDraft()
    {
        var dialog = DlgDocument.Parse(ModuleResourceTemplateFactory.CreateFileContent(
            ResourceType.Dlg, "graph_test", "Graph test"));
        dialog.Entries[0].Text = "NPC text " + new string('x', 3000);
        var window = new ConversationGraphWindow(dialog, "graph_test");
        window.Show();
        try
        {
            window.UpdateLayout();
            var canvas = window.GetVisualDescendants().OfType<GraphCanvas>().Single();
            canvas.Bounds.Width.Should().BeGreaterThan(500);
            canvas.Bounds.Height.Should().BeGreaterThan(250);
            SelectNpc(canvas);
            window.GetVisualDescendants().OfType<TextBox>().Single().Text.Should().Be(dialog.Entries[0].Text);

            dialog.Entries[0].Text = "Edited without saving";
            window.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "RefreshConversationGraph")
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            window.UpdateLayout();
            var refreshed = window.GetVisualDescendants().OfType<GraphCanvas>().Single();
            refreshed.Should().NotBeSameAs(canvas);
            SelectNpc(refreshed);
            window.GetVisualDescendants().OfType<TextBox>().Single().Text.Should().Be("Edited without saving");
            using var frame = window.CaptureRenderedFrame()
                ?? throw new AssertionException("The application graph window did not produce a rendered frame.");
            var image = Path.Combine(TestContext.CurrentContext.TestDirectory, $"swlor-conversation-graph-{Guid.NewGuid():N}.png");
            frame.Save(image);
            TestContext.AddTestAttachment(image, "Real SWLOR conversation graph window rendered with Skia.");
        }
        finally
        {
            window.Close();
        }

        void SelectNpc(GraphCanvas graph)
        {
            var state = graph.Viewport;
            var node = state.NodePositions[new GraphNodeId("Entry/0")];
            var local = state.ToScreen(node + new global::Avalonia.Vector(90, 35));
            var pointer = graph.TranslatePoint(local, window)!.Value;
            window.MouseDown(pointer, MouseButton.Left);
            window.MouseUp(pointer, MouseButton.Left);
        }
    }
}
