using System.Collections.Generic;
using SWLOR.Game.Server.Service.DMToolService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// The blueprints a DM saved as favorites in the DM Creator. Like DMPreferences, these follow the DM's public
    /// CD key onto any avatar and onto creatures they possess.
    /// </summary>
    public class DMCreatorFavorites : EntityBase
    {
        public List<DMFavoriteBlueprint> Blueprints { get; set; }

        public DMCreatorFavorites()
        {
            Blueprints = new List<DMFavoriteBlueprint>();
        }
    }
}
