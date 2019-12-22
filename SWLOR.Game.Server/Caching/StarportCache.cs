using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class StarportCache: CacheBase<Starport>
    {
        private Dictionary<string, Dictionary<Guid, Starport>> ByPlanet { get; } = new Dictionary<string, Dictionary<Guid, Starport>>();
        private Dictionary<Guid, Dictionary<int, Starport>> ByStarportID { get; } = new Dictionary<Guid, Dictionary<int, Starport>>();

        protected override void OnCacheObjectSet(string @namespace, object id, Starport entity)
        {
            SetEntityIntoDictionary(entity.PlanetName, entity.StarportID, entity, ByPlanet);
            SetEntityIntoDictionary(entity.StarportID, entity.ID, entity, ByStarportID);
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, Starport entity)
        {
            RemoveEntityFromDictionary(entity.PlanetName, entity.StarportID, ByPlanet);
            RemoveEntityFromDictionary(entity.StarportID, entity.ID, ByStarportID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public Starport GetByID(int id)
        {
            return ByID(id);
        }

        public Starport GetByIDOrDefault(int id)
        {
            if(!Exists(id))
            {
                return default;
            }

            return ByID(id);
        }

        public Starport GetByStarportID(Guid starportID)
        {
            return ByStarportID[starportID].Values.Single();
        }

        public Starport GetByStarportIDOrDefault(Guid starportID)
        {
            if (!ByStarportID.ContainsKey(starportID))
            {
                return default;
            }

            return ByStarportID[starportID].Values.SingleOrDefault();
        }

        public Starport GetByPlanetNameOrDefault(string planet)
        {
            return ByPlanet[planet].Values.FirstOrDefault();
        }
    }
}
