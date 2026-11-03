using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The Election window opens inside a 1280x720 client with its title bar visible, and each of its two steps (the candidate list
/// and the platform) scrolls inside the window instead of asking for a taller one. The tests read the widget tree the builder
/// validated; they do not run the client's layout solver.
/// </summary>
public class ElectionWindowLayoutTests
{
    [Test]
    public void TheElectionWindowBuildsWithoutLayoutWarnings()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();

        var window = new ElectionDefinition().BuildWindow();

        window.Type.Should().Be(GuiWindowType.Election);
        window.LayoutFindings.Should().BeEmpty();
    }

    [Test]
    public void TheElectionWindowOpensWithinA1280x720Client()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();

        var geometry = new ElectionDefinition().BuildWindow().InitialGeometry;

        // A window taller than the client is moved up until its bottom edge meets the screen, which put the title bar off it.
        geometry.Height.Should().BeLessThanOrEqualTo(680f);
        geometry.Width.Should().BeLessThanOrEqualTo(1280f);
    }

    [TestCase(ElectionViewModel.MainContentPartial)]
    [TestCase(ElectionViewModel.PlatformPartial)]
    public void EveryStepIsABorderlessAutoScrollPanel(string partial)
    {
        var authored = Authored(new ElectionDefinition());

        authored.Keys.Should().Contain(partial);
        // The partial's own root group is borderless and unscrolled; the panel is the first group inside it. The host group
        // only hands the panel the viewport height, so a panel that does not scroll clips a step taller than the window.
        var panel = authored[partial].Elements.SelectMany(Walk).First(IsGroup);

        ReadProperty<bool>(panel, "ShowBorder").Should().BeFalse();
        ReadProperty<NuiScrollbars>(panel, "Scrollbars").Should().Be(NuiScrollbars.Auto, $"{partial} is taller than a 640 pixel window");
    }

    private static IReadOnlyDictionary<string, IGuiWidget> Authored(ElectionDefinition definition)
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();
        ((IGuiWindowDefinition)definition).BuildWindow();

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var builder = typeof(ElectionDefinition).GetField("_builder", flags)!.GetValue(definition)!;
        var authoredWindow = builder.GetType().GetField("_activeWindow", flags)!.GetValue(builder)!;
        return (IReadOnlyDictionary<string, IGuiWidget>)authoredWindow.GetType().GetProperty("PartialViews")!.GetValue(authoredWindow)!;
    }

    private static bool IsGroup(IGuiWidget widget) =>
        widget.GetType().IsGenericType && widget.GetType().GetGenericTypeDefinition() == typeof(GuiGroup<>);

    private static IEnumerable<IGuiWidget> Walk(IGuiWidget widget) =>
        new[] { widget }.Concat(widget.Elements.SelectMany(Walk));

    private static T ReadProperty<T>(object target, string name)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        {
            var property = type.GetProperty(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (property != null)
                return (T)property.GetValue(target)!;
        }

        throw new InvalidOperationException($"Missing widget metadata {target.GetType().Name}.{name}");
    }
}
