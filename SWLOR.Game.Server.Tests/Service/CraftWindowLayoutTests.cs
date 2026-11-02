using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Guards the shape every crafting and gathering minigame window shares: each content page is a borderless Auto-scroll panel, so a
/// page taller than a short screen scrolls instead of running off the window, and the one-line result sentences wrap instead of
/// being cut off. The tests read the widget tree the builder validated; they do not run the client's layout solver.
/// </summary>
public class CraftWindowLayoutTests
{
    private static IEnumerable<TestCaseData> Windows()
    {
        yield return Case<ForgeDefinition>(ForgeViewModel.SetupPartial, ForgeViewModel.CraftPartial);
        yield return Case<CircuitCraftDefinition>(CircuitCraftViewModel.SetupPartial, CircuitCraftViewModel.CraftPartial);
        yield return Case<FabricatorCraftDefinition>(FabricatorCraftViewModel.SetupPartial, FabricatorCraftViewModel.CraftPartial);
        yield return Case<KitchenDefinition>(KitchenViewModel.SetupPartial, KitchenViewModel.CookPartial);
        yield return Case<ReactionDefinition>(ReactionViewModel.SetupPartial, ReactionViewModel.CraftPartial);
        yield return Case<LineDefinition>(LineViewModel.FightPartial);
        yield return Case<ProspectingDefinition>(ProspectingViewModel.NodePartial);
        yield return Case<SearchDefinition>(SearchViewModel.BoardPartial);
        yield return Case<ResearchExperimentDefinition>(ResearchExperimentViewModel.MainPartial);
    }

    private static TestCaseData Case<TDefinition>(params string[] partials) where TDefinition : IGuiWindowDefinition, new() =>
        new TestCaseData(typeof(TDefinition), partials).SetName($"{typeof(TDefinition).Name}");

    private static IEnumerable<TestCaseData> WindowsWithWrappedStatus()
    {
        yield return Case<ReactionDefinition>(ReactionViewModel.SetupPartial, ReactionViewModel.CraftPartial);
        yield return Case<LineDefinition>(LineViewModel.FightPartial);
        yield return Case<ProspectingDefinition>(ProspectingViewModel.NodePartial);
        yield return Case<SearchDefinition>(SearchViewModel.BoardPartial);
    }

    [TestCaseSource(nameof(Windows))]
    public void EveryContentPageIsABorderlessAutoScrollPanel(Type definitionType, string[] partials)
    {
        var authored = Authored(definitionType);

        authored.Keys.Should().Contain(partials);
        authored.Keys.Should().Contain(CraftMinigameHelp.PartialName, "every minigame window carries its help page");
        foreach (var name in partials)
        {
            // The partial's own root group is always borderless and unscrolled; the panel is the first group inside it.
            var panel = authored[name].Elements.SelectMany(Walk).First(IsGroup);

            ReadProperty<bool>(panel, "ShowBorder").Should().BeFalse($"{name} is a borderless panel");
            ReadProperty<NuiScrollbars>(panel, "Scrollbars").Should().Be(
                NuiScrollbars.Auto,
                $"{name} must scroll when the window is shorter than the page; the host group only hands the panel the viewport height");
        }
    }

    [TestCaseSource(nameof(WindowsWithWrappedStatus))]
    public void TheResultLineIsWrappedText_NotAOneLineLabel(Type definitionType, string[] partials)
    {
        var authored = Authored(definitionType);

        foreach (var name in partials)
        {
            var status = Walk(authored[name])
                .Where(widget => TextBindName(widget) == nameof(ForgeViewModel.StatusText))
                .ToArray();

            status.Should().ContainSingle($"{name} binds StatusText once");
            var text = status.Single();
            text.GetType().GetGenericTypeDefinition().Should().Be(typeof(GuiText<>), "a label cuts a long sentence off");
            ReadProperty<string>(text, "ColorBindName").Should().Be(nameof(ForgeViewModel.StatusColor));
            ReadProperty<bool>(text, "ShowBorder").Should().BeFalse();
            ReadProperty<NuiScrollbars>(text, "Scrollbars").Should().Be(NuiScrollbars.None);
            text.DeclaredHeight.Should().BeInRange(44f, 70f, "two or three lines of the panel's width");
        }
    }

    private static IReadOnlyDictionary<string, IGuiWidget> Authored(Type definitionType)
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();
        var definition = Activator.CreateInstance(definitionType)!;
        var window = ((IGuiWindowDefinition)definition).BuildWindow();
        window.LayoutFindings.Should().BeEmpty();

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var builder = definitionType.GetField("_builder", flags)!.GetValue(definition)!;
        var authoredWindow = builder.GetType().GetField("_activeWindow", flags)!.GetValue(builder)!;
        return (IReadOnlyDictionary<string, IGuiWidget>)authoredWindow.GetType().GetProperty("PartialViews")!.GetValue(authoredWindow)!;
    }

    private static bool IsGroup(IGuiWidget widget) =>
        widget.GetType().IsGenericType && widget.GetType().GetGenericTypeDefinition() == typeof(GuiGroup<>);

    private static string TextBindName(IGuiWidget widget) =>
        widget.GetType().IsGenericType &&
        (widget.GetType().GetGenericTypeDefinition() == typeof(GuiText<>) || widget.GetType().GetGenericTypeDefinition() == typeof(GuiLabel<>))
            ? ReadProperty<string>(widget, "TextBindName")
            : null;

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
