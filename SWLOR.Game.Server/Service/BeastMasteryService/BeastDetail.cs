using SWLOR.Game.Server.Core.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    public class BeastDetail
    {
        public AppearanceType Appearance { get; set; }
        public int PortraitId { get; set; }
        public int SoundSetId { get; set; }
        public BeastRoleType Role { get; set; }
        public AbilityType AccuracyStat { get; set; }
        public AbilityType DamageStat { get; set; }
        public Dictionary<int, BeastLevel> Levels { get; set; }

        public BeastDetail()
        {
            Levels = new Dictionary<int, BeastLevel>();
        }
    }
}
