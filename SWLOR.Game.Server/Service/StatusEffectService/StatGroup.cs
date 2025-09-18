using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    public class StatGroup
    {
        public Dictionary<AbilityType, int> Stats { get; set; }
        public Dictionary<CombatDamageType, int> Resists { get; set; }

        public StatGroup()
        {
            Stats = new Dictionary<AbilityType, int>();
            Resists = new Dictionary<CombatDamageType, int>();
        }
    }
}
