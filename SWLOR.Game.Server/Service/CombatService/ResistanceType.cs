namespace SWLOR.Game.Server.Service.CombatService
{
    public enum ResistanceType
    {
        Invalid = 0,

        // Elemental damage and elemental status effects: 1-99.
        Fire = 1,
        Poison = 2,
        Electrical = 3,
        Ice = 4,

        // Status effect families: 100-199.
        Mind = 100,
        Mobility = 101,
        Trauma = 102,
        Disruption = 103,
    }
}
