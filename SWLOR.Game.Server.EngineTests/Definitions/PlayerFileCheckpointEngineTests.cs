using System.Text;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class PlayerFileCheckpointEngineTests
    {
        [EngineTest("Character export persists and reloads its migration checkpoint", Category = "PlayerFileCheckpoint")]
        public static async Task ExportCheckpoint(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () => VerifyExport(ctx, owner));
        }

        [EngineTest("Character export persists and reloads its craft file sequence", Category = "PlayerFileCheckpoint")]
        public static async Task ExportCraftSequence(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () => VerifyCraftSequence(ctx, owner));
        }

        private static unsafe void VerifyCraftSequence(EngineTestContext ctx, uint owner)
        {
            using var file = new CResGFF();
            using var root = new CResStruct();
            using var fileType = new CExoString("BIC ");
            using var format = new CExoString("V2.0");
            ctx.Assert(file.CreateGFFFile(root, fileType, format) != 0, "Create character export");
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(owner).AsNWSCreature();

            // A character that never crafted carries no field.
            ctx.Assert(native.SaveCreature(file, root, 0, 0, 1, 0) != 0, "Native export succeeds");
            var found = 0;
            fixed (byte* field = Encoding.ASCII.GetBytes("CraftFileSeq\0"))
                file.ReadFieldINT(root, field, &found, 0);
            ctx.AssertEqual(0, found, "Export without a craft sequence writes no field");

            SetLocalInt(owner, CraftSessionFileSequence.Variable, 7);
            using var sequenceFile = new CResGFF();
            using var sequenceRoot = new CResStruct();
            ctx.Assert(sequenceFile.CreateGFFFile(sequenceRoot, fileType, format) != 0, "Create second character export");
            ctx.Assert(native.SaveCreature(sequenceFile, sequenceRoot, 0, 0, 1, 0) != 0, "Native export with a sequence succeeds");
            fixed (byte* field = Encoding.ASCII.GetBytes("CraftFileSeq\0"))
                ctx.AssertEqual(7, sequenceFile.ReadFieldINT(sequenceRoot, field, &found, 0), "Export carries the craft file sequence");
            ctx.Assert(found != 0, "The craft file sequence is a real saved field");

            using var restored = new CNWSCreature(OBJECT_INVALID, 0, 1);
            ctx.Assert(restored.LoadCreature(sequenceFile, sequenceRoot, 0, 0, 0, 0) != 0, "Export reloads");
            using var name = new CExoString(CraftSessionFileSequence.Variable);
            ctx.AssertEqual(7, restored.m_ScriptVars.GetInt(name), "Reload restores the craft file sequence");
            using var exportedName = new CExoString(CraftSessionFileSequence.ExportedVariable);
            ctx.AssertEqual(7, restored.m_ScriptVars.GetInt(exportedName), "Reload records the loaded sequence as already in the file");
        }

        private static unsafe void VerifyExport(EngineTestContext ctx, uint owner)
        {
            const string variable = "PLAYER_MIGRATION_VERSION";
            SetLocalInt(owner, variable, 15);
            using var file = new CResGFF();
            using var root = new CResStruct();
            using var fileType = new CExoString("BIC ");
            using var format = new CExoString("V2.0");
            ctx.Assert(file.CreateGFFFile(root, fileType, format) != 0, "Create character export");
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(owner).AsNWSCreature();
            ctx.Assert(native.SaveCreature(file, root, 0, 0, 1, 0) != 0, "Native export succeeds");
            fixed (byte* locals = Encoding.ASCII.GetBytes("VarTable\0"))
                ctx.AssertEqual(uint.MaxValue, file.GetFieldByLabel(root, locals), "Export omits ordinary creature locals");
            var found = 0;
            fixed (byte* field = Encoding.ASCII.GetBytes("PlayerFileVer\0"))
                ctx.AssertEqual(15, file.ReadFieldINT(root, field, &found, 0), "Export carries a dedicated file checkpoint");
            ctx.Assert(found != 0, "Checkpoint is a real saved field");
            using var restored = new CNWSCreature(OBJECT_INVALID, 0, 1);
            ctx.Assert(restored.LoadCreature(file, root, 0, 0, 0, 0) != 0, "Export reloads");
            using var name = new CExoString(variable);
            ctx.AssertEqual(15, restored.m_ScriptVars.GetInt(name), "Reload restores checkpoint for the migration runner");
        }
    }
}
