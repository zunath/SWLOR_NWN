using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PerkCategoryCache: CacheBase<PerkCategory>
    {
        public PerkCategoryCache() 
            : base("PerkCategory")
        {
        }

        private const string ByCategoryIDIndex = "ByCategoryID";

        protected override void OnCacheObjectSet(PerkCategory entity)
        {
            SetIntoIndex(ByCategoryIDIndex, entity.ID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(PerkCategory entity)
        {
            RemoveFromIndex(ByCategoryIDIndex, entity.ID.ToString());
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PerkCategory GetByID(int id)
        {
            return ByID(id);
        }

        public IEnumerable<PerkCategory> GetAllByIDs(IEnumerable<int> perkCategoryIDs)
        {
            var list = new List<PerkCategory>();
            foreach (var perkCategoryID in perkCategoryIDs)
            {
                list.Add(GetFromIndex(ByCategoryIDIndex, perkCategoryID.ToString()));
            }

            return list;
        }
    }
}
