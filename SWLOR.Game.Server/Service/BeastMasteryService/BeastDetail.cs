using SWLOR.Game.Server.Core.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    public class BeastDetail
    {
        public string Name { get; set; }
        public AppearanceType Appearance { get; set; }
        public float AppearanceScale { get; set; }
        public int PortraitId { get; set; }
        public int SoundSetId { get; set; }
        public BeastRoleType Role { get; set; }
        public AbilityType AccuracyStat { get; set; }
        public AbilityType DamageStat { get; set; }
        public Dictionary<int, BeastLevel> Levels { get; set; }
        public List<MutationDetail> PossibleMutations { get; set; }

        public BeastDetail()
        {
            AppearanceScale = 1f;
            Levels = new Dictionary<int, BeastLevel>();
            PossibleMutations = new List<MutationDetail>();
        }
    }
}
