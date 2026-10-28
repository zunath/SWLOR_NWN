namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A seat aboard a ship (plan section 10). Large hulls have the Pilot, a Gunner and the Engineer; Capital hulls
    /// add a second Gunner and Sensors. Small and Medium hulls have the Pilot only. Seats are held in memory while a
    /// ship flies and are never saved. The droid bay (plan section 27) is a droid-only post on Mining Barge,
    /// Industrial and Logistics hulls, never a player's seat.
    /// </summary>
    public enum ShipCrewSeat
    {
        Invalid = 0,
        Pilot = 1,
        Gunner = 2,
        SecondGunner = 3,
        Engineer = 4,
        Sensors = 5,
        DroidBay = 6,
    }
}
