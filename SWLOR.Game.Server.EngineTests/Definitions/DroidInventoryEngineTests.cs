using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NWNXLib = NWN.Native.API.NWNXLib;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class DroidInventoryEngineTests
    {
        [EngineTest("Legacy droid inventory keys survive equipping removal and resummoning", Category = "DroidInventory")]
        public static async Task LegacyInventoryIdentity(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            foreach (var savedId in new[] { string.Empty, "outdated-item-id" })
            {
                var controller = await CreateController(ctx, owner);
                var ring = await CreateRing(ctx, owner);
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    SetLocalString(ring, "DROID_ITEM_ID", savedId);
                    Droid.SaveConstructedDroid(controller, new ConstructedDroid
                    {
                        Name = "Legacy inventory fixture",
                        Inventory = { ["legacy-inventory-key"] = ObjectPlugin.Serialize(ring) }
                    });
                    DestroyObject(ring);
                });
                var droid = await Spawn(ctx, owner, controller);
                ring = Items(droid).Single(item => GetResRef(item) == "advent_ring");
                ctx.AssertEqual("legacy-inventory-key", GetLocalString(ring, "DROID_ITEM_ID"), "The saved inventory key restores the item's identity");
                await Equip(ctx, droid, ring, InventorySlot.RightRing);
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    var saved = Droid.LoadConstructedDroid(controller);
                    ctx.AssertEqual(0, saved.Inventory.Count, "Equipping removes the inventory copy");
                    ctx.AssertEqual(1, saved.EquippedItems.Count, "One equipped copy is saved");
                });
                await Unequip(ctx, droid, ring);
                await Transfer(ctx, owner, ring, owner);
                await Dismiss(ctx, owner, droid);
                droid = await Spawn(ctx, owner, controller);
                ctx.AssertEqual(0, Items(droid).Count(item => GetResRef(item) == "advent_ring"), "A removed legacy item must never return on resummon");
                ctx.AssertEqual(owner, GetItemPossessor(ring), "The removed original remains with its owner");
                ctx.AssertEqual("Legacy inventory fixture", GetName(droid), "Inventory reconciliation preserves controller metadata");
                await Dismiss(ctx, owner, droid);
            }
        }

        [EngineTest("Droid equipment replacements persist without restoring removed equipment", Category = "DroidInventory")]
        public static async Task EquipmentRoundTrip(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var controller = await CreateController(ctx, owner);
            var original = await CreateRing(ctx, owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                SetName(original, "Original equipment");
                DeleteLocalString(original, "DROID_ITEM_ID");
                Droid.SaveConstructedDroid(controller, new ConstructedDroid
                {
                    EquippedItems = { [InventorySlot.RightRing] = ObjectPlugin.Serialize(original) }
                });
                DestroyObject(original);
            });
            var droid = await Spawn(ctx, owner, controller);
            await ctx.WaitUntilAsync(() => GetIsObjectValid(GetItemInSlot(InventorySlot.RightRing, droid)), 5f, "the original equipment to be restored");
            original = GetItemInSlot(InventorySlot.RightRing, droid);
            await Unequip(ctx, droid, original);
            await Transfer(ctx, owner, original, owner);
            var replacement = await CreateRing(ctx, owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () => SetName(replacement, "Replacement equipment"));
            await Transfer(ctx, owner, replacement, droid);
            await Equip(ctx, droid, replacement, InventorySlot.RightRing);
            for (var pass = 0; pass < 2; pass++)
            {
                await Dismiss(ctx, owner, droid);
                droid = await Spawn(ctx, owner, controller);
                await ctx.WaitUntilAsync(() => GetIsObjectValid(GetItemInSlot(InventorySlot.RightRing, droid)), 5f, "the replacement equipment to be restored");
                replacement = GetItemInSlot(InventorySlot.RightRing, droid);
                ctx.AssertEqual("Replacement equipment", GetName(replacement), "Resummoning retains the new equipment");
                ctx.AssertEqual(1, Items(droid).Count(item => GetResRef(item) == "advent_ring"), "Only one replacement item exists");
                ctx.AssertEqual(owner, GetItemPossessor(original), "The removed original remains with its owner");
            }
            await Dismiss(ctx, owner, droid);
        }

        [EngineTest("Rejected droid equipment stays carried across resummoning", Category = "DroidInventory")]
        public static async Task RejectedEquipment(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var controller = await CreateController(ctx, owner);
            var droid = await Spawn(ctx, owner, controller);
            var ring = await CreateRing(ctx, owner, false);
            await Transfer(ctx, owner, ring, droid);
            await ctx.ExecuteInCreatureContextAsync(droid, () =>
            {
                ctx.Assert(!string.IsNullOrWhiteSpace(Item.CanEquip(droid, ring)), "The ring deliberately lacks its droid racial limitation");
                ClearAllActions();
                ActionEquipItem(ring, InventorySlot.RightRing);
            });
            await ctx.DelaySecondsAsync(0.5f);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ctx.Assert(!GetIsObjectValid(GetItemInSlot(InventorySlot.RightRing, droid)), "The rejected ring is not equipped");
                var saved = Droid.LoadConstructedDroid(controller);
                ctx.AssertEqual(0, saved.EquippedItems.Count, "Failed equip attempts do not change saved equipment");
                ctx.AssertEqual(1, saved.Inventory.Count, "The rejected ring remains saved as inventory");
            });
            await Dismiss(ctx, owner, droid);
            droid = await Spawn(ctx, owner, controller);
            ctx.AssertEqual(1, Items(droid).Count(item => GetResRef(item) == "advent_ring"), "The carried ring is restored exactly once");
            await Dismiss(ctx, owner, droid);
        }

        [EngineTest("Droid dismissal checkpoints current carried stack quantities", Category = "DroidInventory")]
        public static async Task StackQuantity(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var controller = await CreateController(ctx, owner);
            var droid = await Spawn(ctx, owner, controller);
            var stack = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                stack = CreateItemOnObject("sardine_ball", owner, 6);
                ctx.Assert(GetIsObjectValid(stack), "The carried stack exists");
            });
            await ctx.WaitFrameAsync();
            await Transfer(ctx, owner, stack, droid);
            await ctx.ExecuteInCreatureContextAsync(droid, () => SetItemStackSize(stack, 3));
            await Dismiss(ctx, owner, droid);
            droid = await Spawn(ctx, owner, controller);
            var restored = Items(droid).Where(item => GetResRef(item) == "sardine_ball").ToArray();
            ctx.AssertEqual(1, restored.Length, "One carried stack returns");
            ctx.AssertEqual(3, GetItemStackSize(restored[0]), "Resummoning cannot restore already consumed items");
            await Dismiss(ctx, owner, droid);
        }

        [EngineTest("Split droid stacks retain both quantities across repeated summons", Category = "DroidInventory")]
        public static async Task SplitStacks(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var controller = await CreateController(ctx, owner);
            var droid = await Spawn(ctx, owner, controller);
            var stack = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                stack = CreateItemOnObject("sardine_ball", owner, 6);
                ctx.Assert(GetIsObjectValid(stack), "The stack fixture exists");
            });
            await ctx.WaitFrameAsync();
            await Transfer(ctx, owner, stack, droid);
            await ctx.ExecuteInCreatureContextAsync(droid, () => SplitStack(droid, stack, 2));
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(2, Items(droid).Count(item => GetResRef(item) == "sardine_ball"), "The native split creates two live stacks");
            for (var pass = 0; pass < 2; pass++)
            {
                await Dismiss(ctx, owner, droid);
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    var saved = Droid.LoadConstructedDroid(controller);
                    ctx.AssertEqual(2, saved.Inventory.Count, "Each half has its own saved inventory entry");
                });
                droid = await Spawn(ctx, owner, controller);
                var restored = Items(droid).Where(item => GetResRef(item) == "sardine_ball").ToArray();
                ctx.AssertEqual(2, restored.Length, "Both stack objects survive resummoning");
                ctx.Assert(restored.Select(GetItemStackSize).OrderBy(amount => amount).SequenceEqual(new[] { 2, 4 }), "Both split quantities are preserved");
                ctx.AssertEqual(2, restored.Select(item => GetLocalString(item, "DROID_ITEM_ID")).Distinct().Count(), "Each stack retains a distinct saved item ID");
            }
            await Dismiss(ctx, owner, droid);
        }

        [EngineTest("Droid dismissal excludes containers awaiting their queued return", Category = "DroidInventory")]
        public static async Task PendingContainerReturn(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var controller = await CreateController(ctx, owner);
            var droid = await Spawn(ctx, owner, controller);
            var bag = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                SetLocalBool(droid, "DROID_IS_SPAWNING", true);
                bag = CreateItemOnObject("bag_ex", droid);
                ctx.Assert(GetIsObjectValid(bag) && GetHasInventory(bag), "The rejected container fixture exists");
                ctx.Track(bag);
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                DeleteLocalBool(droid, "DROID_IS_SPAWNING");
                Item.ReturnItem(owner, bag);
                ctx.AssertEqual(droid, GetItemPossessor(bag), "The container remains carried until the queued return executes");
                Droid.RemoveAssociate();
                ctx.AssertEqual(0, Droid.LoadConstructedDroid(controller).Inventory.Count, "The dismissal checkpoint excludes the rejected container");
                ctx.Assert(ItemPlugin.MoveTo(bag, owner), "Recover the fixture before the dismissed droid is destroyed");
            });
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(droid), 5f, "the droid to be dismissed");
            droid = await Spawn(ctx, owner, controller);
            ctx.AssertEqual(0, Items(droid).Count(item => GetResRef(item) == "bag_ex"), "The rejected container cannot return on resummon");
            ctx.AssertEqual(owner, GetItemPossessor(bag), "The returned fixture remains with its owner");
            await Dismiss(ctx, owner, droid);
        }

        private static unsafe void SplitStack(uint droid, uint item, int amount)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            server.GetGameObject(droid).AsNWSCreature().SplitItem(server.GetGameObject(item).AsNWSItem(), amount);
        }

        private static async Task<uint> CreateController(EngineTestContext ctx, uint owner)
        {
            var controller = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                controller = CreateItemOnObject(Droid.DroidControlItemResref, owner);
                ctx.Assert(GetIsObjectValid(controller), "The controller exists");
                foreach (var stat in new[] { DroidStatSubType.MGT, DroidStatSubType.PER, DroidStatSubType.VIT,
                             DroidStatSubType.WIL, DroidStatSubType.AGI, DroidStatSubType.SOC })
                    AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidStat, (int)stat, 20), controller);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.HP, 200), controller);
            });
            await ctx.WaitFrameAsync();
            return controller;
        }

        private static async Task<uint> CreateRing(EngineTestContext ctx, uint owner, bool usableByDroid = true)
        {
            var ring = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ring = CreateItemOnObject("advent_ring", owner);
                ctx.Assert(GetIsObjectValid(ring), "The equipment fixture exists");
                var properties = new List<SWLOR.NWN.API.Engine.ItemProperty>();
                for (var property = GetFirstItemProperty(ring); GetIsItemPropertyValid(property); property = GetNextItemProperty(ring))
                    properties.Add(property);
                foreach (var property in properties)
                    RemoveItemProperty(ring, property);
                if (usableByDroid)
                    AddItemProperty(DurationType.Permanent, ItemPropertyLimitUseByRace(RacialType.Droid), ring);
            });
            await ctx.WaitFrameAsync();
            return ring;
        }

        private static async Task<uint> Spawn(EngineTestContext ctx, uint owner, uint controller)
        {
            var droid = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                Droid.SpawnDroid(owner, controller);
                droid = Droid.GetDroid(owner);
                ctx.Assert(GetIsObjectValid(droid), "The droid spawned");
                ctx.Track(droid);
            });
            await ctx.DelaySecondsAsync(0.5f);
            return droid;
        }

        private static async Task Dismiss(EngineTestContext ctx, uint owner, uint droid)
        {
            await ctx.ExecuteInCreatureContextAsync(owner, Droid.RemoveAssociate);
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(droid), 5f, "the droid to be dismissed");
        }

        private static async Task Transfer(EngineTestContext ctx, uint actor, uint item, uint recipient)
        {
            await ctx.ExecuteInCreatureContextAsync(actor, () => ctx.Assert(ItemPlugin.MoveTo(item, recipient), "The item transfer succeeds"));
            await ctx.DelaySecondsAsync(0.3f);
            ctx.AssertEqual(recipient, GetItemPossessor(item), "The recipient holds the transferred item");
        }

        private static async Task Equip(EngineTestContext ctx, uint droid, uint item, InventorySlot slot)
        {
            await ctx.ExecuteInCreatureContextAsync(droid, () =>
            {
                ctx.AssertEqual(string.Empty, Item.CanEquip(droid, item), "The equipment is usable by the droid");
                ClearAllActions();
                ActionEquipItem(item, slot);
            });
            await ctx.WaitUntilAsync(() => GetItemInSlot(slot, droid) == item, 5f, "the droid to equip the item");
        }

        private static async Task Unequip(EngineTestContext ctx, uint droid, uint item)
        {
            await ctx.ExecuteInCreatureContextAsync(droid, () =>
            {
                ClearAllActions();
                ActionUnequipItem(item);
            });
            await ctx.WaitUntilAsync(() => Item.GetItemSlot(droid, item) == InventorySlot.Invalid, 5f, "the droid to unequip the item");
        }

        private static IEnumerable<uint> Items(uint owner)
        {
            var items = new HashSet<uint>();
            for (var item = GetFirstItemInInventory(owner); GetIsObjectValid(item); item = GetNextItemInInventory(owner))
                items.Add(item);
            for (var slot = InventorySlot.Head; slot <= InventorySlot.Bolts; slot++)
            {
                var item = GetItemInSlot(slot, owner);
                if (GetIsObjectValid(item))
                    items.Add(item);
            }
            return items;
        }
    }
}
