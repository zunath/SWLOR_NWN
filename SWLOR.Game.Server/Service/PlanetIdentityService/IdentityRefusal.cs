namespace SWLOR.Game.Server.Service.PlanetIdentityService
{
    /// <summary>Why a planet identity recipe cannot be crafted where the crafter stands.</summary>
    public enum IdentityRefusal
    {
        /// <summary>The recipe may be crafted here.</summary>
        None = 0,

        /// <summary>The crafter is not in a player city at all.</summary>
        NotInCity = 1,

        /// <summary>The crafter is in a planet city on another planet, or on a planet the game does not recognize.</summary>
        WrongPlanet = 2,

        /// <summary>The recipe needs a planet city, and the crafter is on a player station.</summary>
        OnStation = 3,

        /// <summary>The recipe needs a player station, and the crafter is in a planet city.</summary>
        NotOnStation = 4
    }
}
