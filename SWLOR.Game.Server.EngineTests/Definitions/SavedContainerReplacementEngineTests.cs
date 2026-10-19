using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum.Item;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    /// <summary>
    /// Migration 23 turns legacy gloves into 2x2 helmets in place. In a full bag the larger item overlaps others or runs
    /// past the bag, and the engine drops an item when it loads such a bag; the upgrade moves it instead.
    /// </summary>
    [EngineTest("A full bag keeps every item when the upgrade makes one of them larger", Category = "SavedContainerReplacement", TimeoutSeconds = 90)]
    public static async Task FullBagKeepsEnlargedItems(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var owner = fixture.Creature;
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            var bag = CreateItemOnObject("bag_sm", owner);
            var legacy = CreateItemOnObject("sabstorm_l3", owner);
            ItemPlugin.SetBaseItemType(legacy, EquipmentTypeMigration.Corrections["sabstorm_l3"].LegacyType);
            ctx.Assert(ItemPlugin.MoveTo(legacy, bag, true), "The legacy item goes in the bag first");
            // Rings fill every other cell of the bag.
            var fillers = 0;
            while (fillers < 40)
            {
                var ring = CreateItemOnObject("advent_ring", owner);
                SetLocalInt(ring, "FILLER", fillers);
                if (!ItemPlugin.MoveTo(ring, bag, true) || GetItemPossessor(ring, true) != bag)
                {
                    DestroyObject(ring);
                    break;
                }
                fillers++;
            }
            ctx.Assert(fillers > 0 && fillers < 40, $"The bag fills with rings ({fillers})");
            var ringsBefore = SavedCharacterMigration.FindItems(owner, "advent_ring").Count;

            var record = SavedCharacterMigration.CopyRecord(DB.Get<Player>(fixture.Id));
            record.Version = SavedCharacterMigration.LatestVersion - 2;
            record.CharacterFileVersion = 0;
            var file = SavedCharacterMigration.WithFileVersion(SavedCharacterMigration.SaveFile(owner), record.Version);
            var upgraded = SavedCharacterMigration.Upgrade(file, record);
            ctx.Assert(upgraded != null, "The character is upgraded");
            var character = SavedCharacterMigration.Load(ctx, upgraded);
            var helmet = SavedCharacterMigration.FindItem(character, "sabstorm_l3");
            ctx.AssertEqual(BaseItem.Helmet, GetBaseItemType(helmet), "The legacy gloves become a helmet");
            ctx.AssertEqual(character, GetItemPossessor(helmet), "The enlarged item is kept by the character");
            ctx.AssertEqual(ringsBefore, SavedCharacterMigration.FindItems(character, "advent_ring").Count, "Every ring in the bag is kept");
            ctx.AssertEqual(0, upgraded.Overflow.Count, "Nothing needs overflow storage while the inventory has room");
        });
    }

    [EngineTest("A full inventory converts its retired upgrade kits into the cells they free", Category = "SavedContainerReplacement", TimeoutSeconds = 90)]
    public static async Task FullInventoryConvertsRetiredKitsInPlace(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var owner = fixture.Creature;
        var holder = ctx.SpawnCreature("nw_bandit001");
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            // 2x2 retired kits fill every 2x2 space the inventory has, so a replacement can only use a kit's own cells.
            // A script can add items past a character's room, so the engine's own placement check decides when it is full.
            var probe = CreateItemOnObject("saber_upg1", holder);
            ctx.Assert(GetIsObjectValid(probe), "The probe kit exists");
            var kits = 0;
            while (kits < 120 && HasInventoryRoom(owner, probe))
            {
                ctx.Assert(GetIsObjectValid(CreateItemOnObject("saber_upg1", owner)), "A retired kit is created");
                kits++;
            }
            ctx.Assert(kits > 0 && kits < 120, $"The inventory fills with retired kits ({kits})");

            var upgraded = SavedCharacterMigration.Run(owner, new Player(fixture.Id), "_15_RemoveObsoleteCombatInstructionDiscs");
            ctx.AssertEqual(0, upgraded.Overflow.Count, "Every replacement fits where its retired kit was");
            var character = SavedCharacterMigration.Load(ctx, upgraded);
            var replaced = 0;
            var retired = 0;
            for (var item = GetFirstItemInInventory(character); GetIsObjectValid(item); item = GetNextItemInInventory(character))
            {
                if (GetResRef(item) == "saber_upg2") replaced++;
                if (GetResRef(item) == "saber_upg1") retired++;
            }
            ctx.AssertEqual(kits, replaced, "Every retired kit is replaced in the inventory");
            ctx.AssertEqual(0, retired, "No retired kit is left");
        });
    }

    [EngineTest("A full saved bag converts its retired upgrade kits into the cells they free", Category = "SavedContainerReplacement", TimeoutSeconds = 60)]
    public static async Task FullSavedBagConvertsRetiredKitsInPlace(EngineTestContext ctx)
    {
        // A non-player holder, so no acquire rule touches the retired kits before the saved-data conversion.
        var owner = ctx.SpawnCreature("nw_bandit001");
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            var bag = CreateItemOnObject("bag_sm", owner);
            ctx.Assert(GetIsObjectValid(bag), "The bag exists");
            // Retired kits are 2x2 and do not stack, so the bag fills before its grid runs out of cells.
            var kits = 0;
            while (kits < 20)
            {
                var kit = CreateItemOnObject("saber_upg1", owner);
                ctx.Assert(GetIsObjectValid(kit), "A retired kit is created");
                if (!ItemPlugin.MoveTo(kit, bag, true) || GetItemPossessor(kit, true) != bag)
                {
                    DestroyObject(kit);
                    break;
                }
                kits++;
            }
            ctx.Assert(kits > 0 && kits < 20, $"The bag fills with retired kits ({kits})");

            SavedCharacterMigration.EnsureDataLoaded();
            var migrate = typeof(Service.Migration).Assembly
                .GetType("SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration.SavedObsoleteItemMigration")!
                .GetMethod("MigrateSerialized", BindingFlags.NonPublic | BindingFlags.Static)!;
            var arguments = new object[] { ObjectPlugin.Serialize(bag), null, null };
            ctx.Assert((bool)migrate.Invoke(null, arguments)!, "The full bag's kits are converted");
            ctx.Assert(!(bool)arguments[2], "The bag itself is kept");

            var migrated = ObjectPlugin.Deserialize((string)arguments[1]);
            ctx.Track(migrated);
            int Count(string resref)
            {
                var count = 0;
                for (var item = GetFirstItemInInventory(migrated); GetIsObjectValid(item); item = GetNextItemInInventory(migrated))
                    if (GetResRef(item) == resref)
                        count++;
                return count;
            }
            ctx.AssertEqual(kits, Count("saber_upg2"), "Every retired kit is replaced");
            ctx.AssertEqual(0, Count("saber_upg1"), "No retired kit is left");
        });
    }

    private static unsafe bool HasInventoryRoom(uint creature, uint item)
    {
        var server = NWNXLib.g_pAppManager.m_pServerExoApp;
        var repository = server.GetGameObject(creature).AsNWSCreature().m_pcItemRepository;
        byte x = 0, y = 0;
        return repository.FindPosition(server.GetItemByGameObjectID(item), &x, &y) != 0;
    }
}
