using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Entity
{
    public class ResearchJob: EntityBase
    {
        [Indexed]
        public string ParentPropertyId { get; set; }

        [Indexed]
        public string PlayerId { get; set; }

        public DateTime DateStarted { get; set; }
        public DateTime DateCompleted { get; set; }

        public string SerializedItem { get; set; }
        public int Level { get; set; }
        public RecipeType Recipe { get; set; }

        /// <summary>The experiment the job ran and its stored outcome. Null for a job created before experiments existed.</summary>
        public ResearchExperimentRecord Experiment { get; set; }

        /// <summary>The character file sequence the start's blueprint and credit removal waits on. 0 for a job started before it was kept.</summary>
        public int StartSequence { get; set; }

        /// <summary>The character file sequence the blueprint handed back by a completion or cancel waits on. 0 until one is made.</summary>
        public int DeliverySequence { get; set; }
    }
}
