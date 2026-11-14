using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Guards the conversation window's speaker name. A one-line label cannot wrap, so a fixed-width parent such as the
/// 140-wide portrait column clipped names like "Nar Shaddaa Security". The tests read the widget tree the builder
/// validated; they do not run the client's layout solver.
/// </summary>
public class ConversationWindowLayoutTests
{
    [Test]
    public void SpeakerName_IsTheFirstRowAndSpansTheWindow()
    {
        var path = PathTo(MainPartial(), IsSpeakerNameLabel);
        path.Should().NotBeNull("the window shows the speaker's name");

        var label = path![^1];
        var nameRow = path[^2];
        var authoredRoot = path[^3];
        authoredRoot.Elements.First().Should().BeSameAs(nameRow, "the name sits above the portrait and dialogue");
        nameRow.Elements.Should().ContainSingle("the name row holds only the name");
        ReadProperty<NuiHorizontalAlign>(label, "HorizontalAlign").Should().Be(NuiHorizontalAlign.Left,
            "the name starts above the portrait and runs across the window");
    }

    [Test]
    public void SpeakerName_HasNoFixedWidthAncestor()
    {
        var path = PathTo(MainPartial(), IsSpeakerNameLabel);

        path.Should().NotBeNull("the window shows the speaker's name");
        path!.Count(IsSpeakerNameLabel).Should().Be(1);
        foreach (var widget in path)
        {
            ReadProperty<float>(widget, "Width").Should().Be(0f,
                $"{widget.GetType().Name} would clip a long name to its fixed width");
        }
    }

    private static bool IsSpeakerNameLabel(IGuiWidget widget) =>
        widget.GetType().IsGenericType &&
        widget.GetType().GetGenericTypeDefinition() == typeof(GuiLabel<>) &&
        ReadProperty<string>(widget, "TextBindName") == nameof(ConversationViewModel.SpeakerName);

    private static IReadOnlyList<IGuiWidget>? PathTo(IGuiWidget widget, Func<IGuiWidget, bool> match)
    {
        if (match(widget))
            return new[] { widget };

        foreach (var child in widget.Elements)
        {
            var path = PathTo(child, match);
            if (path != null)
                return new[] { widget }.Concat(path).ToArray();
        }

        return null;
    }

    private static IGuiWidget MainPartial()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();
        var definition = new ConversationWindowDefinition();
        var window = definition.BuildWindow();
        window.LayoutFindings.Should().BeEmpty();

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var builder = typeof(ConversationWindowDefinition).GetField("_builder", flags)!.GetValue(definition)!;
        var authoredWindow = builder.GetType().GetField("_activeWindow", flags)!.GetValue(builder)!;
        var partials = (IReadOnlyDictionary<string, IGuiWidget>)authoredWindow.GetType().GetProperty("PartialViews")!.GetValue(authoredWindow)!;
        return partials["%%WINDOW_MAIN%%"];
    }

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
