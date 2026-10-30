using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The owner asked that Performance never become a server hog. These guards read the source for the rules the windows
/// follow: no window rebuild per beat, no timer of their own, floating text only as the closed-window fallback.
/// </summary>
public class PerformanceWindowCostTests
{
    private static readonly string[] WindowViewModels =
    {
        "PerformanceCueViewModelBase.cs",
        "PerformanceFootworkViewModel.cs",
        "PerformanceSessionViewModel.cs",
        "PerformanceOptionsViewModel.cs",
        "PerformanceWatchingViewModel.cs"
    };

    [Test]
    public void CueWindows_AreNeverRebuiltPerBeat()
    {
        var baseSource = ViewModelSource("PerformanceCueViewModelBase.cs");

        CountOf(baseSource, "ChangePartialView(").Should().Be(1, "only Initialize applies the layout");
        CountOf(baseSource, "ShowModal(").Should().Be(0);
        ViewModelSource("PerformanceFootworkViewModel.cs").Should().NotContain("ChangePartialView(");
        ViewModelSource("PerformanceSessionViewModel.cs").Should().NotContain("ChangePartialView(");
    }

    [Test]
    public void NoPerformanceWindowRunsATimerOfItsOwn()
    {
        foreach (var file in WindowViewModels)
        {
            var source = ViewModelSource(file);
            source.Should().NotContain("Scheduler.", $"{file} is updated by the beat loop or the audience tick only");
            source.Should().NotContain("DelayCommand(");
            source.Should().NotContain("AssignCommand(");
        }

        var windows = File.ReadAllText(SourcePath("Service", "PerformanceService", "PerformanceWindows.cs"));
        windows.Should().NotContain("Scheduler.");
        windows.Should().NotContain("DelayCommand(");
    }

    [Test]
    public void BeatLoop_PushesToTheWindowAndFallsBackToOneLineOfFloatingText()
    {
        var source = File.ReadAllText(SourcePath("Service", "Performance.cs"));

        CountOf(source, "FloatingTextStringOnCreature(").Should().Be(1, "only the closed-window fallback uses it");
        source.Should().Contain("view.ShowBeat(session, outcome);");
        source.Should().Contain("ShowBeatProgress(");
        source.Should().NotContain("Tempo.Next()", "the Options button opens the Perform window");
        CountOf(source, "Scheduler.ScheduleRepeating(").Should().Be(1, "one schedule per performer, as before");
    }

    [Test]
    public void BeatLoop_NeverTouchesTheGuiServiceDirectly()
    {
        var source = File.ReadAllText(SourcePath("Service", "Performance.cs"));

        source.Should().NotContain("Gui.");
        source.Should().NotContain("NuiSetBind");
    }

    [Test]
    public void WatchingWindow_IsDrawnFromTheAudienceTickAndFollowClicksOnly()
    {
        var source = File.ReadAllText(SourcePath("Service", "PerformanceService", "PerformanceAudience.cs"));

        CountOf(source, "PerformanceWindows.ShowWatching(").Should().Be(2, "the tick and a Follow click");
        CountOf(source, "Scheduler.ScheduleRepeating(").Should().Be(1);
        CountOf(source, "DB.Get<Player>(").Should().Be(1, "the debt and the window share one read per watcher per tick");
        CountOf(source, "DB.Set(").Should().Be(1);
    }

    [Test]
    public void EveryPerformanceWindowHasAnIdAndADebugCommand()
    {
        var commands = File.ReadAllText(SourcePath("Feature", "ChatCommandDefinition", "DebuggingChatCommand.cs"));

        ((int)GuiWindowType.PerformanceFootwork).Should().Be(100);
        ((int)GuiWindowType.PerformanceSession).Should().Be(101);
        ((int)GuiWindowType.PerformanceOptions).Should().Be(102);
        ((int)GuiWindowType.PerformanceWatching).Should().Be(103);
        foreach (var window in new[]
                 {
                     GuiWindowType.PerformanceFootwork, GuiWindowType.PerformanceSession,
                     GuiWindowType.PerformanceOptions, GuiWindowType.PerformanceWatching
                 })
        {
            commands.Should().Contain($"GuiWindowType.{window}");
        }
    }

    private static string ViewModelSource(string file)
    {
        return File.ReadAllText(SourcePath("Feature", "GuiDefinition", "ViewModel", file));
    }

    private static int CountOf(string source, string text)
    {
        var count = 0;
        for (var index = source.IndexOf(text, StringComparison.Ordinal);
             index >= 0;
             index = source.IndexOf(text, index + text.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    private static string SourcePath(params string[] parts)
    {
        return Path.Combine(new[] { FindRepositoryRoot(), "SWLOR.Game.Server" }.Concat(parts).ToArray());
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root holds AGENTS.md");
        return directory.FullName;
    }
}
