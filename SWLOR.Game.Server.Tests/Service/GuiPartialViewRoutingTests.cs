using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Tests.Service;

[TestFixture]
public class GuiPartialViewRoutingTests
{
    private const string ContentSlot = "content_slot";
    private const string ChildSlot = "child_slot";

    private static readonly Dictionary<string, IReadOnlyCollection<string>> PartialElementIds = new()
    {
        [GuiPartialViewRouting.MainViewPartial] = new HashSet<string> { ContentSlot },
        ["TAB_PARTIAL"] = new HashSet<string> { ChildSlot }
    };

    [Test]
    public void MainViewSlotsUseTheRedrawSafeSwap()
    {
        GuiPartialViewRouting.RequiresRedrawSafeSwap(ContentSlot, GuiPartialViewRouting.MainViewPartial, PartialElementIds)
            .Should().BeTrue();
    }

    [Test]
    public void RootSwapsAreAppliedDirectly()
    {
        GuiPartialViewRouting.RequiresRedrawSafeSwap(
                GuiPartialViewRouting.WindowElementId, GuiPartialViewRouting.MainViewPartial, PartialElementIds)
            .Should().BeFalse();
    }

    [Test]
    public void SlotsNestedInsideAnotherPartialAreAppliedDirectly()
    {
        // A root redraw would wipe the parent partial and leave the child without an element.
        GuiPartialViewRouting.RequiresRedrawSafeSwap(ChildSlot, GuiPartialViewRouting.MainViewPartial, PartialElementIds)
            .Should().BeFalse();
        GuiPartialViewRouting.RequiresRedrawSafeSwap("no_such_element_id", GuiPartialViewRouting.MainViewPartial, PartialElementIds)
            .Should().BeFalse();
    }

    [Test]
    public void SwapsWhileAModalIsShowingAreAppliedDirectly()
    {
        // A root redraw would dismiss the modal.
        GuiPartialViewRouting.RequiresRedrawSafeSwap(ContentSlot, "%%WINDOW_MODAL%%", PartialElementIds)
            .Should().BeFalse();
        GuiPartialViewRouting.RequiresRedrawSafeSwap(ContentSlot, null, PartialElementIds)
            .Should().BeFalse();
    }

    [Test]
    public void SettingsContentSlotIsRoutedThroughTheRedrawSafeSwap()
    {
        using var validationBuild = GuiLayoutValidator.BeginValidationOnlyBuild();
        var window = new SettingsDefinition().BuildWindow();

        GuiPartialViewRouting.RequiresRedrawSafeSwap(
                SettingsViewModel.SettingsView, GuiPartialViewRouting.MainViewPartial, window.PartialElementIds)
            .Should().BeTrue();
    }

    [Test]
    public void EveryWindowDescribesItsMainViewElements()
    {
        using var validationBuild = GuiLayoutValidator.BeginValidationOnlyBuild();
        var definitionTypes = typeof(IGuiWindowDefinition).Assembly
            .GetTypes()
            .Where(type => typeof(IGuiWindowDefinition).IsAssignableFrom(type) &&
                           !type.IsInterface &&
                           !type.IsAbstract);

        foreach (var definitionType in definitionTypes)
        {
            var window = ((IGuiWindowDefinition)Activator.CreateInstance(definitionType, true)!).BuildWindow();

            window.PartialElementIds.Should().ContainKey(GuiPartialViewRouting.MainViewPartial,
                $"{definitionType.Name} must expose its main view so nested swaps can be routed");
        }
    }
}
