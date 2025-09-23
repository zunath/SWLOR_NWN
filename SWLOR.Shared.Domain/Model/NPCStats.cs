using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Shared.Domain.Model
{
    public class NPCStats
    {
        public int Level { get; set; }
        public int Attack { get; set; }
        public int ForceAttack { get; set; }
        public int Evasion { get; set; }
        public int FP { get; set; }
        public int Stamina { get; set; }
        public Dictionary<CombatDamageType, int> Defenses { get; set; }
        public Dictionary<SkillType, int> Skills { get; set; }

        public NPCStats()
        {
            Defenses = new Dictionary<CombatDamageType, int>();
            Skills = new Dictionary<SkillType, int>();
        }
    }
}
