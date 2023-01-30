using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Entity
{
    public class Beast: EntityBase
    {
        [Indexed]
        public string Name { get; set; }

        [Indexed]
        public string OwnerPlayerId { get; set; }

        public int Level { get; set; }

        public int XP { get; set; }
        public int UnallocatedSP { get; set; }
        public bool IsDead { get; set; }

        public int PortraitId { get; set; }
        public int SoundSetId { get; set; }

        public BeastType Type { get; set; }

        public BeastFoodType FavoriteFood { get; set; }

        public BeastFoodType HatedFood { get; set; }

        public Dictionary<PerkType, int> Perks { get; set; }

        public int AttackPurity { get; set; }

        public int AccuracyPurity { get; set; }

        public int EvasionPurity { get; set; }
        
        public int LearningPurity { get; set; }

        public Dictionary<CombatDamageType, int> DefensePurities { get; set; }

        public Dictionary<SavingThrow, int> SavingThrowPurities { get; set; }


        public Beast()
        {
            Level = 1;
            Perks = new Dictionary<PerkType, int>();
            PortraitId = -1;
            SoundSetId = -1;

            DefensePurities = new Dictionary<CombatDamageType, int>
            {
                { CombatDamageType.Physical, 0 },
                { CombatDamageType.Force, 0 },
                { CombatDamageType.Fire, 0 },
                { CombatDamageType.Ice, 0 },
                { CombatDamageType.Poison, 0 },
                { CombatDamageType.Electrical, 0 },
            };

            SavingThrowPurities = new Dictionary<SavingThrow, int>
            {
                { SavingThrow.Fortitude, 0},
                { SavingThrow.Will, 0},
                { SavingThrow.Reflex, 0},
            };
        }

    }
}
