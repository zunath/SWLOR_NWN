namespace SWLOR.CombatSim.Engine
{
    public enum HealKind
    {
        InstantAbility,

        /// <summary>A share of the damage an ability dealt, such as Force Drain's; not an instant ability heal.</summary>
        DamageDerived,
        OverTime,
        NaturalRegeneration,
        Revive
    }
}
