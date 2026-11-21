using System.Collections.Generic;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A farmhand's catch-up while it is being saved (Player City Revamp, "Farmhands"). A replay changes the farmhand's hoppers and the
    /// animals and plots it tends, and those are separate records, so a server stop between two of the writes would keep an animal's
    /// advanced timers and lose the products the farmhand collected for them, which exist only in the farmhand's output hopper. The
    /// catch-up therefore writes this record first, with the full new state of every record the replay changed, applies each of them,
    /// and deletes this record last (<see cref="FarmhandService.FarmhandCatchUpProcess"/>). Every write is a whole record, so applying
    /// the same catch-up again changes nothing, and a stop part way is finished at the next catch-up or server start. The id is
    /// <see cref="KeyFor"/> of the farmhand's target, never the target's id itself, since the DB's per-frame entity cache is keyed by id
    /// alone. A new type, so no migration.
    /// </summary>
    public class FarmhandCatchUp : EntityBase
    {
        public FarmhandCatchUp()
        {
            TargetId = string.Empty;
            AssignmentId = string.Empty;
            Animals = new List<Livestock>();
            Plots = new List<GardenPlot>();
            WanderedOff = new List<Livestock>();
            Withered = new List<GardenPlot>();
        }

        /// <summary>The pending catch-up's key for a house's or corral's property id.</summary>
        public static string KeyFor(string targetId) => "farmhand-catchup:" + targetId;

        /// <summary>The house or corral the farmhand works.</summary>
        [Indexed]
        public string TargetId { get; set; }

        /// <summary>The assignment of the droid that did the work. A farmhand with another assignment is not this one, and the catch-up is dropped.</summary>
        public string AssignmentId { get; set; }

        /// <summary>The farmhand's last processed time before the replay, which the replay started from.</summary>
        public DateTime FromUtc { get; set; }

        /// <summary>The time the replay ran to, which the farmhand and every record it kept carry as their last processed time.</summary>
        public DateTime UntilUtc { get; set; }

        /// <summary>The farmhand as the replay left it: its hoppers and last processed time. It is written last.</summary>
        public Farmhand Farmhand { get; set; }

        /// <summary>The animals the replay left standing, as it left them.</summary>
        public List<Livestock> Animals { get; set; }

        /// <summary>The crops the replay left standing, as it left them.</summary>
        public List<GardenPlot> Plots { get; set; }

        /// <summary>The animals that went unfed for 72 hours and are removed.</summary>
        public List<Livestock> WanderedOff { get; set; }

        /// <summary>The crops that were left ripe for 72 hours and are cleared.</summary>
        public List<GardenPlot> Withered { get; set; }
    }
}
