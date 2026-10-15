using System.Text;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class WalkModeEngineTests
    {
        [EngineTest("Walk state survives character export, reload and session restoration", Category = "WalkMode", TimeoutSeconds = 30f)]
        public static async Task WalkStateSurvivesReconnect(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var command = new CharacterChatCommand().BuildChatCommands()["walk"];
            try
            {
                for (var cycle = 0; cycle < 2; cycle++)
                {
                    foreach (var walking in new[] { true, false })
                    {
                        await ctx.ExecuteInCreatureContextAsync(player, () => command.DoAction(player, OBJECT_INVALID, null));
                        var savedState = SaveAndReloadWalkState(ctx, player);
                        PlayerPlugin.SetAlwaysWalk(player, false);
                        SetLocalInt(player, "WALK_TOGGLE", savedState);
                        Stat.RestoreWalkMode(player);
                        ctx.AssertEqual(walking, Stat.IsWalkModeEnabled(player), "reloaded walk state");
                        var expected = walking ? 1f : 1.25f;
                        ctx.Assert(Math.Abs(expected - CreaturePlugin.GetMovementRateFactor(player)) < 0.001f,
                            "session restoration applies the saved state's movement factor");
                    }
                }
            }
            finally
            {
                if (Stat.IsWalkModeEnabled(player))
                    Stat.ToggleWalkMode(player);
            }
        }

        private static unsafe int SaveAndReloadWalkState(EngineTestContext ctx, uint player)
        {
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(player).AsNWSCreature();
            using var file = new CResGFF();
            using var root = new CResStruct();
            using var fileType = new CExoString("BIC ");
            using var format = new CExoString("V2.0");
            ctx.Assert(file.CreateGFFFile(root, fileType, format) != 0, "create character export");
            ctx.Assert(native.SaveCreature(file, root, 0, 0, 1, 0) != 0, "save walk state through native character export");
            fixed (byte* label = Encoding.ASCII.GetBytes("IsPC\0")) file.WriteFieldBYTE(root, 1, label);
            using var restored = new CNWSCreature(OBJECT_INVALID, 1, 1);
            ctx.Assert(restored.LoadCreature(file, root, 0, 0, 0, 0) != 0, "reload native player character");
            using var stateName = new CExoString("WALK_TOGGLE");
            var savedState = restored.m_ScriptVars.GetInt(stateName);
            ctx.AssertEqual(GetLocalInt(player, "WALK_TOGGLE"), savedState, "character file retains walk state");
            return savedState;
        }

        [EngineTest("Walk caps speed while detect mode retains its movement bonus", Category = "WalkMode", TimeoutSeconds = 30f)]
        public static async Task WalkCapsSpeedAndRestoresDetectMovement(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var commands = new CharacterChatCommand().BuildChatCommands();
            var command = commands["walk"];
            ctx.Assert(ReferenceEquals(command, commands["alwayswalk"]), "both walking aliases share the toggle");

            try
            {
                SetActionMode(player, ActionMode.Detect, true);
                Stat.ApplyCreatureMovementRate(player);
                AssertSpeed(1.25f, "detect mode keeps the default movement bonus");

                for (var cycle = 0; cycle < 2; cycle++)
                {
                    await ToggleAsync();
                    ctx.Assert(Stat.IsWalkModeEnabled(player), "walk enabled");
                    AssertSpeed(1.0f, "walk uses normal walking speed");
                    ctx.Assert(GetActionMode(player, ActionMode.Detect), "walk does not disable detect mode");

                    Stat.ApplyCreatureMovementRate(player);
                    AssertSpeed(1.0f, "stat refresh preserves normal walking speed");
                    await ToggleAsync();
                    ctx.Assert(!Stat.IsWalkModeEnabled(player), "walk disabled");
                    AssertSpeed(1.25f, "disabling walk immediately restores the movement bonus");
                }

                SetActionMode(player, ActionMode.Detect, false);
                await ToggleAsync();
                AssertSpeed(1.0f, "normal walking also works outside detect mode");

                TemporaryStatModifier.Add(player, StatType.MovementSpeedPercentAdjustment, 50, 30f);
                Stat.ApplyCreatureMovementRate(player);
                AssertSpeed(1.0f, "movement buffs cannot exceed the walk cap");
                await ToggleAsync();
                AssertSpeed(1.5f, "disabling walk restores the buffed movement cap");
                TemporaryStatModifier.Clear(player);

                foreach (var slow in new[] { 1, 10, 20, 25 })
                {
                    TemporaryStatModifier.Add(player, StatType.MovementSpeedPercentAdjustment, -slow, 30f);
                    await ToggleAsync();
                    AssertSpeed(1.0f - slow * 0.01f, $"walking preserves a {slow}% slow");
                    Stat.ApplyCreatureMovementRate(player);
                    AssertSpeed(1.0f - slow * 0.01f, $"refresh preserves a {slow}% walking slow");
                    await ToggleAsync();
                    AssertSpeed(1.25f - slow * 0.01f, $"disabling walk preserves a {slow}% slow");
                    TemporaryStatModifier.Clear(player);
                }

                TemporaryStatModifier.Add(player, StatType.MovementSpeedPercentAdjustment, -50, 30f);
                await ToggleAsync();
                AssertSpeed(0.5f, "walking applies stronger slows to its normal baseline");
                await ToggleAsync();
                AssertSpeed(0.75f, "disabling walk does not clear a movement penalty");

                TemporaryStatModifier.Add(player, StatType.MovementSpeedDisabled, 1, 30f);
                Stat.ApplyCreatureMovementRate(player);
                var disabledMovementFactor = CreaturePlugin.GetMovementRateFactor(player);
                await ToggleAsync();
                AssertSpeed(0f, "walking preserves disabled movement", disabledMovementFactor);
                await ToggleAsync();
                AssertSpeed(0f, "disabling walk preserves disabled movement", disabledMovementFactor);
            }
            finally
            {
                if (Stat.IsWalkModeEnabled(player))
                    Stat.ToggleWalkMode(player);
                SetActionMode(player, ActionMode.Detect, false);
                TemporaryStatModifier.Clear(player);
                Stat.ApplyCreatureMovementRate(player);
            }

            Task ToggleAsync() => ctx.ExecuteInCreatureContextAsync(player, () =>
                command.DoAction(player, OBJECT_INVALID, null));

            void AssertSpeed(float expected, string label, float? expectedNative = null)
            {
                var calculated = Stat.GetMovementSpeedMultiplier(player);
                var applied = CreaturePlugin.GetMovementRateFactor(player);
                ctx.Assert(Math.Abs(expected - calculated) < 0.001f,
                    $"{label}: calculated multiplier expected {expected}, got {calculated}");
                var nativeExpected = expectedNative ?? expected;
                ctx.Assert(Math.Abs(nativeExpected - applied) < 0.001f,
                    $"{label}: native movement factor expected {nativeExpected}, got {applied}");
            }
        }
    }
}
