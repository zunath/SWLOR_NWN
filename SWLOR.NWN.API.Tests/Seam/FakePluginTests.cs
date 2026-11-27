using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.Testing;

namespace SWLOR.NWN.API.Tests.Seam;

/// <summary>What a test sees when it replaces an NWNX plugin with its recording fake.</summary>
[TestFixture]
public class FakePluginTests
{
    private const uint ObjectInvalid = 0x7F000000;

    [Test]
    public void Calls_AreRecordedWithTheirArguments_AndTheStaticWrapperReachesTheFake()
    {
        var chat = new FakeChatPlugin();
        using var scope = ChatPluginRuntime.Override(chat);

        var sent = ChatPlugin.SendMessage(ChatChannel.PlayerTalk, "hello", 1001u, 1002u);
        ChatPlugin.SkipMessage();

        sent.Should().Be(0);
        chat.Calls.Select(call => call.ToString()).Should().Equal("SendMessage(PlayerTalk, \"hello\", 1001, 1002)", "SkipMessage()");
    }

    [Test]
    public void ConfiguredValues_AreReturned_AndEnumsAreConvertedFromNumbers()
    {
        var chat = new FakeChatPlugin();
        chat.SetReturn("GetMessage", "a message");
        chat.SetReturn("GetSender", 1001u);
        chat.SetReturn("GetChannel", (int)ChatChannel.PlayerShout);
        using var scope = ChatPluginRuntime.Override(chat);

        ChatPlugin.GetMessage().Should().Be("a message");
        ChatPlugin.GetSender().Should().Be(1001u);
        ChatPlugin.GetChannel().Should().Be(ChatChannel.PlayerShout);
    }

    [Test]
    public void ReturnSequencesAndHandlers_Work_ForPluginMembers()
    {
        var creature = new FakeCreaturePlugin();
        creature.SetReturnSequence("GetKnowsFeat", true, false);
        creature.SetReturnHandler("GetMaxHitPointsByLevel", arguments => (int)arguments[1]! * 10);
        using var scope = CreaturePluginRuntime.Override(creature);

        CreaturePlugin.GetKnowsFeat(1001u, SWLOR.NWN.API.NWScript.Enum.FeatType.ArmorProficiencyLight).Should().BeTrue();
        CreaturePlugin.GetKnowsFeat(1001u, SWLOR.NWN.API.NWScript.Enum.FeatType.ArmorProficiencyLight).Should().BeFalse();
        CreaturePlugin.GetMaxHitPointsByLevel(1001u, 3).Should().Be(30);
    }

    [Test]
    public void Defaults_EndTypicalLoops_AndDataResultsAreNeverNull()
    {
        var player = new FakePlayerPlugin();
        var util = new FakeUtilPlugin();
        using var playerScope = PlayerPluginRuntime.Override(player);
        using var utilScope = UtilPluginRuntime.Override(util);

        PlayerPlugin.GetGameObject(1001u).Should().Be(ObjectInvalid, "an object id defaults to OBJECT_INVALID");
        PlayerPlugin.GetJournalEntry(1001u, "quest").Should().NotBeNull("a plain data class comes back as an empty instance");
        PlayerPlugin.GetQuickBarSlot(1001u, 0).Should().NotBeNull();
        UtilPlugin.GetCurrentScriptName().Should().BeEmpty();
        UtilPlugin.GetWorldTime().Should().Be(default(UtilPlugin.WorldTime));
        UtilPlugin.GetHighResTimeStamp().Seconds.Should().Be(0);
    }

    [Test]
    public void NestedDataTypes_CanBeConfigured()
    {
        var util = new FakeUtilPlugin();
        util.SetReturn("GetWorldTime", new UtilPlugin.WorldTime { CalendarDay = 7, TimeOfDay = 12 });
        using var scope = UtilPluginRuntime.Override(util);

        var time = UtilPlugin.GetWorldTime(1.5f);

        time.CalendarDay.Should().Be(7);
        time.TimeOfDay.Should().Be(12);
        util.CallsTo("GetWorldTime").Should().ContainSingle().Which.Arguments.Should().Equal(1.5f);
    }

    [Test]
    public void EachPluginKeepsItsOwnCalls()
    {
        var chat = new FakeChatPlugin();
        var player = new FakePlayerPlugin();
        using var chatScope = ChatPluginRuntime.Override(chat);
        using var playerScope = PlayerPluginRuntime.Override(player);

        ChatPlugin.SkipMessage();
        PlayerPlugin.GetGameObject(1001u);

        chat.Calls.Select(call => call.Member).Should().Equal("SkipMessage");
        player.Calls.Select(call => call.Member).Should().Equal("GetGameObject");
    }

    [Test]
    public void WithoutAnOverride_TheWrappersUseThePassThroughEngine()
    {
        ChatPluginRuntime.Current.Should().BeOfType<NwnCoreChatPlugin>();
        UtilPluginRuntime.Current.Should().BeOfType<NwnCoreUtilPlugin>();
    }
}
