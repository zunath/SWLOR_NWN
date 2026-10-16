namespace SWLOR.CombatSim.Effects
{
    public enum EffectKind
    {
        DirectHeal,
        HealOverTime,
        PartyHealOverTime,
        TemporaryHp,
        PartyTemporaryHp,
        Revive,

        /// <summary>A status on the caster whose stats apply while it lasts, such as a stance.</summary>
        SelfStatus
    }
}
