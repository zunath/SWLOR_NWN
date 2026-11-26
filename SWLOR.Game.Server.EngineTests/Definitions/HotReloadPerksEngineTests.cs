using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.EngineTests.Framework;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// What the perk stat bonus part rebuilds across hot reloads (MaintainabilityRefactorCharacterCompanionsSplit.md, "What a hot reload
    /// must recheck"). One phase per generation like <see cref="HotReloadPlayersEngineTests"/>, on a fresh creature holding a Light Force perk
    /// (a creature, not a headless player, so the scenario leaves the player of that test alone): the first run checks the stat bonus
    /// tables; each later run, in the new build, checks that
    /// <list type="bullet">
    /// <item>exactly one <see cref="PerkStatBonusService"/> boot handler is registered for the cache event, bound to this build's instance;</item>
    /// <item>this build's tables hold each perk's stat bonuses exactly once (the boot handler filled them once for the generation, not twice
    /// and not left empty);</item>
    /// <item>the perk's stat bonus still applies to the creature: the bonus read, its source, what the stat system reports for it, and the
    /// Force side affinity part finds the perk in this build's catalog.</item>
    /// </list>
    /// Reload is off in the normal engine sweep, so there the scenario skips; it runs with <c>scripts/run-hot-reload-check.sh
    /// --engine-filter HotReloadPlayers --reloads 2</c>.
    /// </summary>
    public sealed class HotReloadPerksEngineTests
    {
        private const string Prefix = "SWLOR_HRK_";
        private const int ReloadsToVerify = 2;
        private const PerkType LightPerk = PerkType.ForceBurst;

        // One per loaded build: a run whose token differs from the module's is the first run of a new generation.
        private static readonly string GenerationToken = Guid.NewGuid().ToString("N");

        public HotReloadPerksEngineTests(IPerkService perks, IPerkStatBonusService statBonuses, IPerkForceAffinityService affinity, IStatService stats)
        {
            Perks = perks;
            StatBonuses = statBonuses;
            Affinity = affinity;
            Stats = stats;
        }

        private IPerkService Perks { get; }
        private IPerkStatBonusService StatBonuses { get; }
        private IPerkForceAffinityService Affinity { get; }
        private IStatService Stats { get; }

        [EngineTest("A perk's stat bonus fills once per generation and still applies across hot reloads", Category = "HotReloadPlayers", TimeoutSeconds = 120f)]
        public Task StatBonusSurvivesReloads(EngineTestContext ctx)
        {
            if (Environment.GetEnvironmentVariable("SWLOR_HOT_RELOAD")?.Trim().ToLowerInvariant() is not ("true" or "1" or "yes"))
                ctx.Skip("Hot reload is off; run scripts/run-hot-reload-check.sh --engine-filter HotReloadPlayers --reloads 2.");

            var module = GetModule();
            if (GetLocalInt(module, Prefix + "PREPARED") == 0)
            {
                Verify(ctx);

                SetLocalString(module, Prefix + "TOKEN", GenerationToken);
                SetLocalInt(module, Prefix + "VERIFIED", 0);
                SetLocalInt(module, Prefix + "PREPARED", 1);
                ctx.SetResultDetail("prepared: reload the server " + ReloadsToVerify + " times to verify");
                return Task.CompletedTask;
            }

            if (GetLocalString(module, Prefix + "TOKEN") == GenerationToken)
                ctx.Skip("No reload since the last phase of this scenario ran.");

            Verify(ctx);
            var verified = GetLocalInt(module, Prefix + "VERIFIED") + 1;
            SetLocalString(module, Prefix + "TOKEN", GenerationToken);
            SetLocalInt(module, Prefix + "VERIFIED", verified);
            ctx.SetResultDetail($"verified after reload {verified} of {ReloadsToVerify}");
            if (verified >= ReloadsToVerify)
                Release();
            return Task.CompletedTask;
        }

        private void Verify(EngineTestContext ctx)
        {
            var creature = ctx.SpawnCreature("nw_rat001");
            SetLocalInt(creature, $"PERK_LEVEL_{(int)LightPerk}", 1);

            var own = ServerManager.Scripts.GetActionScripts(ScriptName.OnModuleCacheBefore)
                .Where(entry => entry.Name.Contains("PerkStatBonusService"))
                .ToList();
            ctx.AssertEqual(1, own.Count, "exactly one PerkStatBonusService handler is registered for the cache event");
            ctx.Assert(ReferenceEquals(own[0].Action.Target, StatBonuses), "the cache handler is bound to this build's instance");

            var table = (IDictionary)typeof(PerkStatBonusService)
                .GetField("_statBonusGroupsByStat", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(StatBonuses)!;
            var expected = Perks.EnumerateAllPerks()
                .SelectMany(pair => pair.Value.StatBonuses.Select(bonus => (Stat: bonus.Stat, Perk: pair.Key)))
                .GroupBy(entry => entry)
                .ToDictionary(group => group.Key, group => group.Count());
            ctx.Assert(expected.Count > 0, "the catalog has perk-wide stat bonuses to check");
            foreach (var ((stat, perk), count) in expected)
            {
                ctx.Assert(table.Contains(stat), $"the table of this build has a group list for {stat}");
                var group = ((IEnumerable)table[stat]!).Cast<object>().Single(item => (PerkType)item.GetType().GetProperty("PerkType")!.GetValue(item)! == perk);
                var bonuses = (IList)group.GetType().GetProperty("PerkBonuses")!.GetValue(group)!;
                ctx.AssertEqual(count, bonuses.Count, $"{perk} holds its {stat} bonus exactly once in this build");
            }

            ctx.AssertEqual(1, StatBonuses.GetStatBonus(creature, StatType.ForceAffinity), "the Light perk's stat bonus applies");
            ctx.AssertEqual(1, StatBonuses.GetStatSources(creature, StatType.ForceAffinity).Count, "the bonus has one source");
            ctx.AssertEqual($"perk:{(int)LightPerk}", StatBonuses.GetStatSources(creature, StatType.ForceAffinity)[0].Key, "the source names the perk");
            var held = Stats.GetStatAdjustment(creature, StatType.ForceAffinity);
            SetLocalInt(creature, $"PERK_LEVEL_{(int)LightPerk}", 0);
            var without = Stats.GetStatAdjustment(creature, StatType.ForceAffinity);
            SetLocalInt(creature, $"PERK_LEVEL_{(int)LightPerk}", 1);
            ctx.AssertEqual(1, held - without, "the stat system reads the perk through this build's part");
            ctx.Assert(Affinity.TryGetForceSideAffinity(creature, LightPerk, out _), "the Force affinity part finds the perk in this build's catalog");
        }

        private void Release()
        {
            var module = GetModule();
            foreach (var name in new[] { "VERIFIED", "PREPARED" })
                DeleteLocalInt(module, Prefix + name);
            DeleteLocalString(module, Prefix + "TOKEN");
        }
    }
}
