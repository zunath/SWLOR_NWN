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
        Stage(editor, new GuiColor(0, 0, 0))();
        Begin(editor, applied.Add, button);
        Field(editor, "_tintPickerActive").Should().Be(button == NuiMouseButton.Left);
        Release(editor, button);
        applied.Should().BeEmpty();
        Field(editor, "_pendingPickerColor").Should().BeNull();
        Field(editor, "_pendingPickerApply").Should().BeNull();
        Field(editor, "_tintPickerActive").Should().Be(false);
    }

    [TestCase(140, 94, 53)]
    [TestCase(0, 0, 0)]
    public void ColorWatchBeforeMouseDownAppliesTheClickOnce(byte red, byte green, byte blue)
    {
        var editor = new AppearanceEditorViewModel();
        var applied = new List<TintMapColor>();
        var expireWatch = Stage(editor, new GuiColor(red, green, blue));
        applied.Should().BeEmpty();

        Begin(editor, applied.Add);
        expireWatch();
        Release(editor);

        applied.Should().Equal(new TintMapColor(red, green, blue));
        editor.CustomTintRed.Should().Be(red.ToString());
        editor.CustomTintGreen.Should().Be(green.ToString());
        editor.CustomTintBlue.Should().Be(blue.ToString());
    }

    [Test]
    public void ExpiryOfAnOlderWatchDoesNotDiscardTheNextClickColor()
    {
        var editor = new AppearanceEditorViewModel();
        var applied = new List<TintMapColor>();
        var expireOldWatch = Stage(editor, new GuiColor(10, 20, 30));
        var expireClickWatch = Stage(editor, new GuiColor(140, 94, 53));
        expireOldWatch();
        Begin(editor, applied.Add);
        expireClickWatch();
        Release(editor);

        applied.Should().Equal(new TintMapColor(140, 94, 53));
    }

    [Test]
    public void LoadingAnotherColorDiscardsAnUnclaimedWatch()
    {
        var editor = new AppearanceEditorViewModel();
        var applied = new List<TintMapColor>();
        var expireWatch = Stage(editor, new GuiColor(140, 94, 53));
        Invoke(editor, "SetSelectedTintColor", new GuiColor(100, 110, 120), true);
        expireWatch();
        Begin(editor, applied.Add);
        Release(editor);

        applied.Should().BeEmpty();
        editor.CustomTintRed.Should().Be("100");
        editor.CustomTintGreen.Should().Be("110");
        editor.CustomTintBlue.Should().Be("120");
    }

    [TestCase(NuiMouseButton.Middle)]
    [TestCase(NuiMouseButton.Right)]
    public void OtherMouseButtonsDiscardAnUnclaimedWatch(NuiMouseButton button)
    {
        var editor = new AppearanceEditorViewModel();
        var applied = new List<TintMapColor>();
        Stage(editor, new GuiColor(140, 94, 53));
        Begin(editor, applied.Add, button);
        Begin(editor, applied.Add);
        Release(editor);

        applied.Should().BeEmpty();
    }

    [TestCase(205, 228, 197)]
    [TestCase(0, 0, 0)]
    public void ColorWatchAfterMouseUpDoesNotApplyWithoutAnotherPress(byte red, byte green, byte blue)
    {
        var editor = new AppearanceEditorViewModel();
        var applied = new List<TintMapColor>();
        Begin(editor, applied.Add);
        Release(editor);
        Queue(editor, new GuiColor(red, green, blue)).Should().BeFalse();
        Stage(editor, new GuiColor(red, green, blue))();
        applied.Should().BeEmpty();
    }

    [Test]
    public void DragStillAppliesThePressAndLiveColorsThroughRelease()
    {
        var editor = new AppearanceEditorViewModel();
        var applied = new List<TintMapColor>();
        Stage(editor, new GuiColor(10, 20, 30));
        Begin(editor, applied.Add);
        Field(editor, "_tintPickerActive").Should().Be(true);
        Queue(editor, new GuiColor(40, 50, 60));
        Invoke(editor, "FlushPendingPickerColor");
        Queue(editor, new GuiColor(70, 80, 90));
        Release(editor);

        applied.Should().Equal(new TintMapColor(10, 20, 30), new TintMapColor(40, 50, 60),
            new TintMapColor(70, 80, 90));
    }

    [Test]
    public void ConsecutiveClicksCaptureTheirOwnTargets()
    {
        var editor = new AppearanceEditorViewModel();
        var firstTarget = new List<TintMapColor>();
        var nextTarget = new List<TintMapColor>();
        Stage(editor, new GuiColor(10, 20, 30));
        Begin(editor, firstTarget.Add);
        Release(editor);
        Stage(editor, new GuiColor(70, 80, 90));
        Begin(editor, nextTarget.Add);
        Field(editor, "_tintPickerActive").Should().Be(true);
        Release(editor);
        firstTarget.Should().Equal(new TintMapColor(10, 20, 30));
        nextTarget.Should().Equal(new TintMapColor(70, 80, 90));
    }

    [Test]
    public void LoadingAnotherColorCancelsPendingDragColors()
    {
        var editor = new AppearanceEditorViewModel();
        var applied = new List<TintMapColor>();
        Begin(editor, applied.Add);
        Queue(editor, new GuiColor(70, 80, 90));
        Invoke(editor, "SetSelectedTintColor", new GuiColor(100, 110, 120), true);
        Release(editor);

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
        Release(editor, button);
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

        Release(editor, button);
        Field(editor, "_tintPickerActive").Should().Be(true);
        applied.Should().BeEmpty();
        Queue(editor, new GuiColor(40, 50, 60)).Should().BeTrue();
        Invoke(editor, "FlushPendingPickerColor");
        applied.Should().Equal(new TintMapColor(40, 50, 60));
        Queue(editor, new GuiColor(70, 80, 90)).Should().BeTrue();
        Release(editor);
        applied.Should().Equal(new TintMapColor(40, 50, 60), new TintMapColor(70, 80, 90));
        Queue(editor, new GuiColor(100, 110, 120)).Should().BeFalse();
    }

    private static void Begin(AppearanceEditorViewModel editor, Action<TintMapColor> apply,
        NuiMouseButton button = NuiMouseButton.Left) => Invoke(editor, "BeginTintPickerGesture", button, apply);

    private static void Release(AppearanceEditorViewModel editor, NuiMouseButton button = NuiMouseButton.Left) =>
        Invoke(editor, "ReleaseTintPickerGesture", button);

    private static bool Queue(AppearanceEditorViewModel editor, GuiColor color) =>
        (bool)Invoke(editor, "QueueTintPickerColor", color);

    private static Action Stage(AppearanceEditorViewModel editor, GuiColor color) =>
        (Action)Invoke(editor, "StageTintPickerColor", color);

    private static object Invoke(AppearanceEditorViewModel editor, string method, params object[] args) =>
        typeof(AppearanceEditorViewModel).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(editor, args);

    private static object Field(AppearanceEditorViewModel editor, string name) =>
        typeof(AppearanceEditorViewModel).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor);
}
