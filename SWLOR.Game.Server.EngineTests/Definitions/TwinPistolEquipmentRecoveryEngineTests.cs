using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.EngineTests.Definitions.TwinPistolEquipmentEngineTests;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class TwinPistolEquipmentRecoveryEngineTests
{
    [EngineTest("Twin Pistol removing either hand uses owned bag space when the main inventory is full", Category = "TwinPistolEquipment", TimeoutSeconds = 50f)]
    public static async Task FullInventoryWithBag(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record => record.Skills[SkillType.TwinPistol].Rank = 25);
        var actor = player.Creature;
        var main = await ctx.EquipItemAsync(actor, "b_twinpistol", InventorySlot.RightHand);
        var spare = await WaitForPair(ctx, actor, main);
        var repository = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature().m_pcItemRepository;
        var width = repository.m_nWidth;
        var height = repository.m_nHeight;
        var boundary = repository.m_nBoundary;
        var scalable = repository.m_bScalable;
        var bag = await CreateCarriedItem(ctx, actor, "bag_b");
        try
        {
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                repository.m_nWidth = 2;
                repository.m_nHeight = 3;
                repository.m_nBoundary = 6;
                repository.m_bScalable = 0;
                var probe = CreateObject(ObjectType.Item, "b_twinpistol", GetLocation(actor));
                ctx.Track(probe);
                ctx.Assert(!ItemPlugin.MoveTo(probe, actor, true), "The main inventory cannot accept another real pair item");
                DestroyObject(probe);
            });
            foreach (var requestMain in new[] { false, true })
            {
                var properties = TwinPistolEquipmentEngineTests.Properties(main);
                await ctx.ExecuteInCreatureContextAsync(actor, () => ActionUnequipItem(requestMain ? main : spare));
                await WaitForRemovedPair(ctx, actor, main, spare);
                ctx.AssertEqual(bag, GetItemPossessor(main, true), "Safe removal returns the real item to the player's existing bag");
                ctx.AssertEqual(properties, TwinPistolEquipmentEngineTests.Properties(main), "Safe removal preserves all real item properties");
                AssertOwnedItems(ctx, actor, main, 0);
                if (requestMain) continue;
                await EquipExisting(ctx, actor, main, InventorySlot.RightHand);
                spare = await WaitForPair(ctx, actor, main);
            }
        }
        finally
        {
            repository.m_nWidth = width;
            repository.m_nHeight = height;
            repository.m_nBoundary = boundary;
            repository.m_bScalable = scalable;
        }
    }

    [EngineTest("Twin Pistol packed inventory rejects unsafe removal and later removes the pair without losing its real item", Category = "TwinPistolEquipment", TimeoutSeconds = 55f)]
    public static async Task PackedInventoryWithoutBag(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record => record.Skills[SkillType.TwinPistol].Rank = 25);
        var actor = player.Creature;
        var main = await ctx.EquipItemAsync(actor, "b_twinpistol", InventorySlot.RightHand);
        var spare = await WaitForPair(ctx, actor, main);
        var escrow = ctx.SpawnCreature("civilian", 6f);
        SetCommandable(false, escrow);
        var repository = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature().m_pcItemRepository;
        var width = repository.m_nWidth;
        var height = repository.m_nHeight;
        var boundary = repository.m_nBoundary;
        var scalable = repository.m_bScalable;
        var filler = OBJECT_INVALID;
        try
        {
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                foreach (var item in OwnedItems(actor).Where(item =>
                             GetItemPossessor(item, true) == actor && Item.GetItemSlot(actor, item) == InventorySlot.Invalid).ToArray())
                    ctx.Assert(ItemPlugin.MoveTo(item, escrow, true), "Setup preserves carried objects in test-owned storage");
                filler = CreateItemOnObject("sardine_ball", actor);
                ctx.Track(filler);
                ctx.Assert(GetIsObjectValid(filler), "The packed inventory has a real one-cell item");
                repository.m_nWidth = 1;
                repository.m_nHeight = 1;
                repository.m_nBoundary = 1;
                repository.m_bScalable = 0;
            });
            var properties = TwinPistolEquipmentEngineTests.Properties(main);
            foreach (var requestMain in new[] { false, true, false })
            {
                await ctx.ExecuteInCreatureContextAsync(actor, () => ActionUnequipItem(requestMain ? main : spare));
                await ctx.DelaySecondsAsync(.5f);
                ctx.AssertEqual(main, GetItemInSlot(InventorySlot.RightHand, actor), "Unsafe removal never drops the real pair item");
                ctx.AssertEqual(spare, GetItemInSlot(InventorySlot.LeftHand, actor), "Rejected removal keeps the complete linked arrangement");
                ctx.Assert(TwinPistolEquipment.IsActivePair(actor), "The rejected request does not leave broken half-equipment");
                ctx.AssertEqual(properties, TwinPistolEquipmentEngineTests.Properties(main), "Repeated rejection preserves all real item properties");
                ctx.AssertEqual(actor, GetItemPossessor(filler), "Rejected removal preserves the packed inventory's original contents");
                AssertOwnedItems(ctx, actor, main, 1);
            }
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                repository.m_nWidth = width;
                repository.m_nHeight = height;
                repository.m_nBoundary = boundary;
                repository.m_bScalable = scalable;
                ctx.Assert(ItemPlugin.MoveTo(filler, escrow, true), "Creating space preserves the original filler item");
                ActionUnequipItem(spare);
            });
            await WaitForRemovedPair(ctx, actor, main, spare);
            ctx.AssertEqual(properties, TwinPistolEquipmentEngineTests.Properties(main), "Successful removal retains every property after the packed-inventory retries");
            AssertOwnedItems(ctx, actor, main, 0);
        }
        finally
        {
            repository.m_nWidth = width;
            repository.m_nHeight = height;
            repository.m_nBoundary = boundary;
            repository.m_bScalable = scalable;
            SetCommandable(true, escrow);
        }
    }

    [EngineTest("Twin Pistol rejected removal never creates a recovery bag", Category = "TwinPistolEquipment", TimeoutSeconds = 40f)]
    public static async Task FullInventoryNeverCreatesRecoveryBag(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record => record.Skills[SkillType.TwinPistol].Rank = 25);
        var actor = player.Creature;
        var main = await ctx.EquipItemAsync(actor, "b_twinpistol", InventorySlot.RightHand);
        var spare = await WaitForPair(ctx, actor, main);
        var escrow = ctx.SpawnCreature("civilian", 6f);
        SetCommandable(false, escrow);
        var repository = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature().m_pcItemRepository;
        var width = repository.m_nWidth;
        var height = repository.m_nHeight;
        var boundary = repository.m_nBoundary;
        var scalable = repository.m_bScalable;
        var filler = OBJECT_INVALID;
        try
        {
            // A single large non-container filler is the arrangement migration storage would move into a new bag.
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                foreach (var item in OwnedItems(actor).Where(item =>
                             GetItemPossessor(item, true) == actor && Item.GetItemSlot(actor, item) == InventorySlot.Invalid).ToArray())
                    ctx.Assert(ItemPlugin.MoveTo(item, escrow, true), "Setup preserves carried objects in test-owned storage");
                filler = CreateItemOnObject("tit_rifle", actor);
                ctx.Track(filler);
                ctx.Assert(GetIsObjectValid(filler), "The full inventory has a real bag-sized item");
                repository.m_nWidth = 2;
                repository.m_nHeight = 4;
                repository.m_nBoundary = 8;
                repository.m_bScalable = 0;
            });
            foreach (var requestMain in new[] { false, true, false, true })
            {
                await ctx.ExecuteInCreatureContextAsync(actor, () => ActionUnequipItem(requestMain ? main : spare));
                await ctx.DelaySecondsAsync(.5f);
                ctx.AssertEqual(main, GetItemInSlot(InventorySlot.RightHand, actor), "Rejected removal keeps the real pair item equipped");
                ctx.Assert(TwinPistolEquipment.IsActivePair(actor), "Rejected removal keeps the complete linked arrangement");
                ctx.AssertEqual(actor, GetItemPossessor(filler, true), "Rejected removal leaves the filler directly in the inventory");
                ctx.AssertEqual(0, OwnedItems(actor).Count(item => GetResRef(item) == "bag_b"), "Repeated rejected removal never creates a recovery bag");
                AssertOwnedItems(ctx, actor, main, 1);
            }
        }
        finally
        {
            repository.m_nWidth = width;
            repository.m_nHeight = height;
            repository.m_nBoundary = boundary;
            repository.m_bScalable = scalable;
            SetCommandable(true, escrow);
        }
    }
}
