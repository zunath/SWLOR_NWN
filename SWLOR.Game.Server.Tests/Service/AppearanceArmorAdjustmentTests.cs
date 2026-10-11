using System.Reflection;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class AppearanceArmorAdjustmentTests
{
    private static IEnumerable<AppearanceArmor> Parts => Enum.GetValues<AppearanceArmor>()
        .Where(part => part >= AppearanceArmor.RightFoot && part < AppearanceArmor.Robe);

    [TestCaseSource(nameof(Parts))]
    public void UnloadedEmptyAndStaleOptionsDoNotEditArmor(AppearanceArmor part)
    {
        var editor = new AppearanceEditorViewModel { IsEquipmentSelected = true, HasItemEquipped = true };
        var options = typeof(AppearanceEditorViewModel).GetProperty(PropertyPrefix(part) + "Options")!;
        var notifications = new List<string>();
        editor.PropertyChanged += (_, change) => notifications.Add(change.PropertyName!);

        foreach (var values in new GuiBindingList<GuiComboEntry>[]
                 {
                     null,
                     new(),
                     new() { new("Unavailable selection", 20) }
                 })
        {
            options.SetValue(editor, values);
            notifications.Clear();
            Assert.DoesNotThrow(() => Adjust(editor, part, 1));
            Assert.That(notifications, Is.Empty, "unloaded or stale controls must not change the item");
            Assert.That(SkipField.GetValue(editor), Is.False);
        }
    }

    [TestCaseSource(nameof(Parts))]
    public void SelectionUsesModelIdsAndRestoresSuppressionAfterFailure(AppearanceArmor part)
    {
        var editor = new AppearanceEditorViewModel { IsEquipmentSelected = true, HasItemEquipped = true };
        var prefix = PropertyPrefix(part);
        typeof(AppearanceEditorViewModel).GetProperty(prefix + "Options")!.SetValue(editor,
            new GuiBindingList<GuiComboEntry> { new("None", 0), new("Twenty", 20) });
        var selection = typeof(AppearanceEditorViewModel).GetProperty(prefix + "Selection")!;
        editor.PropertyChanged += (_, change) =>
        {
            Assert.That(change.PropertyName, Is.EqualTo(selection.Name));
            Assert.That(selection.GetValue(editor), Is.EqualTo(20));
            // Fail before ModifyItemPart calls the native engine.
            throw new InvalidOperationException("Selection publication failed");
        };

        foreach (var wasSkipping in new[] { false, true })
        {
            SkipField.SetValue(editor, wasSkipping);
            var failure = Assert.Throws<TargetInvocationException>(() => Adjust(editor, part, 1));
            Assert.That(failure!.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(failure.InnerException!.Message, Is.EqualTo("Selection publication failed"));
            Assert.That(SkipField.GetValue(editor), Is.EqualTo(wasSkipping));
        }
    }

    [TestCase(1, -1, 1)]
    [TestCase(1, 1, 20)]
    [TestCase(20, -1, 1)]
    [TestCase(20, 1, 75)]
    [TestCase(20, 0, 20)]
    [TestCase(75, 1, 75)]
    [TestCase(999, 1, null)]
    public void AdjustmentClampsAtTheEndsAndPreservesSparseModelIds(int value, int adjustBy, int? expected)
    {
        var options = new GuiBindingList<GuiComboEntry> { new("One", 1), new("Twenty", 20), new("Seventy-five", 75) };
        var result = typeof(AppearanceEditorViewModel)
            .GetMethod("ResolveAdjustedArmorValue", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { options, value, adjustBy });
        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase(false, 0, true)]
    [TestCase(true, 1, true)]
    [TestCase(true, 0, false)]
    public void AdjustmentIgnoresInactiveOrUnequippedArmor(bool equipmentSelected, int itemType, bool equipped)
    {
        var editor = new AppearanceEditorViewModel { IsEquipmentSelected = equipmentSelected, HasItemEquipped = equipped };
        // Use the binding store to avoid loading equipment through the native engine.
        typeof(AppearanceEditorViewModel).BaseType!.GetMethod("Set", BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(typeof(int)).Invoke(editor, new object[] { itemType, nameof(editor.SelectedItemTypeIndex) });
        Assert.DoesNotThrow(() => Adjust(editor, AppearanceArmor.Robe, 1));
        if (!equipmentSelected || itemType != 0)
            Assert.DoesNotThrow(() => editor.OnClickAdjustArmorPart(AppearanceArmor.LeftBicep, 1)());
    }

    private static FieldInfo SkipField => typeof(AppearanceEditorViewModel)
        .GetField("_skipAdjustArmorPart", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static string PropertyPrefix(AppearanceArmor part) => part == AppearanceArmor.Torso ? "Chest" : part.ToString();

    private static void Adjust(AppearanceEditorViewModel editor, AppearanceArmor part, int adjustBy) =>
        typeof(AppearanceEditorViewModel).GetMethod("AdjustArmorPart", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(editor, new object[] { part, adjustBy });
}
