using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Service.StatService
{
    public class NPCStats
    {
        public int Level { get; set; }
        public Dictionary<CombatDamageType, int> Defenses { get; set; }

        public NPCStats()
        {
            Defenses = new Dictionary<CombatDamageType, int>();
        }
    }
}
