using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;

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
        public int XPPenaltyPercent { get; set; }
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

        public Dictionary<ResistanceType, int> ResistancePurities { get; set; }


        public Beast()
        {
            Level = 1;
            Perks = new Dictionary<PerkType, int>();
            PortraitId = -1;
            SoundSetId = -1;

            DefensePurities = CombatRules.CreateDefaultDefenseValues();

            ResistancePurities = ResistanceRules.CreateDefaultResistanceValues();
        }

    }
}
