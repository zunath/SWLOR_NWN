using SWLOR.Game.Server.Service.RaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Domain.Character
{
    public interface IRaceService
    {
        /// <summary>
        /// When the module loads, cache all default race appearances.
        /// </summary>
        void LoadRaces();

        /// <summary>
        /// When a player enters the server, apply the proper scaling to their character.
        /// </summary>
        void ApplyWookieeScaling();

        /// <summary>
        /// Sets the default race appearance for the player's racial type.
        /// This should be called exactly one time on player initialization.
        /// </summary>
        /// <param name="player">The player whose appearance will be adjusted.</param>
        void SetDefaultRaceAppearance(uint player);

        /// <summary>
        /// Retrieves the default racial appearance of a specific race.
        /// </summary>
        /// <param name="race">The racial type.</param>
        /// <param name="gender">The gender.</param>
        RacialAppearance GetDefaultAppearance(RacialType race, Gender gender);

        bool IsClassAvailableToRace(ClassType classType, RacialType race);
    }
}
