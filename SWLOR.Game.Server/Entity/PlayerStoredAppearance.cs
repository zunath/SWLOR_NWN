using System.Collections.Generic;

namespace SWLOR.Game.Server.Entity
{
    public class PlayerStoredAppearance : EntityBase
    {
        public PlayerStoredAppearance()
        {
            SavedOutfits = new Dictionary<int, string>();
        }

        public override string KeyPrefix => "PlayerStoredAppearance";

        public Dictionary<int, string> SavedOutfits { get; set; }
    }
}
