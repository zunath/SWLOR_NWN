using System.Collections.Generic;
using SWLOR.Game.Server.Service.DMToolService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// The saved creatures a DM marked as favorites in the Creature Manager. Like DMPreferences, these follow the
    /// DM's public CD key onto any avatar and onto creatures they possess.
    /// </summary>
    public class DMCreatureManagerFavorites : EntityBase
    {
        public List<DMSavedCreatureFavorite> Creatures { get; set; }

        public DMCreatureManagerFavorites()
        {
            Creatures = new List<DMSavedCreatureFavorite>();
        }
    }
}
