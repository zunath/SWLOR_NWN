using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

[TestFixture]
public class GuiPartialViewReplayTests
{
    [Test]
    public void NestedLayoutSwaps_AreQueuedForNextTickReplayByTheBaseViewModel()
    {
        var source = LoadViewModelBaseSource();

        var changePartialView = ExtractSection(
            source,
            "protected void ChangePartialView(string elementId, string partialName, Action onBeforeApply = null,",
            "private void ChangeGroupLayout");
        var changeGroupLayout = ExtractSection(source, "private void ChangeGroupLayout", "private void ApplyGroupLayout");
        var setGroupLayout = ExtractSection(source, "protected void SetGroupLayout", "/// <summary>");

        changePartialView.Should().Contain("ChangeGroupLayout(elementId, window.PartialViews[partialName]");
        setGroupLayout.Should().Contain("ChangeGroupLayout(elementId, layout, null, null);");
        setGroupLayout.Should().NotContain("NuiSetGroupLayout");

        var rootCheck = changeGroupLayout.IndexOf("if (elementId == \"_window_\")", StringComparison.Ordinal);
        var queue = changeGroupLayout.IndexOf("QueuePartialViewReplay(", StringComparison.Ordinal);
        rootCheck.Should().BeGreaterThanOrEqualTo(0);
        queue.Should().BeGreaterThan(rootCheck, "only nested elements are replayed; a root swap clears the queue");
        changeGroupLayout.Should().Contain("_pendingPartialViews.Clear();");
        changeGroupLayout.Should().Contain("_partialViewReplayVersion++;");
        changeGroupLayout.Should().Contain("if (_partialViewApplyDepth == 0)");
    }

    [Test]
    public void Replay_IsSkippedForStaleOrClosedWindows()
    {
        var source = LoadViewModelBaseSource();
        var queue = ExtractSection(source, "private void QueuePartialViewReplay", "/// <summary>");

        queue.Should().Contain("DelayCommand(0.0f,");
        queue.Should().Contain("if (replayVersion != _partialViewReplayVersion)");
        queue.Should().Contain("bindingGeneration != _bindingGeneration");
        queue.Should().Contain("windowToken != WindowToken");
        queue.Should().Contain("!Gui.IsWindowOpen(Player, WindowType)");
        queue.Should().Contain("_pendingPartialViews.RemoveAll(existing => existing.ElementId == pending.ElementId);");
    }

    [Test]
    public void SwapNestedPartialView_UsesTheSharedReplayInsteadOfItsOwnDelay()
    {
        var source = LoadViewModelBaseSource();
        var swap = ExtractSection(source, "public void SwapNestedPartialView", "public string ModalPromptText");

        swap.Should().Contain("ChangePartialView(\"_window_\", \"%%WINDOW_MAIN%%\");");
        swap.Should().Contain("ChangePartialView(elementId, partialName, onBeforeApply, onAfterApply);");
        swap.Should().NotContain("DelayCommand");
    }

    [Test]
    public void ViewModels_DoNotScheduleTheirOwnNestedLayoutReapply()
    {
        var root = FindRepositoryRoot();
        var viewModelDirectory = Path.Combine(
            root.FullName, "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel");
        var manualReapply = new Regex(
            @"DelayCommand\([^;]{0,200}?(ChangePartialView\((?!""_window_"")|SetGroupLayout\(|ReapplyContentLayout)",
            RegexOptions.Singleline);

        var offenders = Directory.GetFiles(viewModelDirectory, "*.cs")
            // The NUI gallery deliberately probes timing hazards.
            .Where(path => Path.GetFileName(path) != "DebugNuiGalleryViewModel.cs")
            .Where(path => manualReapply.IsMatch(File.ReadAllText(path)))
            .Select(Path.GetFileName)
            .ToList();

        offenders.Should().BeEmpty(
            "GuiViewModelBase already reapplies nested layouts on the next tick");
    }

    private static string ExtractSection(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"'{startMarker}' should exist");
        var end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start, $"'{endMarker}' should follow '{startMarker}'");
        return source[start..end];
    }

    private static string LoadViewModelBaseSource()
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(
            root.FullName, "SWLOR.Game.Server", "Service", "GuiService", "GuiViewModelBase.cs"));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("repository root should be discoverable from the test directory");
        return directory!;
    }
}
