using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Tests.Service;

[TestFixture]
public class GuiPartialViewStateTests
{
    private const string TabSlot = "tab_slot";
    private const string ChildSlot = "child_slot";
    private const string SidebarSlot = "sidebar_slot";
    private const string ParentWithChild = "PARENT_WITH_CHILD";
    private const string ParentWithoutChild = "PARENT_WITHOUT_CHILD";
    private const string Leaf = "LEAF";

    private static readonly Dictionary<string, IReadOnlyCollection<string>> PartialElementIds = new()
    {
        [GuiPartialViewState.MainViewPartial] = new[] { TabSlot, SidebarSlot },
        [ParentWithChild] = new[] { ChildSlot },
        [ParentWithoutChild] = Array.Empty<string>(),
        [Leaf] = Array.Empty<string>()
    };

    private static GuiPartialViewState CreateState()
    {
        var state = new GuiPartialViewState(name => PartialElementIds.TryGetValue(name, out var ids) ? ids : null);
        state.SetRootPartial(GuiPartialViewState.MainViewPartial);
        return state;
    }

    private static IEnumerable<(string, string)> Layouts(GuiPartialViewState state) =>
        state.NestedLayouts.Select(layout => (layout.ElementId, layout.PartialName));

    [Test]
    public void SwitchingTabsReplacesTheTrackedPartialInPlace()
    {
        var state = CreateState();

        state.SetNestedPartial(TabSlot, Leaf);
        state.SetNestedPartial(SidebarSlot, Leaf);
        state.SetNestedPartial(TabSlot, ParentWithoutChild);

        Layouts(state).Should().Equal((TabSlot, ParentWithoutChild), (SidebarSlot, Leaf));
    }

    [Test]
    public void ChildrenAreKeptInParentFirstOrder()
    {
        var state = CreateState();

        state.SetNestedPartial(TabSlot, ParentWithChild);
        state.SetNestedPartial(ChildSlot, Leaf);
        state.SetNestedPartial(TabSlot, ParentWithChild);

        Layouts(state).Should().Equal((TabSlot, ParentWithChild), (ChildSlot, Leaf));
    }

    [Test]
    public void ReplacingAParentForgetsItsChildren()
    {
        var state = CreateState();

        state.SetNestedPartial(TabSlot, ParentWithChild);
        state.SetNestedPartial(ChildSlot, Leaf);
        state.SetNestedPartial(TabSlot, ParentWithoutChild);
        state.SetNestedPartial(TabSlot, ParentWithChild);

        Layouts(state).Should().Equal((TabSlot, ParentWithChild));
    }

    [Test]
    public void ElementsOutsideTheLayoutTreeAreNotTracked()
    {
        var state = CreateState();

        state.SetNestedPartial("no_such_element_id", Leaf);
        state.SetNestedPartial(ChildSlot, Leaf);

        state.NestedLayouts.Should().BeEmpty("re-applying a missing element id raises a client-side layout error");
        state.IsTracked(ChildSlot).Should().BeFalse();
    }

    [Test]
    public void NestedLayoutsSurviveAModalAndAreClearedOnReset()
    {
        var state = CreateState();
        state.SetNestedPartial(TabSlot, Leaf);

        state.SetRootPartial("%%WINDOW_MODAL%%");
        state.IsMainViewShown.Should().BeFalse();
        state.SetNestedPartial(TabSlot, ParentWithChild);

        state.SetRootPartial(GuiPartialViewState.MainViewPartial);
        state.IsMainViewShown.Should().BeTrue();
        Layouts(state).Should().Equal((TabSlot, ParentWithChild));

        state.Reset();
        state.IsMainViewShown.Should().BeFalse();
        state.NestedLayouts.Should().BeEmpty();
    }

    [Test]
    public void SettingsWindowExposesItsContentSlotToTheTracker()
    {
        using var validationBuild = GuiLayoutValidator.BeginValidationOnlyBuild();
        var window = new SettingsDefinition().BuildWindow();

        window.PartialElementIds[GuiPartialViewState.MainViewPartial]
            .Should().Contain(SettingsViewModel.SettingsView);

        foreach (var partial in new[]
                 {
                     SettingsViewModel.GeneralPartial,
                     SettingsViewModel.IdentityPartial,
                     SettingsViewModel.ChatPartial
                 })
        {
            window.PartialElementIds.Should().ContainKey(partial);
        }
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
            var window = ((IGuiWindowDefinition)Activator.CreateInstance(definitionType)!).BuildWindow();

            window.PartialElementIds.Should().ContainKey(GuiPartialViewState.MainViewPartial,
                $"{definitionType.Name} must expose its main view so nested partials can be restored");
        }
    }
}
