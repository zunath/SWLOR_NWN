using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// What the beast event service registers across hot reloads (MaintainabilityRefactorCharacterCompanionsSplit.md, "What a hot reload must
    /// recheck"). One phase per generation like <see cref="HotReloadIncubationEngineTests"/>: the first run leaves a marker; each run in a later
    /// generation checks that
    /// <list type="bullet">
    /// <item>exactly one <see cref="BeastEventService"/> handler is registered for each of the fifteen beast event scripts (the twelve beast
    /// events and the stables terminal, the despawn on space entry and on losing the associate, and the module acquire), bound to this build's
    /// instance, and exactly one <see cref="BeastMasteryService"/> handler for each of the two scripts that stay on it (the module cache and the
    /// combat point XP);</item>
    /// <item>a beast rest event, run through the script dispatch on a creature, reaches this build's handler and puts the rest status effect on
    /// it.</item>
    /// </list>
    /// Reload is off in the normal engine sweep, so there the scenario skips; it runs with <c>scripts/run-hot-reload-check.sh
    /// --engine-filter HotReloadPlayers --reloads 2</c>.
    /// </summary>
    public sealed class HotReloadBeastEventEngineTests
    {
        private const string Prefix = "SWLOR_HRB_";
        private const int ReloadsToVerify = 2;

        // One per loaded build: a run whose token differs from the module's is the first run of a new generation.
        private static readonly string GenerationToken = Guid.NewGuid().ToString("N");

        private static readonly string[] EventScripts =
        {
            ScriptName.OnAssociateRemoveBefore, ScriptName.OnSpaceEnter, ScriptName.OnModuleAcquire,
            ScriptName.OnBeastBlocked, ScriptName.OnBeastRoundEnd, ScriptName.OnBeastConversation, ScriptName.OnBeastDamaged,
            ScriptName.OnBeastDeath, ScriptName.OnBeastDisturbed, ScriptName.OnBeastHeartbeat, ScriptName.OnBeastAttacked,
            ScriptName.OnBeastRest, ScriptName.OnBeastSpawn, ScriptName.OnBeastSpellCast, ScriptName.OnBeastUserDefined,
            ScriptName.OnBeastTerminate,
        };

        public HotReloadBeastEventEngineTests(IBeastEventService events, IBeastMasteryService beastMastery, IStatusEffectService statusEffect)
        {
            Events = events;
            BeastMastery = beastMastery;
            StatusEffect = statusEffect;
        }

        private IBeastEventService Events { get; }
        private IBeastMasteryService BeastMastery { get; }
        private IStatusEffectService StatusEffect { get; }

        [EngineTest("Beast events register each handler once and reach this build across hot reloads", Category = "HotReloadPlayers", TimeoutSeconds = 60f)]
        public async Task BeastEventsSurviveReloads(EngineTestContext ctx)
        {
            if (Environment.GetEnvironmentVariable("SWLOR_HOT_RELOAD")?.Trim().ToLowerInvariant() is not ("true" or "1" or "yes"))
                ctx.Skip("Hot reload is off; run scripts/run-hot-reload-check.sh --engine-filter HotReloadPlayers --reloads 2.");

            var module = GetModule();
            if (GetLocalInt(module, Prefix + "PREPARED") == 0)
            {
                await Verify(ctx);
                SetLocalString(module, Prefix + "TOKEN", GenerationToken);
                SetLocalInt(module, Prefix + "VERIFIED", 0);
                SetLocalInt(module, Prefix + "PREPARED", 1);
                ctx.SetResultDetail("prepared: reload the server " + ReloadsToVerify + " times to verify");
                return;
            }

            if (GetLocalString(module, Prefix + "TOKEN") == GenerationToken)
                ctx.Skip("No reload since the last phase of this scenario ran.");

            await Verify(ctx);

            var verified = GetLocalInt(module, Prefix + "VERIFIED") + 1;
            SetLocalString(module, Prefix + "TOKEN", GenerationToken);
            SetLocalInt(module, Prefix + "VERIFIED", verified);
            ctx.SetResultDetail($"verified after reload {verified} of {ReloadsToVerify}");
            if (verified >= ReloadsToVerify)
            {
                DeleteLocalInt(module, Prefix + "PREPARED");
                DeleteLocalInt(module, Prefix + "VERIFIED");
                DeleteLocalString(module, Prefix + "TOKEN");
            }
        }

        private async Task Verify(EngineTestContext ctx)
        {
            foreach (var script in EventScripts)
            {
                var own = Handlers(script, "BeastEventService");
                ctx.AssertEqual(1, own.Count, $"exactly one BeastEventService handler is registered for {script}");
                ctx.Assert(ReferenceEquals(own[0].Action.Target, Events), $"the {script} handler is bound to this build's instance");
            }

            foreach (var script in new[] { ScriptName.OnModuleCacheBefore, ScriptName.OnCombatPointXPDistribute })
            {
                var own = Handlers(script, "BeastMasteryService");
                ctx.AssertEqual(1, own.Count, $"exactly one BeastMasteryService handler is registered for {script}");
                ctx.Assert(ReferenceEquals(own[0].Action.Target, BeastMastery), $"the {script} handler is bound to this build's instance");
            }

            var creature = ctx.SpawnCreature("civilian");
            ctx.Assert(!StatusEffect.HasStatusEffect<RestStatusEffect>(creature), "the creature starts without the rest status effect");
            await ctx.ExecuteInCreatureContextAsync(creature, () => ExecuteScript(ScriptName.OnBeastRest, creature));
            await ctx.DelaySecondsAsync(1f);
            ctx.Assert(StatusEffect.HasStatusEffect<RestStatusEffect>(creature), "the beast rest event reached this build's handler and put the rest status effect on the creature");
        }

        private static List<(string Name, Action Action)> Handlers(string script, string className)
        {
            return ServerManager.Scripts.GetActionScripts(script)
                .Where(entry => entry.Name.Contains(className + "."))
                .Select(entry => (entry.Name, entry.Action))
                .ToList();
        }
    }
}
