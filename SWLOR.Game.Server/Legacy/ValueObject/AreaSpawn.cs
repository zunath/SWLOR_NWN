using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Legacy.ValueObject
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

        public AreaSpawn Clone()
        {
            return new AreaSpawn
            {
                SecondsEmpty = SecondsEmpty,
                HasSpawned = HasSpawned,
                Placeables = Placeables.ToList(),
                Creatures = Creatures.ToList()
            };
        }
    }
}
