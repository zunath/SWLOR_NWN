namespace SWLOR.Game.Server.Service.CombatService
{
    public enum CombatDamageType
    {
        Invalid = 0,

        // Characters
        Physical = 1,
        Force = 2,
        Fire = 3,
        Poison = 4,
        Electrical = 5,
        Ice = 6,

        // Ships
        Thermal = 20,
        Explosive = 21,
        EM = 22,
    }
}
