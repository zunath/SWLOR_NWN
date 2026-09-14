using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class SavedPlayerClassLayoutEngineTests
    {
        [EngineTest("Saved player class rows retain levels and unrelated data", Category = "PlayerFileCheckpoint")]
        public static async Task NormalizeRepeatedClasses(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                Verify(ctx, owner, new byte[] { 66, 66 }, new byte[] { 5, 1 }, true);
                Verify(ctx, owner, new byte[] { 66, 66, 67 }, new byte[] { 5, 3, 32 }, true);
                Verify(ctx, owner, new byte[] { 66, 66, 66 }, new byte[] { 5, 3, 32 }, true);
                Verify(ctx, owner, new byte[] { 66, 67 }, new byte[] { 8, 32 }, false);
                Verify(ctx, owner, new byte[] { 66, 66 }, new byte[] { 5, 1 }, false, extraMetadata: true);
                Verify(ctx, owner, new byte[] { 66, 66 }, new byte[] { 5, 1 }, false, player: false);
                Verify(ctx, owner, new byte[] { 66, 66 }, new byte[] { 20, 21 }, false);
                Verify(ctx, owner, new byte[] { 255, 255 }, new byte[] { 5, 1 }, false);
            });
        }

        private static unsafe void Verify(EngineTestContext ctx, uint owner, byte[] classes, byte[] levels,
            bool expectedChange, bool extraMetadata = false, bool player = true)
        {
            using var file = new CResGFF();
            using var root = new CResStruct();
            using var type = new CExoString("BIC ");
            using var version = new CExoString("V2.0");
            ctx.Assert(file.CreateGFFFile(root, type, version) != 0, "Create saved character fixture");
            fixed (byte* label = Encoding.ASCII.GetBytes("IsPC\0"))
                file.WriteFieldBYTE(root, player ? (byte)1 : (byte)0, label);
            using var list = new CResList();
            fixed (byte* label = Encoding.ASCII.GetBytes("ClassList\0"))
                ctx.Assert(file.AddList(list, root, label) != 0, "Create class rows");
            for (var index = 0; index < classes.Length; index++)
            {
                using var row = new CResStruct();
                file.AddListElement(row, list, 2);
                fixed (byte* label = Encoding.ASCII.GetBytes("Class\0"))
                    file.WriteFieldINT(row, classes[index], label);
                fixed (byte* label = Encoding.ASCII.GetBytes("ClassLevel\0"))
                    file.WriteFieldSHORT(row, levels[index], label);
            }
            if (extraMetadata)
            {
                using var row = new CResStruct();
                file.GetListElement(row, list, 1);
                fixed (byte* label = Encoding.ASCII.GetBytes("UnknownMetadata\0"))
                    file.WriteFieldINT(row, 1234, label);
            }
            fixed (byte* label = Encoding.ASCII.GetBytes("AuditSentinel\0"))
                file.WriteFieldINT(root, 4567, label);
            var normalize = typeof(Migration).Assembly.GetType("SWLOR.Game.Server.Native.SavedPlayerClassLayout")
                .GetMethod("Normalize", BindingFlags.Static | BindingFlags.NonPublic);
            using var normalized = (IDisposable)normalize.Invoke(null, new object[] { file, root });
            ctx.AssertEqual(expectedChange, normalized != null, "Only supported repeated player class rows are normalized");
            var checkFile = normalized == null ? file : (CResGFF)normalized.GetType().GetProperty("File").GetValue(normalized);
            var checkRoot = normalized == null ? root : (CResStruct)normalized.GetType().GetProperty("Root").GetValue(normalized);
            ctx.AssertEqual((uint)classes.Length, file.GetListCount(list), "The input resource is not changed");
            ctx.AssertEqual(3u, checkFile.GetFieldCount(checkRoot), "The class list replaces its field without adding a duplicate label");
            fixed (byte* label = Encoding.ASCII.GetBytes("ClassList\0"))
                ctx.Assert(checkFile.GetList(list, checkRoot, label) != 0, "Read normalized class rows");
            ctx.AssertEqual(expectedChange ? (uint)classes.Distinct().Count() : (uint)classes.Length,
                checkFile.GetListCount(list), "Only repeated class references are removed");
            var total = 0;
            for (uint index = 0; index < checkFile.GetListCount(list); index++)
            {
                using var row = new CResStruct();
                checkFile.GetListElement(row, list, index);
                var found = 0;
                fixed (byte* label = Encoding.ASCII.GetBytes("ClassLevel\0"))
                    total += checkFile.ReadFieldSHORT(row, label, &found, 0);
            }
            var expectedTotal = 0;
            foreach (var level in levels) expectedTotal += level;
            ctx.AssertEqual(expectedTotal, total, "Every native level is retained");
            var present = 0;
            fixed (byte* label = Encoding.ASCII.GetBytes("AuditSentinel\0"))
                ctx.AssertEqual(4567, checkFile.ReadFieldINT(checkRoot, label, &present, 0), "Unrelated saved fields survive");
            using var repeated = (IDisposable)normalize.Invoke(null, new object[] { checkFile, checkRoot });
            ctx.Assert(repeated == null, "Normalization is idempotent");
        }
    }
}
