using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class CraftBlueprintCategoryCache: CacheBase<CraftBlueprintCategory>
    {
        public CraftBlueprintCategoryCache() 
            : base("CraftBlueprintCategory")
        {
        }

        private const string CategoryIDActiveIndex = "CategoryID";
        private const string CategoryIDActiveValue = "Active";
        
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
            if (!entity.IsActive && ExistsInListIndex(CategoryIDActiveIndex, CategoryIDActiveValue, entity))
                RemoveFromListIndex(CategoryIDActiveIndex, CategoryIDActiveValue, entity);
            else if (entity.IsActive)
                SetIntoListIndex(CategoryIDActiveIndex, CategoryIDActiveValue, entity);

        }

        private void RemoveByCategoryIDActive(CraftBlueprintCategory entity)
        {
            if (ExistsInListIndex(CategoryIDActiveIndex, CategoryIDActiveValue, entity))
                RemoveFromListIndex(CategoryIDActiveIndex, CategoryIDActiveValue, entity);
        }

        public CraftBlueprintCategory GetByID(int id)
        {
            return ByID(id);
        }

        public IEnumerable<CraftBlueprintCategory> GetAllActiveByIDs(IEnumerable<int> craftBlueprintCategoryIDs)
        {
            return GetFromListIndex(CategoryIDActiveIndex, CategoryIDActiveValue);
        }
    }
}
