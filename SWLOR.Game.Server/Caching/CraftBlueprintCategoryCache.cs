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
                ByCategoryIDActive[entity.ID] = (CraftBlueprintCategory)entity.Clone();

        }

        private void RemoveByCategoryIDActive(CraftBlueprintCategory entity)
        {
            if (ByCategoryIDActive.ContainsKey(entity.ID))
                ByCategoryIDActive.Remove(entity.ID);
        }

        public CraftBlueprintCategory GetByID(int id)
        {
            return (CraftBlueprintCategory)ByID[id].Clone();
        }

        public IEnumerable<CraftBlueprintCategory> GetAllActiveByIDs(IEnumerable<int> craftBlueprintCategoryIDs)
        {
            var list = new List<CraftBlueprintCategory>();
            foreach (var id in craftBlueprintCategoryIDs)
            {
                list.Add( (CraftBlueprintCategory)ByCategoryIDActive[id].Clone());
            }

            return list;
        }
    }
}
