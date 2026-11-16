using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Foundation;

/// <summary>
/// The color tokens wrap text in the engine's <c>&lt;cRGB&gt;...&lt;/c&gt;</c> markup, one character per channel. The named
/// colors are the ones the game shows players, so each is checked against its intended color.
/// </summary>
[TestFixture]
public class ColorTokenTests
{
    private static readonly (string Name, Func<string, string> Wrap, byte R, byte G, byte B)[] NamedColors =
    {
        ("Black", ColorToken.Black, 0, 0, 0),
        ("Blue", ColorToken.Blue, 0, 0, 255),
        ("Gray", ColorToken.Gray, 127, 127, 127),
        ("Green", ColorToken.Green, 0, 255, 0),
        ("LightPurple", ColorToken.LightPurple, 175, 48, 255),
        ("Orange", ColorToken.Orange, 255, 127, 0),
        ("Pink", ColorToken.Pink, 255, 0, 255),
        ("Purple", ColorToken.Purple, 127, 0, 255),
        ("Red", ColorToken.Red, 255, 0, 0),
        ("White", ColorToken.White, 255, 255, 255),
        ("Yellow", ColorToken.Yellow, 255, 255, 0),
        ("Cyan", ColorToken.Cyan, 0, 255, 255),
        ("Combat", ColorToken.Combat, 255, 102, 0),
        ("Dialog", ColorToken.Dialog, 255, 255, 255),
        ("DialogAction", ColorToken.DialogAction, 1, 254, 1),
        ("DialogCheck", ColorToken.DialogCheck, 254, 1, 1),
        ("DialogHighlight", ColorToken.DialogHighlight, 1, 1, 254),
        ("DialogReply", ColorToken.DialogReply, 102, 178, 255),
        ("DM", ColorToken.DM, 16, 223, 255),
        ("GameEngine", ColorToken.GameEngine, 204, 119, 255),
        ("Script", ColorToken.Script, 255, 255, 0),
        ("Server", ColorToken.Server, 176, 176, 176),
        ("Shout", ColorToken.Shout, 255, 239, 80),
        ("SkillCheck", ColorToken.SkillCheck, 0, 102, 255),
        ("Talk", ColorToken.Talk, 240, 240, 240),
        ("Tell", ColorToken.Tell, 32, 255, 32),
        ("Whisper", ColorToken.Whisper, 128, 128, 128)
    };

    public static IEnumerable<TestCaseData> Named() =>
        NamedColors.Select(c => new TestCaseData(c.Name, c.Wrap, c.R, c.G, c.B).SetName($"Named_{c.Name}"));

    [TestCaseSource(nameof(Named))]
    public void ANamedColor_WrapsTheTextInItsToken(string name, Func<string, string> wrap, byte r, byte g, byte b)
    {
        var wrapped = wrap("text");

        wrapped.Should().Be(ColorToken.TokenStart(r, g, b) + "text" + ColorToken.TokenEnd(), name);
        wrapped.Should().StartWith("<c").And.EndWith("</c>");
    }

    [TestCaseSource(nameof(Named))]
    public void ANamedColor_RefusesEmptyText(string name, Func<string, string> wrap, byte r, byte g, byte b)
    {
        foreach (var text in new[] { null!, string.Empty, "   " })
            ((Action)(() => wrap(text))).Should().Throw<ArgumentException>(name);
    }

    [Test]
    public void TheTokenStartWritesOneCharacterPerChannel()
    {
        ColorToken.TokenStart(0, 0, 0).Should().Be("<c\u0001\u0001\u0001>", "zero maps to the first character of the table");
        ColorToken.TokenStart(65, 66, 67).Should().HaveLength(6);
        var white = ColorToken.TokenStart(255, 255, 255);
        white.Should().HaveLength(6);
        white[2].Should().Be(white[3]).And.Be(white[4]);
    }

    [Test]
    public void CustomWrapsTextAndTreatsNullAsEmpty()
    {
        ColorToken.Custom("hi", 1, 2, 3).Should().Be(ColorToken.TokenStart(1, 2, 3) + "hi</c>");
        ColorToken.Custom(null!, 1, 2, 3).Should().Be(ColorToken.TokenStart(1, 2, 3) + "</c>");
    }

    [Test]
    public void PlayerAndNpcNamesGetTheirOwnShadeOfBlueOrPurple()
    {
        ColorToken.GetPCColor("Han").Should().Be(ColorToken.Custom("Han", 153, 255, 255));
        ColorToken.GetNPCColor("Guard").Should().Be(ColorToken.Custom("Guard", 204, 153, 204));
        ColorToken.GetPCColor("Han").Should().NotBe(ColorToken.GetNPCColor("Han"));
    }

    [Test]
    public void TheCreatureNameHelpersColorTheEngineName()
    {
        using var engine = new PlayerEngine();
        engine.Engine.SetReturn("GetName", "Leia");

        ColorToken.GetNamePCColor(1001).Should().Be(ColorToken.GetPCColor("Leia"));
        ColorToken.GetNameNPCColor(1001).Should().Be(ColorToken.GetNPCColor("Leia"));
    }

    [TestCase(255, 0, 0)]
    [TestCase(0, 255, 0)]
    [TestCase(0, 0, 255)]
    [TestCase(255, 255, 255)]
    [TestCase(1, 254, 1)]
    [TestCase(254, 1, 1)]
    [TestCase(1, 1, 254)]
    [TestCase(175, 48, 255)]
    [TestCase(102, 178, 255)]
    public void ASemanticColor_DecodesBackToItself(byte r, byte g, byte b)
    {
        // 254 and 255 share a character, so the semantic colors are looked up before the character table.
        ColorToken.TryDecodeStartToken(ColorToken.TokenStart(r, g, b), 0, out var red, out var green, out var blue)
            .Should().BeTrue();

        (red, green, blue).Should().Be((r, g, b));
    }

    [Test]
    public void AnOrdinaryColor_DecodesBackToItself()
    {
        ColorToken.TryDecodeStartToken(ColorToken.TokenStart(100, 110, 120), 0, out var red, out var green, out var blue)
            .Should().BeTrue();

        (red, green, blue).Should().Be(((byte)100, (byte)110, (byte)120));
    }

    [Test]
    public void ATokenInsideALongerTextIsFoundAtItsIndex()
    {
        var text = "ab" + ColorToken.TokenStart(40, 50, 60) + "cd";

        ColorToken.TryDecodeStartToken(text, 2, out var red, out var green, out var blue).Should().BeTrue();

        (red, green, blue).Should().Be(((byte)40, (byte)50, (byte)60));
    }

    [Test]
    public void ATokenThatIsNotThereIsNotDecoded()
    {
        var token = ColorToken.TokenStart(100, 110, 120);

        ColorToken.TryDecodeStartToken(null!, 0, out _, out _, out _).Should().BeFalse();
        ColorToken.TryDecodeStartToken(string.Empty, 0, out _, out _, out _).Should().BeFalse();
        ColorToken.TryDecodeStartToken(token, -1, out _, out _, out _).Should().BeFalse();
        ColorToken.TryDecodeStartToken(token, 1, out _, out _, out _).Should().BeFalse("the text ends before a token could");
        ColorToken.TryDecodeStartToken("<d" + token[2..], 0, out _, out _, out _).Should().BeFalse();
        ColorToken.TryDecodeStartToken(token[..5] + "x", 0, out _, out _, out _).Should().BeFalse("the token is not closed");
        ColorToken.TryDecodeStartToken("<c一一一>", 0, out _, out _, out _).Should().BeFalse("those are not color characters");
    }
}
