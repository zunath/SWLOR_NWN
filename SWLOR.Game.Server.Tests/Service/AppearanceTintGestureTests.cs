using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Tests.Service;

public class AppearanceTintGestureTests
{
    [TestCase(NuiMouseButton.Left)]
    [TestCase(NuiMouseButton.Middle)]
    [TestCase(NuiMouseButton.Right)]
    public void HydrationThenClickWithoutColorUpdateDoesNotQueueAnEdit(NuiMouseButton button)
    {
        var editor = new AppearanceEditorViewModel();
        var applied = new List<TintMapColor>();
        editor.SelectedTintColor = new GuiColor(0, 0, 0);
        Begin(editor, applied.Add, button);
        Field(editor, "_tintPickerActive").Should().Be(button == NuiMouseButton.Left);
        Release(editor, button)?.Invoke();
        applied.Should().BeEmpty();
        Field(editor, "_pendingPickerColor").Should().BeNull();
        Field(editor, "_pendingPickerApply").Should().BeNull();
        Field(editor, "_tintPickerActive").Should().Be(false);
    }

    [TestCase(205, 228, 197)]
    [TestCase(0, 0, 0)]
    public void ColorWatchAfterMouseUpAppliesTheClickOnce(byte red, byte green, byte blue)
    {
        var editor = new AppearanceEditorViewModel();
        var applied = new List<TintMapColor>();
        Begin(editor, applied.Add);
        var finishRelease = Release(editor);

        Queue(editor, new GuiColor(red, green, blue)).Should().BeTrue();
        finishRelease();
        finishRelease();

        applied.Should().Equal(new TintMapColor(red, green, blue));
        editor.CustomTintRed.Should().Be(red.ToString());
        editor.CustomTintGreen.Should().Be(green.ToString());
        editor.CustomTintBlue.Should().Be(blue.ToString());
        Queue(editor, new GuiColor(1, 2, 3)).Should().BeFalse();
    }

    [Test]
    public void DragStillAppliesLiveColorsAndTheFinalReleaseWatch()
    {
        var editor = new AppearanceEditorViewModel();
        var applied = new List<TintMapColor>();
        Begin(editor, applied.Add);
        Queue(editor, new GuiColor(10, 20, 30));
        Invoke(editor, "FlushPendingPickerColor");
        Field(editor, "_tintPickerActive").Should().Be(true);
        Queue(editor, new GuiColor(40, 50, 60));
        var finishRelease = Release(editor);
        Queue(editor, new GuiColor(70, 80, 90));
        finishRelease();

        applied.Should().Equal(new TintMapColor(10, 20, 30), new TintMapColor(40, 50, 60),
            new TintMapColor(70, 80, 90));
    }

    [Test]
    public void PreviousReleaseCannotEndTheNextGestureOrUseItsOldTarget()
    {
        var editor = new AppearanceEditorViewModel();
        var firstTarget = new List<TintMapColor>();
        var nextTarget = new List<TintMapColor>();
        Begin(editor, firstTarget.Add);
        var finishPreviousRelease = Release(editor);
        Begin(editor, nextTarget.Add);
        Queue(editor, new GuiColor(70, 80, 90));
        finishPreviousRelease();

        Field(editor, "_tintPickerActive").Should().Be(true);
        firstTarget.Should().BeEmpty();
        nextTarget.Should().BeEmpty();
        Release(editor)();
        nextTarget.Should().Equal(new TintMapColor(70, 80, 90));
    }

    [Test]
    public void LoadingAnotherColorCancelsTheReleaseAndRejectsLateWatches()
    {
        var editor = new AppearanceEditorViewModel();
        var applied = new List<TintMapColor>();
        Begin(editor, applied.Add);
        var finishRelease = Release(editor);
        Queue(editor, new GuiColor(70, 80, 90));
        Invoke(editor, "SetSelectedTintColor", new GuiColor(100, 110, 120), true);
        finishRelease();

        applied.Should().BeEmpty();
        Queue(editor, new GuiColor(0, 0, 0)).Should().BeFalse();
        editor.CustomTintRed.Should().Be("100");
        editor.CustomTintGreen.Should().Be("110");
        editor.CustomTintBlue.Should().Be("120");
    }

    [TestCase(NuiMouseButton.Middle)]
    [TestCase(NuiMouseButton.Right)]
    public void OtherMouseButtonsDoNotAuthorizeColorWatches(NuiMouseButton button)
    {
        var editor = new AppearanceEditorViewModel();
        var applied = new List<TintMapColor>();
        Begin(editor, applied.Add, button);
        Queue(editor, new GuiColor(70, 80, 90)).Should().BeFalse();
        Release(editor, button)?.Invoke();
        applied.Should().BeEmpty();
    }

    [TestCase(NuiMouseButton.Middle)]
    [TestCase(NuiMouseButton.Right)]
    public void OtherMouseButtonReleasesDoNotEndAnActiveLeftDrag(NuiMouseButton button)
    {
        var editor = new AppearanceEditorViewModel();
        var applied = new List<TintMapColor>();
        Begin(editor, applied.Add);
        Queue(editor, new GuiColor(10, 20, 30));

        Release(editor, button).Should().BeNull();
        applied.Should().BeEmpty();
        Queue(editor, new GuiColor(40, 50, 60)).Should().BeTrue();
        Invoke(editor, "FlushPendingPickerColor");
        applied.Should().Equal(new TintMapColor(40, 50, 60));
        Queue(editor, new GuiColor(70, 80, 90)).Should().BeTrue();
        Release(editor)();
        applied.Should().Equal(new TintMapColor(40, 50, 60), new TintMapColor(70, 80, 90));
        Queue(editor, new GuiColor(100, 110, 120)).Should().BeFalse();
    }

    private static void Begin(AppearanceEditorViewModel editor, Action<TintMapColor> apply,
        NuiMouseButton button = NuiMouseButton.Left) => Invoke(editor, "BeginTintPickerGesture", button, apply);

    private static Action Release(AppearanceEditorViewModel editor, NuiMouseButton button = NuiMouseButton.Left) =>
        (Action)Invoke(editor, "ReleaseTintPickerGesture", button);

    private static bool Queue(AppearanceEditorViewModel editor, GuiColor color) =>
        (bool)Invoke(editor, "QueueTintPickerColor", color);

    private static object Invoke(AppearanceEditorViewModel editor, string method, params object[] args) =>
        typeof(AppearanceEditorViewModel).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(editor, args);

    private static object Field(AppearanceEditorViewModel editor, string name) =>
        typeof(AppearanceEditorViewModel).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor);
}
