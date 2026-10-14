using System.Threading.Tasks;
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
                    ctx.AssertEqual(1, GetLocalInt(player, Stat.WalkToggleVariable), "walk enabled");
                    AssertSpeed(1.0f, "walk uses normal walking speed");
                    ctx.Assert(GetActionMode(player, ActionMode.Detect), "walk does not disable detect mode");

                    Stat.ApplyCreatureMovementRate(player);
                    AssertSpeed(1.0f, "stat refresh preserves normal walking speed");
                    await ToggleAsync();
                    ctx.AssertEqual(0, GetLocalInt(player, Stat.WalkToggleVariable), "walk disabled");
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

                TemporaryStatModifier.Add(player, StatType.MovementSpeedPercentAdjustment, -50, 30f);
                await ToggleAsync();
                AssertSpeed(0.75f, "walking does not raise a slower movement rate");
                await ToggleAsync();
                AssertSpeed(0.75f, "disabling walk does not clear a movement penalty");

                TemporaryStatModifier.Add(player, StatType.MovementSpeedDisabled, 1, 30f);
                await ToggleAsync();
                AssertSpeed(0f, "walking cannot bypass disabled movement");
                await ToggleAsync();
                AssertSpeed(0f, "disabling walk cannot bypass disabled movement");
            }
            finally
            {
                PlayerPlugin.SetAlwaysWalk(player, false);
                DeleteLocalInt(player, Stat.WalkToggleVariable);
                SetActionMode(player, ActionMode.Detect, false);
                TemporaryStatModifier.Clear(player);
                Stat.ApplyCreatureMovementRate(player);
            }

            Task ToggleAsync() => ctx.ExecuteInCreatureContextAsync(player, () =>
                command.DoAction(player, OBJECT_INVALID, null));

            void AssertSpeed(float expected, string label)
            {
                var calculated = Stat.GetMovementSpeedMultiplier(player);
                var applied = CreaturePlugin.GetMovementRateFactor(player);
                ctx.Assert(Math.Abs(expected - calculated) < 0.001f,
                    $"{label}: calculated multiplier expected {expected}, got {calculated}");
                ctx.Assert(Math.Abs(expected - applied) < 0.001f,
                    $"{label}: native movement factor expected {expected}, got {applied}");
            }
        }
    }
}
