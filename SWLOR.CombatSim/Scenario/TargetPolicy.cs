namespace SWLOR.CombatSim.Scenario
{
    public enum TargetPolicy
    {
        Random,
        LowestHp,
        Focus,

        /// <summary>The whole team shares one target, picked as the lowest HP percent among a random sample of three enemies.</summary>
        Coordinated
    }
}
