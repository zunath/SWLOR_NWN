namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Where the Engineer routes the ship's power (plan section 10, power routing). Saved on the ship status;
    /// a ship launches Balanced and returns to Balanced when nobody works the Engineer seat.
    /// </summary>
    public enum ShipPowerRouting
    {
        Balanced = 0,
        Weapons = 1,
        Shields = 2,
        Engines = 3,
    }
}
