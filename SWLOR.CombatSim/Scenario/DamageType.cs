namespace SWLOR.CombatSim.Scenario
{
    /// <summary>The damage types the simulator tells apart: damage reductions in the game target physical or Force damage.</summary>
    public enum DamageType
    {
        Physical,
        Force,

        /// <summary>Fire, electrical, sonic, poison and the like.</summary>
        Other
    }
}
