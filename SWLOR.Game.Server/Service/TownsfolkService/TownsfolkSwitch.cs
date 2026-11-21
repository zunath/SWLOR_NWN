namespace SWLOR.Game.Server.Service.TownsfolkService
{
    /// <summary>
    /// The two Manage City switches for street life. People and wildlife are separate, so a city can keep its streets clear
    /// for roleplay and still keep its critters.
    /// </summary>
    public enum TownsfolkSwitch
    {
        Invalid = 0,

        /// <summary>The wandering townsfolk and the guards.</summary>
        Townsfolk = 1,

        /// <summary>The street critters.</summary>
        Critters = 2
    }
}
