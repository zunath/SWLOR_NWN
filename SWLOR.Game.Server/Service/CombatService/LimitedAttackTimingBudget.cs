namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>Maps a limited effect's matching attempts onto the hands in a timed attack cycle.</summary>
    public readonly record struct LimitedAttackTimingBudget(int RemainingAttacks, bool MainHand, bool OffHand)
    {
        public int CountMatchingRolls(int rolls, int attacksPerCycle)
        {
            if (attacksPerCycle == 1 || MainHand && OffHand)
                return rolls;
            return MainHand ? (rolls + 1) / 2 : rolls / 2;
        }

        public int RollLimit(int attacksPerCycle)
        {
            if (RemainingAttacks <= 0)
                return 0;
            if (attacksPerCycle == 1 || MainHand && OffHand)
                return RemainingAttacks;
            return RemainingAttacks * 2 + (OffHand ? 1 : 0);
        }

        public int WithExtraMatchingRoll(int baselineRolls, int attacksPerCycle)
        {
            var matching = CountMatchingRolls(baselineRolls, attacksPerCycle);
            var rolls = baselineRolls + 1;
            while (CountMatchingRolls(rolls, attacksPerCycle) == matching)
                rolls++;
            return rolls;
        }

        public bool IsExhausted(int rolls, int attacksPerCycle)
        {
            return CountMatchingRolls(rolls, attacksPerCycle) >= RemainingAttacks;
        }
    }
}
