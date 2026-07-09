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
            FavoritePlayerIds = new List<string>();
        }

        [Indexed]
        public string ObserverPlayerId { get; set; }
        public List<string> FavoritePlayerIds { get; set; }
    }
}
