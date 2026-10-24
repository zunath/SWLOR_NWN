using System.Collections.Generic;
using Newtonsoft.Json;
using SWLOR.Game.Server.Service.CraftService.Research;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>Where a research job's experiment stands. Saved by name, so a member is never renamed.</summary>
    public enum ResearchExperimentStatus
    {
        /// <summary>The player is still working the experiment, or left without it being settled yet.</summary>
        InProgress = 0,

        /// <summary>The experiment ended and its outcome is stored; completion grants exactly that.</summary>
        Settled = 1
    }

    /// <summary>
    /// The experiment a research job ran when it started, and the outcome it stored. It is saved on the job after every action, so a
    /// logout or restart can settle it with the evidence so far. A job created before experiments existed has none, and finishes
    /// under the original random roll.
    /// </summary>
    public class ResearchExperimentRecord
    {
        public ResearchExperimentStatus Status { get; set; }

        /// <summary>The research level the job brings the blueprint to.</summary>
        public int TargetLevel { get; set; }

        /// <summary>The job's real-time length in seconds before any time cut.</summary>
        public int FullDurationSeconds { get; set; }

        public int TrialsTotal { get; set; }
        public int TrialsUsed { get; set; }

        /// <summary>The evidence on each candidate now, after any Analyze has spent some.</summary>
        public Dictionary<ResearchCandidate, int> Evidence { get; set; } = new();

        /// <summary>The evidence Analyze has spent on the time cut.</summary>
        public int SpentEvidence { get; set; }

        /// <summary>The share of the job's time the experiment cut, in percent. The job's completion time already includes it.</summary>
        public int TimeCutPercent { get; set; }

        /// <summary>True when the extra guaranteed bonus was rolled at the start and will be granted on completion.</summary>
        public bool ExtraBonus { get; set; }

        /// <summary>
        /// At a random level (3, 5 and 8), the licensed runs (1 to 3) the level grants, rolled when the experiment opened; 0 when the
        /// level grants a credit or time reduction that evidence decides, and at every other level.
        /// </summary>
        public int LicensedRunsGrant { get; set; }

        /// <summary>The winning candidate once settled; <see cref="ResearchCandidate.None"/> when a random level gathered no evidence.</summary>
        public ResearchCandidate Winner { get; set; }

        /// <summary>The winner's licensed runs or percent, once settled.</summary>
        public int Magnitude { get; set; }

        /// <summary>True once the experiment's Fabrication XP was given.</summary>
        public bool XpGranted { get; set; }

        /// <summary>The share of the experiment the player played, 0 to 100: the trials used out of the trials available.</summary>
        [JsonIgnore]
        public int EffortPercent =>TrialsTotal <= 0 ? 0 : Math.Clamp(TrialsUsed * 100 / TrialsTotal, 0, 100);
    }
}
