namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    /// <summary>
    /// What produced a recorded damage, healing, temporary HP or resource change.
    /// </summary>
    public enum CombatRecordSourceKind : byte
    {
        Unattributed = 0,
        Ability = 1,
        AbilityCost = 2,
        Autoattack = 3,
        StatusEffect = 4,
        Lifesteal = 5,
        OnKill = 6,
        Regeneration = 7,
        Rest = 8,
        Item = 9,
        StatTrigger = 10,
        DamageShare = 11,
        Respawn = 12,
        Subdual = 13,
        DungeonMaster = 14
    }
}
