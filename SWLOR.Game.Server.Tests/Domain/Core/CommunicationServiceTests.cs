using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using static SWLOR.Game.Server.Tests.Support.DMPossessionEngine;

namespace SWLOR.Game.Server.Tests.Domain.Core;

/// <summary>
/// Unit tests of the chat instance's event handlers, the emote style and the defaults of its engine seams, against
/// the scripted world in <see cref="CommunicationBench"/>. They replace the tests that compiled the handlers out of
/// the old static class (the shout label, the DM possess event, the emote style of DMs and players). The chat
/// routing itself is in <see cref="CommunicationChatTests"/> and <see cref="CommunicationCommsTests"/>.
/// </summary>
[TestFixture]
public class CommunicationServiceTests
{
    private const uint DM = 1;
    private const uint Creature = 2;
    private const uint Player = 3;
    private const uint Npc = 50;
    private const uint PlayerFamiliar = 6;
    private const uint Cantina = 100;
    private const uint Spaceport = 101;

    private CommunicationBench _bench = null!;

    private CommunicationService Service => _bench.Service;

    [SetUp]
    public void SetUp()
    {
        _bench = new CommunicationBench();
    }

    [TearDown]
    public void TearDown()
    {
        _bench.Dispose();
    }

    private IReadOnlyList<(uint Player, int StrRef, string Label)> TlkOverrides() =>
        _bench.World.Plugin.CallsTo("SetTlkOverride")
            .Select(call => ((uint)call.Arguments[0]!, (int)call.Arguments[1]!, (string)call.Arguments[2]!))
            .ToList();

    // ---- chat channel labels

    [Test]
    public void ApplyCommsChannelName_RelabelsThePartyChannelAndItsMessagePrefixForAPlayer()
    {
        _bench.AddPlayer(Player);
        _bench.Engine.SetReturn("GetEnteringObject", Player);

        Service.ApplyCommsChannelName();

        // Neither player-facing label exposes the Party transport; the prefix is one label, not a body append.
        TlkOverrides().Should().Equal((Player, 66755, "Comms"), (Player, 10303, "[Comms] "));
    }

    [Test]
    public void ApplyCommsChannelName_LeavesNonPlayersAlone()
    {
        _bench.AddCreature(Npc);
        _bench.Engine.SetReturn("GetEnteringObject", Npc);

        Service.ApplyCommsChannelName();

        TlkOverrides().Should().BeEmpty();
    }

    [Test]
    public void ApplyShoutChannelName_SendsOnePersonalOverrideEachWithoutChangingOtherClients()
    {
        _bench.AddPlayer(Player);
        _bench.AddPlayer(DM, isDM: true);
        _bench.AddCreature(Npc);

        Login(Player);
        TlkOverrides().Should().Equal((Player, 66751, "Disabled"));

        // A DM avatar sees Shout; a global override would also label the DM's Shout as disabled.
        Login(DM);
        TlkOverrides().Should().Equal((Player, 66751, "Disabled"), (DM, 66751, "Shout"));

        Login(Npc);
        TlkOverrides().Should().HaveCount(2, "non-player objects have no client");
    }

    [Test]
    public void ApplyShoutChannelName_GivesAPossessingDMShoutWhateverTheCreatureReportsAs()
    {
        _bench.AddPlayer(DM, isDM: true);
        _bench.World.Possess(DM, Creature, DMPossess);

        Login(Creature);

        TlkOverrides().Should().Equal((Creature, 66751, "Shout"));
    }

    [Test]
    public void ApplyShoutChannelName_GivesAShoutToADMAvatarThatDoesNotReportAsAPlayer()
    {
        _bench.AddPlayer(DM, isDM: true);
        _bench.Engine.SetReturnHandler("GetIsPC", _ => false);

        Login(DM);

        TlkOverrides().Should().Equal((DM, 66751, "Shout"));
    }

    private void Login(uint entering)
    {
        _bench.Engine.SetReturn("GetEnteringObject", entering);
        Service.ApplyShoutChannelName();
    }

    // ---- DM possession

    [Test]
    public void OnDMPossess_ClearsTheBusyStatusOfTheTarget()
    {
        _bench.AddPlayer(DM, isDM: true);
        _bench.AddCreature(Creature);

        PossessEvent(Creature);

        _bench.Engine.CallsTo("DeleteLocalBool").Select(call => (call.Arguments[0], call.Arguments[1]))
            .Should().Equal(((object)Creature, (object)"IS_BUSY"));
        _bench.Engine.CallsTo("DeleteLocalInt").Select(call => (call.Arguments[0], call.Arguments[1]))
            .Should().Equal(((object)Creature, (object)"BUSY_TYPE"));
    }

    [Test]
    public void OnDMPossess_DoesNothingWhenThereIsNoTarget()
    {
        _bench.AddPlayer(DM, isDM: true);

        PossessEvent(Invalid);

        _bench.Engine.CallsTo("DeleteLocalBool").Should().BeEmpty();
        _bench.Engine.CallsTo("DeleteLocalInt").Should().BeEmpty();
    }

    [Test]
    public void OnDMPossess_RunsBeforeTheEngineRefusesACreatureThatHasAMaster()
    {
        // The possess events fire before the engine checks the target. The engine then refuses a familiar, so the DM
        // never leaves the avatar, and the handler must not have assumed the possession happened.
        _bench.AddPlayer(Player, area: Cantina);
        _bench.AddPlayer(DM, isDM: true, area: Cantina);
        _bench.AddCreature(PlayerFamiliar, Spaceport);

        PossessEvent(PlayerFamiliar);

        _bench.Engine.CallsTo("DeleteLocalBool").Select(call => call.Arguments[0]).Should().Equal(PlayerFamiliar);
        _bench.DMPresence.GetControlledCreature(DM).Should().Be(DM);
    }

    private void PossessEvent(uint target)
    {
        _bench.Events.SetReturnHandler("GetEventData", arguments => (string)arguments[0]! == "TARGET" ? target.ToString() : string.Empty);
        Service.OnDMPossess();
    }

    // ---- typing indicator and the native chat script

    [Test]
    public void TypingIndicator_AppliesATaggedBubbleWhenThePlayerFocusesTheChatBar()
    {
        _bench.AddPlayer(Player);
        GuiEvent(Player, GuiEventType.ChatBarFocus);

        Service.TypingIndicator();

        var applied = _bench.Engine.CallsTo("ApplyEffectToObject").Single();
        applied.Arguments[0].Should().Be(DurationType.Temporary);
        applied.Arguments[2].Should().Be(Player);
        applied.Arguments[3].Should().Be(120.0f);
        var bubble = _bench.Engine.CallsTo("EffectVisualEffect").Single();
        bubble.Arguments[0].Should().Be(VisualEffect.Vfx_Dur_Chat_Bubble);
        bubble.Arguments[1].Should().Be(false);
        bubble.Arguments[2].Should().Be(0.5f);
        _bench.Engine.CallsTo("TagEffect").Single().Arguments[1].Should().Be("typingindicator");
        _bench.Engine.CallsTo("RemoveEffectByTag").Should().BeEmpty();
    }

    [Test]
    public void TypingIndicator_RemovesTheBubbleWhenTheChatBarLosesFocus()
    {
        _bench.AddPlayer(Player);
        GuiEvent(Player, GuiEventType.ChatBarUnfocus);

        Service.TypingIndicator();

        var removed = _bench.Engine.CallsTo("RemoveEffectByTag").Single();
        removed.Arguments[0].Should().Be(Player);
        ((string[])removed.Arguments[1]!).Should().Equal("typingindicator");
        _bench.Engine.CallsTo("ApplyEffectToObject").Should().BeEmpty();
    }

    [Test]
    public void TypingIndicator_IgnoresOtherGuiEventsAndNonPlayers()
    {
        _bench.AddPlayer(Player);
        _bench.AddCreature(Npc);

        GuiEvent(Player, GuiEventType.ChatBarFocus);
        _bench.Engine.SetReturn("GetLastGuiEventType", GuiEventType.JournalOpen);
        Service.TypingIndicator();
        GuiEvent(Npc, GuiEventType.ChatBarFocus);
        Service.TypingIndicator();

        _bench.Engine.CallsTo("ApplyEffectToObject").Should().BeEmpty();
        _bench.Engine.CallsTo("RemoveEffectByTag").Should().BeEmpty();
    }

    private void GuiEvent(uint player, GuiEventType type)
    {
        _bench.Engine.SetReturn("GetLastGuiEventPlayer", player);
        _bench.Engine.SetReturn("GetLastGuiEventType", type);
    }

    [Test]
    public void ProcessNativeChatMessage_RunsTheDMFIVoiceCommandScriptOnTheSpeaker()
    {
        _bench.Engine.SetReturn("OBJECT_SELF", Player);

        Service.ProcessNativeChatMessage();

        var call = _bench.Engine.CallsTo("ExecuteScript").Single();
        call.Arguments[0].Should().Be("dmfi_onplychat");
        call.Arguments[1].Should().Be(Player);
    }

    // ---- default colors

    [Test]
    public void TheDefaultChatColorsAreDarkGrayForOutOfCharacterTextAndGreenForEmotes()
    {
        Service.OOCChatColor.Should().Be(((byte)64, (byte)64, (byte)64));
        Service.EmoteChatColor.Should().Be(((byte)0, (byte)255, (byte)0));
    }

    // ---- emote style

    [Test]
    public void GetEmoteStyle_ReturnsAPlayersSavedStyleAndRegularWithoutARecord()
    {
        _bench.AddPlayer(Player);
        Service.GetEmoteStyle(Player).Should().Be(EmoteStyle.Regular);

        _bench.Record(Player).EmoteStyle = EmoteStyle.Novel;
        Service.GetEmoteStyle(Player).Should().Be(EmoteStyle.Novel);

        _bench.Db.Saved.Should().BeEmpty("reading never saves");
    }

    [Test]
    public void GetEmoteStyle_ReturnsRegularForAPlayerWhoseRecordIsMissing()
    {
        _bench.World.AddPlayer(Player);

        Service.GetEmoteStyle(Player).Should().Be(EmoteStyle.Regular);
    }

    [Test]
    public void GetEmoteStyle_ReturnsRegularForCreaturesThatAreNotControlled()
    {
        Service.GetEmoteStyle(Npc).Should().Be(EmoteStyle.Regular);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void EmoteStyle_IsSharedByTheDMAvatarAndPossessedCreatures(int possessionType)
    {
        _bench.AddPlayer(DM, isDM: true);
        Service.GetEmoteStyle(DM).Should().Be(EmoteStyle.Regular);

        Service.SetEmoteStyle(DM, EmoteStyle.Novel).Should().BeTrue();
        _bench.World.Possess(DM, Creature, possessionType);

        Service.GetEmoteStyle(Creature).Should().Be(EmoteStyle.Novel, "the style set on the avatar applies while possessing");
        Service.SetEmoteStyle(Creature, EmoteStyle.Regular).Should().BeTrue("the toggle works while possessing a creature");
        Service.GetEmoteStyle(Creature).Should().Be(EmoteStyle.Regular);
        Service.GetEmoteStyle(DM).Should().Be(EmoteStyle.Regular);
    }

    [Test]
    public void SetEmoteStyle_SavesAPlayersStyleOnTheirCharacter()
    {
        _bench.AddPlayer(Player);

        Service.SetEmoteStyle(Player, EmoteStyle.Novel).Should().BeTrue();

        _bench.Record(Player).EmoteStyle.Should().Be(EmoteStyle.Novel);
        _bench.Db.Saved.Should().ContainSingle().Which.Id.Should().Be(Player.ToString());
        Service.GetEmoteStyle(Player).Should().Be(EmoteStyle.Novel);
    }

    [Test]
    public void SetEmoteStyle_HasNowhereToSaveForAPlayerWithoutARecord()
    {
        _bench.World.AddPlayer(Player);

        Service.SetEmoteStyle(Player, EmoteStyle.Novel).Should().BeFalse();

        _bench.Db.Saved.Should().BeEmpty();
    }

    [Test]
    public void SetEmoteStyle_HasNowhereToSaveForCreaturesThatAreNotControlled()
    {
        Service.SetEmoteStyle(Npc, EmoteStyle.Novel).Should().BeFalse();

        _bench.Db.Saved.Should().BeEmpty();
    }

    // ---- the defaults of the engine seams (each body is the original call)

    [Test]
    public void TheSpaceModeSeamDefaultsToTheSpaceServiceCheck()
    {
        _bench.AddCreature(Npc);

        new DefaultSeams(_bench).IsInSpace(Npc).Should().BeFalse("a creature that is not a player is never in space mode");
    }

    [Test]
    public void TheChatNameSeamDefaultsToThePlayerNameService()
    {
        _bench.AddPlayer(Player);
        _bench.AddCreature(Npc, name: "Gamorrean Guard");

        new DefaultSeams(_bench).ChatName(Player, Npc)
            .Should().Be(SWLOR.Game.Server.Service.ColorToken.GetNameNPCColor(Npc)).And.Contain("Gamorrean Guard");
    }

    [Test]
    public void TheNameOverrideSeamDefaultsToThePlayerNameServiceAndStillSendsTheMessage()
    {
        _bench.AddCreature(Npc);
        var sent = 0;

        new DefaultSeams(_bench).WithOverride(Npc, Npc, () => sent++);

        sent.Should().Be(1, "the message goes out once, with no name override for something that is not a player");
    }

    private sealed class DefaultSeams : CommunicationService
    {
        public DefaultSeams(CommunicationBench bench)
            : base(bench.Db, bench.DMPresence, bench.DMPreference, bench.Party, bench.HoloCom, bench.Planet, bench.Area, bench.Property)
        {
        }

        public bool IsInSpace(uint player) => IsPlayerInSpaceMode(player);

        public string ChatName(uint observer, uint target) => GetColoredChatDisplayName(observer, target);

        public void WithOverride(uint observer, uint target, Action send) => SendChatMessageWithChatNameOverride(observer, target, send);
    }
}
