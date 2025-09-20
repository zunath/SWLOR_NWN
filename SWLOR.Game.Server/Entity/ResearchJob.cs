using System;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Shared.Abstractions;

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
    }
}
