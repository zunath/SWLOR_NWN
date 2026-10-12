using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    public static class BeastCombatStatCalculator
    {
        public static int CalculateAttackBonus(BeastLevel level, Beast beast)
        {
            return CalculateBonus(level.MaxAttackBonus, beast.AttackPurity);
        }

        public static int CalculateAccuracyBonus(BeastLevel level, Beast beast)
        {
            return CalculateBonus(level.MaxAccuracyBonus, beast.AccuracyPurity);
        }

        public static int CalculateEvasionBonus(BeastLevel level, Beast beast)
        {
            return CalculateBonus(level.MaxEvasionBonus, beast.EvasionPurity);
        }

        public static int CalculateAttack(BeastDetail detail, BeastLevel level, Beast beast)
        {
            return Stat.GetAttack(
                beast.Level,
                level.Stats[detail.DamageStat],
                CalculateAttackBonus(level, beast));
        }

        public static int CalculateAccuracy(BeastDetail detail, BeastLevel level, Beast beast)
        {
            return Stat.GetAccuracy(
                beast.Level,
                level.Stats[detail.AccuracyStat],
                CalculateAccuracyBonus(level, beast));
        }

        public static int CalculateEvasion(BeastLevel level, Beast beast)
        {
            return Stat.GetEvasion(
                beast.Level,
                level.Stats[AbilityType.Agility],
                CalculateEvasionBonus(level, beast));
        }

        public static int CalculateDefense(BeastLevel level, Beast beast, CombatDamageType damageType)
        {
            return Stat.CalculateDefense(
                level.Stats[damageType.GetDefenseAbilityType()],
                beast.Level,
                BeastResistanceCalculator.CalculateDefenseBonus(level, beast, damageType));
        }

        private static int CalculateBonus(int max, int purity)
        {
            return (int)(max * (purity * 0.01f));
        }
    }
}
