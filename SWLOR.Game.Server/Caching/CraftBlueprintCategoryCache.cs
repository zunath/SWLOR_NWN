using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class CraftBlueprintCategoryCache: CacheBase<CraftBlueprintCategory>
    {
        // Indexed by CraftBlueprintCategoryID
        // Excludes inactive entries.
        private Dictionary<int, CraftBlueprintCategory> ByCategoryIDActive { get; } = new Dictionary<int, CraftBlueprintCategory>();

        protected override void OnCacheObjectSet(CraftBlueprintCategory entity)
        {
            SetByCategoryIDActive(entity);
        }

        protected override void OnCacheObjectRemoved(CraftBlueprintCategory entity)
        {
            RemoveByCategoryIDActive(entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        private void SetByCategoryIDActive(CraftBlueprintCategory entity)
        {
            if (!entity.IsActive && ByCategoryIDActive.ContainsKey(entity.ID))
                ByCategoryIDActive.Remove(entity.ID);
            else if (entity.IsActive)
                ByCategoryIDActive[entity.ID] = entity;

        }

        private void RemoveByCategoryIDActive(CraftBlueprintCategory entity)
        {
            if (ByCategoryIDActive.ContainsKey(entity.ID))
                ByCategoryIDActive.Remove(entity.ID);
        }

        public CraftBlueprintCategory GetByID(int id)
        {
            return ByID[id];
        }

        public IEnumerable<CraftBlueprintCategory> GetAllActiveByIDs(IEnumerable<int> craftBlueprintCategoryIDs)
        {
            foreach (var id in craftBlueprintCategoryIDs)
            {
                if (ByCategoryIDActive.ContainsKey(id))
                    yield return ByCategoryIDActive[id];
            }
        }
    }
}
