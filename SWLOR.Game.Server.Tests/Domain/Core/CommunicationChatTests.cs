using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Tests.Support.DMPossessionEngine;
using SkillType = SWLOR.Game.Server.Service.SkillService.SkillType;

namespace SWLOR.Game.Server.Tests.Domain.Core;

/// <summary>
/// Unit tests of the chat handler for talk, whisper, out-of-character text, emotes, languages and the channels that
/// are not spoken (DM messages, shout, slash commands, the dead), against the scripted world in
/// <see cref="CommunicationBench"/>. The Comms channel is in <see cref="CommunicationCommsTests"/>. They replace the
/// emote parser reflection test, the source-text checks of the old static class and the harness that compiled its DM
/// routing.
/// </summary>
[TestFixture]
public class CommunicationChatTests
{
    private const uint DM = 1;
    private const uint Creature = 2;
    private const uint Alice = 10;
    private const uint Bob = 11;
    private const uint Carol = 12;
    private const uint Dave = 13;
    private const uint Npc = 50;
    private const uint PlayerFamiliar = 51;
    private const uint Hologram = 60;
    private const uint Cantina = 100;
    private const uint Spaceport = 101;

    private CommunicationBench _bench = null!;

    [SetUp]
    public void SetUp()
    {
        _bench = new CommunicationBench();
        _bench.AddPlayer(Alice, area: Cantina);
        _bench.AddPlayer(Bob, area: Cantina);
    }

    [TearDown]
    public void TearDown()
    {
        _bench.Dispose();
    }

    // Basic is white. Every component is wrapped in its color, and nothing else is added to a Basic message.
    private static string Basic(string text) => ColorToken.Custom(text, 255, 255, 255);

    private static string Emote(string text) => ColorToken.Custom(text, 0, 255, 0);

    private static string Huttese(string text)
    {
        var (r, g, b) = Language.GetColor(SkillType.Huttese);
        return ColorToken.Custom(text, r, g, b);
    }

    private static string HutteseLabel => Huttese("[Huttese] ");

    private static string Translated(uint listener, string snippet) => TestCommunicationService.Translated(listener, snippet);

    private List<string> MessagesTo(uint receiver) =>
        _bench.Sent.Where(sent => sent.Target == receiver).Select(sent => sent.Message).ToList();

    // ---- local speech and who hears it

    [Test]
    public void Talk_ReachesTheSpeakerAndEveryoneWithinTwentyUnitsInTheArea()
    {
        _bench.AddPlayer(Carol, area: Cantina);
        _bench.AddPlayer(Dave, area: Spaceport);
        _bench.At(Bob, 20f);
        _bench.At(Carol, 20.01f);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "hello there");

        _bench.Skipped.Should().BeTrue("the engine's own message is replaced by the processed ones");
        _bench.Receivers.Should().Equal(Alice, Bob);
        _bench.Sent.Should().OnlyContain(sent => sent.Channel == ChatChannel.PlayerTalk && sent.Sender == Alice);
        _bench.Sent.Select(sent => sent.Message).Should().OnlyContain(message => message == Basic("hello there"));
    }

    [Test]
    public void Whisper_ReachesOnlyThoseWithinFourUnits()
    {
        _bench.AddPlayer(Carol, area: Cantina);
        _bench.At(Bob, 4f);
        _bench.At(Carol, 4.01f);

        _bench.Says(ChatChannel.PlayerWhisper, Alice, "psst");

        _bench.Receivers.Should().Equal(Alice, Bob);
        _bench.Sent.Should().OnlyContain(sent => sent.Channel == ChatChannel.PlayerWhisper);
    }

    [TestCase(ChatChannel.DMTalk, 20f, 21f)]
    [TestCase(ChatChannel.DMWhisper, 4f, 5f)]
    public void ADMAvatarsTalkAndWhisperChannelsAreHandledLikeThePlayerOnes(ChatChannel channel, float near, float far)
    {
        // The engine only uses the DM channels while the DM is not possessing a creature.
        _bench.AddPlayer(DM, isDM: true, area: Cantina);
        _bench.At(Alice, near);
        _bench.At(Bob, far);

        _bench.Says(channel, DM, "Welcome.");

        _bench.Receivers.Should().Equal(DM, Alice);
        _bench.Sent.Should().OnlyContain(sent => sent.Channel == channel && sent.Sender == DM);
    }

    [Test]
    public void TheSpeakerIsSentTheirOwnMessageOnceEvenWhenTheyHearThemselves()
    {
        _bench.Says(ChatChannel.PlayerTalk, Alice, "hi");

        _bench.Receivers.Should().Equal(Alice, Bob);
    }

    [Test]
    public void EachMessageIsSentThroughTheObserversChatNameOverrideOfTheSpeaker()
    {
        _bench.Says(ChatChannel.PlayerTalk, Alice, "hi");

        // The override applies to one observer at a time, and the message is sent while it is in place.
        _bench.Service.NameOverrides.Should().Equal((Alice, Alice, 1), (Bob, Alice, 1));
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void LocalSpeech_ReachesAPossessingDMThroughTheCreature(int possessionType)
    {
        _bench.AddPlayer(DM, isDM: true, area: Spaceport);
        _bench.World.Possess(DM, Creature, possessionType, Cantina);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "hi");

        _bench.Receivers.Should().Equal(Alice, Bob, Creature);
        _bench.Receivers.Select(_bench.World.ReceivingPlayer).Should().Equal(Alice, Bob, DM);
    }

    [Test]
    public void LocalSpeech_ReachesTheDMAvatarAfterRelease()
    {
        _bench.AddPlayer(DM, isDM: true, area: Cantina);
        _bench.World.Possess(DM, Creature, DMPossess, Spaceport);
        _bench.World.Release(DM);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "hi");

        _bench.Receivers.Should().Equal(Alice, Bob, DM);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void LocalSpeech_StillReachesTheDMAfterTheEngineRefusesToPossessAFamiliar(int possessionType)
    {
        // The possess events fire before the engine checks the target. The engine then refuses any creature that has a
        // master, so the DM never leaves their avatar and no release event follows.
        _bench.AddPlayer(DM, isDM: true, area: Cantina);
        _bench.AddCreature(PlayerFamiliar, Spaceport);
        _bench.Events.SetReturnHandler("GetEventData", _ => PlayerFamiliar.ToString());
        _bench.Service.OnDMPossess();

        _bench.Says(ChatChannel.PlayerTalk, Alice, "hi");
        _bench.Receivers.Should().Equal(Alice, Bob, DM);

        // Possessing a creature the engine accepts and releasing it again changes nothing.
        _bench.World.Possess(DM, Creature, possessionType, Spaceport);
        _bench.World.Release(DM);
        _bench.Says(ChatChannel.PlayerTalk, Alice, "hi");
        _bench.Receivers.Should().Equal(Alice, Bob, DM);
    }

    [Test]
    public void LocalSpeech_SkipsListenersInOtherAreas()
    {
        _bench.AddPlayer(Dave, area: Spaceport);
        _bench.AddPlayer(DM, isDM: true, area: Spaceport);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "hi");

        _bench.Receivers.Should().Equal(Alice, Bob);
    }

    // ---- messages that are not spoken

    [Test]
    public void AMessageToADMIsEchoedBackToTheSenderAndLeftToTheEngine()
    {
        _bench.Says(ChatChannel.PlayerDM, Alice, "  can someone help?  ");

        _bench.Sent.Should().Equal(new SentMessage(ChatChannel.ServerMessage, "(Sent to DM) can someone help?", Alice, Alice));
        _bench.Skipped.Should().BeFalse();
    }

    [TestCase(ChatChannel.PlayerTell)]
    [TestCase(ChatChannel.DMTell)]
    [TestCase(ChatChannel.DMDM)]
    [TestCase(ChatChannel.DMShout)]
    [TestCase(ChatChannel.DMParty)]
    [TestCase(ChatChannel.ServerMessage)]
    public void OtherChannelsAreLeftToTheEngine(ChatChannel channel)
    {
        _bench.Says(channel, Alice, "hello");

        _bench.Sent.Should().BeEmpty();
        _bench.Skipped.Should().BeFalse();
        _bench.Notices.Should().BeEmpty();
    }

    [Test]
    public void ADMsDotCommandsAreLeftToTheDMFIScript()
    {
        _bench.AddPlayer(DM, isDM: true, area: Cantina);

        _bench.Says(ChatChannel.DMTalk, DM, ".dmfi voice");

        _bench.Sent.Should().BeEmpty();
        _bench.Skipped.Should().BeFalse();
    }

    [Test]
    public void ADotAtTheStartIsOrdinarySpeechForAPlayer()
    {
        _bench.Says(ChatChannel.PlayerTalk, Alice, "...hm");

        _bench.Receivers.Should().Equal(Alice, Bob);
        _bench.Sent[0].Message.Should().Be(Basic("...hm"));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void ABlankMessageIsIgnored(string message)
    {
        _bench.Says(ChatChannel.PlayerTalk, Alice, message);

        _bench.Sent.Should().BeEmpty();
        _bench.Skipped.Should().BeFalse();
    }

    [TestCase("/shout hello")]
    [TestCase("/ hello")]
    public void ASlashCommandBypassesEverythingIncludingTheDisabledChannelNotice(string message)
    {
        _bench.Says(ChatChannel.PlayerShout, Alice, message);

        _bench.Sent.Should().BeEmpty();
        _bench.Skipped.Should().BeFalse("the chat command handler skips it");
        _bench.Notices.Should().BeEmpty();
    }

    [Test]
    public void ALoneSlashIsSpokenAndADoubleSlashIsOutOfCharacterText()
    {
        _bench.Says(ChatChannel.PlayerTalk, Alice, "/");
        _bench.Sent[0].Message.Should().Be(Basic("/"));

        _bench.Says(ChatChannel.PlayerTalk, Alice, "// not a command");
        _bench.Sent[0].Message.Should().Be(ColorToken.Custom("// not a command", 64, 64, 64));
    }

    // ---- shout and the dead

    [Test]
    public void Shout_IsDisabledForPlayers()
    {
        _bench.Says(ChatChannel.PlayerShout, Alice, "HEY");

        _bench.Skipped.Should().BeTrue();
        _bench.Sent.Should().BeEmpty();
        _bench.Notices.Should().Equal(ColorToken.Red("This chat channel is disabled."));
    }

    [Test]
    public void Shout_IsDisabledForTheDeadToo()
    {
        _bench.Dead(Alice);

        _bench.Says(ChatChannel.PlayerShout, Alice, "HEY");

        _bench.Notices.Should().Equal(ColorToken.Red("This chat channel is disabled."));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Shout_IsLeftToTheEngineForADMAndAPossessingDM(bool possessing)
    {
        _bench.AddPlayer(DM, isDM: true, area: Cantina);
        if (possessing)
            _bench.World.Possess(DM, Creature, DMPossess, Cantina);

        _bench.Says(ChatChannel.PlayerShout, possessing ? Creature : DM, "ATTENTION");

        _bench.Skipped.Should().BeFalse();
        _bench.Sent.Should().BeEmpty();
        _bench.Notices.Should().BeEmpty();
    }

    [Test]
    public void ADeadPlayerCannotSpeak()
    {
        _bench.Dead(Alice);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "help");

        _bench.Skipped.Should().BeTrue();
        _bench.Sent.Should().BeEmpty();
        _bench.Notices.Should().Equal(ColorToken.Red("You cannot speak while dead."));
    }

    [Test]
    public void ADeadPlayerCanStillWriteOutOfCharacterText()
    {
        _bench.Dead(Alice);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "// brb");

        _bench.Notices.Should().BeEmpty();
        _bench.Receivers.Should().Equal(Alice, Bob);
    }

    // ---- out-of-character text

    [TestCase("// brb")]
    [TestCase("(( brb ))")]
    public void OutOfCharacterText_IsDarkGrayAndNeverTranslatedOrColoredAsAnEmote(string message)
    {
        _bench.Speaks(Alice, SkillType.Huttese);

        _bench.Says(ChatChannel.PlayerTalk, Alice, message);

        // Only the language label keeps the language color.
        _bench.Sent.Select(sent => sent.Message).Should().OnlyContain(
            text => text == HutteseLabel + ColorToken.Custom(message, 64, 64, 64));
        _bench.Service.Translations.Should().BeEmpty();
    }

    [Test]
    public void OutOfCharacterText_UsesTheReceiversOwnColorWhenTheyChoseOne()
    {
        _bench.Record(Bob).Settings.OOCChatColor = new PlayerColor(1, 2, 3);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "// brb");

        MessagesTo(Alice).Should().Equal(ColorToken.Custom("// brb", 64, 64, 64));
        MessagesTo(Bob).Should().Equal(ColorToken.Custom("// brb", 1, 2, 3));
    }

    // ---- languages

    [Test]
    public void ALanguageOtherThanBasicLabelsTheMessageAndTranslatesItPerListener()
    {
        _bench.Speaks(Alice, SkillType.Huttese);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "hello there");

        MessagesTo(Alice).Should().Equal(HutteseLabel + Huttese(Translated(Alice, "hello there")));
        MessagesTo(Bob).Should().Equal(HutteseLabel + Huttese(Translated(Bob, "hello there")));
        _bench.Service.Translations.Should().Equal(
            (Alice, Alice, SkillType.Huttese, "hello there"),
            (Alice, Bob, SkillType.Huttese, "hello there"));
    }

    [Test]
    public void BasicIsNeitherLabeledNorTranslated()
    {
        _bench.Says(ChatChannel.PlayerTalk, Alice, "hello there");

        MessagesTo(Bob).Should().Equal(Basic("hello there"));
        _bench.Service.Translations.Should().BeEmpty();
    }

    [Test]
    public void EmotesAreNeverTranslated()
    {
        _bench.Speaks(Alice, SkillType.Huttese);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "hello [waves] there");

        MessagesTo(Bob).Should().Equal(
            HutteseLabel +
            Huttese(Translated(Bob, "hello ")) +
            Emote("waves") +
            Huttese(Translated(Bob, " there")));
    }

    [Test]
    public void AReceiverCanChooseTheirOwnColorForALanguage()
    {
        _bench.Speaks(Alice, SkillType.Huttese);
        _bench.Record(Bob).Settings.LanguageChatColors[SkillType.Huttese] = new PlayerColor(9, 8, 7);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "hi");

        MessagesTo(Bob).Should().Equal(
            ColorToken.Custom("[Huttese] ", 9, 8, 7) + ColorToken.Custom(Translated(Bob, "hi"), 9, 8, 7));
        MessagesTo(Alice).Should().Equal(HutteseLabel + Huttese(Translated(Alice, "hi")));
    }

    [Test]
    public void AReceiversColorForBasicRecolorsTheMessageWithoutALabel()
    {
        _bench.Record(Bob).Settings.LanguageChatColors[SkillType.Basic] = new PlayerColor(5, 6, 7);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "hi");

        MessagesTo(Bob).Should().Equal(ColorToken.Custom("hi", 5, 6, 7));
        MessagesTo(Alice).Should().Equal(Basic("hi"));
    }

    [Test]
    public void AWookieeCanOnlySpeakShyriiwookAndTheLanguageIsSwitchedOnce()
    {
        _bench.Wookiee(Alice);
        _bench.Speaks(Alice, SkillType.Basic);
        _bench.Locals.ClearCalls();

        _bench.Says(ChatChannel.PlayerTalk, Alice, "hi");

        _bench.Language(Alice).Should().Be(SkillType.Shyriiwook);
        _bench.Engine.CallsTo("SetLocalInt").Count(call => (string)call.Arguments[1]! == "ACTIVE_LANGUAGE")
            .Should().Be(1, "the language is resolved once before dispatching, not per receiver");
        var (r, g, b) = Language.GetColor(SkillType.Shyriiwook);
        MessagesTo(Bob).Should().Equal(
            ColorToken.Custom("[Shyriiwook] ", r, g, b) + ColorToken.Custom(Translated(Bob, "hi"), r, g, b));
    }

    [Test]
    public void AWookieeWhoAlreadySpeaksShyriiwookIsLeftAlone()
    {
        _bench.Wookiee(Alice);
        _bench.Speaks(Alice, SkillType.Shyriiwook);
        _bench.Locals.ClearCalls();

        _bench.Says(ChatChannel.PlayerTalk, Alice, "hi");

        _bench.Engine.CallsTo("SetLocalInt").Where(call => (string)call.Arguments[1]! == "ACTIVE_LANGUAGE").Should().BeEmpty();
    }

    [Test]
    public void ASpeakerWhoIsNotAWookieeKeepsTheirLanguage()
    {
        _bench.Speaks(Alice, SkillType.Huttese);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "hi");

        _bench.Language(Alice).Should().Be(SkillType.Huttese);
    }

    // ---- emote styles

    [Test]
    public void RegularStyle_BracketEmote_IsCustomColoredAndNotTranslatable()
    {
        _bench.Speaks(Alice, SkillType.Huttese);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "[waves]");

        MessagesTo(Bob).Should().Equal(HutteseLabel + Emote("waves"));
        _bench.Service.Translations.Should().BeEmpty("emotes are never language-translated");
    }

    [Test]
    public void RegularStyle_AsteriskEmote_KeepsItsAsterisks()
    {
        _bench.Speaks(Alice, SkillType.Huttese);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "*waves*");

        MessagesTo(Bob).Should().Equal(HutteseLabel + Emote("*waves*"));
    }

    [Test]
    public void RegularStyle_DoubleColonEmote_KeepsItsColons()
    {
        _bench.Speaks(Alice, SkillType.Huttese);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "::waves::");

        MessagesTo(Bob).Should().Equal(HutteseLabel + Emote("::waves::"));
    }

    [Test]
    public void RegularStyle_AnEmoteInTheMiddleSplitsSpokenTextAroundIt()
    {
        _bench.Speaks(Alice, SkillType.Huttese);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "hello *waves* there");

        MessagesTo(Bob).Should().Equal(
            HutteseLabel + Huttese(Translated(Bob, "hello ")) + Emote("*waves*") + Huttese(Translated(Bob, " there")));
    }

    [Test]
    public void RegularStyle_BracketsMayNest()
    {
        _bench.Says(ChatChannel.PlayerTalk, Alice, "[a [b] c]");

        MessagesTo(Bob).Should().Equal(Emote("a [b] c"));
    }

    [Test]
    public void RegularStyle_ASingleColonIsOrdinaryText()
    {
        _bench.Speaks(Alice, SkillType.Huttese);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "note: this is fine");

        MessagesTo(Bob).Should().Equal(HutteseLabel + Huttese(Translated(Bob, "note: this is fine")));
    }

    [Test]
    public void RegularStyle_AnEmoteThatNeverClosesIsShownAsAnEmote()
    {
        _bench.Speaks(Alice, SkillType.Huttese);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "hello *waves");

        MessagesTo(Bob).Should().Equal(HutteseLabel + Huttese(Translated(Bob, "hello ")) + Emote("*waves"));
    }

    [Test]
    public void ReceiversCanChooseTheirOwnEmoteColor()
    {
        _bench.Record(Bob).Settings.EmoteChatColor = new PlayerColor(10, 20, 30);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "*waves*");

        MessagesTo(Alice).Should().Equal(Emote("*waves*"));
        MessagesTo(Bob).Should().Equal(ColorToken.Custom("*waves*", 10, 20, 30));
    }

    [Test]
    public void NovelStyle_QuotedSpeechIsSpokenAndTheNarrationIsAnEmote()
    {
        _bench.Record(Alice).EmoteStyle = EmoteStyle.Novel;
        _bench.Speaks(Alice, SkillType.Huttese);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "\"Hi\" she nods [waves] now");

        MessagesTo(Bob).Should().Equal(
            HutteseLabel +
            Huttese(Translated(Bob, "\"Hi\"")) +
            Emote(" she nods ") +
            Emote("waves") +
            Emote(" now"));
    }

    [Test]
    public void NovelStyle_AMessageWithoutQuotesIsAllNarration()
    {
        _bench.Record(Alice).EmoteStyle = EmoteStyle.Novel;

        _bench.Says(ChatChannel.PlayerTalk, Alice, "she waves");

        MessagesTo(Bob).Should().Equal(Emote("she waves"));
    }

    [Test]
    public void NovelStyle_ABracketInsideQuotesIsNarrationThatStopsTheSpeech()
    {
        _bench.Record(Alice).EmoteStyle = EmoteStyle.Novel;
        _bench.Speaks(Alice, SkillType.Huttese);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "\"Hi [sighs] there\"");

        MessagesTo(Bob).Should().Equal(
            HutteseLabel +
            Huttese(Translated(Bob, "\"Hi ")) +
            Emote("sighs") +
            Huttese(Translated(Bob, " there\"")));
    }

    [Test]
    public void ADMSpeaksInTheEmoteStyleOfTheirPreferences()
    {
        _bench.AddPlayer(DM, isDM: true, area: Cantina);
        _bench.DMPreference.Update(DM, preferences => preferences.EmoteStyle = EmoteStyle.Novel).Should().BeTrue();

        _bench.Says(ChatChannel.DMTalk, DM, "He nods");

        MessagesTo(Bob).Should().Equal(Emote("He nods"));
    }

    [Test]
    public void APossessingDMSpeaksInTheStyleTheyChoseOnTheirAvatar()
    {
        _bench.AddPlayer(DM, isDM: true, area: Spaceport);
        _bench.DMPreference.Update(DM, preferences => preferences.EmoteStyle = EmoteStyle.Novel).Should().BeTrue();
        _bench.World.Possess(DM, Creature, DMPossess, Cantina);

        _bench.Says(ChatChannel.PlayerTalk, Creature, "He nods");

        MessagesTo(Bob).Should().Equal(Emote("He nods"));
    }

    // ---- HoloCom relays

    [Test]
    public void AHologramRelaysItsOwnersWordsInTheOwnersEmoteStyle()
    {
        // The hologram creature is the speaker the engine reports; its owner holds the emote style and the language.
        _bench.AddCreature(Hologram, Cantina);
        _bench.HologramOwners[Hologram] = Alice;
        _bench.Record(Alice).EmoteStyle = EmoteStyle.Novel;

        _bench.Says(ChatChannel.PlayerTalk, Hologram, "she waves");

        MessagesTo(Bob).Should().Equal(
            TestCommunicationService.ChatName(Bob, Alice) + ": " + Emote("she waves"));
    }

    [Test]
    public void AHologramRelaysOnTheOriginalChannelThroughTheHologramAsTheTransportSpeaker()
    {
        _bench.AddCreature(Hologram, Cantina);
        _bench.HologramOwners[Hologram] = Alice;
        _bench.Speaks(Alice, SkillType.Huttese);

        _bench.Says(ChatChannel.PlayerWhisper, Hologram, "hi");

        // Talk and Whisper need the area-local hologram as their native sender. The body names the owner for each
        // receiver, and no per-observer name override is involved.
        _bench.Sent.Should().OnlyContain(sent => sent.Channel == ChatChannel.PlayerWhisper && sent.Sender == Hologram);
        _bench.Service.NameOverrides.Should().BeEmpty();
        MessagesTo(Bob).Should().Equal(
            TestCommunicationService.ChatName(Bob, Alice) + ": " + HutteseLabel + Huttese(Translated(Bob, "hi")));
        _bench.Service.Translations.Should().OnlyContain(translation => translation.Speaker == Alice);
    }

    [Test]
    public void AHologramRelayAppliesTheWookieeLanguageRuleToItsOwner()
    {
        _bench.AddCreature(Hologram, Cantina);
        _bench.HologramOwners[Hologram] = Alice;
        _bench.Wookiee(Alice);

        _bench.Says(ChatChannel.PlayerTalk, Hologram, "hi");

        _bench.Language(Alice).Should().Be(SkillType.Shyriiwook);
    }

    // ---- the Comms prefix is never part of a talk message

    [Test]
    public void TalkIsNeverLabeledWithTheCommsPrefixOrTheOrangeCommsColor()
    {
        _bench.Says(ChatChannel.PlayerTalk, Alice, "hi");

        _bench.Sent.Select(sent => sent.Message).Should().OnlyContain(
            message => !message.Contains("[Comms]") && message == Basic("hi"));
    }

    [Test]
    public void MessagesNeverUseTheDMTalkChannelToReachAPlayer()
    {
        // NWNX_Rename does not patch DM Talk, so a processed message is never re-sent on it.
        _bench.AddPlayer(Carol, area: Cantina);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "hi");
        _bench.Says(ChatChannel.PlayerWhisper, Alice, "hi");

        _bench.Sent.Should().NotContain(sent => sent.Channel == ChatChannel.DMTalk);
    }
}
