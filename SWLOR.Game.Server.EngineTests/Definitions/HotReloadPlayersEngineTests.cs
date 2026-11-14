using System.Threading.Tasks;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// A player who stays online across hot reloads (MaintainabilityRefactorHotReload.md, P4-15). The scenario spans generations of
    /// the build, so it keeps its state on the module and runs in phases, one per generation (the runner reruns after a reload,
    /// <see cref="EngineTest.ScheduleEngineTests"/>):
    /// <list type="number">
    /// <item>The first run makes a headless player (<see cref="PlayerAbilityFixture"/>) in a kept arena, takes it below full HP,
    /// gives it a tagged native effect, a long status effect, a cooldown saved in Redis and two locals a login or logout clears,
    /// writes what it expects on the module and leaves the player online.</item>
    /// <item>Each run in a later generation (after a reload) checks the same creature: still a player, the same HP, effect, status
    /// effect (the same instance id, carried), cooldown and position, both locals still set (no login or logout handler ran on
    /// it), and its window view models rebuilt by the restore step.</item>
    /// <item>The run after the second reload also releases the player and the arena.</item>
    /// </list>
    /// Reload is off in the normal engine sweep, so there the scenario skips; it runs with <c>scripts/run-hot-reload-check.sh
    /// --engine-filter HotReloadPlayers --reloads 2</c>, which stages the engine tests in every generation and fails unless every
    /// generation's run passes.
    /// </summary>
    public static class HotReloadPlayersEngineTests
    {
        private const string Prefix = "SWLOR_HRP_";
        private const string EffectTag = Prefix + "EFFECT";
        private const int ReloadsToVerify = 2;
        private const float StatusSeconds = 3600f;
        private const float CooldownSeconds = 3600f;
        private const RecastGroup Cooldown = RecastGroup.MedKit;

        // One per loaded build: a run whose token differs from the module's is the first run of a new generation.
        private static readonly string GenerationToken = Guid.NewGuid().ToString("N");

        [EngineTest("A player online across hot reloads keeps HP, effects, status effects, cooldowns and position", Category = "HotReloadPlayers",
            TimeoutSeconds = 120f)]
        public static async Task PlayerStaysOnlineAcrossReloads(EngineTestContext ctx)
        {
            // The host's switch (SWLOR.Game.Host HostSettings): without it no reload comes, and a player left online would only
            // outlive the run into the server's shutdown.
            if (Environment.GetEnvironmentVariable("SWLOR_HOT_RELOAD")?.Trim().ToLowerInvariant() is not ("true" or "1" or "yes"))
                ctx.Skip("Hot reload is off; run scripts/run-hot-reload-check.sh --engine-filter HotReloadPlayers --reloads 2.");

            var module = GetModule();
            if (GetLocalInt(module, Prefix + "PREPARED") == 0)
            {
                await PrepareAsync(ctx);
                ctx.SetResultDetail($"prepared {GetLocalObject(module, Prefix + "CREATURE")}: reload the server {ReloadsToVerify} times to verify");
                return;
            }

            if (GetLocalString(module, Prefix + "TOKEN") == GenerationToken)
                ctx.Skip("No reload since the last phase of this scenario ran.");

            var verified = Verify(ctx) + 1;
            SetLocalString(module, Prefix + "TOKEN", GenerationToken);
            SetLocalInt(module, Prefix + "VERIFIED", verified);
            ctx.SetResultDetail($"verified after reload {verified} of {ReloadsToVerify}");
            if (verified >= ReloadsToVerify)
                Release(ctx);

            await ctx.WaitFrameAsync();
        }

        private static async Task PrepareAsync(EngineTestContext ctx)
        {
            var module = GetModule();
            var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var creature = fixture.Creature;
            ctx.Untrack(creature);
            fixture.KeepAcrossReloads();
            SetLocalInt(module, EngineTest.KeepArenaVariable, 1);

            ApplyEffectToObject(DurationType.Instant, EffectDamage(17), creature);
            ApplyEffectToObject(DurationType.Permanent, TagEffect(SupernaturalEffect(EffectACIncrease(2)), EffectTag), creature);
            ctx.Assert(StatusEffect.ApplyStatusEffect(creature, creature, typeof(HobbleStatusEffect), StatusSeconds), "Hobble applies");
            Recast.ApplyRecastDelay(creature, Cooldown, CooldownSeconds);
            SetLocalBool(creature, "IS_BUSY", true);
            SetLocalInt(creature, "STEALTH_GRANTED_WINDOW", 1);
            await ctx.WaitFrameAsync();

            var status = StatusEffect.GetCreatureStatusEffects(creature).GetEffect<HobbleStatusEffect>();
            ctx.Assert(status != null, "the status effect is tracked");
            ctx.Assert(Recast.IsOnRecastDelay(creature, Cooldown).Item1, "the cooldown runs");
            var position = GetPosition(creature);

            SetLocalObject(module, Prefix + "CREATURE", creature);
            SetLocalInt(module, Prefix + "CLIENT", (int)fixture.ClientId);
            SetLocalInt(module, Prefix + "WAS_PLAYER", fixture.WasPlayer);
            SetLocalInt(module, Prefix + "HP", GetCurrentHitPoints(creature));
            SetLocalString(module, Prefix + "STATUS_ID", status!.Id);
            SetLocalFloat(module, Prefix + "X", position.X);
            SetLocalFloat(module, Prefix + "Y", position.Y);
            SetLocalFloat(module, Prefix + "Z", position.Z);
            SetLocalString(module, Prefix + "TOKEN", GenerationToken);
            SetLocalInt(module, Prefix + "VERIFIED", 0);
            SetLocalInt(module, Prefix + "PREPARED", 1);
        }

        /// <summary>Checks the player the first phase left online; returns how many reloads were verified before this one.</summary>
        private static int Verify(EngineTestContext ctx)
        {
            var module = GetModule();
            var creature = GetLocalObject(module, Prefix + "CREATURE");
            ctx.Assert(GetIsObjectValid(creature), "the player is still in the world");
            ctx.Assert(GetIsPC(creature), "the player is still connected");

            ctx.AssertEqual(GetLocalInt(module, Prefix + "HP"), GetCurrentHitPoints(creature), "current HP");
            ctx.Assert(HasTaggedEffect(creature), "the native effect stays on the player");

            var status = StatusEffect.GetCreatureStatusEffects(creature).GetEffect<HobbleStatusEffect>();
            ctx.Assert(status != null, "the status effect is carried into the new build");
            ctx.AssertEqual(GetLocalString(module, Prefix + "STATUS_ID"), status!.Id, "status effect id (its native effect's tag)");
            ctx.Assert(status.GetRemainingDurationSeconds(DateTime.UtcNow) > 0f, "the status effect has time left");

            ctx.Assert(Recast.IsOnRecastDelay(creature, Cooldown).Item1, "the cooldown saved in Redis still runs");

            var expected = new System.Numerics.Vector3(GetLocalFloat(module, Prefix + "X"), GetLocalFloat(module, Prefix + "Y"), GetLocalFloat(module, Prefix + "Z"));
            ctx.Assert(System.Numerics.Vector3.Distance(expected, GetPosition(creature)) < 0.05f, $"the player stays at {expected}, is at {GetPosition(creature)}");

            ctx.Assert(GetLocalBool(creature, "IS_BUSY"), "no login handler cleared the busy flag (ActivityService.WipeStatusOnEntry)");
            ctx.AssertEqual(1, GetLocalInt(creature, "STEALTH_GRANTED_WINDOW"),
                "granted-stealth window (cleared by StealthService's login and logout handlers)");

            ctx.Assert(Gui.GetPlayerWindow(creature, GuiWindowType.PlayerStatus) != null,
                "the restore step rebuilt the player's window view models (GuiService.CreatePlayerWindowsFor)");
            return GetLocalInt(module, Prefix + "VERIFIED");
        }

        private static bool HasTaggedEffect(uint creature)
        {
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                if (GetEffectTag(effect) == EffectTag)
                    return true;
            }

            return false;
        }

        /// <summary>The last phase: takes the player offline and lets the runner destroy the arena again.</summary>
        private static void Release(EngineTestContext ctx)
        {
            var module = GetModule();
            var creature = GetLocalObject(module, Prefix + "CREATURE");
            using (PlayerAbilityFixture.Reattach(creature, (uint)GetLocalInt(module, Prefix + "CLIENT"), GetLocalInt(module, Prefix + "WAS_PLAYER")))
            {
            }

            ctx.Track(creature);
            foreach (var name in new[] { "CREATURE" })
                DeleteLocalObject(module, Prefix + name);
            foreach (var name in new[] { "CLIENT", "WAS_PLAYER", "HP", "VERIFIED", "PREPARED" })
                DeleteLocalInt(module, Prefix + name);
            foreach (var name in new[] { "STATUS_ID", "TOKEN" })
                DeleteLocalString(module, Prefix + name);
            foreach (var name in new[] { "X", "Y", "Z" })
                DeleteLocalFloat(module, Prefix + name);
            DeleteLocalInt(module, EngineTest.KeepArenaVariable);
        }
    }
}
