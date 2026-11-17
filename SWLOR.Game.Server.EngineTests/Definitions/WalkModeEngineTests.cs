using System.Text;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class WalkModeEngineTests
    {
        [EngineTest("Uncontrolled NPCs ignore and clear stale possessed walk state", Category = "WalkMode", TimeoutSeconds = 30f)]
        public static async Task UncontrolledNpcClearsWalkState(EngineTestContext ctx)
        {
            var creature = ctx.SpawnCreature("civilian");
            await ctx.DelaySecondsAsync(1f);
            var baseline = CreatureStat.GetMovementSpeedMultiplier(creature);
            SetLocalInt(creature, "WALK_TOGGLE", 1);
            ctx.Assert(!CreatureStat.IsWalkModeEnabled(creature), "uncontrolled NPCs cannot retain player walk mode");
            CreatureStat.ApplyCreatureMovementRate(creature);
            ctx.AssertEqual(0, GetLocalInt(creature, "WALK_TOGGLE"), "stale walk marker clears on refresh");
            ctx.Assert(Math.Abs(baseline - CreaturePlugin.GetMovementRateFactor(creature)) < 0.001f,
                "NPC movement refresh retains its normal movement factor");
        }

        [EngineTest("Player exports reset temporary walk state on reconnect", Category = "WalkMode", TimeoutSeconds = 30f)]
        public static async Task PlayerExportsResetTemporaryWalkState(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var command = new CharacterAppearanceChatCommand().BuildChatCommands()["walk"];
            try
            {
                for (var cycle = 0; cycle < 2; cycle++)
                {
                    foreach (var walking in new[] { true, false })
                    {
                        if (CreatureStat.IsWalkModeEnabled(player) != walking)
                            await ctx.ExecuteInCreatureContextAsync(player, () => command.DoAction(player, OBJECT_INVALID, null));
                        var savedState = ExportAndReloadWalkState(ctx, player);
                        PlayerPlugin.SetAlwaysWalk(player, false);
                        SetLocalInt(player, "WALK_TOGGLE", savedState);
                        CreatureStat.ApplyCreatureMovementRate(player);
                        ctx.Assert(!CreatureStat.IsWalkModeEnabled(player), "temporary walk state does not survive a player export");
                        ctx.Assert(Math.Abs(1.25f - CreaturePlugin.GetMovementRateFactor(player)) < 0.001f,
                            "entry refresh retains normal movement when the new session is not forced to walk");
                    }
                }
            }
            finally
            {
                if (CreatureStat.IsWalkModeEnabled(player))
                    CreatureStat.ToggleWalkMode(player);
            }
        }

        private static unsafe int ExportAndReloadWalkState(EngineTestContext ctx, uint player)
        {
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(player).AsNWSCreature();
            using var file = new CResGFF();
            using var root = new CResStruct();
            using var fileType = new CExoString("BIC ");
            using var format = new CExoString("V2.0");
            ctx.Assert(file.CreateGFFFile(root, fileType, format) != 0, "create character export");
            ctx.Assert(native.SaveCreature(file, root, 0, 0, 1, 0) != 0, "save walk state through native character export");
            fixed (byte* label = Encoding.ASCII.GetBytes("VarTable\0"))
                ctx.AssertEqual(uint.MaxValue, file.GetFieldByLabel(root, label), "player exports omit ordinary local variables");
            using var restored = new CNWSCreature(OBJECT_INVALID, 1, 1);
            ctx.AssertEqual(0u, restored.m_pStats.ReadStatsFromGff(file, root, restored.m_cAppearance, 0, 1, 0, 1),
                "reload through the native player character stats path");
            using var stateName = new CExoString("WALK_TOGGLE");
            var savedState = restored.m_ScriptVars.GetInt(stateName);
            ctx.AssertEqual(0, savedState, "native player reload does not restore the temporary walk marker");
            return savedState;
        }

        [EngineTest("Walk caps speed while detect mode retains its movement bonus", Category = "WalkMode", TimeoutSeconds = 30f)]
        public static async Task WalkCapsSpeedAndRestoresDetectMovement(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var commands = new CharacterAppearanceChatCommand().BuildChatCommands();
            var command = commands["walk"];
            ctx.Assert(ReferenceEquals(command, commands["alwayswalk"]), "both walking aliases share the toggle");

            try
            {
                SetActionMode(player, ActionMode.Detect, true);
                CreatureStat.ApplyCreatureMovementRate(player);
                AssertSpeed(1.25f, "detect mode keeps the default movement bonus");

                for (var cycle = 0; cycle < 2; cycle++)
                {
                    await ToggleAsync();
                    ctx.Assert(CreatureStat.IsWalkModeEnabled(player), "walk enabled");
                    ctx.Assert(IsNativeForcedWalkEnabled(player), "walk enables the native forced-walk flag");
                    AssertSpeed(1.0f, "walk uses normal walking speed");
                    ctx.Assert(GetActionMode(player, ActionMode.Detect), "walk does not disable detect mode");

                    CreatureStat.ApplyCreatureMovementRate(player);
                    AssertSpeed(1.0f, "stat refresh preserves normal walking speed");
                    await ToggleAsync();
                    ctx.Assert(!CreatureStat.IsWalkModeEnabled(player), "walk disabled");
                    ctx.Assert(!IsNativeForcedWalkEnabled(player), "disabling walk clears the native forced-walk flag");
                    AssertSpeed(1.25f, "disabling walk immediately restores the movement bonus");
                }

                SetActionMode(player, ActionMode.Detect, false);
                await ToggleAsync();
                AssertSpeed(1.0f, "normal walking also works outside detect mode");

                TemporaryStatModifier.Add(player, StatType.MovementSpeedPercentAdjustment, 50, 30f);
                CreatureStat.ApplyCreatureMovementRate(player);
                AssertSpeed(1.0f, "movement buffs cannot exceed the walk cap");
                await ToggleAsync();
                AssertSpeed(1.5f, "disabling walk restores the buffed movement cap");
                TemporaryStatModifier.Clear(player);

                foreach (var slow in new[] { 1, 10, 20, 25 })
                {
                    TemporaryStatModifier.Add(player, StatType.MovementSpeedPercentAdjustment, -slow, 30f);
                    await ToggleAsync();
                    AssertSpeed(1.0f - slow * 0.01f, $"walking preserves a {slow}% slow");
                    CreatureStat.ApplyCreatureMovementRate(player);
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
                CreatureStat.ApplyCreatureMovementRate(player);
                var disabledMovementFactor = CreaturePlugin.GetMovementRateFactor(player);
                await ToggleAsync();
                AssertSpeed(0f, "walking preserves disabled movement", disabledMovementFactor);
                await ToggleAsync();
                AssertSpeed(0f, "disabling walk preserves disabled movement", disabledMovementFactor);
            }
            finally
            {
                if (CreatureStat.IsWalkModeEnabled(player))
                    CreatureStat.ToggleWalkMode(player);
                SetActionMode(player, ActionMode.Detect, false);
                TemporaryStatModifier.Clear(player);
                CreatureStat.ApplyCreatureMovementRate(player);
            }

            Task ToggleAsync() => ctx.ExecuteInCreatureContextAsync(player, () =>
                command.DoAction(player, OBJECT_INVALID, null));

            void AssertSpeed(float expected, string label, float? expectedNative = null)
            {
                var calculated = CreatureStat.GetMovementSpeedMultiplier(player);
                var applied = CreaturePlugin.GetMovementRateFactor(player);
                ctx.Assert(Math.Abs(expected - calculated) < 0.001f,
                    $"{label}: calculated multiplier expected {expected}, got {calculated}");
                var nativeExpected = expectedNative ?? expected;
                ctx.Assert(Math.Abs(nativeExpected - applied) < 0.001f,
                    $"{label}: native movement factor expected {nativeExpected}, got {applied}");
            }
        }

        private static bool IsNativeForcedWalkEnabled(uint creature) =>
            NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature).AsNWSCreature().m_bForcedWalk != 0;
    }
}
