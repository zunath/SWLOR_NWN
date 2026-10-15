using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Feature;

[TestFixture]
public class ExamineItemTextTests
{
    [Test]
    public void LicenseDescription_PreservesInlineColorsAndLineBreaksWithoutShowingTokens()
    {
        var text = ColorToken.Custom("Name: ", 99, 113, 68) + ColorToken.White("Cinder") + "\n" +
                   ColorToken.Custom("Species: ", 99, 212, 63) + ColorToken.White("Charhound");

        var runs = ExamineItemText.Wrap(text);

        runs.Select(run => run.Text).Should().Equal("Name: ", "Cinder", "Species: ", "Charhound");
        runs[0].Color.Should().BeEquivalentTo(new { R = (byte)99, G = (byte)113, B = (byte)68 });
        runs[1].Color.Should().BeEquivalentTo(new { R = (byte)255, G = (byte)255, B = (byte)255 });
        runs[1].X.Should().BeGreaterThan(0);
        runs[1].Y.Should().Be(runs[0].Y);
        runs[2].X.Should().Be(0);
        runs[2].Y.Should().Be(ExamineItemText.LineHeight);
        runs[2].Color.Should().BeEquivalentTo(new { R = (byte)99, G = (byte)212, B = (byte)63 });
        ExamineItemText.PlainText(text).Should().Be("Name: Cinder\nSpecies: Charhound");
    }

    [Test]
    public void ClosingToken_RestoresDefaultColorIncludingUppercaseClosers()
    {
        var runs = ExamineItemText.Parse(ColorToken.TokenStart(255, 0, 0) + "Warning</C> ordinary");

        runs.Select(run => run.Text).Should().Equal("Warning", " ordinary");
        runs[0].Color.G.Should().Be(0);
        runs[1].Color.G.Should().Be(255);
    }

    [Test]
    public void AuthoredRawCp1252Tokens_DecodeFullIntensityChannels()
    {
        var runs = ExamineItemText.Parse("<cÿÿÿ>White</c><cÿ\u0001\u0001>Red</c>");

        runs.Select(run => run.Text).Should().Equal("White", "Red");
        runs[0].Color.Should().BeEquivalentTo(new { R = (byte)255, G = (byte)255, B = (byte)255 });
        runs[1].Color.Should().BeEquivalentTo(new { R = (byte)255, G = (byte)1, B = (byte)1 });
    }

    [Test]
    public void ColorCanContinueAcrossParagraphsAndWeaponPreviewReturnsToWhite()
    {
        var runs = ExamineItemText.Wrap(ColorToken.Green("Line one\r\n\r\nLine two") + "\n\nSingle Weapon is active.");

        runs.Select(run => run.Text).Should().Equal("Line one", "Line two", "Single Weapon is active.");
        runs[1].Y.Should().Be(ExamineItemText.LineHeight * 2);
        runs[1].Color.R.Should().Be(0);
        runs[2].Y.Should().Be(ExamineItemText.LineHeight * 4);
        runs[2].Color.R.Should().Be(255);
    }

    [Test]
    public void WordWrapping_KeepsColorsInsideAWordAndDoesNotLoseText()
    {
        var text = "Prefix " + ColorToken.Red("color") + ColorToken.Green("ful") + " suffix";
        var runs = ExamineItemText.Wrap(text, 65f);

        string.Concat(runs.Select(run => run.Text)).Should().Be("Prefix colorful suffix");
        var red = runs.Single(run => run.Text == "color");
        var green = runs.Single(run => run.Text == "ful");
        red.X.Should().Be(0);
        red.Y.Should().BeGreaterThan(0);
        green.Y.Should().Be(red.Y);
        green.X.Should().BeGreaterThan(red.X);
    }

    [Test]
    public void LongUnbrokenText_WrapsWithoutDroppingCharactersOrSplittingUnicodeScalars()
    {
        var text = new string('W', 200) + "é😀";
        var runs = ExamineItemText.Wrap(ColorToken.Red(text), 40f);

        string.Concat(runs.Select(run => run.Text)).Should().Be(text);
        runs.Should().HaveCountGreaterThan(10);
        runs.Should().OnlyContain(run => run.X >= 0 && run.X < 40f);
        runs.Should().NotContain(run => char.IsLowSurrogate(run.Text[0]) || char.IsHighSurrogate(run.Text[run.Text.Length - 1]));
    }

    [Test]
    public void SpaceThatForcesAWrap_DoesNotIndentTheNextLine()
    {
        var runs = ExamineItemText.Wrap("WWWWW WWWWW", 74f);

        runs.Should().HaveCount(2);
        runs[1].X.Should().Be(0);
        runs[1].Text.Should().Be("WWWWW");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("Plain description\nwith <ordinary> text.")]
    [TestCase("Incomplete <cABC")]
    public void PlainOrMalformedText_IsPreserved(string text)
    {
        ExamineItemText.PlainText(text).Should().Be(text ?? string.Empty);
    }

    [Test]
    public void ItemTitle_HasPlainTextAndItsAuthoredColor()
    {
        var title = ColorToken.Custom("Veles City Animal License", 99, 120, 32);

        ExamineItemText.PlainText(title).Should().Be("Veles City Animal License");
        ExamineItemText.TitleColor(title).Should().BeEquivalentTo(new { R = (byte)99, G = (byte)120, B = (byte)32 });
        ExamineItemText.TitleColor(null).R.Should().Be(255);
    }
}
