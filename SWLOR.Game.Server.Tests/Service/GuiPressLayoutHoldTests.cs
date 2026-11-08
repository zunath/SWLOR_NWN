using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Tests.Service;

// NUI fires "click" on the press. A layout that replaces the pressed control before the release locks
// the window (Player List "Send Tell" bug report): it ignores every later press, close button included.
[TestFixture]
public class GuiPressLayoutHoldTests
{
    [Test]
    public void LayoutIsSentAtOnceWhenNothingIsPressed()
    {
        var hold = new GuiPressLayoutHold();

        hold.Hold().Should().BeFalse();
        hold.HasHeldLayout.Should().BeFalse();
    }

    [Test]
    public void LayoutWaitsForTheReleaseOfThePressThatTriggeredIt()
    {
        var hold = new GuiPressLayoutHold();

        hold.Press();
        hold.Hold().Should().BeTrue();
        hold.HasHeldLayout.Should().BeTrue();

        hold.Release().Should().BeTrue("the held layout is sent on release");
        hold.IsPressed.Should().BeFalse();
        hold.HasHeldLayout.Should().BeFalse();
        hold.Release().Should().BeFalse("a held layout is sent once");
    }

    [Test]
    public void ReleaseWithoutAHeldLayoutSendsNothing()
    {
        var hold = new GuiPressLayoutHold();

        hold.Press();

        hold.Release().Should().BeFalse();
        hold.Hold().Should().BeFalse();
    }

    [Test]
    public void ClickAndMouseDownOfOnePressKeepOnePressId()
    {
        var hold = new GuiPressLayoutHold();

        hold.Press();
        var pressId = hold.PressId;
        hold.Press();
        hold.Press();

        hold.PressId.Should().Be(pressId);
    }

    [Test]
    public void FallbackSendsTheLayoutWhenTheReleaseNeverArrives()
    {
        var hold = new GuiPressLayoutHold();
        hold.Press();
        hold.Hold();

        hold.Expire(hold.PressId).Should().BeTrue();
        hold.IsPressed.Should().BeFalse();
        hold.HasHeldLayout.Should().BeFalse();
    }

    [Test]
    public void FallbackForAnEarlierPressDoesNotEndALaterOne()
    {
        var hold = new GuiPressLayoutHold();
        hold.Press();
        hold.Hold();
        var firstPress = hold.PressId;
        hold.Release();

        hold.Press();
        hold.Hold();

        hold.Expire(firstPress).Should().BeFalse();
        hold.IsPressed.Should().BeTrue();
        hold.HasHeldLayout.Should().BeTrue();
    }

    [Test]
    public void ResetForgetsThePressAndCancelsItsFallback()
    {
        var hold = new GuiPressLayoutHold();
        hold.Press();
        hold.Hold();
        var pressId = hold.PressId;

        hold.Reset();

        hold.IsPressed.Should().BeFalse();
        hold.HasHeldLayout.Should().BeFalse();
        hold.Expire(pressId).Should().BeFalse();
        hold.Hold().Should().BeFalse();
    }

    [Test]
    public void NuiEventsRecordThePressBeforeAnyHandlerRuns()
    {
        var source = ServerSource("Domain", "UI", "GuiService.cs");
        var handler = source[source.IndexOf("public void HandleNuiEvents()", StringComparison.Ordinal)..];
        handler = handler[..handler.IndexOf("public void HandleNuiWatchEvent()", StringComparison.Ordinal)];

        handler.Should().Contain("eventType == \"click\" || eventType == \"mousedown\"");
        handler.Should().Contain("viewModel.OnPointerPressed()");
        handler.Should().Contain("eventType == \"mouseup\"");
        handler.Should().Contain("viewModel.OnPointerReleased()");

        var press = handler.IndexOf("viewModel.OnPointerPressed()", StringComparison.Ordinal);
        handler.IndexOf("_elementEvents.ContainsKey(eventKey)", StringComparison.Ordinal).Should().BeGreaterThan(press,
            "elements without handlers still report the press and release");
        handler.IndexOf("method?.Invoke(", StringComparison.Ordinal).Should().BeGreaterThan(press,
            "the click handler's layout change must see the press");
    }

    [Test]
    public void RootLayoutIsHeldDuringAPress()
    {
        var source = ServerSource("Service", "GuiService", "GuiViewModelBase.cs");
        var apply = source[source.IndexOf("private void ApplyRootLayout(", StringComparison.Ordinal)..];

        var hold = apply.IndexOf("_pressHold.Hold()", StringComparison.Ordinal);
        hold.Should().BeGreaterThan(0);
        apply.IndexOf("NuiSetGroupLayout(", StringComparison.Ordinal).Should().BeGreaterThan(hold);
        apply.IndexOf("ApplyRefreshBugFix()", StringComparison.Ordinal).Should().BeGreaterThan(hold);

        source.Should().Contain("_pressHold.Reset();", "a reopened window starts without a press");
    }

    private static string ServerSource(params string[] parts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("repository root should be discoverable from the test directory");
        return File.ReadAllText(Path.Combine(new[] { directory!.FullName, "SWLOR.Game.Server" }.Concat(parts).ToArray()));
    }
}
