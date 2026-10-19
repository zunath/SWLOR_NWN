using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
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
}
