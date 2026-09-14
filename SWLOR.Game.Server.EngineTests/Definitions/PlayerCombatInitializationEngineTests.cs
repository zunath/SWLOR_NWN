using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using ClassType = SWLOR.NWN.API.NWScript.Enum.ClassType;
using RacialType = SWLOR.NWN.API.NWScript.Enum.RacialType;
using SavingThrow = SWLOR.NWN.API.NWScript.Enum.SavingThrow;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class PlayerCombatInitializationEngineTests
    {
        [EngineTest("Standard character combat baseline survives initialization and reconnect", Category = "PlayerInitialization")]
        public static Task StandardCombatStats(EngineTestContext ctx) => Verify(ctx, ClassType.Standard);

        [EngineTest("Force character combat baseline survives initialization and reconnect", Category = "PlayerInitialization")]
        public static Task ForceCombatStats(EngineTestContext ctx) => Verify(ctx, ClassType.ForceSensitive);

        private static async Task Verify(EngineTestContext ctx, ClassType characterClass)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () => VerifyNative(ctx, owner, characterClass));
        }

        private static unsafe void VerifyNative(EngineTestContext ctx, uint owner, ClassType characterClass)
        {
            CreaturePlugin.SetClassByPosition(owner, 0, characterClass);
            CreaturePlugin.SetRacialType(owner, RacialType.Human);
            foreach (var feat in new[] { FeatType.GreatFortitude, FeatType.LightningReflexes, FeatType.IronWill })
                CreaturePlugin.AddFeat(owner, feat);
            var player = new Player(GetObjectUUID(owner));
            typeof(PlayerInitialization).GetMethod("InitializeNewCharacter", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { owner, player });
            AssertBaseline(ctx, owner, "Initial character setup");

            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(owner).AsNWSCreature();
            using var file = new CResGFF();
            using var root = new CResStruct();
            using var fileType = new CExoString("BIC ");
            using var format = new CExoString("V2.0");
            ctx.Assert(file.CreateGFFFile(root, fileType, format) != 0, "Create a real character export");
            ctx.Assert(native.SaveCreature(file, root, 0, 0, 1, 0) != 0, "Export initialized character");
            fixed (byte* label = Encoding.ASCII.GetBytes("IsPC\0")) file.WriteFieldBYTE(root, 1, label);

            using var restored = new CNWSCreature(OBJECT_INVALID, 1, 1);
            ctx.Assert(restored.LoadCreature(file, root, 0, 0, 0, 0) != 0, "Reload with native PC semantics");
            ctx.Assert(restored.m_pStats.GetBaseAttackBonus(0) > 1, "PC loading reconstructs class BAB instead of keeping the initialization override");
            PlayerInitialization.InitializeNativeCombatStats(restored.m_idSelf);
            AssertBaseline(ctx, restored.m_idSelf, "Returning character login");
            PlayerInitialization.InitializeNativeCombatStats(restored.m_idSelf);
            AssertBaseline(ctx, restored.m_idSelf, "Repeated login baseline application");
        }

        private static void AssertBaseline(EngineTestContext ctx, uint player, string stage)
        {
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(player).AsNWSCreature();
            ctx.AssertEqual(40, GetHitDice(player), stage + " retains native level 40");
            ctx.AssertEqual(1, (int)native.m_pStats.GetBaseAttackBonus(0), stage + " has the intended attack baseline");
            foreach (var save in new[] { SavingThrow.Fortitude, SavingThrow.Reflex, SavingThrow.Will })
                ctx.AssertEqual(0, CreaturePlugin.GetBaseSavingThrow(player, (int)save), stage + " has a neutral " + save + " baseline");
        }
    }
}
