using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// A player who stays online across hot reloads (MaintainabilityRefactorHotReload.md). The scenario spans generations of
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
    /// A second headless player names the first one and the first goes AFK, with a stale name override left on it, so each later run
    /// also checks what the name override service carries: the player is still AFK, the overrides were applied again (the stale one is
    /// gone and the AFK tag is on the name), and a tell between the two named players still goes through the tell handler. The run
    /// after the last reload moves the player, so the AFK movement check the reload had to start again clears the flag.
    /// Reload is off in the normal engine sweep, so there the scenario skips; it runs with <c>scripts/run-hot-reload-check.sh
    /// --engine-filter HotReloadPlayers --reloads 2</c>, which stages the engine tests in every generation and fails unless every
    /// generation's run passes.
    /// </summary>
    public static class HotReloadPlayersEngineTests
    {
        private const string Prefix = "SWLOR_HRP_";
        private const string EffectTag = Prefix + "EFFECT";
        private const string NamedAs = "Vessa Tal";
        private static readonly string AfkSuffix = " " + ColorToken.Purple("[AFK]");
        private const string LoggedOutKey = Prefix + "LOGGED_OUT_PLAYER";
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
            await VerifyTellAsync(ctx);
            ctx.SetResultDetail($"verified after reload {verified} of {ReloadsToVerify}");
            if (verified >= ReloadsToVerify)
            {
                await VerifyMovementClearsAfkAsync(ctx);
                Release(ctx);
            }

            await ctx.WaitFrameAsync();
        }

        private static async Task PrepareAsync(EngineTestContext ctx)
        {
            var module = GetModule();
            var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var creature = fixture.Creature;
            ctx.Untrack(creature);
            fixture.KeepAcrossReloads();
            var observerFixture = await PlayerAbilityFixture.CreateAsync(ctx, 3f);
            var observer = observerFixture.Creature;
            ctx.Untrack(observer);
            observerFixture.KeepAcrossReloads();
            SetLocalInt(module, EngineTest.KeepArenaVariable, 1);

            var statusEffects = StatusEffect;
            ApplyEffectToObject(DurationType.Instant, EffectDamage(17), creature);
            ApplyEffectToObject(DurationType.Permanent, TagEffect(SupernaturalEffect(EffectACIncrease(2)), EffectTag), creature);
            ctx.Assert(StatusEffectApplication.ApplyStatusEffect(creature, creature, typeof(HobbleStatusEffect), StatusSeconds), "Hobble applies");
            Recast.ApplyRecastDelay(creature, Cooldown, CooldownSeconds);
            HoldLoggedOutPlayer(module, statusEffects);
            SetLocalBool(creature, "IS_BUSY", true);
            SetLocalInt(creature, "STEALTH_GRANTED_WINDOW", 1);
            await ctx.WaitFrameAsync();

            var status = statusEffects.GetCreatureStatusEffects(creature).GetEffect<HobbleStatusEffect>();
            ctx.Assert(status != null, "the status effect is tracked");
            ctx.Assert(Recast.IsOnRecastDelay(creature, Cooldown).Item1, "the cooldown runs");
            var position = GetPosition(creature);

            GameServices.PlayerKnownName.SetKnownName(observer, creature, NamedAs);
            ctx.Assert(PlayerNameOverride.ToggleAfk(creature), "the player goes AFK");
            var expectedOverride = PlayerName.GetDisplayName(observer, creature) + AfkSuffix;
            ctx.AssertEqual(expectedOverride, RenamePlugin.GetPCNameOverride(creature, observer), "the observer sees the known name with the AFK tag");
            RenamePlugin.SetPCNameOverride(creature, "Stale name", string.Empty, string.Empty, PlayerNameOverrideType.Default, observer);
            SetLocalString(module, Prefix + "EXPECTED_OVERRIDE", expectedOverride);
            SetLocalObject(module, Prefix + "OBSERVER", observer);
            SetLocalInt(module, Prefix + "OBSERVER_CLIENT", (int)observerFixture.ClientId);
            SetLocalInt(module, Prefix + "OBSERVER_WAS_PLAYER", observerFixture.WasPlayer);

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

            var statusEffects = StatusEffect;
            var status = statusEffects.GetCreatureStatusEffects(creature).GetEffect<HobbleStatusEffect>();
            ctx.Assert(status != null, "the status effect is carried into the new build");
            ctx.AssertEqual(GetLocalString(module, Prefix + "STATUS_ID"), status!.Id, "status effect id (its native effect's tag)");
            ctx.Assert(status.GetRemainingDurationSeconds(DateTime.UtcNow) > 0f, "the status effect has time left");

            ctx.Assert(Recast.IsOnRecastDelay(creature, Cooldown).Item1, "the cooldown saved in Redis still runs");
            VerifyLoggedOutPlayer(ctx, module, statusEffects);

            var expected = new System.Numerics.Vector3(GetLocalFloat(module, Prefix + "X"), GetLocalFloat(module, Prefix + "Y"), GetLocalFloat(module, Prefix + "Z"));
            ctx.Assert(System.Numerics.Vector3.Distance(expected, GetPosition(creature)) < 0.05f, $"the player stays at {expected}, is at {GetPosition(creature)}");

            ctx.Assert(GetLocalBool(creature, "IS_BUSY"), "no login handler cleared the busy flag (ActivityService.WipeStatusOnEntry)");
            ctx.AssertEqual(1, GetLocalInt(creature, "STEALTH_GRANTED_WINDOW"),
                "granted-stealth window (cleared by StealthService's login and logout handlers)");

            ctx.Assert(Gui.GetPlayerWindow(creature, GuiWindowType.PlayerStatus) != null,
                "the restore step rebuilt the player's window view models (GuiService.CreatePlayerWindowsFor)");

            var observer = GetLocalObject(module, Prefix + "OBSERVER");
            ctx.Assert(PlayerNameOverride.IsAfk(creature), "the AFK flag is carried into the new build");
            ctx.AssertEqual(GetLocalString(module, Prefix + "EXPECTED_OVERRIDE"), RenamePlugin.GetPCNameOverride(creature, observer),
                "the overrides were applied again (the stale name is gone) and the name carries the AFK tag");
            return GetLocalInt(module, Prefix + "VERIFIED");
        }

        /// <summary>
        /// A tell from the observer to the player they named goes through the tell handler of this build: while the chat message is
        /// delivered, the observer sees the player's chat name (the known name alone), not the name with the descriptor beside it.
        /// </summary>
        private static async Task VerifyTellAsync(EngineTestContext ctx)
        {
            var module = GetModule();
            var creature = GetLocalObject(module, Prefix + "CREATURE");
            var observer = GetLocalObject(module, Prefix + "OBSERVER");
            var restored = RenamePlugin.GetPCNameOverride(creature, observer);
            var duringTell = string.Empty;
            await ctx.ExecuteInCreatureContextAsync(observer, () =>
            {
                ChatPlugin.SendMessage(SWLOR.Game.Server.Core.NWNX.Enum.ChatChannel.PlayerTell, "Reload check", observer, creature);
                duringTell = RenamePlugin.GetPCNameOverride(creature, observer);
            });
            ctx.AssertEqual(NamedAs + AfkSuffix, duringTell, "the tell handler put the chat name on the player the observer named");
            await ctx.WaitUntilAsync(() => RenamePlugin.GetPCNameOverride(creature, observer) == restored, 3f, "the tell handler to restore the name again");
        }

        /// <summary>The last phase: moving the player ends AFK, which needs the movement check the reload started again.</summary>
        private static async Task VerifyMovementClearsAfkAsync(EngineTestContext ctx)
        {
            var module = GetModule();
            var creature = GetLocalObject(module, Prefix + "CREATURE");
            var observer = GetLocalObject(module, Prefix + "OBSERVER");
            ObjectPlugin.SetPosition(creature, GetPosition(creature) + new System.Numerics.Vector3(1f, 0f, 0f));
            await ctx.WaitUntilAsync(() => !PlayerNameOverride.IsAfk(creature), 4f, "the movement check of the new build to clear AFK");
            ctx.AssertEqual(PlayerName.GetDisplayName(observer, creature), RenamePlugin.GetPCNameOverride(creature, observer),
                "the name has no AFK tag after moving");
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

        // The table of the effects logged-out players hold: StatusEffectSessionService's since the status effect split (found through the holders the status
        // effect tracker registered), StatusEffectService's before.
        private static IDictionary LoggedOutTable(object statusEffects)
        {
            const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
            var owner = statusEffects.GetType().GetField("_loggedOutSources", Private)?.GetValue(statusEffects) is IEnumerable sources
                ? System.Linq.Enumerable.Single(System.Linq.Enumerable.Cast<object>(sources))
                : statusEffects;
            return (IDictionary)owner.GetType().GetField("_loggedOutPlayerEffects", Private)!.GetValue(owner)!;
        }

        /// <summary>Puts a logged-out player's held effect in the table, as OnPlayerExit would, so the reload has one to carry.</summary>
        private static void HoldLoggedOutPlayer(uint module, object statusEffects)
        {
            var held = new HobbleStatusEffect();
            held.RestartDuration(5);
            var effects = new CreatureStatusEffect();
            effects.Add(held);
            var table = LoggedOutTable(statusEffects);
            table[LoggedOutKey] = Activator.CreateInstance(table.GetType().GenericTypeArguments[1], 4242u, effects, DateTime.UtcNow);
            SetLocalString(module, Prefix + "LOGGED_OUT_ID", held.Id);
        }

        private static void VerifyLoggedOutPlayer(EngineTestContext ctx, uint module, object statusEffects)
        {
            var table = LoggedOutTable(statusEffects);
            ctx.Assert(table.Contains(LoggedOutKey), "the logged-out player's held effects are carried into the new build");
            var entry = table[LoggedOutKey]!;
            var effects = (CreatureStatusEffect)entry.GetType().GetProperty("Effects")!.GetValue(entry)!;
            ctx.AssertEqual(GetLocalString(module, Prefix + "LOGGED_OUT_ID"), System.Linq.Enumerable.Single(effects.GetAllEffects()).Id, "the held effect is the same instance id");
            ctx.AssertEqual(4242u, (uint)entry.GetType().GetProperty("Creature")!.GetValue(entry)!, "the creature the held effects belong to");
        }

        /// <summary>The last phase: takes the player offline and lets the runner destroy the arena again.</summary>
        private static void Release(EngineTestContext ctx)
        {
            var module = GetModule();
            var creature = GetLocalObject(module, Prefix + "CREATURE");
            var observer = GetLocalObject(module, Prefix + "OBSERVER");
            using (PlayerAbilityFixture.Reattach(creature, (uint)GetLocalInt(module, Prefix + "CLIENT"), GetLocalInt(module, Prefix + "WAS_PLAYER")))
            using (PlayerAbilityFixture.Reattach(observer, (uint)GetLocalInt(module, Prefix + "OBSERVER_CLIENT"), GetLocalInt(module, Prefix + "OBSERVER_WAS_PLAYER")))
            {
            }

            ctx.Track(creature);
            ctx.Track(observer);
            foreach (var names in DB.Search(new DBQuery<PlayerKnownName>()
                         .AddFieldSearch(nameof(Entity.PlayerKnownName.ObserverPlayerId), GetObjectUUID(observer), false)
                         .AddPaging(50, 0)).ToList())
                DB.Delete<PlayerKnownName>(names.Id);
            LoggedOutTable(StatusEffect).Remove(LoggedOutKey);
            DeleteLocalString(module, Prefix + "LOGGED_OUT_ID");
            foreach (var name in new[] { "CREATURE", "OBSERVER" })
                DeleteLocalObject(module, Prefix + name);
            foreach (var name in new[] { "CLIENT", "WAS_PLAYER", "HP", "VERIFIED", "PREPARED", "OBSERVER_CLIENT", "OBSERVER_WAS_PLAYER" })
                DeleteLocalInt(module, Prefix + name);
            foreach (var name in new[] { "STATUS_ID", "TOKEN", "EXPECTED_OVERRIDE" })
                DeleteLocalString(module, Prefix + name);
            foreach (var name in new[] { "X", "Y", "Z" })
                DeleteLocalFloat(module, Prefix + name);
            DeleteLocalInt(module, EngineTest.KeepArenaVariable);
        }
    }
}
