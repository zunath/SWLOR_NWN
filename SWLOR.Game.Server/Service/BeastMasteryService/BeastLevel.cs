using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    public class BeastLevel
    {
        public int HP { get; set; }
        public int STM { get; set; }
        public int FP { get; set; }
        public int DMG { get; set; }
        public Dictionary<AbilityType, int> Stats { get; set; }

        public int MaxAttackBonus { get; set; }
        public int MaxAccuracyBonus { get; set; }
        public int MaxEvasionBonus { get; set; }
        public Dictionary<CombatDamageType, int> MaxDefenseBonuses { get; set; }
        public Dictionary<SavingThrow, int> MaxSavingThrowBonuses { get; set; }

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

            MaxDefenseBonuses = new Dictionary<CombatDamageType, int>
            {
                { CombatDamageType.Physical, 0},
                { CombatDamageType.Force, 0},
                { CombatDamageType.Fire, 0},
                { CombatDamageType.Poison, 0},
                { CombatDamageType.Electrical, 0},
                { CombatDamageType.Ice, 0},
            };

            MaxSavingThrowBonuses = new Dictionary<SavingThrow, int>
            {
                { SavingThrow.Fortitude, 0},
                { SavingThrow.Will, 0},
                { SavingThrow.Reflex, 0},
            };
        }
    }
}
