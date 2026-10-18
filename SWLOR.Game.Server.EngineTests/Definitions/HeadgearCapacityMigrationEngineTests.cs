using System;
using System.Reflection;
using System.Threading.Tasks;
using NWNXLib = NWN.Native.API.NWNXLib;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Full inventory defers visor migration without blocking login or advancing checkpoints and retries without drift", Category = HeadgearCategory, TimeoutSeconds = 90)]
    public static async Task HeadgearCapacityFailureIsAtomic(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var oldChest = GetItemInSlot(InventorySlot.Chest, player);
        if (GetIsObjectValid(oldChest))
        {
            await UnequipAsync(ctx, player, oldChest);
            DestroyObject(oldChest);
            await ctx.WaitFrameAsync();
        }
        var visor = await CreateItemAsync(ctx, player, "ruptorvanecr", player);
        var head = await CreateItemAsync(ctx, player, "charm_cowl", player);
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            StampLegacyVisor(visor);
            StampLegacyLoot(head, NoEnhancements);
            RemoveRequirement(head);
        });
        var baseline = ReadEquipmentTotals(player);
        await EquipAsync(ctx, player, head, InventorySlot.Head);
        await EquipAsync(ctx, player, visor, InventorySlot.Chest);
        var totals = ReadEquipmentTotals(player);
        var visorProperties = DescribeProperties(visor);
        var headProperties = DescribeProperties(head);
        var uuid = GetObjectUUID(visor);
        var repository = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(player).AsNWSCreature().m_pcItemRepository;
        var height = repository.m_nHeight;
        var scalable = repository.m_bScalable;
        var fileVersion = 21;
        var record = DB.Get<Player>(fixture.Id);
        record.Version = 21;
        DB.Set(record);
        var apply = typeof(Migration).GetMethod("ApplyPlayerMigration", BindingFlags.Static | BindingFlags.NonPublic);
        bool ApplyMigration() => (bool)apply.Invoke(null, new object[] { new _22_ReviseLootStats(), player,
            (Func<Player>)(() => DB.Get<Player>(fixture.Id)), (Action<Player>)(updated => DB.Set(updated)),
            (Func<int>)(() => fileVersion), (Action<int>)(version => fileVersion = version) });
        try
        {
            repository.m_nHeight = 0;
            repository.m_bScalable = 0;
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                for (var attempt = 0; attempt < 2; attempt++)
                {
                    ctx.Assert(!ApplyMigration(), "The actual login migration path defers without propagating an exception or booting the player");
                    ctx.AssertEqual(21, fileVersion, "Deferred repair leaves the character checkpoint pending");
                    ctx.AssertEqual(21, DB.Get<Player>(fixture.Id).Version, "Deferred repair leaves the database checkpoint pending");
                    ctx.AssertEqual(totals, ReadEquipmentTotals(player), "Failed login attempts leave all persisted equipment totals unchanged");
                    ctx.AssertEqual(visorProperties, DescribeProperties(visor), "Failed migration leaves visor properties unchanged");
                    ctx.AssertEqual(headProperties, DescribeProperties(head), "An earlier equipment slot is not revised before chest storage succeeds");
                    ctx.AssertEqual(visor, GetItemInSlot(InventorySlot.Chest, player), "The legacy visor stays equipped after failure");
                    ctx.AssertEqual(head, GetItemInSlot(InventorySlot.Head, player), "The existing helmet stays equipped after failure");
                    ctx.AssertEqual(0, GetLocalInt(visor, Feature.MigrationDefinition.LootStatRevisionMigration.RevisionVariable), "Failure does not advance the visor revision marker");
                    ctx.AssertEqual(1, CountHeadgear(player, "ruptorvanecr"), "Failure neither loses nor duplicates the visor");
                }
            });
        }
        finally { repository.m_nHeight = height; repository.m_bScalable = scalable; }
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            ctx.Assert(ApplyMigration(), "Freeing inventory space lets the pending login migration complete");
            ctx.AssertEqual(22, fileVersion, "Successful repair advances the character checkpoint");
            ctx.AssertEqual(22, DB.Get<Player>(fixture.Id).Version, "Successful repair advances the database checkpoint");
            ctx.Assert(ApplyMigration(), "A completed login migration is a no-op");
        });
        ctx.AssertEqual(BaseItem.Helmet, GetBaseItemType(visor), "Retry converts the visor after storage becomes available");
        AssertRevisedLoot(ctx, visor, VisorEnhancements);
        var expectedAttack = baseline.Attack + SumProperty(head, ItemPropertyType.Attack, 0);
        ctx.AssertEqual(expectedAttack, ReadEquipmentTotals(player).Attack, "Only the equipped helmet contributes Attack after retry");
        for (var cycle = 0; cycle < 2; cycle++)
        {
            InventoryItem saved = null;
            await ctx.ExecuteInCreatureContextAsync(player, () => saved = Bank.DepositItem(player, visor));
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(visor), 5f, "the visor to finish bank deposit");
            await ctx.ExecuteInCreatureContextAsync(player, () => { new _28_CorrectStoredEquipmentTypes().Migrate(); Bank.WithdrawItem(player, saved.Id); });
            await ctx.WaitFrameAsync();
            visor = FindHeadgear(player, "ruptorvanecr");
            ctx.AssertEqual(uuid, GetObjectUUID(visor), "Failed-login recovery retains identity through normal bank reload");
            ctx.AssertEqual(player, GetItemPossessor(visor), "The recovered visor remains owned");
            ctx.AssertEqual(1, CountHeadgear(player, "ruptorvanecr"), "Normal reload retains exactly one recovered visor");
            ctx.AssertEqual(expectedAttack, ReadEquipmentTotals(player).Attack, "Repeated reload has no persisted Attack drift");
        }
    }
}
