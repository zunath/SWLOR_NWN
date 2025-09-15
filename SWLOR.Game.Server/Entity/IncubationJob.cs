using System;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Entity
{
    public class IncubationJob: EntityBase
    {
        [Indexed]
        public string ParentPropertyId { get; set; }

        [Indexed]
        public string PlayerId { get; set; }

        public int CurrentStage { get; set; }

        public BeastType BeastDNAType { get; set; }

        public int MutationChance { get; set; }

        public int AttackPurity { get; set; }

        public int AccuracyPurity { get; set; }

        public int EvasionPurity { get; set; }

        public int LearningPurity { get; set; }

        public Dictionary<CombatDamageType, int> DefensePurities { get; set; }

        public Dictionary<SavingThrow, int> SavingThrowPurities { get; set; }

        public int XPPenalty { get; set; }

        public DateTime DateStarted { get; set; }
        public DateTime DateCompleted { get; set; }

        public Dictionary<EnzymeColorType, int> LyaseColors { get; set; }
        public Dictionary<EnzymeColorType, int> IsomeraseColors { get; set; }
        public Dictionary<EnzymeColorType, int> HydrolaseColors { get; set; }

        public IncubationJob()
        {
            DefensePurities = new Dictionary<CombatDamageType, int>();
            SavingThrowPurities = new Dictionary<SavingThrow, int>();
            LyaseColors = new Dictionary<EnzymeColorType, int>
            {
                { EnzymeColorType.Blue, 0 },
                { EnzymeColorType.Orange, 0 },
                { EnzymeColorType.Red, 0 },
                { EnzymeColorType.Purple, 0 },
                { EnzymeColorType.White, 0 },
                { EnzymeColorType.Green, 0 },
                { EnzymeColorType.Yellow, 0 },
                { EnzymeColorType.Black, 0 },
            };
            IsomeraseColors = new Dictionary<EnzymeColorType, int>
            {
                { EnzymeColorType.Blue, 0 },
                { EnzymeColorType.Orange, 0 },
                { EnzymeColorType.Red, 0 },
                { EnzymeColorType.Purple, 0 },
                { EnzymeColorType.White, 0 },
                { EnzymeColorType.Green, 0 },
                { EnzymeColorType.Yellow, 0 },
                { EnzymeColorType.Black, 0 },
            };
            HydrolaseColors = new Dictionary<EnzymeColorType, int>
            {
                { EnzymeColorType.Blue, 0 },
                { EnzymeColorType.Orange, 0 },
                { EnzymeColorType.Red, 0 },
                { EnzymeColorType.Purple, 0 },
                { EnzymeColorType.White, 0 },
                { EnzymeColorType.Green, 0 },
                { EnzymeColorType.Yellow, 0 },
                { EnzymeColorType.Black, 0 },
            };
        }
    }
}
