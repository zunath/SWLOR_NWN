using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Covers the pure text-processing pieces of HoloCom message playback. The sentence
/// splitter drives per-sentence animation and speech scheduling, so its boundary
/// behavior matters: terminators stay with their sentence, terminator runs (?!, ...)
/// stay together, and unterminated text still plays.
/// </summary>
public class HoloComMessagingTests
{
    [Test]
    public void SplitIntoSentences_SplitsOnTerminators_KeepingThemWithTheSentence()
    {
        var sentences = HoloComMessaging.SplitIntoSentences("Hello there. How are you? Great!");

        sentences.Should().Equal("Hello there.", "How are you?", "Great!");
    }

    [Test]
    public void SplitIntoSentences_KeepsTerminatorRunsTogether()
    {
        var sentences = HoloComMessaging.SplitIntoSentences("Wait... Really?! Yes.");

        sentences.Should().Equal("Wait...", "Really?!", "Yes.");
    }

    [Test]
    public void SplitIntoSentences_UnterminatedText_IsASingleSentence()
    {
        var sentences = HoloComMessaging.SplitIntoSentences("meet me at the cantina tonight");

        sentences.Should().Equal("meet me at the cantina tonight");
    }

    [Test]
    public void SplitIntoSentences_TrailingUnterminatedFragment_IsKept()
    {
        var sentences = HoloComMessaging.SplitIntoSentences("First part. and then some");

        sentences.Should().Equal("First part.", "and then some");
    }

    [Test]
    public void SplitIntoSentences_EmptyOrWhitespace_YieldsNothing()
    {
        HoloComMessaging.SplitIntoSentences("").Should().BeEmpty();
        HoloComMessaging.SplitIntoSentences("   ").Should().BeEmpty();
        HoloComMessaging.SplitIntoSentences(null).Should().BeEmpty();
    }
}
