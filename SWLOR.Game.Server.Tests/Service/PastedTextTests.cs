using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Service;

public class PastedTextTests
{
    [TestCase(null, "")]
    [TestCase("", "")]
    [TestCase("Plain text, nothing to change.", "Plain text, nothing to change.")]
    public void Clean_LeavesPlainTextAlone(string text, string expected)
    {
        PastedText.Clean(text).Should().Be(expected);
    }

    [Test]
    public void Clean_ChangesWindowsLineEndingsToNewlines()
    {
        PastedText.Clean("First.\r\n\r\nSecond.\rThird.\n")
            .Should().Be("First.\n\nSecond.\nThird.\n");
    }

    [Test]
    public void Clean_ChangesUnicodeLineAndParagraphSeparatorsToNewlines()
    {
        PastedText.Clean("One\u2028Two\u2029Three\u0085Four")
            .Should().Be("One\nTwo\nThree\nFour");
    }

    [Test]
    public void Clean_ChangesTypographicQuotesToAscii()
    {
        PastedText.Clean("\u201CShe\u2019s here,\u201D he said. \u2018Fine.\u2019 \u201EYes\u201F")
            .Should().Be("\"She's here,\" he said. 'Fine.' \"Yes\"");
    }

    [Test]
    public void Clean_ChangesDashesEllipsisAndBulletsToAscii()
    {
        PastedText.Clean("Tall \u2013 scarred \u2014 quiet\u2026\n\u2022 Pilot\n\u25CF Smuggler")
            .Should().Be("Tall - scarred - quiet...\n- Pilot\n- Smuggler");
    }

    [Test]
    public void Clean_ChangesSpecialSpacesToPlainSpaces()
    {
        PastedText.Clean("a\u00A0b\u2009c\u202Fd\u3000e\tf")
            .Should().Be("a b c d e    f");
    }

    [Test]
    public void Clean_RemovesInvisibleCharacters()
    {
        PastedText.Clean("\uFEFFZero\u200Bwidth\u200D soft\u00ADhyphen\u200E\u0007\u0090")
            .Should().Be("Zerowidth softhyphen");
    }

    [Test]
    public void Clean_KeepsAccentedLettersAndWindows1252Letters()
    {
        PastedText.Clean("Caf\u00E9 \u00D1o\u00F1o \u0152uvre \u0161\u017E\u0178 \u00E6")
            .Should().Be("Caf\u00E9 \u00D1o\u00F1o \u0152uvre \u0161\u017E\u0178 \u00E6");
    }

    [Test]
    public void Clean_ReplacesCharactersTheGameCannotShowWithQuestionMarks()
    {
        PastedText.Clean("Star \u2605 face \U0001F600 kanji \u6F22")
            .Should().Be("Star ? face ? kanji ?");
    }

    [Test]
    public void Clean_KeepsColorTokensWhoseChannelsAreControlCharacters()
    {
        PastedText.Clean("<c\u0001\u0090\u00FF>Red\u2019s</c>")
            .Should().Be("<c\u0001\u0090\u00FF>Red's</c>");
    }
}
