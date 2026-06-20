using System.Collections.Generic;

namespace SWLOR.Game.Server.Entity
{
    public class PlayerKnownName : EntityBase
    {
        public PlayerKnownName()
        {
            Init();
        }

        public PlayerKnownName(string observerPlayerId)
        {
            Init();
            ObserverPlayerId = observerPlayerId;
        }

        private void Init()
        {
            ObserverPlayerId = string.Empty;
            KnownNames = new Dictionary<string, string>();
        }

        [Indexed]
        public string ObserverPlayerId { get; set; }
        public Dictionary<string, string> KnownNames { get; set; }
    }
}
