using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Service.MessagingService;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Core;

/// <summary>
/// Unit tests of the nearby broadcast: the engine is a recording fake whose creature search walks a scripted list of
/// players ordered by distance, so the range cut-off, the sender and the per-receiver text can be asserted.
/// </summary>
[TestFixture]
public class MessagingServiceTests
{
    private const uint Sender = 1001;
    private const uint Near = 1002;
    private const uint Mid = 1003;
    private const uint Far = 1004;
    private const uint Invalid = 0x7F000000;

    private FakeNWScript _engine = null!;
    private IDisposable _engineScope = null!;
    private MessagingService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new FakeNWScript();
        _engineScope = NWScriptRuntime.Override(_engine);
        _service = new MessagingService();
        PlayersAround(new[] { (Near, 3f), (Mid, 10f), (Far, 10.5f) });
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != Invalid);
    }

    [TearDown]
    public void TearDown() => _engineScope.Dispose();

    // The engine lists the players around the sender nearest first; the sender is the first in the list when it is a player.
    private void PlayersAround((uint Player, float Distance)[] others, bool senderIsListed = false)
    {
        (uint Player, float Distance)[] ordered = senderIsListed ? new[] { (Sender, 0f) }.Concat(others).ToArray() : others;
        _engine.SetReturnHandler("GetNearestCreature", arguments =>
        {
            var nth = (int)arguments[3]!;
            return nth <= ordered.Length ? ordered[nth - 1].Player : Invalid;
        });
        _engine.SetReturnHandler("GetDistanceBetween", arguments =>
            ordered.Where(entry => entry.Player == (uint)arguments[1]!).Select(entry => entry.Distance).DefaultIfEmpty(0f).First());
    }

    private (uint Receiver, string Text)[] Delivered() =>
        _engine.CallsTo("SendMessageToPC").Select(call => ((uint)call.Arguments[0]!, (string)call.Arguments[1]!)).ToArray();

    [Test]
    public void The_sender_and_every_player_within_range_get_the_message()
    {
        _service.SendMessageNearbyToPlayers(Sender, "Hello", 10f);

        Delivered().Should().Equal((Sender, "Hello"), (Near, "Hello"), (Mid, "Hello"));
    }

    [Test]
    public void The_range_defaults_to_ten_meters()
    {
        _service.SendMessageNearbyToPlayers(Sender, "Hello");

        Delivered().Select(delivery => delivery.Receiver).Should().Equal(Sender, Near, Mid);
    }

    [Test]
    public void A_wider_range_reaches_players_further_away()
    {
        _service.SendMessageNearbyToPlayers(Sender, "Hello", 11f);

        Delivered().Select(delivery => delivery.Receiver).Should().Equal(Sender, Near, Mid, Far);
    }

    [Test]
    public void Each_player_gets_the_text_built_for_them()
    {
        _service.SendMessageNearbyToPlayers(Sender, receiver => $"Hello {receiver}", 10f);

        Delivered().Should().Equal((Sender, $"Hello {Sender}"), (Near, $"Hello {Near}"), (Mid, $"Hello {Mid}"));
    }

    [Test]
    public void The_sender_is_not_told_twice_when_the_search_lists_them()
    {
        PlayersAround(new[] { (Near, 3f) }, senderIsListed: true);

        _service.SendMessageNearbyToPlayers(Sender, "Hello", 10f);

        Delivered().Select(delivery => delivery.Receiver).Should().Equal(Sender, Near);
    }

    [Test]
    public void The_search_looks_for_player_characters_around_the_sender()
    {
        _service.SendMessageNearbyToPlayers(Sender, "Hello", 5f);

        var searches = _engine.CallsTo("GetNearestCreature");
        searches.Should().HaveCount(2, "the nearest player is in range, then the next one is out of range and ends the walk");
        searches[0].Arguments[0].Should().Be(CreatureType.PlayerCharacter);
        searches[0].Arguments[1].Should().Be(1);
        searches[0].Arguments[2].Should().Be(Sender);
        searches[0].Arguments[3].Should().Be(1);
        searches[1].Arguments[3].Should().Be(2);
    }

    [Test]
    public void Nobody_nearby_means_only_the_sender_is_told()
    {
        PlayersAround(Array.Empty<(uint, float)>());

        _service.SendMessageNearbyToPlayers(Sender, "Hello", 10f);

        Delivered().Should().Equal((Sender, "Hello"));
    }

    [Test]
    public void A_missing_message_builder_is_rejected_before_anything_is_sent()
    {
        var send = () => _service.SendMessageNearbyToPlayers(Sender, (BuildMessageDelegate)null!);

        send.Should().Throw<ArgumentNullException>().WithParameterName("buildMessage");
        _engine.CountOf("SendMessageToPC").Should().Be(0);
    }
}
