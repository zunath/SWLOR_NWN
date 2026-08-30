using System.Collections.Generic;

namespace SWLOR.Game.Server.Entity
{
    public class HoloComFavorite: EntityBase
    {
        public HoloComFavorite()
        {
            Init();
        }

        public HoloComFavorite(string observerPlayerId)
        {
            Init();
            ObserverPlayerId = observerPlayerId;
        }

        private void Init()
        {
            ObserverPlayerId = string.Empty;
            Favorites = new List<HoloComFavoriteEntry>();
        }

        [Indexed]
        public string ObserverPlayerId { get; set; }
        public List<HoloComFavoriteEntry> Favorites { get; set; }
    }

    public class HoloComFavoriteEntry
    {
        public string IdentityKey { get; set; }
        public string Descriptor { get; set; }
        public string FallbackName { get; set; }

        public HoloComFavoriteEntry()
        {
            IdentityKey = string.Empty;
            Descriptor = string.Empty;
            FallbackName = string.Empty;
        }
    }
}
