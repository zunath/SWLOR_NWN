using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    public class StatGroup
    {
        public Dictionary<StatType, int> Stats { get; set; }
        public Dictionary<AbilityType, int> Abilities { get; set; }
        public Dictionary<ResistanceType, int> Resists { get; set; }
        public Dictionary<CraftSkillBonusType, Dictionary<SkillType, int>> CraftSkillBonuses { get; set; }

        public StatGroup()
        {
            Stats = new Dictionary<StatType, int>();
            Abilities = new Dictionary<AbilityType, int>();
            Resists = new Dictionary<ResistanceType, int>();
            CraftSkillBonuses = new Dictionary<CraftSkillBonusType, Dictionary<SkillType, int>>();
            PopulateStats();
            PopulateAbilities();
            PopulateResists();
            PopulateCraftSkillBonuses();
        }

        private void PopulateStats()
        {
            foreach (var type in System.Enum.GetValues(typeof(StatType)).Cast<StatType>())
            {
                Stats[type] = 0;
            }
        }

        private void PopulateAbilities()
        {
            foreach (var type in System.Enum.GetValues(typeof(AbilityType)).Cast<AbilityType>())
            {
                Abilities[type] = 0;
            }
        }

        private void PopulateResists()
        {
            foreach (var type in Resistance.GetAllResistanceTypes())
            {
                Resists[type] = 0;
            }
        }

        private void PopulateCraftSkillBonuses()
        {
            foreach (var bonusType in System.Enum.GetValues(typeof(CraftSkillBonusType)).Cast<CraftSkillBonusType>())
            {
                CraftSkillBonuses[bonusType] = new Dictionary<SkillType, int>();
                foreach (var skill in System.Enum.GetValues(typeof(SkillType)).Cast<SkillType>())
                {
                    CraftSkillBonuses[bonusType][skill] = 0;
                }
            }
        }
    }
}
