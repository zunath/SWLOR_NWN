using System.Collections.Generic;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition.StatusEffectData
{
    public class FoodEffectData
    {
        public int HP { get; set; }
        public int FP { get; set; }
        public int STM { get; set; }
        public int HPRegen { get; set; }
        public int FPRegen { get; set; }
        public int STMRegen { get; set; }
        public int RestRegen { get; set; }
        public int XPBonusPercent { get; set; }
        public int RecastReductionPercent { get; set; }
        public int Attack { get; set; }
        public int Accuracy { get; set; }
        public int DefensePhysical { get; set; }
        public int DefenseForce { get; set; }
        public int DefensePoison { get; set; }
        public int DefenseFire { get; set; }
        public int DefenseIce { get; set; }
        public int DefenseElectrical { get; set; }
        public int Evasion { get; set; }

        public Dictionary<SkillType, int> Control { get; set; }
        public Dictionary<SkillType, int> Craftsmanship { get; set; }

        public FoodEffectData()
        {
            Control = new Dictionary<SkillType, int>
            {
                { SkillType.Agriculture, 0},
                { SkillType.Smithery, 0},
                { SkillType.Engineering, 0},
                { SkillType.Fabrication, 0},
            };
            Craftsmanship = new Dictionary<SkillType, int>
            {
                { SkillType.Agriculture, 0},
                { SkillType.Smithery, 0},
                { SkillType.Engineering, 0},
                { SkillType.Fabrication, 0},
            };
        }
    }
}
