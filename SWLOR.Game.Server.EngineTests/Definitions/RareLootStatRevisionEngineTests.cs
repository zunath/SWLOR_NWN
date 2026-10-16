using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Rare loot correction preserves legacy and already-nerfed Zero-State equipment through repeated reloads",
        Category = LootCategory, TimeoutSeconds = 120f)]
    public static async Task RareLootPlayerCorrection(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        CreaturePlugin.AddFeat(player, FeatType.WeaponProficiencyExotic);
        var baseline = DB.Get<Player>(GetObjectUUID(player)).Attack;
        foreach (var alreadyRebalanced in new[] { false, true })
        {
            var weapon = await CreateItemAsync(ctx, player, "stw_zerostate", player);
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                StampRareLootBeforeCorrection(weapon, alreadyRebalanced);
                RemoveRequirement(weapon);
                ctx.AssertEqual(alreadyRebalanced ? 8 : 17, SumProperty(weapon, ItemPropertyType.Attack, 0),
                    "The legacy fixture includes exactly one merged +5 Attack enhancement");
            });
            await EquipAsync(ctx, player, weapon, InventorySlot.RightHand);
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                new _23_ReviseRareLootStats().Migrate(player);
                AssertRevisedLoot(ctx, weapon, new() { [(ItemPropertyType.Attack, 0)] = 5 });
                ctx.AssertEqual(22, SumProperty(weapon, ItemPropertyType.DMG, 0), "DMG stays normalized");
                ctx.AssertEqual(baseline + 17, DB.Get<Player>(GetObjectUUID(player)).Attack,
                    "The equipped weapon counts the restored Attack and its enhancement exactly once");
                ctx.Assert(!LootStatRevisionMigration.MigrateObject(player), "Retrying makes no further changes");
            });
            for (var cycle = 1; cycle <= 2; cycle++)
            {
                await UnequipAsync(ctx, player, weapon);
                ctx.AssertEqual(baseline, DB.Get<Player>(GetObjectUUID(player)).Attack, "Unequipping removes the corrected bonus");
                string saved = null;
                string identity = null;
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    SetLocalInt(weapon, "RARE_LOOT_TEST_CYCLE", cycle);
                    identity = GetObjectUUID(weapon);
                    saved = ObjectPlugin.Serialize(weapon);
                    DestroyObject(weapon);
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    weapon = Deserialize(ctx, saved);
                    ctx.Assert(ObjectPlugin.AcquireItem(player, weapon), "The player reacquires the reloaded item");
                });
                await ctx.WaitFrameAsync();
                // Locate the reacquired item and verify the inventory did not gain a duplicate.
                weapon = GetFirstItemInInventory(player);
                var matching = new List<uint>();
                for (var item = weapon; GetIsObjectValid(item); item = GetNextItemInInventory(player))
                    if (GetResRef(item) == "stw_zerostate") matching.Add(item);
                ctx.AssertEqual(1, matching.Count, "Reload keeps exactly one weapon");
                weapon = matching.Single();
                ctx.Track(weapon);
                ctx.AssertEqual(player, GetItemPossessor(weapon), "Ownership survives the reload");
                ctx.AssertEqual(1, GetItemStackSize(weapon), "Quantity survives the reload");
                ctx.AssertEqual(cycle, GetLocalInt(weapon, "RARE_LOOT_TEST_CYCLE"), "Changed state survives the reload");
                ctx.AssertEqual(identity, GetObjectUUID(weapon), "Identity survives the reload");
                AssertRevisedLoot(ctx, weapon, new() { [(ItemPropertyType.Attack, 0)] = 5 });
                await EquipAsync(ctx, player, weapon, InventorySlot.RightHand);
                ctx.AssertEqual(baseline + 17, DB.Get<Player>(GetObjectUUID(player)).Attack, "Reloaded equipment counts once");
            }
            await UnequipAsync(ctx, player, weapon);
            DestroyObject(weapon);
            await ctx.WaitFrameAsync();
        }
    }

    [EngineTest("Rare loot correction updates already-nerfed bank items without repeating enhancements",
        Category = LootCategory, TimeoutSeconds = 60f)]
    public static async Task RareLootStoredCorrection(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("nw_rat001");
        var weapon = await CreateItemAsync(ctx, owner, "stw_zerostate", owner);
        InventoryItem record = null;
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            StampRareLootBeforeCorrection(weapon, true);
            ctx.AssertEqual(8, SumProperty(weapon, ItemPropertyType.Attack, 0), "The saved fixture starts at revised Attack 3 plus enhancement 5");
            record = new InventoryItem { Data = ObjectPlugin.Serialize(weapon), Name = "Saved Zero-State", Quantity = 1 };
            DB.Set(record);
        });
        try
        {
            for (var cycle = 1; cycle <= 2; cycle++)
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    new _28_ReviseRareLootStats().Migrate();
                    var saved = DB.Get<InventoryItem>(record.Id);
                    var restored = Deserialize(ctx, saved.Data);
                    AssertRevisedLoot(ctx, restored, new() { [(ItemPropertyType.Attack, 0)] = 5 });
                    ctx.AssertEqual(1, saved.Quantity, "Bank quantity is preserved");
                    ctx.AssertEqual("Saved Zero-State", saved.Name, "Bank metadata is preserved");
                    ctx.AssertEqual(22, SumProperty(restored, ItemPropertyType.DMG, 0), "Bank DMG stays normalized");
                    ctx.Assert(!LootStatRevisionMigration.MigrateObject(restored), "A stored retry is stable");
                });
        }
        finally
        {
            DB.Delete<InventoryItem>(record.Id);
        }
    }

    private static void StampRareLootBeforeCorrection(uint item, bool alreadyRebalanced)
    {
        var enhancements = new Dictionary<(ItemPropertyType, int), int>
        {
            [(ItemPropertyType.Attack, 0)] = 5,
        };
        StampLegacyLoot(item, enhancements);
        if (!alreadyRebalanced)
            return;
        foreach (var change in LootStatRevisionCatalog.Changes[GetResRef(item)])
        {
            RemoveMatching(item, change.Type, change.Subtype);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(change.Type, LootSubtype(change),
                change.Current + enhancements.GetValueOrDefault((change.Type, change.Subtype))), item);
        }
        SetLocalInt(item, LootStatRevisionMigration.RevisionVariable, LootStatRevisionMigration.CurrentRevision);
    }
}
