using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class KorcryptRareWeaponEngineTests
{
    [EngineTest("Sith Crypt Depths rare elites spawn with their longsword in hand", Category = "KorcryptRare", TimeoutSeconds = 30f)]
    public static async Task RareElitesSpawnArmed(EngineTestContext ctx)
    {
        var offset = -4f;
        foreach (var resref in new[] { "markahunger", "cryptwarden", "eclipseshade" })
        {
            var elite = ctx.SpawnCreature(resref, offset, 0f);
            offset += 4f;
            await ctx.WaitFrameAsync();

            var weapon = GetItemInSlot(InventorySlot.RightHand, elite);
            ctx.Assert(GetIsObjectValid(weapon), $"{resref} must hold its weapon instead of carrying it in inventory");
            ctx.AssertEqual($"{resref}_wp", GetResRef(weapon), $"{resref} right hand");
        }
    }
}
