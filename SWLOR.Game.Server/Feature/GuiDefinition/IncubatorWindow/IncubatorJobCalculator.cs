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
    /// The incubator's job arithmetic: the stored job, whether it is complete, how long a stage takes, the Erratic Genius bonus,
    /// and the projected mutation chance and stat totals. It reads the window's session through the host and uses no other part.
    /// </summary>
    public class IncubatorJobCalculator
    {
        private IDBService DB { get; }
        private IPerkService Perk { get; }

        public IncubatorJobCalculator(IDBService db, IPerkService perk)
        {
            DB = db;
            Perk = perk;
        }

        public const int BaseSecondsBetweenStages = 129600; // 129600 = 36 hours
        public const int NumberOfStages = IncubationProjection.NumberOfStages;
        public const int MaxStageIncrease = 100; // 10.0%
        public const int MaxStat = 1000; // 100.0%

        /// <summary>A job can be completed once its last stage has run its full time.</summary>
        public static bool IsJobComplete(IncubationJob job, DateTime now)
        {
            return job != null &&
                   job.CurrentStage >= NumberOfStages &&
                   now >= job.DateCompleted;
        }

        public IncubationJob GetJob(IIncubatorHost host)
        {
            var dbQuery = new DBQuery<IncubationJob>()
                .AddFieldSearch(nameof(IncubationJob.ParentPropertyId), host.Session.IncubatorPropertyId, false);
            return DB.Search(dbQuery).FirstOrDefault();
        }

        public int GetErraticGeniusBonus(IIncubatorHost host)
        {
            return Perk.GetPerkLevel(host.Player, PerkType.ErraticGenius) switch
            {
                1 => 2,
                2 => 4,
                3 => 8,
                _ => 0
            };
        }

        public int GetProjectedMutationChance(IIncubatorHost host, IncubationJob job, bool hasPendingStage)
        {
            host.Session.BaseStats.TryGetValue(IncubationStatType.MutationChance, out var baseValue);
            host.Session.StageStats.TryGetValue(IncubationStatType.MutationChance, out var stageValue);
            var gain = hasPendingStage ? Math.Clamp(stageValue, 0, MaxStageIncrease) : 0;
            var erraticGenius = job == null && host.IsErraticGeniusChecked ? GetErraticGeniusBonus(host) * 10 : 0;
            return Math.Min(baseValue + gain + erraticGenius, MaxStat);
        }

        public int CalculateIncubationSeconds(IIncubatorHost host)
        {
            var social = GetAbilityScore(host.Player, AbilityType.Social) - 10;
            var socialBonus = 0.5f * (social <= 0 ? 0 : social);
            if (socialBonus > 10)
                socialBonus = 10;

            var timeReductionPercentage = 0.01f * (Perk.GetPerkLevel(host.Player, PerkType.IncubationProcessing) * 10 + socialBonus);
            return BaseSecondsBetweenStages - (int)(BaseSecondsBetweenStages * timeReductionPercentage);
        }

        public static int GetStageTotal(IIncubatorHost host, IncubationStatType stat)
        {
            host.Session.BaseStats.TryGetValue(stat, out var baseValue);
            host.Session.StageStats.TryGetValue(stat, out var stageValue);
            return Math.Min(baseValue + Math.Clamp(stageValue, 0, MaxStageIncrease), MaxStat);
        }
    }
}
