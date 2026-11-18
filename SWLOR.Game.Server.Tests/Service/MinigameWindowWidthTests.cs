using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Guards the minimum width of the minigame windows whose sentences wrap to the window: their text boxes are sized for the default
/// width, so a narrower window would cut the text off and is widened back.
/// </summary>
public class MinigameWindowWidthTests
{
    [Test]
    public void ANarrowerWindow_IsWidenedToTheMinimum_KeepingItsPlaceAndHeight()
    {
        var widened = MinigameWindowWidth.Widened(new GuiRectangle(40f, 60f, 420f, 500f), 620f);

        widened.Should().NotBeNull();
        widened!.X.Should().Be(40f);
        widened.Y.Should().Be(60f);
        widened.Width.Should().Be(620f);
        widened.Height.Should().Be(500f);
    }

    [TestCase(620f)]
    [TestCase(1150f)]
    public void AWindowAtLeastTheMinimum_IsLeftAlone(float width)
    {
        MinigameWindowWidth.Widened(new GuiRectangle(0f, 0f, width, 660f), 620f).Should().BeNull();
    }

    [Test]
    public void AWindowWithNoGeometryYet_IsLeftAlone()
    {
        MinigameWindowWidth.Widened(null, 620f).Should().BeNull();
    }

    private static IEnumerable<TestCaseData> Windows()
    {
        yield return Case<ProspectingDefinition, ProspectingViewModel>(ProspectingDefinition.WindowWidth);
        yield return Case<LineDefinition, LineViewModel>(LineDefinition.WindowWidth);
        yield return Case<SearchDefinition, SearchViewModel>(SearchDefinition.WindowWidth);
        yield return Case<ReactionDefinition, ReactionViewModel>(ReactionDefinition.WindowWidth);
        yield return Case<ForgeDefinition, ForgeViewModel>(ForgeDefinition.WindowWidth);
        yield return Case<KitchenDefinition, KitchenViewModel>(KitchenDefinition.WindowWidth);
        yield return Case<CircuitCraftDefinition, CircuitCraftViewModel>(CircuitCraftDefinition.WindowWidth);
        yield return Case<FabricatorCraftDefinition, FabricatorCraftViewModel>(FabricatorCraftDefinition.WindowWidth);
    }

    private static TestCaseData Case<TDefinition, TViewModel>(float width) =>
        new TestCaseData(typeof(TViewModel), width).SetName(typeof(TDefinition).Name);

    [TestCaseSource(nameof(Windows))]
    public void TheWindow_KeepsItsDefaultWidthAsItsMinimum(Type viewModelType, float defaultWidth)
    {
        var property = viewModelType.GetProperty("MinimumWindowWidth", BindingFlags.Instance | BindingFlags.NonPublic)!;
        property.DeclaringType.Should().Be(viewModelType, "each window sets its own minimum");

        var model = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(viewModelType);
        ((float)property.GetValue(model)!).Should().Be(defaultWidth);
    }
}
