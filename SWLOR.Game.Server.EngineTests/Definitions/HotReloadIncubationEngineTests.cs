using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// What the beast incubation service keeps and registers across hot reloads (MaintainabilityRefactorCharacterCompanionsSplit.md,
    /// "What a hot reload must recheck"). One phase per generation like <see cref="HotReloadNamesEngineTests"/>: the first run sets a weekday
    /// override and leaves a marker; each run in a later generation checks that
    /// <list type="bullet">
    /// <item>the override of the previous build is gone (<c>_incubationDayOverrides</c> is runtime state, never carried), and that this build
    /// takes and answers a new one;</item>
    /// <item>exactly one handler of <see cref="BeastIncubationService"/> is registered for each of the incubator terminal, the DNA extract
    /// object and the property deletion events, bound to this build's instance, so each fires once per event;</item>
    /// <item>the property deletion event, signalled as the property service signals it, removes the jobs of that property through the handler.</item>
    /// </list>
    /// Reload is off in the normal engine sweep, so there the scenario skips; it runs with <c>scripts/run-hot-reload-check.sh
    /// --engine-filter HotReloadPlayers --reloads 2</c>.
    /// </summary>
    public sealed class HotReloadIncubationEngineTests
    {
        private const string Prefix = "SWLOR_HRI_";
        private const int ReloadsToVerify = 2;
        private const string TesterId = "hot-reload-incubation-tester";
        private const string PropertyId = "hot-reload-incubation-property";

        // One per loaded build: a run whose token differs from the module's is the first run of a new generation.
        private static readonly string GenerationToken = Guid.NewGuid().ToString("N");

        public HotReloadIncubationEngineTests(IBeastIncubationService incubation, IDBService db)
        {
            Incubation = incubation;
            DB = db;
        }

        private IBeastIncubationService Incubation { get; }
        private IDBService DB { get; }

        [EngineTest("Beast incubation keeps no weekday override and registers each handler once across hot reloads", Category = "HotReloadPlayers", TimeoutSeconds = 60f)]
        public Task IncubationSurvivesReloads(EngineTestContext ctx)
        {
            if (Environment.GetEnvironmentVariable("SWLOR_HOT_RELOAD")?.Trim().ToLowerInvariant() is not ("true" or "1" or "yes"))
                ctx.Skip("Hot reload is off; run scripts/run-hot-reload-check.sh --engine-filter HotReloadPlayers --reloads 2.");

            var module = GetModule();
            if (GetLocalInt(module, Prefix + "PREPARED") == 0)
            {
                VerifyHandlersAndOverride(ctx);
                SetLocalString(module, Prefix + "TOKEN", GenerationToken);
                SetLocalInt(module, Prefix + "VERIFIED", 0);
                SetLocalInt(module, Prefix + "PREPARED", 1);
                ctx.SetResultDetail("prepared: reload the server " + ReloadsToVerify + " times to verify");
                return Task.CompletedTask;
            }

            if (GetLocalString(module, Prefix + "TOKEN") == GenerationToken)
                ctx.Skip("No reload since the last phase of this scenario ran.");

            ctx.Assert(!Incubation.HasIncubationDayOverride(TesterId), "the weekday override of the previous build is not carried");
            ctx.AssertEqual(DateTime.UtcNow.DayOfWeek, Incubation.GetIncubationServerDay(TesterId), "the new build answers the server day");
            VerifyHandlersAndOverride(ctx);

            var verified = GetLocalInt(module, Prefix + "VERIFIED") + 1;
            SetLocalString(module, Prefix + "TOKEN", GenerationToken);
            SetLocalInt(module, Prefix + "VERIFIED", verified);
            ctx.SetResultDetail($"verified after reload {verified} of {ReloadsToVerify}");
            if (verified >= ReloadsToVerify)
            {
                Incubation.ClearIncubationDayOverride(TesterId);
                DeleteLocalInt(module, Prefix + "PREPARED");
                DeleteLocalInt(module, Prefix + "VERIFIED");
                DeleteLocalString(module, Prefix + "TOKEN");
            }

            return Task.CompletedTask;
        }

        private void VerifyHandlersAndOverride(EngineTestContext ctx)
        {
            Incubation.SetIncubationDayOverride(TesterId, DayOfWeek.Wednesday);
            ctx.Assert(Incubation.HasIncubationDayOverride(TesterId), "this build holds the override it was given");
            ctx.AssertEqual(DayOfWeek.Wednesday, Incubation.GetIncubationServerDay(TesterId), "this build answers the override");

            foreach (var script in new[] { ScriptName.OnIncubatorTerminal, ScriptName.OnDNAExtractUsed, ScriptName.OnSwlorDeleteProperty })
            {
                var own = ServerManager.Scripts.GetActionScripts(script)
                    .Where(entry => entry.Name.Contains("BeastIncubationService"))
                    .ToList();
                ctx.AssertEqual(1, own.Count, $"exactly one BeastIncubationService handler is registered for {script}");
                ctx.Assert(ReferenceEquals(own[0].Action.Target, Incubation), $"the {script} handler is bound to this build's instance");
            }

            var job = new IncubationJob
            {
                ParentPropertyId = PropertyId,
                PlayerId = TesterId,
                BeastDNAType = BeastType.Aradile,
                DateCompleted = DateTime.UtcNow.AddHours(1),
            };
            DB.Set(job);
            ctx.AssertEqual(1, CountJobs(), "the job is stored");
            EventsPlugin.PushEventData("PROPERTY_ID", PropertyId);
            EventsPlugin.SignalEvent("SWLOR_DELETE_PROPERTY", GetModule());
            ctx.AssertEqual(0, CountJobs(), "the property deletion event removes the jobs of the property through this build's handler");
        }

        private int CountJobs() => DB.Search(new DBQuery<IncubationJob>()
            .AddFieldSearch(nameof(IncubationJob.ParentPropertyId), PropertyId, false)).Count();
    }
}
