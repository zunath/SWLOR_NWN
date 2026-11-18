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
        yield return Case<ResearchExperimentDefinition>(ResearchExperimentViewModel.MainPartial);
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

    private static IEnumerable<TestCaseData> WindowsThatReflow()
    {
        yield return Case<ForgeDefinition>(ForgeViewModel.SetupPartial, ForgeViewModel.CraftPartial);
        yield return Case<CircuitCraftDefinition>(CircuitCraftViewModel.SetupPartial, CircuitCraftViewModel.CraftPartial);
        yield return Case<FabricatorCraftDefinition>(FabricatorCraftViewModel.SetupPartial, FabricatorCraftViewModel.CraftPartial);
        yield return Case<KitchenDefinition>(KitchenViewModel.SetupPartial, KitchenViewModel.CookPartial);
        yield return Case<ReactionDefinition>(ReactionViewModel.SetupPartial, ReactionViewModel.CraftPartial);
        yield return Case<LineDefinition>(LineViewModel.FightPartial);
        yield return Case<SearchDefinition>(SearchViewModel.BoardPartial);
    }

    [TestCaseSource(nameof(WindowsThatReflow))]
    public void ThePanelHasNoFixedWidth_SoItsTextReflowsWithTheWindow(Type definitionType, string[] partials)
    {
        var authored = Authored(definitionType);

        foreach (var name in partials)
        {
            var panel = authored[name].Elements.SelectMany(Walk).First(IsGroup);
            ReadProperty<float>(panel, "Width").Should().Be(0f, $"{name}: a fixed-width panel leaves the text clipped however wide the window is made");
        }
    }

    private static IEnumerable<TestCaseData> LongSentences()
    {
        TestCaseData Sentence<TDefinition>(string partial, string bindName, int lines) where TDefinition : IGuiWindowDefinition, new() =>
            new TestCaseData(typeof(TDefinition), partial, bindName, lines).SetName($"{typeof(TDefinition).Name}_{bindName}");

        for (var index = 0; index < ReactionViewModel.ActionCount; index++)
            yield return Sentence<ReactionDefinition>(ReactionViewModel.CraftPartial, ReactionViewModel.ActionSummaryProperty(index), 3);

        yield return Sentence<LineDefinition>(LineViewModel.FightPartial, nameof(LineViewModel.QuickReelSummary), 3);
        for (var index = 0; index < LineViewModel.ActionCount; index++)
            yield return Sentence<LineDefinition>(LineViewModel.FightPartial, LineViewModel.ActionSummaryProperty(index), 2);

        yield return Sentence<SearchDefinition>(SearchViewModel.BoardPartial, nameof(SearchViewModel.QuickSearchSummary), 3);
        yield return Sentence<SearchDefinition>(SearchViewModel.BoardPartial, nameof(SearchViewModel.StyleLabels), 3);
        yield return Sentence<SearchDefinition>(SearchViewModel.BoardPartial, nameof(SearchViewModel.StyleHeader), 2);
        yield return Sentence<SearchDefinition>(SearchViewModel.BoardPartial, nameof(SearchViewModel.BoardLabelText), 2);
        yield return Sentence<SearchDefinition>(SearchViewModel.BoardPartial, nameof(SearchViewModel.SelectedText), 2);
        for (var index = 0; index < SearchViewModel.ActionCount; index++)
            yield return Sentence<SearchDefinition>(SearchViewModel.BoardPartial, SearchViewModel.ActionSummaryProperty(index), 2);

        yield return Sentence<ForgeDefinition>(ForgeViewModel.SetupPartial, nameof(ForgeViewModel.SlotLabels), 2);
        yield return Sentence<ForgeDefinition>(ForgeViewModel.SetupPartial, nameof(ForgeViewModel.EnhancementSummaryText), 2);
        yield return Sentence<ForgeDefinition>(ForgeViewModel.CraftPartial, nameof(ForgeViewModel.OpportunityText), 2);
        yield return Sentence<ForgeDefinition>(ForgeViewModel.CraftPartial, nameof(ForgeViewModel.ThermalText), 2);
        yield return Sentence<ForgeDefinition>(ForgeViewModel.CraftPartial, nameof(ForgeViewModel.ThresholdsText), 2);
        yield return Sentence<ForgeDefinition>(ForgeViewModel.CraftPartial, nameof(ForgeViewModel.PerkText), 2);
        yield return Sentence<ForgeDefinition>(ForgeViewModel.CraftPartial, nameof(ForgeViewModel.ActionPreviews), 2);

        yield return Sentence<KitchenDefinition>(KitchenViewModel.SetupPartial, nameof(KitchenViewModel.EnhancementTexts), 2);
        yield return Sentence<KitchenDefinition>(KitchenViewModel.CookPartial, nameof(KitchenViewModel.DistanceText), 2);
        yield return Sentence<KitchenDefinition>(KitchenViewModel.CookPartial, nameof(KitchenViewModel.ThresholdText), 2);
        yield return Sentence<KitchenDefinition>(KitchenViewModel.CookPartial, nameof(KitchenViewModel.ActionSummaries), 2);

        yield return Sentence<CircuitCraftDefinition>(CircuitCraftViewModel.SetupPartial, nameof(CircuitCraftViewModel.GearSummary), 2);
        yield return Sentence<CircuitCraftDefinition>(CircuitCraftViewModel.SetupPartial, nameof(CircuitCraftViewModel.CircuitSummary), 2);
        yield return Sentence<CircuitCraftDefinition>(CircuitCraftViewModel.SetupPartial, nameof(CircuitCraftViewModel.SlotHeader), 2);
        yield return Sentence<CircuitCraftDefinition>(CircuitCraftViewModel.CraftPartial, nameof(CircuitCraftViewModel.OpportunityText), 2);
        yield return Sentence<CircuitCraftDefinition>(CircuitCraftViewModel.CraftPartial, nameof(CircuitCraftViewModel.ThresholdText), 2);

        yield return Sentence<FabricatorCraftDefinition>(FabricatorCraftViewModel.SetupPartial, nameof(FabricatorCraftViewModel.GearSummary), 2);
        yield return Sentence<FabricatorCraftDefinition>(FabricatorCraftViewModel.SetupPartial, nameof(FabricatorCraftViewModel.SlotHeader), 2);
        yield return Sentence<FabricatorCraftDefinition>(FabricatorCraftViewModel.CraftPartial, nameof(FabricatorCraftViewModel.OpportunityText), 2);
        yield return Sentence<FabricatorCraftDefinition>(FabricatorCraftViewModel.CraftPartial, nameof(FabricatorCraftViewModel.QualityText), 2);
        yield return Sentence<FabricatorCraftDefinition>(FabricatorCraftViewModel.CraftPartial, nameof(FabricatorCraftViewModel.ThresholdText), 2);
    }

    [TestCaseSource(nameof(LongSentences))]
    public void ALongSentence_IsWrappedText_TallEnoughForItsLines(Type definitionType, string partial, string bindName, int lines)
    {
        var authored = Authored(definitionType);
        var matches = WalkWithList(authored[partial], null).Where(found => TextBindName(found.Widget) == bindName).ToArray();

        matches.Should().ContainSingle($"{bindName} is bound once in {partial}");
        var (text, list) = matches.Single();
        text.GetType().GetGenericTypeDefinition().Should().Be(typeof(GuiText<>), "a label cuts a long sentence off at one line");
        ReadProperty<bool>(text, "ShowBorder").Should().BeFalse();

        // A text in a list cell takes the list's row height; any other text declares its own.
        var height = list != null ? ReadProperty<float>(list, "RowHeight") : text.DeclaredHeight;
        height.Should().BeGreaterThanOrEqualTo(lines * 20f, $"{bindName} needs {lines} lines in the default window");
    }

    private static IEnumerable<TestCaseData> LabelsSharingARow()
    {
        TestCaseData Label<TDefinition>(string partial, string bindName) where TDefinition : IGuiWindowDefinition, new() =>
            new TestCaseData(typeof(TDefinition), partial, bindName).SetName($"{typeof(TDefinition).Name}_{bindName}");

        yield return Label<ReactionDefinition>(ReactionViewModel.CraftPartial, nameof(ReactionViewModel.PhaseNowText));
        yield return Label<ReactionDefinition>(ReactionViewModel.CraftPartial, nameof(ReactionViewModel.PhaseNextText));
        yield return Label<ReactionDefinition>(ReactionViewModel.CraftPartial, nameof(ReactionViewModel.PhaseThenText));
        yield return Label<SearchDefinition>(SearchViewModel.BoardPartial, nameof(SearchViewModel.FindsText));
    }

    [TestCaseSource(nameof(LabelsSharingARow))]
    public void ALabelSharingARowInAPanelWithNoWidth_HasAWidthOfItsOwn(Type definitionType, string partial, string bindName)
    {
        var authored = Authored(definitionType);
        var label = Walk(authored[partial]).Single(widget => TextBindName(widget) == bindName);

        // In the client the first label without a width takes the whole row and its neighbours are cut down to nothing.
        ReadProperty<float>(label, "Width").Should().BeGreaterThan(0f, $"{bindName} shares its row with other labels");
    }

    private static IEnumerable<TestCaseData> RecipeDetailLists()
    {
        yield return new TestCaseData(typeof(CircuitCraftDefinition), CircuitCraftViewModel.SetupPartial).SetName(nameof(CircuitCraftDefinition));
        yield return new TestCaseData(typeof(FabricatorCraftDefinition), FabricatorCraftViewModel.SetupPartial).SetName(nameof(FabricatorCraftDefinition));
    }

    [TestCaseSource(nameof(RecipeDetailLists))]
    public void TheRecipeDetailList_HasAHeightOfItsOwn(Type definitionType, string partial)
    {
        var authored = Authored(definitionType);
        var list = WalkWithList(authored[partial], null)
            .Single(found => TextBindName(found.Widget) == nameof(CircuitCraftViewModel.RecipeDescription)).List;

        list.DeclaredHeight.Should().BeGreaterThan(0f, "a list with no height of its own collapses to a sliver in a scrolled panel");
    }

    private static IEnumerable<(IGuiWidget Widget, IGuiWidget List)> WalkWithList(IGuiWidget widget, IGuiWidget list)
    {
        yield return (widget, list);
        var inner = IsList(widget) ? widget : list;
        foreach (var found in widget.Elements.SelectMany(child => WalkWithList(child, inner)))
            yield return found;
    }

    private static bool IsList(IGuiWidget widget) =>
        widget.GetType().IsGenericType && widget.GetType().GetGenericTypeDefinition() == typeof(GuiList<>);

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
