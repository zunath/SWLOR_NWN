using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class SpaceEncounterCache: CacheBase<SpaceEncounter>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, SpaceEncounter entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, SpaceEncounter entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }
        public SpaceEncounter GetByID(int id)
        {
            return ByID(id);
        }

        public IEnumerable<SpaceEncounter> GetAllByPlanet(string planet)
        {
            var list = new List<SpaceEncounter>();
            foreach (var record in ByID.Values.Where(x => x.Planet == planet))
            {
                list.Add((SpaceEncounter)record.Clone());
            }

            return list;
        }
    }
}
