using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.DroidService
{
    public class DroidItemPropertyDetails
    {
        public string CustomName { get; set; }
        public int Tier { get; set; }
        public int Level { get; set; }
        public int HP { get; set; }
        public int STM { get; set; }
        public int AISlots { get; set; }
        public int AGI { get; set; }
        public int MGT { get; set; }
        public int PER { get; set; }
        public int SOC { get; set; }
        public int VIT { get; set; }
        public int WIL { get; set; }
        public DroidPersonalityType PersonalityType { get; set; }

        public Dictionary<SkillType, int> Skills { get; set; }
        public Dictionary<PerkType, int> Perks { get; set; }

        public DroidItemPropertyDetails()
        {
            Tier = 1;
            CustomName = string.Empty;
            Skills = new Dictionary<SkillType, int>();
            Perks = new Dictionary<PerkType, int>();
            PersonalityType = DroidPersonalityType.Bland;
        }
    }
}
