using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    public class BeastLevel
    {
        public int HP { get; set; }
        public int STM { get; set; }
        public int FP { get; set; }
        public Dictionary<AbilityType, int> Stats { get; set; }

        public BeastLevel()
        {
            Stats = new Dictionary<AbilityType, int>
            {
                {AbilityType.Might, 0},
                {AbilityType.Perception, 0},
                {AbilityType.Vitality, 0},
                {AbilityType.Willpower, 0},
                {AbilityType.Agility, 0},
                {AbilityType.Social, 0}
            };
        }
    }
}
