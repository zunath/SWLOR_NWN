namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The environment of a Wild Space system (plan section 23), stored on the area as the local int
    /// WILD_SPACE_ENVIRONMENT. Each puts stat changes on every ship in the system.
    /// </summary>
    public enum WildSpaceEnvironment
    {
        Invalid = 0,
        Pulsar = 1,
        BlackHole = 2,
        IonStorm = 3,
        Magnetar = 4,
        RedGiant = 5,
        ShipGraveyard = 6,
    }
}
