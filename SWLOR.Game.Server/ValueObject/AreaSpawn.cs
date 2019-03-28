using System.Collections.Generic;

namespace SWLOR.Game.Server.ValueObject
{
    public class AreaSpawn
    {
        public List<ObjectSpawn> Placeables { get; set; }
        public List<ObjectSpawn> Creatures { get; set; }
        public float SecondsEmpty { get; set; }
        public bool HasSpawned { get; set; }

        public AreaSpawn()
        {
            Placeables = new List<ObjectSpawn>();
            Creatures = new List<ObjectSpawn>();
        }
    }
}
