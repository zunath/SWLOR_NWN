using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.GuiDefinition.IncubatorWindow
{
    /// <summary>
    /// The incubator's job actions: validating and starting a stage, and the start, continue, cancel and complete clicks.
    /// </summary>
    public class IncubatorJobService
    {
        private IBeastMasteryService BeastMastery { get; }
        private IDBService DB { get; }
        private IGuiService Gui { get; }
        private IncubatorJobCalculator IncubatorJobCalculator { get; }
        private IPerkService Perk { get; }

        public IncubatorJobService(IBeastMasteryService beastMastery, IDBService db, IGuiService gui, IncubatorJobCalculator incubatorJobCalculator, IPerkService perk)
        {
            BeastMastery = beastMastery;
            DB = db;
            Gui = gui;
            IncubatorJobCalculator = incubatorJobCalculator;
            Perk = perk;
        }

        private string ValidateCreateJob(IIncubatorHost host)
        {
            var playerId = GetObjectUUID(host.Player);
            var maxConcurrentJobs = Perk.GetPerkLevel(host.Player, PerkType.IncubationManagement) + 1;
            var dbQuery = new DBQuery<IncubationJob>()
                .AddFieldSearch(nameof(IncubationJob.PlayerId), playerId, false);
            var currentJobs = DB.Search(dbQuery).ToList();
            var currentJobCount = currentJobs.Count(x => x.ParentPropertyId != host.Session.IncubatorPropertyId);

            if (currentJobCount >= maxConcurrentJobs)
            {
                return $"You may only have {maxConcurrentJobs} incubation job(s) active at one time.";
            }

            var job = IncubatorJobCalculator.GetJob(host);
            if (job == null)
                return string.Empty;

            if (job.CurrentStage >= IncubatorJobCalculator.NumberOfStages)
                return "Max stage reached.";

            if (Clock.UtcNow < job.DateCompleted)
                return "The current stage is still running.";

            if (host.IsErraticGeniusChecked && Perk.GetPerkLevel(host.Player, PerkType.ErraticGenius) <= 0)
            {
                return "You do not have the Erratic Genius perk purchased and cannot start this job.";
            }

            return string.Empty;
        }

        private void StartJob(IIncubatorHost host, IncubationJob job)
        {
            var incubationSeconds = IncubatorJobCalculator.CalculateIncubationSeconds(host);
            var now = Clock.UtcNow;
            var hasPendingStage = true;

            var validationError = ValidateCreateJob(host);
            if (string.IsNullOrWhiteSpace(validationError))
            {
                job.MutationChance = IncubatorJobCalculator.GetProjectedMutationChance(host, job.CurrentStage <= 0 ? null : job, hasPendingStage);
                if (job.CurrentStage <= 0)
                    job.DNAIconResref = host.DNAItemResref;
                job.CurrentStage++;
                job.IsMutationSuppressed = !host.IsMutationAllowed;

                job.AttackPurity = IncubatorJobCalculator.GetStageTotal(host, IncubationStatType.AttackPurity);
                job.AccuracyPurity = IncubatorJobCalculator.GetStageTotal(host, IncubationStatType.AccuracyPurity);
                job.EvasionPurity = IncubatorJobCalculator.GetStageTotal(host, IncubationStatType.EvasionPurity);
                job.LearningPurity = IncubatorJobCalculator.GetStageTotal(host, IncubationStatType.LearningPurity);
                job.XPPenalty = IncubatorJobCalculator.GetStageTotal(host, IncubationStatType.XPPenalty);

                job.DefensePurities[CombatDamageType.Physical] = IncubatorJobCalculator.GetStageTotal(host, IncubationStatType.PhysicalDefensePurity);
                job.DefensePurities[CombatDamageType.Force] = IncubatorJobCalculator.GetStageTotal(host, IncubationStatType.ForceDefensePurity);

                foreach (var stat in IncubationProjection.ResistancePurityStats)
                {
                    job.ResistancePurities[IncubationProjection.GetResistanceType(stat)] = IncubatorJobCalculator.GetStageTotal(host, stat);
                }

                job.ResistancePurities = BeastResistanceCalculator.CreateResistancePurities(job.ResistancePurities);

                if (host.Session.LyaseColor != EnzymeColorType.Invalid)
                    job.LyaseColors[host.Session.LyaseColor]++;
                if (host.Session.HydrolaseColor != EnzymeColorType.Invalid)
                    job.HydrolaseColors[host.Session.HydrolaseColor]++;
                if (host.Session.IsomeraseColor != EnzymeColorType.Invalid)
                    job.IsomeraseColors[host.Session.IsomeraseColor]++;

                job.DateStarted = now;
                job.DateCompleted = now.AddSeconds(incubationSeconds);

                DB.Set(job);

                host.Session.DnaItem = string.Empty;
                host.Session.HydrolaseItem = string.Empty;
                host.Session.IsomeraseItem = string.Empty;
                host.Session.LyaseItem = string.Empty;
                // The enzymes are consumed, so they no longer count toward the next stage.
                host.Session.ResetPendingEnzymes();
                Gui.CloseWindow(host.Player, GuiWindowType.Incubator, host.Player);
                FloatingTextStringOnCreature($"Incubation stage {job.CurrentStage} started!", host.Player, false);
            }
            else
            {
                SendMessageToPC(host.Player, $"Unable to start Incubation Job. Reason: {validationError}");
                Log.Write(LogGroup.Incubation, $"Job could not be created on incubator Id {host.Session.IncubatorPropertyId} due to reason: {validationError}");
            }
        }

        public Action OnClickStartJob(IIncubatorHost host) => () =>
        {
            host.ShowModal("Start stage 1 of this job? The DNA and enzymes go into the incubator.", () =>
            {
                var job = new IncubationJob
                {
                    ParentPropertyId = host.Session.IncubatorPropertyId,
                    PlayerId = GetObjectUUID(host.Player),
                    BeastDNAType = host.Session.DnaType
                };
                StartJob(host, job);

                host.SwitchViews();
            }, host.SwitchViews);
        };

        public Action OnClickContinueJob(IIncubatorHost host) => () =>
        {
            var job = IncubatorJobCalculator.GetJob(host);
            if (job == null)
                return;

            host.ShowModal($"Start stage {job.CurrentStage + 1}? These enzymes go into the incubator.", () =>
            {
                StartJob(host, job);
                host.SwitchViews();
            }, host.SwitchViews);
        };

        public Action OnClickCancelJob(IIncubatorHost host) => () =>
        {
            host.ShowModal("Are you sure you want to cancel this job? All DNA and enzyme items will be permanently lost!",
            () =>
            {
                var dbJob = IncubatorJobCalculator.GetJob(host);
                if (dbJob == null)
                    return;

                DB.Delete<IncubationJob>(dbJob.Id);
                Gui.CloseWindow(host.Player, GuiWindowType.Incubator, host.Player);
                Log.Write(LogGroup.Incubation, $"Player '{GetName(host.Player)}' ({GetObjectUUID(host.Player)}) canceled incubation job '{dbJob.Id}' on incubator property Id '{dbJob.ParentPropertyId}'.");
                FloatingTextStringOnCreature("Incubation job cancelled!", host.Player, false);

                host.SwitchViews();
            }, host.SwitchViews);
        };

        public Action OnClickCompleteJob(IIncubatorHost host) => () =>
        {
            var job = IncubatorJobCalculator.GetJob(host);
            if (job == null)
                return;

            var outcomes = IncubationProjection.GetOutcomes(BeastMastery.GetBeastDetail(job.BeastDNAType), job, !job.IsMutationSuppressed);
            var mutationOdds = outcomes
                .Where(x => x.Type != job.BeastDNAType)
                .Sum(x => x.Percent);
            var prompt = mutationOdds > 0f
                ? $"Complete this job? It has a {mutationOdds:0.0}% chance to mutate into another beast."
                : "Complete this job? The egg keeps its DNA's beast type.";

            host.ShowModal(prompt, () =>
            {
                var current = IncubatorJobCalculator.GetJob(host);
                if (!IncubatorJobCalculator.IsJobComplete(current, Clock.UtcNow))
                {
                    host.SwitchViews();
                    return;
                }

                BeastMastery.CreateBeastEgg(current, host.Player);
                Gui.CloseWindow(host.Player, GuiWindowType.Incubator, host.Player);

                host.SwitchViews();
            }, host.SwitchViews);
        };
    }
}
