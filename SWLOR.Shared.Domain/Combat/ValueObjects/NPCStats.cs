using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Shared.Domain.Combat.ValueObjects
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
