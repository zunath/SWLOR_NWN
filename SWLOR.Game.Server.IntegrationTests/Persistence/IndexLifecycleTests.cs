using FluentAssertions;
using NUnit.Framework;
using StackExchange.Redis;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.IntegrationTests.Support;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.IntegrationTests.Persistence;

/// <summary>What <c>DB.Load</c> does with indexes that already exist, are missing, or disagree with the stored records.</summary>
[TestFixture]
public class IndexLifecycleTests : IntegrationTestBase
{
    private static string IndexKey(string id) => $"Index:InventoryItem:{id}";

    private void Store(string id, string name)
    {
        Db.Set(new InventoryItem { Id = id, PlayerId = "player-a", StorageId = "GLOBAL_BANK", Name = name, Quantity = 1, Data = "blob" });
    }

    [Test]
    public void Load_RegistersEveryEntityTypeWithAFingerprintedIndex()
    {
        foreach (var type in EntityFactory.EntityTypes)
        {
            Redis.KeyExists(SearchIndexDefinition.FingerprintKey(type.Name)).Should().BeTrue(type.Name);
            ((RedisResult[])Redis.Execute("FT.INFO", type.Name)).Should().NotBeEmpty(type.Name);
        }
    }

    [Test]
    public void LoadingAgain_LeavesCurrentIndexesAndTheirEntriesAlone()
    {
        Store("i-1", "Blaster");
        var before = Redis.HashGetAll(IndexKey("i-1"));

        new IntegrationDBService().Load();

        Redis.HashGetAll(IndexKey("i-1")).Should().BeEquivalentTo(before);
        Db.SearchCount(new DBQuery<InventoryItem>().AddFieldSearch(nameof(EntityBase.Id), "i-1", false)).Should().Be(1);
    }

    [Test]
    public void AMissingIndex_IsRebuiltFromTheRecordsAndTheSearchEntriesAreResynced()
    {
        Store("i-1", "Blaster");
        Store("i-2", "Rifle");
        Store("i-3", "Pistol");

        // Another build dropped the index, left a search entry for a deleted record, lost one and corrupted one.
        Redis.Execute("FT.DROPINDEX", "InventoryItem");
        Redis.KeyDelete(SearchIndexDefinition.FingerprintKey("InventoryItem"));
        Redis.HashSet(IndexKey("ghost"), new[] { new HashEntry("EntityType", "InventoryItem"), new HashEntry("Id", "ghost") });
        Redis.KeyDelete(IndexKey("i-2"));
        Redis.HashSet(IndexKey("i-3"), "Name", "Corrupted");

        var reloaded = new IntegrationDBService();
        reloaded.Load();

        Redis.KeyExists(IndexKey("ghost")).Should().BeFalse("an entry without a record is removed");
        Redis.KeyExists(IndexKey("i-2")).Should().BeTrue("an entry without its search hash is written again");
        Redis.HashGet(IndexKey("i-3"), "Name").ToString().Should().Be("Pistol", "a stale entry is rewritten from the record");

        var all = new DBQuery<InventoryItem>().AddFieldSearch(nameof(InventoryItem.PlayerId), "player-a", false);
        reloaded.SearchIds(all).Should().BeEquivalentTo("i-1", "i-2", "i-3");
        reloaded.SearchIds(new DBQuery<InventoryItem>().AddFieldSearch(nameof(InventoryItem.Name), "Pistol", false)).Should().Equal("i-3");
        reloaded.SearchCount(new DBQuery<InventoryItem>().AddFieldSearch(nameof(EntityBase.Id), "ghost", false)).Should().Be(0);
    }

    [Test]
    public void ARecordThatCannotBeRead_DoesNotStopTheRebuild()
    {
        Store("i-1", "Blaster");
        Redis.Execute("JSON.SET", "InventoryItem:broken", ".", "{\"Quantity\":\"not a number\",\"Id\":\"broken\"}");
        Redis.Execute("FT.DROPINDEX", "InventoryItem");
        Redis.KeyDelete(SearchIndexDefinition.FingerprintKey("InventoryItem"));

        var reloaded = new IntegrationDBService();
        reloaded.Load();

        reloaded.SearchIds(new DBQuery<InventoryItem>().AddFieldSearch(nameof(EntityBase.Id), "i-1", false)).Should().Equal("i-1");
    }
}
