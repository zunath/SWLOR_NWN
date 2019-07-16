using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class SpaceStarportCache: CacheBase<SpaceStarport>
    {
        private Dictionary<string, Dictionary<Guid, SpaceStarport>> ByPlanet { get; } = new Dictionary<string, Dictionary<Guid, SpaceStarport>>();

        protected override void OnCacheObjectSet(SpaceStarport entity)
        {
            SetEntityIntoDictionary(entity.Planet, entity.ID, entity, ByPlanet);
        }

        protected override void OnCacheObjectRemoved(SpaceStarport entity)
        {
            RemoveEntityFromDictionary(entity.Planet, entity.ID, ByPlanet);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public SpaceStarport GetByID(Guid id)
        {
            return ByID[id];
        }

        public SpaceStarport GetByIDOrDefault(Guid id)
        {
            if(!ByID.ContainsKey(id))
            {
                return default;
            }

            return ByID[id];
        }

        public IEnumerable<SpaceStarport> GetAllByPlanet(string planet)
        {
            if(!ByPlanet.ContainsKey(planet))
                return new List<SpaceStarport>();

            return ByPlanet[planet].Values;
        }
    }
}
