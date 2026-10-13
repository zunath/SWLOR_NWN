using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class PlayerAppearanceRepairEngineTests
{
    [EngineTest("Saved riding appearances recover and preserve later appearance changes", Category = "AppearancePersistence")]
    public static async Task SavedRidingAppearance(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        await ctx.ExecuteInCreatureContextAsync(owner, () => VerifyLegacyRider(ctx, owner));
    }

    private static void VerifyLegacyRider(EngineTestContext ctx, uint owner)
    {
        SetPhenoType(PhenoType.SpeederBike, owner);
        SetCreatureTailType(TailType.SpeederBike, owner);
        var item = CreateItemOnObject("sardine_ball", owner);
        ctx.Assert(GetIsObjectValid(item), "Inventory fixture exists");
        SetItemStackSize(item, 3);
        var equipment = Enumerable.Range(0, NumberOfInventorySlots)
            .Select(slot => GetResRef(GetItemInSlot((InventorySlot)slot, owner))).ToArray();

        VerifyReload(ctx, owner, 0, TailType.SpeederBike, equipment);
    }

    private static unsafe void VerifyReload(EngineTestContext ctx, uint owner, int cycle,
        TailType expectedTail, string[] equipment)
    {
        using var file = new CResGFF();
        using var root = new CResStruct();
        using var type = new CExoString("BIC ");
        using var version = new CExoString("V2.0");
        ctx.Assert(file.CreateGFFFile(root, type, version) != 0, "Create saved character");
        var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(owner).AsNWSCreature();
        ctx.Assert(native.SaveCreature(file, root, 0, 0, 0, 0) != 0, "Save character appearance and inventory");
        fixed (byte* label = Encoding.ASCII.GetBytes("IsPC\0"))
            file.WriteFieldBYTE(root, 1, label);

        using var loaded = new CNWSCreature(OBJECT_INVALID, 0, 1);
        ctx.Assert(loaded.LoadCreature(file, root, 0, 0, 0, 0) != 0, "Reload saved character");
        // A saved-character object ID is not a live world object. Register a temporary
        // client as the other player fixtures do so GetIsPC exercises the native path.
        loaded.SetArea(null);
        loaded.m_bPlayerCharacter = 1;
        var server = NWNXLib.g_pAppManager.m_pServerExoApp;
        uint clientId = 0x7fff0000u;
        while (server.GetClientObjectByPlayerId(clientId) is { } existing && existing.Pointer != System.IntPtr.Zero)
            clientId++;
        using var client = new CNWSPlayer(clientId);
        client.SetGameObject(server.GetGameObject(loaded.m_idSelf).AsNWSObject());
        client.m_oidPCObject = loaded.m_idSelf;
        server.GetPlayerList().Add(client);
        try
        {
            var player = loaded.m_idSelf;
            ctx.Assert(GetIsPC(player), "Reloaded fixture follows the player appearance path");
            ctx.AssertEqual(cycle == 0 ? PhenoType.SpeederBike : PhenoType.Normal,
                GetPhenoType(player), "Saved phenotype loads before repair");
            ctx.AssertEqual(expectedTail, GetCreatureTailType(player), "Saved tail loads before repair");
            ctx.AssertEqual(cycle == 0, PlayerAppearanceRepair.Restore(player), "Only the legacy appearance needs repair");
            ctx.AssertEqual(PhenoType.Normal, GetPhenoType(player), "Player has a supported body");
            ctx.AssertEqual(cycle == 0 ? TailType.None : expectedTail, GetCreatureTailType(player),
                "Repair preserves later legitimate tail changes");

            var stacks = 0;
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                if (GetResRef(item) != "sardine_ball") continue;
                stacks++;
                ctx.AssertEqual(3, GetItemStackSize(item), "Saved stack quantity stays unchanged");
                ctx.AssertEqual(player, GetItemPossessor(item), "Reloaded inventory belongs to the character");
            }
            ctx.AssertEqual(1, stacks, "Repair does not duplicate or lose inventory");
            for (var slot = 0; slot < equipment.Length; slot++)
                ctx.AssertEqual(equipment[slot], GetResRef(GetItemInSlot((InventorySlot)slot, player)),
                    "Equipment survives every save/reload cycle");

            if (cycle < 2)
            {
                var nextTail = cycle == 0 ? TailType.Lizard : TailType.None;
                SetCreatureTailType(nextTail, player);
                VerifyReload(ctx, player, cycle + 1, nextTail, equipment);
            }
        }
        finally
        {
            server.GetPlayerList().Remove(client);
            client.m_oidPCObject = OBJECT_INVALID;
            loaded.m_bPlayerCharacter = 0;
        }
    }
}
