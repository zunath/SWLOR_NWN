using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    public class BeastLevel
    {
        public int HP { get; set; }
        public int STM { get; set; }
        public int FP { get; set; }
        public int DMG { get; set; }
        public int Delay { get; set; }
        public Dictionary<AbilityType, int> Stats { get; set; }

        public int MaxAttackBonus { get; set; }
        public int MaxAccuracyBonus { get; set; }
        public int MaxEvasionBonus { get; set; }
        public Dictionary<CombatDamageType, int> MaxDefenseBonuses { get; set; }
        public Dictionary<ResistanceType, int> MaxResistanceBonuses { get; set; }

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

            MaxDefenseBonuses = Combat.CreateDefaultDefenseValues();

            MaxResistanceBonuses = Resistance.CreateDefaultResistanceValues();
        }
    }
}
