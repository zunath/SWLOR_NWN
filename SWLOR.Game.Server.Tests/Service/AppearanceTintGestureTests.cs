using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
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
        editor.SelectedTintColor = new GuiColor(0, 0, 0);
        typeof(AppearanceEditorViewModel).GetMethod("BeginTintPickerGesture", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(editor, new object[] { button });
        Field(editor, "_tintPickerActive").Should().Be(button == NuiMouseButton.Left);
        editor.OnMouseUpTintPicker()();
        Field(editor, "_pendingPickerColor").Should().BeNull();
        Field(editor, "_pendingPickerApply").Should().BeNull();
        Field(editor, "_tintPickerActive").Should().Be(false);
    }

    private static object Field(AppearanceEditorViewModel editor, string name) =>
        typeof(AppearanceEditorViewModel).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor);
}
