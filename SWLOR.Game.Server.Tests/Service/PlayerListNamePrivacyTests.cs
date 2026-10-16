using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Native;

namespace SWLOR.Game.Server.Tests.Service;

[TestFixture]
public class PlayerListNamePrivacyTests
{
    private const uint AllGameMasters = 0xFFFFFFF6;
    private const uint AllPlayers = 0xFFFFFFF7;
    private const uint AllClients = 0xFFFFFFFF;
    private static readonly Func<uint, IEnumerable<(uint PlayerId, bool IsDM)>, Func<uint, int>, int> Dispatch =
        typeof(PlayerListNamePrivacy).GetMethod("Dispatch", BindingFlags.NonPublic | BindingFlags.Static)!
            .CreateDelegate<Func<uint, IEnumerable<(uint PlayerId, bool IsDM)>, Func<uint, int>, int>>();

    [Test]
    public void PlayerBroadcastNeverSerializesAnotherObserversPrivateLabel()
    {
        var players = new (uint PlayerId, bool IsDM)[] { (1, false), (2, false), (3, false), (4, true) };
        var names = new Dictionary<uint, string>
        {
            [1] = "Alice's private label", [2] = "Bob's private label",
            [3] = "A quiet traveler", [4] = "Canonical staff name"
        };
        var received = new Dictionary<uint, string>();

        // Model the native Rename path: it changes one shared creature for every
        // observer, then the engine serializes that final name into a single packet.
        int SerializeWithRename(uint recipient)
        {
            var observers = recipient == AllPlayers
                ? players.Where(player => !player.IsDM).Select(player => player.PlayerId).ToArray()
                : new[] { recipient };
            var sharedName = observers.Select(observer => names[observer]).Last();
            foreach (var observer in observers) received[observer] = sharedName;
            return 1;
        }

        SerializeWithRename(AllPlayers);
        received[1].Should().Be(names[3], "an unsplit native broadcast reproduces the leak");
        received.Clear();

        Dispatch(AllPlayers, players, SerializeWithRename).Should().Be(1);

        received.Should().BeEquivalentTo(new Dictionary<uint, string>
        {
            [1] = names[1], [2] = names[2], [3] = names[3]
        });
    }

    [TestCase(AllPlayers, new uint[] { 1, 3 })]
    [TestCase(AllGameMasters, new uint[] { 2, 4 })]
    [TestCase(AllClients, new uint[] { 1, 2, 3, 4 })]
    public void BroadcastsPreserveTheirNativeRecipientGroup(uint recipient, uint[] expected)
    {
        var players = new (uint, bool)[] { (1, false), (2, true), (3, false), (4, true) };
        var sent = new List<uint>();
        Dispatch(recipient, players, id => { sent.Add(id); return 1; }).Should().Be(1);
        sent.Should().Equal(expected);
    }

    [Test]
    public void DirectUpdatesDoNotEnumerateOrNotifyOtherPlayers()
    {
        var sent = new List<uint>();
        Dispatch(17, null!, id => { sent.Add(id); return 7; }).Should().Be(7);
        sent.Should().Equal(17);
    }

    [Test]
    public void FailedDeliveryStillAttemptsOtherRecipientsWithoutBroadcastFallback()
    {
        var sent = new List<uint>();
        Dispatch(AllPlayers, new (uint, bool)[] { (1, false), (2, false), (3, false) }, id =>
        {
            sent.Add(id);
            return id == 2 ? 0 : 1;
        }).Should().Be(0);
        sent.Should().Equal(1, 2, 3);
    }

    [Test]
    public void RecipientsAreCapturedBeforeNativeSendingChangesThePlayerList()
    {
        var players = new List<(uint, bool)> { (1, false), (2, false) };
        var sent = new List<uint>();
        Dispatch(AllPlayers, players, id =>
        {
            sent.Add(id);
            players.Clear();
            return 1;
        }).Should().Be(1);
        sent.Should().Equal(1, 2);
    }
}
