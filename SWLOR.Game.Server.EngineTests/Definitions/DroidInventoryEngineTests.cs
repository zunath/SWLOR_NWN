using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NWNXLib = NWN.Native.API.NWNXLib;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class DroidInventoryEngineTests
    {
        private const string RepositoryFixture = "DROID_TEST_REPOSITORY";
        private const string RepositoryHeight = "DROID_TEST_REPOSITORY_HEIGHT";

        [NWNEventHandler(ScriptName.OnAssociateAddAfter)]
        public static void ConfigureRepositoryFixture()
        {
            if (!GetLocalBool(OBJECT_SELF, RepositoryFixture))
                return;

            var droid = StringToObject(EventsPlugin.GetEventData("ASSOCIATE_OBJECT_ID"));
            if (GetResRef(droid) != Droid.DroidResref)
                return;

            var repository = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(droid).AsNWSCreature().m_pcItemRepository;
            repository.m_nWidth = 2;
            repository.m_nHeight = (byte)GetLocalInt(OBJECT_SELF, RepositoryHeight);
            repository.m_nBoundary = (byte)(repository.m_nHeight * 2);
            repository.m_bScalable = 0;
        }

        [EngineTest("Droid restoration completes every equipment slot before immediate dismissal", Category = "DroidInventory")]
        public static async Task ImmediateDismissalAfterRestoration(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var controller = await CreateController(ctx, owner);
            await SeedRestorationItems(ctx, owner, controller, 0);
            for (var pass = 0; pass < 2; pass++)
            {
                var droid = OBJECT_INVALID;
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    Droid.SpawnDroid(owner, controller);
                    droid = Droid.GetDroid(owner);
                    ctx.Track(droid);
                    AssertRestoredItems(ctx, controller, droid, 0);
                    Droid.RemoveAssociate();
                    ctx.AssertEqual(5, Droid.LoadConstructedDroid(controller).EquippedItems.Count, "Same-script dismissal retains every intended equipment slot");
                });
                await ctx.WaitUntilAsync(() => !GetIsObjectValid(droid), 5f, "immediate dismissal to finish");
            }
        }

        [EngineTest("Droid restoration retains a full repository alongside equipped gear", Category = "DroidInventory")]
        public static async Task FullRepositoryRestoration(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var controller = await CreateController(ctx, owner);
            await SeedRestorationItems(ctx, owner, controller, 4);
            SetLocalBool(owner, RepositoryFixture, true);
            SetLocalInt(owner, RepositoryHeight, 2);
            try
            {
                for (var pass = 0; pass < 2; pass++)
                {
                    var droid = await Spawn(ctx, owner, controller);
                    AssertRestoredItems(ctx, controller, droid, 4);
                    var overflow = await CreateRing(ctx, owner);
                    await ctx.ExecuteInCreatureContextAsync(owner, () =>
                    {
                        ctx.Assert(!ItemPlugin.MoveTo(overflow, droid), "The four carried items completely fill the restored repository");
                        DestroyObject(overflow);
                    });
                    await Dismiss(ctx, owner, droid);
                }
            }
            finally
            {
                DeleteLocalBool(owner, RepositoryFixture);
                DeleteLocalInt(owner, RepositoryHeight);
            }
        }

        [EngineTest("Failed droid restoration retains items and equipment slots for later retry", Category = "DroidInventory")]
        public static async Task FailedRestorationRetry(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var controller = await CreateController(ctx, owner);
            await SeedRestorationItems(ctx, owner, controller, 1);
            SetLocalBool(owner, RepositoryFixture, true);
            SetLocalInt(owner, RepositoryHeight, 0);
            try
            {
                for (var pass = 0; pass < 2; pass++)
                {
                    var droid = await Spawn(ctx, owner, controller);
                    ctx.AssertEqual(0, Items(droid).Count(item => GetResRef(item) == "advent_ring"), "No fixture item fits in the disabled repository");
                    var saved = Droid.LoadConstructedDroid(controller);
                    ctx.AssertEqual(0, saved.EquippedItems.Count, "Failed acquisitions have no live equipment");
                    ctx.AssertEqual(6, saved.Inventory.Count, "Every failed item retains one durable retry payload");
                    ctx.Assert(saved.Inventory.Keys.All(key => key.StartsWith("DROID_RESTORE_ITEM:", System.StringComparison.Ordinal)), "Only retry payloads remain saved");
                    await Dismiss(ctx, owner, droid);
                    ctx.AssertEqual(6, Droid.LoadConstructedDroid(controller).Inventory.Count, "Dismissal cannot discard failed restorations");
                }
                SetLocalInt(owner, RepositoryHeight, 2);
                for (var pass = 0; pass < 2; pass++)
                {
                    var droid = await Spawn(ctx, owner, controller);
                    AssertRestoredItems(ctx, controller, droid, 1);
                    ctx.Assert(!Droid.LoadConstructedDroid(controller).Inventory.Keys.Any(key => key.StartsWith("DROID_RESTORE_ITEM:", System.StringComparison.Ordinal)), "Successful restoration clears all retry payloads");
                    await Dismiss(ctx, owner, droid);
                }
            }
            finally
            {
                DeleteLocalBool(owner, RepositoryFixture);
                DeleteLocalInt(owner, RepositoryHeight);
            }
        }

        private static async Task SeedRestorationItems(EngineTestContext ctx, uint owner, uint controller, int carriedCount)
        {
            var saved = new ConstructedDroid();
            foreach (var (slot, baseItem) in RestorationSlots())
            {
                var item = await CreateRing(ctx, owner);
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    ItemPlugin.SetBaseItemType(item, baseItem);
                    SetName(item, "Restored " + slot);
                    saved.EquippedItems[slot] = ObjectPlugin.Serialize(item);
                    DestroyObject(item);
                });
            }
            for (var index = 0; index < carriedCount; index++)
            {
                var item = await CreateRing(ctx, owner);
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    SetName(item, "Carried " + index);
                    saved.Inventory["carried-" + index] = ObjectPlugin.Serialize(item);
                    DestroyObject(item);
                });
            }
            Droid.SaveConstructedDroid(controller, saved);
            await ctx.WaitFrameAsync();
        }

        private static (InventorySlot Slot, BaseItem BaseItem)[] RestorationSlots() => new[]
        {
            (InventorySlot.RightRing, BaseItem.Ring), (InventorySlot.LeftRing, BaseItem.Ring),
            (InventorySlot.Neck, BaseItem.Amulet), (InventorySlot.Belt, BaseItem.Belt), (InventorySlot.Boots, BaseItem.Boots)
        };

        private static void AssertRestoredItems(EngineTestContext ctx, uint controller, uint droid, int carriedCount)
        {
            foreach (var (slot, _) in RestorationSlots())
                ctx.AssertEqual("Restored " + slot, GetName(GetItemInSlot(slot, droid)), "Every intended equipment slot is restored synchronously");
            var saved = Droid.LoadConstructedDroid(controller);
            ctx.AssertEqual(5, saved.EquippedItems.Count, "Every restored equipment slot is checkpointed");
            ctx.AssertEqual(carriedCount, saved.Inventory.Count, "Only the actual carried items are saved");
            ctx.AssertEqual(5 + carriedCount, Items(droid).Count(item => GetResRef(item) == "advent_ring"), "Each fixture item exists exactly once");
            ctx.AssertEqual(5 + carriedCount, Items(droid).Where(item => GetResRef(item) == "advent_ring").Select(item => GetName(item)).Distinct().Count(), "Every distinct fixture item survives");
        }

        [EngineTest("Migration preserves legacy droid inventory through equipping removal and resummoning", Category = "DroidInventory")]
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
                        LearnedPerks = { new DroidPerk((PerkType)23, 1) },
                        Inventory = { ["legacy-inventory-key"] = ObjectPlugin.Serialize(ring) }
                    });
                    DestroyObject(ring);
                });
                controller = await MigrateController(ctx, owner, controller);
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
                for (var pass = 0; pass < 2; pass++)
                {
                    await Dismiss(ctx, owner, droid);
                    droid = await Spawn(ctx, owner, controller);
                    ctx.AssertEqual(0, Items(droid).Count(item => GetResRef(item) == "advent_ring"), "A removed legacy item must never return on resummon");
                    ctx.AssertEqual(owner, GetItemPossessor(ring), "The removed original remains with its owner");
                    ctx.AssertEqual("Legacy inventory fixture", GetName(droid), "Inventory reconciliation preserves controller metadata");
                }
                await Dismiss(ctx, owner, droid);
            }
        }

        [EngineTest("Migration preserves droid equipment replacements across repeated summons", Category = "DroidInventory")]
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
                    LearnedPerks = { new DroidPerk((PerkType)23, 1) },
                    EquippedItems = { [InventorySlot.RightRing] = ObjectPlugin.Serialize(original) }
                });
                DestroyObject(original);
            });
            controller = await MigrateController(ctx, owner, controller);
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

        [EngineTest("Droid dismissal returns rejected containers and their contents", Category = "DroidInventory")]
        public static Task PendingContainerReturn(EngineTestContext ctx) => VerifyContainerReturn(ctx, false, false);

        [EngineTest("Droid dismissal preserves rejected containers when the owner inventory is full", Category = "DroidInventory")]
        public static Task PendingContainerReturnFullInventory(EngineTestContext ctx) => VerifyContainerReturn(ctx, true, false);

        [EngineTest("Droid death returns rejected containers and their contents", Category = "DroidInventory")]
        public static Task PendingContainerReturnOnDeath(EngineTestContext ctx) => VerifyContainerReturn(ctx, false, true);

        private static async Task VerifyContainerReturn(EngineTestContext ctx, bool fullInventory, bool death)
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
                ctx.Assert(GetIsObjectValid(CreateItemOnObject("sardine_ball", bag, 7)), "The rejected bag contains a stack to preserve");
            });
            await ctx.WaitFrameAsync();
            var repository = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(owner).AsNWSCreature().m_pcItemRepository;
            var height = repository.m_nHeight;
            var scalable = repository.m_bScalable;
            try
            {
                if (fullInventory)
                {
                    repository.m_nHeight = 0;
                    repository.m_bScalable = 0;
                }
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    DeleteLocalBool(droid, "DROID_IS_SPAWNING");
                    Item.ReturnItem(owner, bag);
                    ctx.AssertEqual(droid, GetItemPossessor(bag), "The container remains carried until the queued return executes");
                    if (fullInventory)
                        ctx.Assert(!ItemPlugin.MoveTo(bag, owner, true), "The full inventory fixture cannot accept the bag");
                    if (death)
                        ApplyEffectToObject(DurationType.Instant, EffectDeath(), droid);
                    else
                        Droid.RemoveAssociate();
                });
                await ctx.WaitUntilAsync(() => !GetIsObjectValid(Droid.GetDroid(owner)), 5f, "the droid to finish dismissal or death");
                await ctx.DelaySecondsAsync(0.3f);
                if (fullInventory)
                {
                    var saved = Droid.LoadConstructedDroid(controller);
                    ctx.AssertEqual(1, saved.Inventory.Count, $"An undeliverable bag has one durable recovery entry ({string.Join(", ", saved.Inventory.Keys)})");
                    ctx.Assert(!GetIsObjectValid(bag), "Only the saved recovery copy remains after teardown");
                    droid = await Spawn(ctx, owner, controller);
                    ctx.AssertEqual(0, Items(droid).Count(item => GetResRef(item) == "bag_ex"), "Recovery entries never become droid storage");
                    ctx.AssertEqual(1, Droid.LoadConstructedDroid(controller).Inventory.Count, "The recovery copy survives another summon with no inventory room");
                    await Dismiss(ctx, owner, droid);
                }
                else
                {
                    ctx.AssertEqual(owner, GetItemPossessor(bag), "Production returns the original bag before teardown");
                    ctx.AssertEqual(0, Droid.LoadConstructedDroid(controller).Inventory.Count, "A delivered bag has no saved duplicate");
                }
            }
            finally
            {
                repository.m_nHeight = height;
                repository.m_bScalable = scalable;
            }
            for (var pass = 0; pass < 2; pass++)
            {
                droid = await Spawn(ctx, owner, controller);
                ctx.AssertEqual(0, Items(droid).Count(item => GetResRef(item) == "bag_ex"), "The rejected bag never returns to the droid");
                var returned = Items(owner).Where(item => GetResRef(item) == "bag_ex").ToArray();
                ctx.AssertEqual(1, returned.Length, "Exactly one returned bag remains with its owner");
                var contents = Items(returned.Single()).Where(item => GetResRef(item) == "sardine_ball").ToArray();
                ctx.AssertEqual(1, contents.Length, "The returned bag retains its contained stack");
                ctx.AssertEqual(7, GetItemStackSize(contents.Single()), "The complete contained quantity survives");
                ctx.AssertEqual(0, Droid.LoadConstructedDroid(controller).Inventory.Count, "Delivery clears the recovery entry");
                await Dismiss(ctx, owner, droid);
            }
        }

        [EngineTest("Droid summons regenerate ammunition without saving engine-owned stacks", Category = "DroidInventory", TimeoutSeconds = 90f)]
        public static async Task GeneratedAmmunition(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            foreach (var (weaponBase, ammoSlot) in new[]
                     {
                         (BaseItem.Sling, InventorySlot.Bullets),
                         (BaseItem.Longbow, InventorySlot.Arrows),
                         (BaseItem.Rifle, InventorySlot.Bolts)
                     })
            {
                var controller = await CreateController(ctx, owner);
                var droid = await Spawn(ctx, owner, controller);
                var weapon = await CreateEquipment(ctx, owner, "b_longsword");
                var realAmmo = await CreateEquipment(ctx, owner, "blaster_bullets");
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    ItemPlugin.SetBaseItemType(weapon, weaponBase);
                    AddItemProperty(DurationType.Permanent, ItemPropertyUnlimitedAmmo(), weapon);
                    SetLocalString(realAmmo, "DROID_TEST_REAL_AMMO", "real");
                    SetItemStackSize(realAmmo, 37);
                });
                await Transfer(ctx, owner, realAmmo, droid);
                await Transfer(ctx, owner, weapon, droid);
                await Equip(ctx, droid, weapon, InventorySlot.RightHand);
                for (var pass = 0; pass < 2; pass++)
                {
                    ctx.Assert(HasGeneratedAmmunition(droid, ammoSlot), "The equipped weapon supplies native magical ammunition");
                    await Dismiss(ctx, owner, droid);
                    var saved = Droid.LoadConstructedDroid(controller);
                    ctx.AssertEqual(1, saved.EquippedItems.Count, "Only the real weapon is saved as equipment");
                    ctx.Assert(!saved.EquippedItems.ContainsKey(ammoSlot), "Engine-owned ammunition is never serialized");
                    ctx.AssertEqual(1, saved.Inventory.Count, "Only the real carried ammunition is saved");
                    droid = await Spawn(ctx, owner, controller);
                    await ctx.WaitUntilAsync(() => HasGeneratedAmmunition(droid, ammoSlot), 5f, "native ammunition to regenerate after resummoning");
                    var realStacks = Items(droid).Where(item => GetLocalString(item, "DROID_TEST_REAL_AMMO") == "real").ToArray();
                    ctx.AssertEqual(1, realStacks.Length, "One real carried stack survives alongside regenerated ammunition");
                    ctx.AssertEqual(37, GetItemStackSize(realStacks.Single()), "Carried ammunition retains its full quantity");
                    ctx.AssertEqual(2, Items(droid).Count(item => GetBaseItemType(item) is BaseItem.Arrow or BaseItem.Bullet or BaseItem.Bolt), "There is one real stack and one generated stack");
                }
                await Dismiss(ctx, owner, droid);
            }
        }

        [EngineTest("Droid summons retain real equipped ammunition and its quantity", Category = "DroidInventory")]
        public static async Task RealEquippedAmmunition(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var controller = await CreateController(ctx, owner);
            var droid = await Spawn(ctx, owner, controller);
            var ammo = await CreateEquipment(ctx, owner, "blaster_bullets");
            await ctx.ExecuteInCreatureContextAsync(owner, () => SetItemStackSize(ammo, 37));
            await Transfer(ctx, owner, ammo, droid);
            await Equip(ctx, droid, ammo, InventorySlot.Bullets);
            for (var pass = 0; pass < 2; pass++)
            {
                await Dismiss(ctx, owner, droid);
                ctx.Assert(Droid.LoadConstructedDroid(controller).EquippedItems.ContainsKey(InventorySlot.Bullets), "Real equipped ammunition is saved");
                droid = await Spawn(ctx, owner, controller);
                await ctx.WaitUntilAsync(() => GetIsObjectValid(GetItemInSlot(InventorySlot.Bullets, droid)), 5f, "real ammunition to be restored");
                ctx.Assert(!HasGeneratedAmmunition(droid, InventorySlot.Bullets), "The restored stack is real ammunition");
                ctx.AssertEqual(37, GetItemStackSize(GetItemInSlot(InventorySlot.Bullets, droid)), "The full real equipped quantity survives");
                ctx.AssertEqual(1, Items(droid).Count(item => GetBaseItemType(item) == BaseItem.Bullet), "No extra real ammunition appears");
            }
            await Dismiss(ctx, owner, droid);
        }

        [EngineTest("Droid inventory changes near hostiles preserve the equipped rifle across summons", Category = "DroidInventory", TimeoutSeconds = 90f)]
        public static async Task InventoryChangesPreserveRifle(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var controller = await CreateController(ctx, owner);
            var droid = await Spawn(ctx, owner, controller);
            var rifle = await CreateEquipment(ctx, owner, "oph_rifle");
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
                AddItemProperty(DurationType.Permanent, ItemPropertyUnlimitedAmmo(), rifle));
            await Transfer(ctx, owner, rifle, droid);
            await Equip(ctx, droid, rifle, InventorySlot.RightHand);

            var enemy = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            ctx.MakeHostile(enemy);
            SetAILevel(enemy, AILevel.VeryLow);
            SetPlotFlag(enemy, true);

            for (var pass = 0; pass < 2; pass++)
            {
                // An NPC owner keeps companion combat idle, isolating inventory-event AI.
                ObjectPlugin.SetPosition(enemy, GetPosition(droid) + new System.Numerics.Vector3(1f, 0f, 0f));
                CreaturePlugin.DoPerceptionUpdateOnCreature(droid, enemy);
                ctx.Assert(GetObjectSeen(enemy, droid), "The droid perceives the nearby hostile");
                var carried = OBJECT_INVALID;
                await ctx.ExecuteInCreatureContextAsync(droid, () =>
                {
                    ClearAllActions();
                    carried = CreateItemOnObject("blaster_bullets", droid, 3);
                    ctx.Assert(GetIsObjectValid(carried), "Carried ammunition is created");
                    SetItemStackSize(carried, 3);
                });
                await DispatchHostileInventoryDisturbance(ctx, droid, enemy, carried, DisturbType.Added);
                for (var tick = 0; tick < 12; tick++)
                {
                    await ctx.DelaySecondsAsync(0.25f);
                    ctx.AssertEqual(rifle, GetItemInSlot(InventorySlot.RightHand, droid), "Inventory acquisition never switches the rifle to unarmed combat");
                }
                await Transfer(ctx, droid, carried, owner);
                await DispatchHostileInventoryDisturbance(ctx, droid, enemy, carried, DisturbType.Removed);
                await ctx.DelaySecondsAsync(3f);
                ctx.AssertEqual(rifle, GetItemInSlot(InventorySlot.RightHand, droid), "Inventory removal preserves the rifle");
                ctx.AssertEqual(droid, GetItemPossessor(rifle), "The droid still owns the rifle");
                ctx.AssertEqual(owner, GetItemPossessor(carried), "The owner receives the removed ammunition");
                ctx.AssertEqual(3, GetItemStackSize(carried), "Removing ammunition preserves its quantity");
                await ctx.ExecuteInCreatureContextAsync(owner, () => DestroyObject(carried));
                await Dismiss(ctx, owner, droid);
                var saved = Droid.LoadConstructedDroid(controller);
                ctx.AssertEqual(1, saved.EquippedItems.Count, "Only the rifle is saved as equipment");
                ctx.AssertEqual(0, saved.Inventory.Count, "Removed and engine-owned ammunition are not saved");
                droid = await Spawn(ctx, owner, controller);
                rifle = GetItemInSlot(InventorySlot.RightHand, droid);
                ctx.Assert(GetIsObjectValid(rifle) && GetResRef(rifle) == "oph_rifle", "The same rifle blueprint is restored in hand");
                ctx.AssertEqual(1, Items(droid).Count(item => GetResRef(item) == "oph_rifle"), "Exactly one rifle survives resummoning");
            }
            await Dismiss(ctx, owner, droid);
        }

        private static Task DispatchHostileInventoryDisturbance(
            EngineTestContext ctx, uint droid, uint enemy, uint item, DisturbType type)
        {
            return ctx.ExecuteInCreatureContextAsync(droid, () =>
            {
                ClearAllActions();
                // Supply the native event state explicitly: item creation alone can leave
                // GetLastDisturbed invalid or friendly, bypassing the stock combat branch.
                var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(droid).AsNWSCreature();
                native.m_oidLastDisturbed = enemy;
                native.m_oidInventoryDisturbItem = item;
                native.m_nInventoryDisturbType = (int)type;
                native.m_oidAttemptedAttackTarget = OBJECT_INVALID;
                native.m_oidAttemptedSpellTarget = OBJECT_INVALID;
                ctx.AssertEqual(enemy, GetLastDisturbed(), "The inventory disturbance has a valid hostile actor");
                ctx.Assert(GetIsEnemy(enemy), "The inventory disturber is hostile to the droid");
                ctx.AssertEqual(type, GetInventoryDisturbType(), "The native disturbance type is installed");
                ctx.AssertEqual(item, GetInventoryDisturbItem(), "The native disturbance item is installed");
                ExecuteScript(ScriptName.OnDroidDisturbed, droid);
            });
        }

        private static bool HasGeneratedAmmunition(uint droid, InventorySlot slot)
        {
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(droid)?.AsNWSCreature();
            return native != null && GetIsObjectValid(GetItemInSlot(slot, droid)) && slot switch
            {
                InventorySlot.Arrows => native.m_bMagicalArrowsEquipped != 0,
                InventorySlot.Bullets => native.m_bMagicalBulletsEquipped != 0,
                InventorySlot.Bolts => native.m_bMagicalBoltsEquipped != 0,
                _ => false
            };
        }

        private static unsafe void SplitStack(uint droid, uint item, int amount)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            server.GetGameObject(droid).AsNWSCreature().SplitItem(server.GetGameObject(item).AsNWSItem(), amount);
        }

        private static async Task<uint> MigrateController(EngineTestContext ctx, uint owner, uint controller)
        {
            var migrated = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var migrate = typeof(Droid).Assembly
                    .GetType("SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration.StoredItemDataMigration")
                    .GetMethod("MigrateSerializedObject", BindingFlags.NonPublic | BindingFlags.Static);
                var result = migrate.Invoke(null, new object[] { ObjectPlugin.Serialize(controller) });
                ctx.AssertEqual(true, (bool)result.GetType().GetProperty("Changed").GetValue(result), "The legacy controller requires an actual migration");
                migrated = ObjectPlugin.Deserialize((string)result.GetType().GetProperty("Data").GetValue(result));
                ctx.Assert(GetIsObjectValid(migrated), "The migrated controller reloads");
                ctx.Track(migrated);
                ctx.Assert(ItemPlugin.MoveTo(migrated, owner), "The owner receives the migrated controller");
                DestroyObject(controller);
                ctx.AssertEqual(0, Droid.LoadConstructedDroid(migrated).LearnedPerks.Count, "Migration removes the historical reassigned instruction");
                var retry = migrate.Invoke(null, new object[] { ObjectPlugin.Serialize(migrated) });
                ctx.AssertEqual(false, (bool)retry.GetType().GetProperty("Changed").GetValue(retry), "The migrated controller is stable on retry before gameplay changes");
            });
            await ctx.WaitFrameAsync();
            return migrated;
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

        private static Task<uint> CreateRing(EngineTestContext ctx, uint owner, bool usableByDroid = true) =>
            CreateEquipment(ctx, owner, "advent_ring", usableByDroid);

        private static async Task<uint> CreateEquipment(EngineTestContext ctx, uint owner, string resref, bool usableByDroid = true)
        {
            var ring = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ring = CreateItemOnObject(resref, owner);
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
