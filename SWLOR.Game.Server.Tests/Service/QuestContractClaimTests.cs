using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.QuestContractService;

namespace SWLOR.Game.Server.Tests.Service;

public class QuestContractClaimTests
{
    [TestCase("character-before", false)]
    [TestCase("character-before", true)]
    [TestCase("character-after", false)]
    [TestCase("character-after", true)]
    [TestCase("database-before", false)]
    [TestCase("database-before", true)]
    [TestCase("database-after", false)]
    [TestCase("database-after", true)]
    public void InterruptedClaim_RetainsValueWithoutReplayingAwards(string failure, bool reconnect)
    {
        var persisted = Delivery();
        var liveItems = 0;
        var liveGold = 0;
        var savedItems = 0;
        var savedGold = 0;
        string marker = "";
        string savedMarker = "";
        var interrupt = true;

        QuestContractDelivery Claim() => QuestContractClaim.Claim(Clone(persisted),
            () => JsonConvert.DeserializeObject<QuestContractClaim>(marker)!,
            value => marker = JsonConvert.SerializeObject(value),
            _ => { liveItems++; return true; },
            credits => liveGold += credits,
            () =>
            {
                if (interrupt && failure == "character-before") throw new IOException("file save interrupted");
                savedItems = liveItems;
                savedGold = liveGold;
                savedMarker = marker;
                if (interrupt && failure == "character-after") throw new IOException("file save acknowledgement lost");
            },
            remaining =>
            {
                if (interrupt && failure == "database-before") throw new IOException("database save interrupted");
                persisted = Clone(remaining);
                if (interrupt && failure == "database-after") throw new IOException("database acknowledgement lost");
            });

        Assert.Throws<IOException>(() => Claim());
        if (reconnect)
        {
            liveItems = savedItems;
            liveGold = savedGold;
            marker = savedMarker;
        }
        interrupt = false;
        Claim();
        Claim();
        liveItems.Should().Be(2, "the restored inventory must contain one copy of each reward");
        liveGold.Should().Be(125, "the live and reconnect paths pay credits exactly once");
        persisted.Items.Should().BeEmpty();
        persisted.Credits.Should().Be(0);
    }

    [Test]
    public void PartialAcquisition_CheckpointsOnlySuccessfulItemsAndDefersCredits()
    {
        var persisted = Delivery();
        string marker = "";
        var received = new List<string>();
        var credits = 0;
        var rejectSecond = true;
        void Claim() => QuestContractClaim.Claim(Clone(persisted),
            () => JsonConvert.DeserializeObject<QuestContractClaim>(marker)!,
            value => marker = JsonConvert.SerializeObject(value),
            item =>
            {
                if (rejectSecond && item.Data == "second") return false;
                received.Add(item.Data);
                return true;
            }, amount => credits += amount, () => { }, value => persisted = Clone(value));
        Claim();
        received.Should().Equal("first");
        credits.Should().Be(0);
        persisted.Items.Select(item => item.Data).Should().Equal("second");
        rejectSecond = false;
        Claim();
        received.Should().Equal("first", "second");
        credits.Should().Be(125);
        persisted.Items.Should().BeEmpty();
    }

    [Test]
    public void FailedPersistence_DoesNotMutateTheCachedDelivery()
    {
        var original = Delivery();
        Assert.Throws<IOException>(() => QuestContractClaim.Claim(original,
            () => null!, _ => { }, _ => true, _ => { }, () => { },
            _ => throw new IOException("database unavailable")));
        original.Items.Should().HaveCount(2);
        original.Credits.Should().Be(125);
        original.ClaimRevision.Should().Be(0);
    }

    private static QuestContractDelivery Delivery() => new()
    {
        IsRewardPayment = true, Credits = 125,
        Items = new List<QuestContractItem> { new() { Data = "first" }, new() { Data = "second" } }
    };

    private static QuestContractDelivery Clone(QuestContractDelivery value) =>
        JsonConvert.DeserializeObject<QuestContractDelivery>(JsonConvert.SerializeObject(value))!;
}
