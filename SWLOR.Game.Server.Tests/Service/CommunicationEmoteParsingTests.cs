using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

// Covers the pure emote-splitting parser used to color and translate in-character chat.
// These methods make no NWN native calls, so they can be exercised directly via reflection.
public class CommunicationEmoteParsingTests
{
    private sealed class ParsedComponent
    {
        public string Text { get; init; }
        public bool IsTranslatable { get; init; }
        public bool IsCustomColor { get; init; }
    }

    [Test]
    public void RegularStyle_BracketEmote_IsCustomColoredAndNotTranslatable()
    {
        var components = SplitRegular("[waves]");

        var emote = components.Single(c => c.Text == "waves");
        emote.IsCustomColor.Should().BeTrue("bracketed emotes must carry the emote color");
        emote.IsTranslatable.Should().BeFalse("emotes are never language-translated");
    }

    [Test]
    public void RegularStyle_AsteriskEmote_IsCustomColored()
    {
        var components = SplitRegular("*waves*");

        var emote = components.Single(c => c.Text == "*waves*");
        emote.IsCustomColor.Should().BeTrue();
        emote.IsTranslatable.Should().BeFalse();
    }

    [Test]
    public void RegularStyle_BracketEmoteWithSpokenText_SplitsColorAndTranslationCorrectly()
    {
        var components = SplitRegular("hello [waves] there");

        var spokenBefore = components.Single(c => c.Text == "hello ");
        spokenBefore.IsTranslatable.Should().BeTrue();
        spokenBefore.IsCustomColor.Should().BeFalse();

        var emote = components.Single(c => c.Text == "waves");
        emote.IsCustomColor.Should().BeTrue();
        emote.IsTranslatable.Should().BeFalse();

        var spokenAfter = components.Single(c => c.Text == " there");
        spokenAfter.IsTranslatable.Should().BeTrue();
        spokenAfter.IsCustomColor.Should().BeFalse();
    }

    private static List<ParsedComponent> SplitRegular(string message)
    {
        var result = (IEnumerable)typeof(Communication)
            .GetMethod(
                "SplitMessageIntoComponents_Regular",
                BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { message })!;

        return result.Cast<object>().Select(Project).ToList();
    }

    private static ParsedComponent Project(object component)
    {
        var type = component.GetType();
        return new ParsedComponent
        {
            Text = (string)type.GetProperty("Text")!.GetValue(component),
            IsTranslatable = (bool)type.GetProperty("IsTranslatable")!.GetValue(component),
            IsCustomColor = (bool)type.GetProperty("IsCustomColor")!.GetValue(component)
        };
    }
}
