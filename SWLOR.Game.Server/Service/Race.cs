using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Service.RaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the racial appearances and race rules. The logic lives in <see cref="IRaceService"/>;
    /// the event handlers stay here and every member forwards to the container's instance.
    /// </summary>
    public static class Race
    {
        private static IRaceService Instance => FacadeAccessor.Resolve<IRaceService>();

        /// <summary>
        /// When the module loads, cache all default race appearances.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void LoadRaces()
        {
            Instance.LoadRaces();
        }

        /// <summary>
        /// When a player enters the server, apply the proper scaling to their character.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ApplyWookieeScaling()
        {
            Instance.ApplyWookieeScaling();
        }

        /// <summary>
        /// Sets the default race appearance for the player's racial type.
        /// This should be called exactly one time on player initialization.
        /// </summary>
        /// <param name="player">The player whose appearance will be adjusted.</param>
        public static void SetDefaultRaceAppearance(uint player)
        {
            Instance.SetDefaultRaceAppearance(player);
        }

        /// <summary>
        /// Retrieves the default racial appearance of a specific race.
        /// </summary>
        /// <param name="race">The racial type.</param>
        /// <param name="gender">The gender.</param>
        public static RacialAppearance GetDefaultAppearance(RacialType race, Gender gender)
        {
            return Instance.GetDefaultAppearance(race, gender);
        }

        public static bool IsClassAvailableToRace(ClassType classType, RacialType race)
        {
            return Instance.IsClassAvailableToRace(classType, race);
        }
    }
}
