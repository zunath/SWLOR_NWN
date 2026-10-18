using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Rare loot correction preserves legacy and already-nerfed Zero-State equipment in the saved character through repeated reloads",
        Category = LootCategory, TimeoutSeconds = 300f)]
    public static async Task RareLootPlayerCorrection(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        CreaturePlugin.AddFeat(player, FeatType.WeaponProficiencyExotic);
        var baseline = DB.Get<Player>(GetObjectUUID(player)).Attack;
        foreach (var alreadyRebalanced in new[] { false, true })
        {
            var original = await CreateItemAsync(ctx, player, "stw_zerostate", player);
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                StampRareLootBeforeCorrection(original, alreadyRebalanced);
                RemoveRequirement(original);
                ctx.AssertEqual(alreadyRebalanced ? 8 : 17, SumProperty(original, ItemPropertyType.Attack, 0),
                    "The legacy fixture includes exactly one merged +5 Attack enhancement");
            });
            await EquipAsync(ctx, player, original, InventorySlot.RightHand);
            SavedCharacterMigration.Upgraded upgraded = null;
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                upgraded = SavedCharacterMigration.Run(player, DB.Get<Player>(fixture.Id), "_22_ReviseLootStats");
                var migrated = GetItemInSlot(InventorySlot.RightHand, SavedCharacterMigration.Load(ctx, upgraded));
                ctx.AssertEqual("stw_zerostate", GetResRef(migrated), "The weapon stays equipped");
                AssertRevisedLoot(ctx, migrated, new() { [(ItemPropertyType.Attack, 0)] = 5 });
                ctx.AssertEqual(22, SumProperty(migrated, ItemPropertyType.DMG, 0), "DMG stays normalized");
                ctx.AssertEqual(baseline + 17, upgraded.Record.Attack,
                    "The equipped weapon counts the restored Attack and its enhancement exactly once");
                var retried = SavedCharacterMigration.Run(upgraded.File, upgraded.Record, "_22_ReviseLootStats");
                ctx.Assert(retried.File.SequenceEqual(upgraded.File), "Retrying makes no further changes");
                ctx.AssertEqual(upgraded.Record.Attack, retried.Record.Attack, "Retrying changes no record total");
            });

            // The player logs in with the corrected weapon and plays on through repeated reloads.
            using var login = await PlayerAbilityFixture.CreateFromCharacterAsync(ctx, upgraded.File, upgraded.Record, 3f);
            var loggedIn = login.Creature;
            var weapon = GetItemInSlot(InventorySlot.RightHand, loggedIn);
            ctx.AssertEqual("stw_zerostate", GetResRef(weapon), "The upgraded character logs in with the weapon equipped");
            ctx.AssertEqual(baseline + 17, DB.Get<Player>(login.Id).Attack, "The equipped weapon counts once after login");
            for (var cycle = 1; cycle <= 2; cycle++)
            {
                await UnequipAsync(ctx, loggedIn, weapon);
                ctx.AssertEqual(baseline, DB.Get<Player>(login.Id).Attack, "Unequipping removes the corrected bonus");
                string saved = null;
                await ctx.ExecuteInCreatureContextAsync(loggedIn, () =>
                {
                    SetLocalInt(weapon, "RARE_LOOT_TEST_CYCLE", cycle);
                    saved = ObjectPlugin.Serialize(weapon);
                    DestroyObject(weapon);
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(loggedIn, () =>
                {
                    weapon = Deserialize(ctx, saved);
                    ctx.Assert(ObjectPlugin.AcquireItem(loggedIn, weapon), "The player reacquires the reloaded item");
                });
                await ctx.WaitFrameAsync();
                // Locate the reacquired item and verify the inventory did not gain a duplicate.
                var matching = SavedCharacterMigration.FindItems(loggedIn, "stw_zerostate");
                ctx.AssertEqual(1, matching.Count, "Reload keeps exactly one weapon");
                weapon = matching.Single();
                ctx.Track(weapon);
                ctx.AssertEqual(loggedIn, GetItemPossessor(weapon), "Ownership survives the reload");
                ctx.AssertEqual(1, GetItemStackSize(weapon), "Quantity survives the reload");
                ctx.AssertEqual(cycle, GetLocalInt(weapon, "RARE_LOOT_TEST_CYCLE"), "Changed state survives the reload");
                AssertRevisedLoot(ctx, weapon, new() { [(ItemPropertyType.Attack, 0)] = 5 });
                await EquipAsync(ctx, loggedIn, weapon, InventorySlot.RightHand);
                ctx.AssertEqual(baseline + 17, DB.Get<Player>(login.Id).Attack, "Reloaded equipment counts once");
            }
            await UnequipAsync(ctx, loggedIn, weapon);
            DestroyObject(weapon);
            await ctx.WaitFrameAsync();

            await UnequipAsync(ctx, player, original);
            DestroyObject(original);
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
                    new _28_CorrectStoredEquipmentTypes().Migrate();
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
