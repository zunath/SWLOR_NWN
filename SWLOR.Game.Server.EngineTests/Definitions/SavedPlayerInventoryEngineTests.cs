using System.Text;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class SavedPlayerInventoryEngineTests
    {
        [EngineTest("Character loading retains differently priced saved stacks", Category = "PlayerFileCheckpoint")]
        public static async Task PreserveSavedStacks(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            var otherOwner = ctx.SpawnCreature("nw_rat001", 3f);
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () => Verify(ctx, owner, otherOwner));
        }

        private static unsafe void Verify(EngineTestContext ctx, uint owner, uint otherOwner)
        {
            var first = CreateItemOnObject("sardine_ball", owner);
            var second = CreateItemOnObject("sardine_ball", otherOwner);
            ctx.Assert(GetIsObjectValid(first) && GetIsObjectValid(second), "Both saved stack fixtures exist");
            SetItemStackSize(first, 6);
            SetItemStackSize(second, 99);
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var firstItem = server.GetGameObject(first).AsNWSItem();
            var secondItem = server.GetGameObject(second).AsNWSItem();
            firstItem.m_nAdditionalCost = 21;
            secondItem.m_nAdditionalCost = 102;
            ctx.Assert(firstItem.CompareItem(secondItem) != 0, "Ordinary gameplay still uses native stacking rules");

            using var file = new CResGFF();
            using var root = new CResStruct();
            using var type = new CExoString("BIC ");
            using var version = new CExoString("V2.0");
            ctx.Assert(file.CreateGFFFile(root, type, version) != 0, "Create saved inventory fixture");
            ctx.Assert(server.GetGameObject(owner).AsNWSCreature().SaveCreature(file, root, 0, 0, 0, 0) != 0, "Save first stack");
            fixed (byte* label = Encoding.ASCII.GetBytes("IsPC\0"))
                file.WriteFieldBYTE(root, 1, label);
            using var list = new CResList();
            fixed (byte* label = Encoding.ASCII.GetBytes("ItemList\0"))
                ctx.Assert(file.GetList(list, root, label) != 0, "Find saved inventory");
            using var row = new CResStruct();
            ctx.Assert(file.AddListElement(row, list, 0) != 0, "Add distinct saved stack");
            ctx.Assert(secondItem.SaveItem(file, row, 0) != 0, "Save second stack without acquiring it");
            fixed (byte* label = Encoding.ASCII.GetBytes("Repos_PosX\0"))
                file.WriteFieldBYTE(row, 2, label);
            using var loaded = new CNWSCreature(OBJECT_INVALID, 1, 1);
            ctx.Assert(loaded.LoadCreature(file, root, 0, 0, 0, 0) != 0, "Saved player inventory loads");
            var stacks = 0;
            var cheap = 0;
            var expensive = 0;
            for (var item = GetFirstItemInInventory(loaded.m_idSelf); GetIsObjectValid(item); item = GetNextItemInInventory(loaded.m_idSelf))
            {
                if (GetResRef(item) != "sardine_ball") continue;
                stacks++;
                var native = server.GetGameObject(item).AsNWSItem();
                if (native.m_nAdditionalCost == 21) cheap += GetItemStackSize(item);
                if (native.m_nAdditionalCost == 102) expensive += GetItemStackSize(item);
            }
            ctx.AssertEqual(2, stacks, "Loading keeps both original stacks");
            ctx.AssertEqual(6, cheap, "The cheaper stack retains its quantity");
            ctx.AssertEqual(99, expensive, "The more expensive stack retains its quantity");
            ctx.Assert(firstItem.CompareItem(secondItem) != 0, "Native stacking resumes after the load scope ends");
        }
    }
}
