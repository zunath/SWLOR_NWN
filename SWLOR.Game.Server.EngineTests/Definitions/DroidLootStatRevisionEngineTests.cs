using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static partial class DroidInventoryEngineTests
    {
        private const string SavedGlovesKey = "loot-revision-gloves";

        private static Dictionary<(ItemPropertyType, int), int> NoLootEnhancements => MigrationEngineTests.NoEnhancements;
        private static Dictionary<(ItemPropertyType, int), int> GloveLootEnhancements => MigrationEngineTests.GloveEnhancements;

        [EngineTest("Migration revises legacy loot saved on a droid controller through equipping, removal and resummoning", Category = "DroidInventory", TimeoutSeconds = 60f)]
        public static async Task LootRevisionSavedController(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var controller = await CreateController(ctx, owner);
            var pistol = await CreateLoot(ctx, owner, MigrationEngineTests.RevisedPistol);
            var gloves = await CreateLoot(ctx, owner, MigrationEngineTests.RevisedGloves);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                MigrationEngineTests.StampLegacyLoot(pistol, NoLootEnhancements);
                MigrationEngineTests.RemoveRequirement(pistol);
                MigrationEngineTests.StampLegacyLoot(gloves, GloveLootEnhancements);
                Droid.SaveConstructedDroid(controller, new ConstructedDroid
                {
                    Name = "Loot revision fixture",
                    EquippedItems = { [InventorySlot.RightHand] = ObjectPlugin.Serialize(pistol) },
                    Inventory = { [SavedGlovesKey] = ObjectPlugin.Serialize(gloves) }
                });
                DestroyObject(pistol);
                DestroyObject(gloves);

                ctx.Assert(LootStatRevisionMigration.MigrateObject(controller), "The droid's saved loot needs revision");
                ctx.Assert(!LootStatRevisionMigration.MigrateObject(controller), "A revised controller needs no further change");
                var saved = Droid.LoadConstructedDroid(controller);
                AssertSavedLoot(ctx, saved.EquippedItems[InventorySlot.RightHand], NoLootEnhancements);
                AssertSavedLoot(ctx, saved.Inventory[SavedGlovesKey], GloveLootEnhancements);
                ctx.AssertEqual("Loot revision fixture", saved.Name, "The revision keeps the controller's other data");
            });
            await ctx.DelaySecondsAsync(0.3f);

            var droid = await Spawn(ctx, owner, controller);
            try
            {
                for (var summon = 1; summon <= 3; summon++)
                {
                    var equipped = GetItemInSlot(InventorySlot.RightHand, droid);
                    ctx.AssertEqual(MigrationEngineTests.RevisedPistol, GetResRef(equipped), $"Summon {summon}: the droid equips its saved pistol");
                    MigrationEngineTests.AssertRevisedLoot(ctx, equipped, NoLootEnhancements);
                    ctx.AssertEqual(1, Items(droid).Count(item => GetResRef(item) == MigrationEngineTests.RevisedPistol), $"Summon {summon}: exactly one pistol");
                    var carried = Items(droid).Where(item => GetResRef(item) == MigrationEngineTests.RevisedGloves).ToList();
                    ctx.AssertEqual(1, carried.Count, $"Summon {summon}: exactly one pair of gloves");
                    MigrationEngineTests.AssertRevisedLoot(ctx, carried[0], GloveLootEnhancements);

                    var withPistol = SkinAttack(droid);
                    await Unequip(ctx, droid, equipped);
                    ctx.AssertEqual(withPistol - RevisedPistolAttack(), SkinAttack(droid), $"Summon {summon}: unequipping removes exactly the revised Attack");
                    await Equip(ctx, droid, equipped, InventorySlot.RightHand);
                    ctx.AssertEqual(withPistol, SkinAttack(droid), $"Summon {summon}: re-equipping restores it");

                    if (summon == 3)
                        break;
                    await Dismiss(ctx, owner, droid);
                    droid = await Spawn(ctx, owner, controller);
                }
            }
            finally
            {
                if (GetIsObjectValid(droid))
                    await Dismiss(ctx, owner, droid);
            }
        }

        [EngineTest("Migration revises loot a summoned droid acquires or already holds and saves it to the controller", Category = "DroidInventory", TimeoutSeconds = 60f)]
        public static async Task LootRevisionSummonedDroid(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var controller = await CreateController(ctx, owner);
            var droid = await Spawn(ctx, owner, controller);
            try
            {
                var baseline = SkinAttack(droid);
                var pistol = await CreateLoot(ctx, owner, MigrationEngineTests.RevisedPistol);
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    MigrationEngineTests.StampLegacyLoot(pistol, NoLootEnhancements);
                    MigrationEngineTests.RemoveRequirement(pistol);
                });
                await Transfer(ctx, owner, pistol, droid);
                MigrationEngineTests.AssertRevisedLoot(ctx, pistol, NoLootEnhancements);
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                    AssertSavedLoot(ctx, Droid.LoadConstructedDroid(controller).Inventory.Values.Single(), NoLootEnhancements));

                await Equip(ctx, droid, pistol, InventorySlot.RightHand);
                ctx.AssertEqual(baseline + RevisedPistolAttack(), SkinAttack(droid), "The equipped pistol adds its revised Attack");
                await StampEquippedLegacyPistol(ctx, droid, pistol);
                ctx.AssertEqual(baseline + LegacyPistolAttack(), SkinAttack(droid), "The legacy fixture adds its shipped Attack");

                await ctx.ExecuteInCreatureContextAsync(droid, () =>
                {
                    ctx.Assert(LootStatRevisionMigration.MigrateObject(droid), "The summoned droid's loot needs revision");
                    MigrationEngineTests.AssertRevisedLoot(ctx, pistol, NoLootEnhancements);
                    ctx.AssertEqual(baseline + RevisedPistolAttack(), SkinAttack(droid), "The droid's skin counts the revised Attack");
                    ctx.Assert(!LootStatRevisionMigration.MigrateObject(droid), "A retry makes no further change");
                    AssertSavedLoot(ctx, Droid.LoadConstructedDroid(controller).EquippedItems[InventorySlot.RightHand], NoLootEnhancements);
                });

                await Unequip(ctx, droid, pistol);
                ctx.AssertEqual(baseline, SkinAttack(droid), "Unequipping returns the droid's Attack to its baseline");
                await Equip(ctx, droid, pistol, InventorySlot.RightHand);
                for (var pass = 1; pass <= 2; pass++)
                {
                    await Dismiss(ctx, owner, droid);
                    droid = await Spawn(ctx, owner, controller);
                    var equipped = GetItemInSlot(InventorySlot.RightHand, droid);
                    MigrationEngineTests.AssertRevisedLoot(ctx, equipped, NoLootEnhancements);
                    ctx.AssertEqual(1, Items(droid).Count(item => GetResRef(item) == MigrationEngineTests.RevisedPistol), $"Resummon {pass}: exactly one pistol");
                    ctx.AssertEqual(baseline + RevisedPistolAttack(), SkinAttack(droid), $"Resummon {pass}: the droid counts the revised Attack");
                }
            }
            finally
            {
                if (GetIsObjectValid(droid))
                    await Dismiss(ctx, owner, droid);
            }
        }

        [EngineTest("Migration revises a player's summoned droid loot during the character migration", Category = "DroidInventory", TimeoutSeconds = 60f)]
        public static async Task LootRevisionPlayerDroid(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            var owner = player.Creature;
            var controller = await CreateController(ctx, owner);
            var droid = await Spawn(ctx, owner, controller);
            try
            {
                var baseline = SkinAttack(droid);
                var pistol = await CreateLoot(ctx, owner, MigrationEngineTests.RevisedPistol);
                var gloves = await CreateLoot(ctx, owner, MigrationEngineTests.RevisedGloves);
                await ctx.ExecuteInCreatureContextAsync(owner, () => MigrationEngineTests.RemoveRequirement(pistol));
                await Transfer(ctx, owner, pistol, droid);
                await Transfer(ctx, owner, gloves, droid);
                await Equip(ctx, droid, pistol, InventorySlot.RightHand);
                await StampEquippedLegacyPistol(ctx, droid, pistol);
                await ctx.ExecuteInCreatureContextAsync(droid, () => MigrationEngineTests.StampLegacyLoot(gloves, GloveLootEnhancements));
                ctx.AssertEqual(baseline + LegacyPistolAttack(), SkinAttack(droid), "The legacy fixture adds its shipped Attack");

                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    new _22_ReviseLootStats().Migrate(owner);
                    MigrationEngineTests.AssertRevisedLoot(ctx, pistol, NoLootEnhancements);
                    MigrationEngineTests.AssertRevisedLoot(ctx, gloves, GloveLootEnhancements);
                    ctx.AssertEqual(baseline + RevisedPistolAttack(), SkinAttack(droid), "The droid's skin counts the revised Attack");
                    ctx.AssertEqual(pistol, GetItemInSlot(InventorySlot.RightHand, droid), "The droid keeps its pistol equipped");
                    var saved = Droid.LoadConstructedDroid(controller);
                    AssertSavedLoot(ctx, saved.EquippedItems[InventorySlot.RightHand], NoLootEnhancements);
                    AssertSavedLoot(ctx, saved.Inventory.Values.Single(), GloveLootEnhancements);
                    ctx.Assert(!LootStatRevisionMigration.MigrateObject(owner), "A retry makes no further change");
                });

                await Dismiss(ctx, owner, droid);
                droid = await Spawn(ctx, owner, controller);
                MigrationEngineTests.AssertRevisedLoot(ctx, GetItemInSlot(InventorySlot.RightHand, droid), NoLootEnhancements);
                var carried = Items(droid).Where(item => GetResRef(item) == MigrationEngineTests.RevisedGloves).ToList();
                ctx.AssertEqual(1, carried.Count, "Exactly one pair of gloves returns on resummon");
                MigrationEngineTests.AssertRevisedLoot(ctx, carried[0], GloveLootEnhancements);
            }
            finally
            {
                if (GetIsObjectValid(droid))
                    await Dismiss(ctx, owner, droid);
            }
        }

        private static async Task<uint> CreateLoot(EngineTestContext ctx, uint owner, string resref)
        {
            var item = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                item = CreateItemOnObject(resref, owner);
                ctx.Assert(GetIsObjectValid(item), $"The {resref} fixture exists");
                ctx.Track(item);
            });
            await ctx.WaitFrameAsync();
            return item;
        }

        /// <summary>Gives an equipped pistol its shipped values while the droid's skin counts them, as before the revision.</summary>
        private static async Task StampEquippedLegacyPistol(EngineTestContext ctx, uint droid, uint pistol)
        {
            await Unequip(ctx, droid, pistol);
            await ctx.ExecuteInCreatureContextAsync(droid, () => MigrationEngineTests.StampLegacyLoot(pistol, NoLootEnhancements));
            await Equip(ctx, droid, pistol, InventorySlot.RightHand);
        }

        private static void AssertSavedLoot(EngineTestContext ctx, string serialized, Dictionary<(ItemPropertyType, int), int> enhancements)
        {
            var item = ObjectPlugin.Deserialize(serialized);
            ctx.Assert(GetIsObjectValid(item), "The saved droid item deserializes");
            ctx.Track(item);
            MigrationEngineTests.AssertRevisedLoot(ctx, item, enhancements);
            DestroyObject(item);
        }

        private static int SkinAttack(uint droid) =>
            Stat.GetNPCSkinStats(GetItemInSlot(InventorySlot.CreatureArmor, droid)).Attack;

        private static int RevisedPistolAttack() =>
            RareLootStatRevisionCatalog.Changes[MigrationEngineTests.RevisedPistol].Single(x => x.Type == ItemPropertyType.Attack).Current;

        private static int LegacyPistolAttack() =>
            MigrationEngineTests.LegacyValue(MigrationEngineTests.RevisedPistol, ItemPropertyType.Attack, 0, NoLootEnhancements);
    }
}
