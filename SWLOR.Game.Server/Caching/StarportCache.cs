using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class StarportCache: CacheBase<Starport>
    {
        public StarportCache() 
            : base("Starport")
        {
        }

        private const string ByPlanetIndex = "ByPlanet";
        private const string ByStarportIDIndex = "ByStarportID";

        protected override void OnCacheObjectSet(Starport entity)
        {
            SetIntoIndex(ByPlanetIndex, entity.PlanetName, entity);
            SetIntoIndex(ByStarportIDIndex, entity.StarportID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(Starport entity)
        {
            RemoveFromIndex(ByPlanetIndex, entity.PlanetName);
            RemoveFromIndex(ByStarportIDIndex, entity.StarportID.ToString());
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
            return GetFromIndex(ByStarportIDIndex, starportID.ToString());
        }

        public Starport GetByStarportIDOrDefault(Guid starportID)
        {
            if (!ExistsByIndex(ByStarportIDIndex, starportID.ToString()))
            {
                return default;
            }

            return GetFromIndex(ByStarportIDIndex, starportID.ToString());
        }

        public Starport GetByPlanetNameOrDefault(string planet)
        {
            if (!ExistsByIndex(ByPlanetIndex, planet))
                return default;

            return GetFromIndex(ByPlanetIndex, planet);
        }
    }
}
