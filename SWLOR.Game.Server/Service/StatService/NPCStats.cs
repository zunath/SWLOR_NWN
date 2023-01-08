using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.StatService
{
    public class NPCStats
    {
        public int Level { get; set; }
        public Dictionary<CombatDamageType, int> Defenses { get; set; }
        public Dictionary<SkillType, int> Skills { get; set; }

        public NPCStats()
        {
            Defenses = new Dictionary<CombatDamageType, int>();
            Skills = new Dictionary<SkillType, int>();
        }
    }
}
