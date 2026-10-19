using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Dungeon saber exchange covers equipment, bags, archives and droids once", Category = "DungeonSaberMigration")]
    public static async Task DungeonSaberInventoryExchange(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        await ctx.EquipItemAsync(owner, "sabstorm_l1", InventorySlot.RightHand);
        var bag = await CreateItemAsync(ctx, owner, "bag_sm", owner);
        await CreateItemAsync(ctx, owner, "sabcycl_l2", bag);
        var normal = await CreateItemAsync(ctx, owner, "b_longsword", owner);
        var controller = await CreateItemAsync(ctx, owner, Droid.DroidControlItemResref, owner);
        var root = await CreateItemAsync(ctx, owner, "eclipse_w1", owner);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            SetLocalString(root, "DROID_ITEM_ID", "dungeon-saber-fixture");
            var saved = ObjectPlugin.Serialize(root);
            ctx.Assert(DungeonSaberMigration.MigrateSerializedObject(saved, out var migrated), "Saved root is converted");
            ctx.AssertEqual("eclipse_w3", GetResRef(Deserialize(ctx, migrated)), "Root becomes same-tier nonweapon gear");
            ctx.Assert(!DungeonSaberMigration.MigrateSerializedObject(migrated, out var retry), "Converted root is idempotent");
            ctx.AssertEqual(migrated, retry, "Unchanged archive stays byte-for-byte intact");
            var archive = ObjectPlugin.Serialize(owner);
            ctx.Assert(DungeonSaberMigration.MigrateSerializedObject(archive, out var creature), "Saved equipment and bags are repaired");
            var restored = Deserialize(ctx, creature);
            ctx.Assert(!GetIsObjectValid(GetItemInSlot(InventorySlot.RightHand, restored)), "Replacement cannot stay in weapon slot");
            ctx.AssertEqual("sabcycl_l4", GetResRef(GetFirstItemInInventory(
                GetItemPossessedBy(restored, GetTag(bag)))), "Nested bag item is converted");

            SetLocalString(controller, "CONSTRUCTED_DROID", JsonConvert.SerializeObject(new ConstructedDroid
            {
                EquippedItems = { [InventorySlot.RightHand] = saved }
            }));

            // The same exchange on the saved character file, as the boot-time upgrade runs it.
            var upgraded = SavedCharacterMigration.Run(owner, new Player(GetObjectUUID(owner)), "_18_ReplaceDungeonSabers");
            var character = SavedCharacterMigration.Load(ctx, upgraded);
            ctx.Assert(!GetIsObjectValid(GetItemInSlot(InventorySlot.RightHand, character)), "The saber is unequipped");
            var characterBag = SavedCharacterMigration.FindItem(character, "bag_sm");
            ctx.AssertEqual("sabcycl_l4", GetResRef(GetFirstItemInInventory(characterBag)), "The nested weapon is exchanged");
            ctx.AssertEqual("b_longsword", GetResRef(SavedCharacterMigration.FindItem(character, "b_longsword")),
                "Unrelated weapon remains untouched");
            var droid = JsonConvert.DeserializeObject<ConstructedDroid>(GetLocalString(
                SavedCharacterMigration.FindItem(character, Droid.DroidControlItemResref), "CONSTRUCTED_DROID"));
            ctx.AssertEqual(0, droid.EquippedItems.Count, "Droid weapon slot is cleared");
            ctx.AssertEqual("eclipse_w3", GetResRef(Deserialize(ctx, droid.Inventory["dungeon-saber-fixture"])),
                "Droid keeps the replacement under its original inventory ID");
            ctx.Assert(!DungeonSaberMigration.MigrateObject(character), "The migrated character holds no saber the acquisition path would exchange");

            var retried = SavedCharacterMigration.Run(upgraded.File, upgraded.Record, "_18_ReplaceDungeonSabers");
            ctx.Assert(retried.File.SequenceEqual(upgraded.File), "A retry cannot grant another replacement");
        });
    }


    [EngineTest("Replacing a worn dungeon saber takes its stats off the record", Category = "DungeonSaberMigration")]
    public static async Task DungeonSaberWornStatsLeaveRecord(EngineTestContext ctx)
    {
        // Players cannot acquire a retired dungeon saber (the acquire rule exchanges it), so a creature wears it, as an
        // old character file still can.
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        var saber = await ctx.EquipItemAsync(owner, "sabstorm_l1", InventorySlot.RightHand);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            var attack = SumProperty(saber, ItemPropertyType.Attack, 0);
            var record = new Player(Guid.NewGuid().ToString()) { Version = 17, Attack = 100 + attack };
            ctx.Assert(attack > 0, "The fixture saber grants Attack");
            var upgraded = SavedCharacterMigration.Run(owner, record, "_18_ReplaceDungeonSabers");
            ctx.AssertEqual(record.Attack - attack, upgraded.Record.Attack, "The replaced saber's Attack leaves the record");
        });
    }
}
