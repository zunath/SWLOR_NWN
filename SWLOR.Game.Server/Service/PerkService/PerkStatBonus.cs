using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.PerkService
{
    public delegate int PerkStatBonusCalculation(uint creature);

    public class PerkStatBonus
    {
        private readonly PerkStatBonusCalculation _calculation;

        public StatType Stat { get; }
        public SkillType CraftingSkill { get; }

        public PerkStatBonus(StatType stat, int amount)
            : this(stat, _ => amount)
        {
        }

        public PerkStatBonus(StatType stat, int amount, SkillType craftingSkill) : this(stat, _ => amount, craftingSkill) { }

        public PerkStatBonus(StatType stat, PerkStatBonusCalculation calculation, SkillType craftingSkill = SkillType.Invalid)
        {
            Stat = stat;
            CraftingSkill = craftingSkill;
            _calculation = calculation ?? throw new ArgumentNullException(nameof(calculation));
        }

        public int Calculate(uint creature)
        {
            return _calculation(creature);
        }
    }
}
