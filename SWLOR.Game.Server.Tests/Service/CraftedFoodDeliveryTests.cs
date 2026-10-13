using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Tests.Service;

public class CraftedFoodDeliveryTests
{
    [Test]
    public void FullInventory_PreservesAllFourConfiguredFoodsAcrossTwoReloads()
    {
        var fixture = new ClaimFixture(0);
        var expected = fixture.Persisted.Items.ToArray();

        for (var cycle = 0; cycle < 2; cycle++)
        {
            fixture.Claim();
            fixture.ReloadDatabaseAndCharacter();
            Assert.That(fixture.Persisted.Items, Is.EqualTo(expected));
            Assert.That(fixture.Inventory, Is.Empty);
            Assert.That(fixture.Persisted.ClaimRevision, Is.Zero);
        }

        Assert.That(fixture.DestroyRequests, Is.EqualTo(8),
            "rejected native objects are queued for destruction, so a failed attempt does not create inventory room");
        fixture.AdvanceFrame();
        fixture.FreeSlots = 4;
        fixture.Claim();
        fixture.Claim();

        Assert.That(fixture.Inventory, Is.EqualTo(expected));
        Assert.That(fixture.Acquisitions, Is.EqualTo(4));
        Assert.That(fixture.Persisted.Items, Is.Empty);
    }

    [TestCase(1)]
    [TestCase(3)]
    public void PartialInventory_ClaimsOnlyAvailableSpaceAndRetainsExactPropertiesAndCharges(int freeSlots)
    {
        var fixture = new ClaimFixture(freeSlots);
        var expected = fixture.Persisted.Items.ToArray();

        fixture.Claim();
        Assert.That(fixture.Inventory, Is.EqualTo(expected.Take(freeSlots)));
        Assert.That(fixture.Persisted.Items, Is.EqualTo(expected.Skip(freeSlots)));
        Assert.That(fixture.QueuedDestructions, Has.Count.EqualTo(4 - freeSlots));
        Assert.That(fixture.Persisted.ClaimRevision, Is.EqualTo(1));

        for (var cycle = 0; cycle < 2; cycle++)
        {
            fixture.ReloadDatabaseAndCharacter();
            fixture.Claim();
            Assert.That(fixture.Persisted.Items, Is.EqualTo(expected.Skip(freeSlots)),
                "reopening and reconnecting do not rebuild, reroll, or lose the remaining completed foods");
            Assert.That(fixture.Acquisitions, Is.EqualTo(freeSlots));
        }

        fixture.AdvanceFrame();
        fixture.FreeSlots = 4 - freeSlots;
        fixture.Claim();
        fixture.Claim();
        Assert.That(fixture.Inventory, Is.EqualTo(expected));
        Assert.That(fixture.Inventory.Select(data => JsonConvert.DeserializeObject<FoodPayload>(data)!.Charges),
            Is.EqualTo(new[] { 7, 7, 7, 7 }));
        Assert.That(fixture.Inventory.Select(data => JsonConvert.DeserializeObject<FoodPayload>(data)!.Properties),
            Is.All.EqualTo(new[] { "FoodBonus:Accuracy:9", "FoodBonus:Duration:18", "VendorBonus:231" }));
        Assert.That(fixture.Persisted.Items, Is.Empty);
        Assert.That(fixture.Acquisitions, Is.EqualTo(4));
    }

    [TestCase("character-before", false)]
    [TestCase("character-before", true)]
    [TestCase("character-after", false)]
    [TestCase("character-after", true)]
    [TestCase("database-before", false)]
    [TestCase("database-before", true)]
    [TestCase("database-after", false)]
    [TestCase("database-after", true)]
    public void InterruptedPersistence_RecoversExactlyOnceInLiveAndReconnectPaths(string failure, bool reconnect)
    {
        var fixture = new ClaimFixture(4) { Failure = failure };
        var expected = fixture.Persisted.Items.ToArray();

        Assert.Throws<IOException>(() => fixture.Claim());
        if (reconnect)
            fixture.ReloadDatabaseAndCharacter();
        fixture.Failure = null;
        fixture.Claim();
        fixture.Claim();

        Assert.That(fixture.Inventory, Is.EqualTo(expected));
        Assert.That(fixture.Persisted.Items, Is.Empty);
        Assert.That(fixture.Persisted.ClaimRevision, Is.EqualTo(1));
    }

    [TestCase("character-before")]
    [TestCase("character-after")]
    [TestCase("database-before")]
    [TestCase("database-after")]
    public void RetryAfterAwardIsConsumedOrTraded_UsesReceiptInsteadOfSearchingCurrentInventory(string failure)
    {
        var fixture = new ClaimFixture(4) { Failure = failure };
        Assert.Throws<IOException>(() => fixture.Claim());
        Assert.That(fixture.Acquisitions, Is.EqualTo(4));

        // A player can consume or trade the acquired food before retrying a failed save.
        // The inventory no longer proves the award, but its matching claim receipt does.
        fixture.Inventory.Clear();
        fixture.FreeSlots = 4;
        fixture.SaveSubsequentGameplay();
        fixture.Failure = null;
        fixture.Claim();
        fixture.ReloadDatabaseAndCharacter();
        fixture.Claim();

        Assert.That(fixture.Inventory, Is.Empty);
        Assert.That(fixture.Acquisitions, Is.EqualTo(4));
        Assert.That(fixture.Persisted.Items, Is.Empty);
    }

    [TestCase("other-delivery")]
    [TestCase("ahead-revision")]
    [TestCase("negative-index")]
    [TestCase("missing-index")]
    [TestCase("duplicate-index")]
    public void InvalidReceipt_IsRejectedBeforeAnyAwardOrPersistence(string mismatch)
    {
        var fixture = new ClaimFixture(4);
        var checkpoint = new CraftedFoodClaim
        {
            DeliveryId = fixture.Persisted.Id, Revision = 1, ItemIndexes = new() { 0 }
        };
        switch (mismatch)
        {
            case "other-delivery": checkpoint.DeliveryId = Guid.NewGuid().ToString(); break;
            case "ahead-revision": checkpoint.Revision = 2; break;
            case "negative-index": checkpoint.ItemIndexes = new() { -1 }; break;
            case "missing-index": checkpoint.ItemIndexes = new() { 4 }; break;
            case "duplicate-index": checkpoint.ItemIndexes = new() { 0, 0 }; break;
        }
        fixture.Checkpoint = JsonConvert.SerializeObject(checkpoint);
        var original = JsonConvert.SerializeObject(fixture.Persisted);

        Assert.Throws<InvalidOperationException>(() => fixture.Claim());
        Assert.That(fixture.Acquisitions, Is.Zero);
        Assert.That(fixture.CharacterSaves, Is.Zero);
        Assert.That(fixture.DatabaseSaves, Is.Zero);
        Assert.That(JsonConvert.SerializeObject(fixture.Persisted), Is.EqualTo(original));
    }

    [Test]
    public void DatabaseFailure_DoesNotMutateCachedDeliveryOrItsSerializedOutputs()
    {
        var fixture = new ClaimFixture(4) { Failure = "database-before" };
        var cached = fixture.Persisted;
        var original = JsonConvert.SerializeObject(cached);

        Assert.Throws<IOException>(() => fixture.Claim());

        Assert.That(fixture.Persisted, Is.SameAs(cached));
        Assert.That(JsonConvert.SerializeObject(cached), Is.EqualTo(original));
        Assert.That(cached.Items, Has.Count.EqualTo(4));
        Assert.That(cached.ClaimRevision, Is.Zero);
    }

    private sealed class FoodPayload
    {
        public int Portion { get; set; }
        public int Charges { get; set; }
        public string[] Properties { get; set; } = Array.Empty<string>();
    }

    private sealed class ClaimFixture
    {
        public CraftedFoodDelivery Persisted { get; private set; } = new()
        {
            PlayerId = "food-owner",
            Items = Enumerable.Range(0, 4).Select(index => JsonConvert.SerializeObject(new FoodPayload
            {
                Portion = index, Charges = 7,
                Properties = new[] { "FoodBonus:Accuracy:9", "FoodBonus:Duration:18", "VendorBonus:231" }
            })).ToList()
        };
        public List<string> Inventory { get; } = new();
        public List<string> QueuedDestructions { get; } = new();
        public string Checkpoint { get; set; } = string.Empty;
        public string Failure { get; set; }
        public int FreeSlots { get; set; }
        public int Acquisitions { get; private set; }
        public int CharacterSaves { get; private set; }
        public int DatabaseSaves { get; private set; }
        public int DestroyRequests { get; private set; }
        private List<string> _savedInventory = new();
        private int _savedFreeSlots;
        private string _savedCheckpoint = string.Empty;

        public ClaimFixture(int freeSlots)
        {
            FreeSlots = freeSlots;
            _savedFreeSlots = freeSlots;
        }

        public void Claim() => CraftedFoodClaim.Claim(Persisted,
            () => JsonConvert.DeserializeObject<CraftedFoodClaim>(Checkpoint)!,
            checkpoint => Checkpoint = JsonConvert.SerializeObject(checkpoint),
            data =>
            {
                if (FreeSlots == 0)
                {
                    DestroyRequests++;
                    QueuedDestructions.Add(data);
                    return false;
                }
                FreeSlots--;
                Acquisitions++;
                Inventory.Add(data);
                return true;
            },
            () =>
            {
                CharacterSaves++;
                if (Failure == "character-before") throw new IOException("character save interrupted");
                _savedInventory = Inventory.ToList();
                _savedFreeSlots = FreeSlots;
                _savedCheckpoint = Checkpoint;
                if (Failure == "character-after") throw new IOException("character save acknowledgement lost");
            },
            remaining =>
            {
                DatabaseSaves++;
                if (Failure == "database-before") throw new IOException("database unavailable");
                Persisted = Clone(remaining);
                if (Failure == "database-after") throw new IOException("database save acknowledgement lost");
            });

        public void ReloadDatabaseAndCharacter()
        {
            Persisted = Clone(Persisted);
            Inventory.Clear();
            Inventory.AddRange(_savedInventory);
            FreeSlots = _savedFreeSlots;
            Checkpoint = _savedCheckpoint;
            AdvanceFrame();
        }

        public void AdvanceFrame() => QueuedDestructions.Clear();

        public void SaveSubsequentGameplay()
        {
            _savedInventory = Inventory.ToList();
            _savedFreeSlots = FreeSlots;
            _savedCheckpoint = Checkpoint;
        }

        private static CraftedFoodDelivery Clone(CraftedFoodDelivery delivery) =>
            JsonConvert.DeserializeObject<CraftedFoodDelivery>(JsonConvert.SerializeObject(delivery))!;
    }
}
