using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class SpaceEncounterCache: CacheBase<SpaceEncounter>
    {
        public SpaceEncounterCache() 
            : base("SpaceEncounter")
        {
        }

        protected override void OnCacheObjectSet(SpaceEncounter entity)
        {
        }

        protected override void OnCacheObjectRemoved(SpaceEncounter entity)
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
            return GetAll().Where(x => x.Planet == planet);
        }
    }
}
