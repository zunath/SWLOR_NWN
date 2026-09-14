using System.Text;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;

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
