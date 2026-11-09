using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class RareEliteWeaponEngineTests
{
    private static readonly string[] RareElites =
    {
        "banecaller", "barrieroverse", "beaconmarks", "blackoutwrd", "bulwark", "bunkerbreak", "canyonbulwrk",
        "chemslinger", "conduitmatrn", "cryptwarden", "cycloneadpt", "deadhandzeph", "decurioncmd", "demolisherzr9",
        "dunedeadeye", "eclipseshade", "enclavesentl", "fdy_sorter", "flameweaver", "flurrychamp", "forgewright",
        "hexcaller", "invictus", "ironjaw", "markahunger", "overwatch", "quickdraw", "ruptorvane", "sabraetrial",
        "slagborn", "suppressor", "thermlancer", "triagewarden",
    };

    [EngineTest("Rare elites spawn with their longsword in hand", Category = "RareEliteWeapon", TimeoutSeconds = 60f)]
    public static async Task RareElitesSpawnArmed(EngineTestContext ctx)
    {
        foreach (var resref in RareElites)
        {
            var elite = ctx.SpawnCreature(resref);
            await ctx.WaitFrameAsync();

            var weapon = GetItemInSlot(InventorySlot.RightHand, elite);
            ctx.Assert(GetIsObjectValid(weapon), $"{resref} must hold its weapon instead of carrying it in inventory");
            ctx.AssertEqual(BaseItem.Longsword, GetBaseItemType(weapon), $"{resref} right hand");
            DestroyObject(elite);
            await ctx.WaitFrameAsync();
        }
    }
}
