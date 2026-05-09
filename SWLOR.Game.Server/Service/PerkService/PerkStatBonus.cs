using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.PerkService
{
    public delegate int PerkStatBonusCalculation(uint creature);

    public class PerkStatBonus
    {
        private readonly PerkStatBonusCalculation _calculation;

        public StatType Stat { get; }

        public PerkStatBonus(StatType stat, int amount)
            : this(stat, _ => amount)
        {
        }

        public PerkStatBonus(StatType stat, PerkStatBonusCalculation calculation)
        {
            Stat = stat;
            _calculation = calculation ?? throw new ArgumentNullException(nameof(calculation));
        }

        public int Calculate(uint creature)
        {
            return _calculation(creature);
        }
    }
}
