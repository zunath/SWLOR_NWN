using System.Collections.Generic;
using SWLOR.Game.Server.Service.ClerkRecoveryService;
using SWLOR.Game.Server.Service.LivestockService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A City Hall clerk action under way on a crop or an animal (Player City Revamp, "Recovery"). It is saved on the crop's
    /// <see cref="GardenPlot"/> or the animal's <see cref="Livestock"/> before anything else is written, with everything the action
    /// gives, so a restart before the action ends is finished at the next processing: the overflow records are written again under
    /// the same ids, then the crop or animal is deleted or updated. A crop or animal with no action under way has no mark.
    /// </summary>
    public class ClerkRecoveryMark
    {
        public ClerkRecoveryMark()
        {
            Id = string.Empty;
            PlayerId = string.Empty;
            Items = new Dictionary<string, int>();
        }

        /// <summary>Tells this action from any other on the same crop or animal. The overflow record ids are built from it.</summary>
        public string Id { get; set; }

        public ClerkRecoveryAction Action { get; set; }

        /// <summary>The character whose overflow storage receives what the action gives: the crop's planter or the animal's owner.</summary>
        public string PlayerId { get; set; }

        /// <summary>When the action was taken. A collection records this time.</summary>
        public DateTime AtUtc { get; set; }

        /// <summary>The items the action gives, by blueprint resref.</summary>
        public Dictionary<string, int> Items { get; set; }

        /// <summary>The animal packed as it stood, when the action crates it.</summary>
        public LivestockCrate Crate { get; set; }
    }
}
