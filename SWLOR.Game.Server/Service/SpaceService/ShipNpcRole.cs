namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The role of a preset NPC ship (Space Enemies, Roles). It picks the shared behavior the ship AI runs.
    /// </summary>
    public enum ShipNpcRole
    {
        Invalid = 0,
        Fighter = 1,
        Interceptor = 2,
        Bomber = 3,
        Sniper = 4,
        ElectronicWarfare = 5,
        Gunship = 6,
        Logistics = 7,
        Carrier = 8,
        Minelayer = 9,
    }
}
