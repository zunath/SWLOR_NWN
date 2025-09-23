
using SWLOR.Component.Character.Model;

namespace SWLOR.Component.Character.Contracts
{
    public interface IRaceService
    {
        /// <summary>
        /// When the module loads, cache all default race appearances.
        /// </summary>
        void LoadRaces();

        /// <summary>
        /// When a player enters the server, apply the proper scaling to their character.
        /// 
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
        /// <param name="race"></param>
        /// <param name="gender"></param>
        /// <returns></returns>
        RacialAppearance GetDefaultAppearance(object race, object gender);
    }
}