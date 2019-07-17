using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PerkCategoryCache: CacheBase<PerkCategory>
    {
        // Indexed by PerkCategoryID
        // Excludes inactive entries.
        private Dictionary<int, PerkCategory> ByCategoryID { get; } = new Dictionary<int, PerkCategory>();

        protected override void OnCacheObjectSet(PerkCategory entity)
        {
            ByCategoryID[entity.ID] = (PerkCategory)entity.Clone();
        }

        protected override void OnCacheObjectRemoved(PerkCategory entity)
        {
            ByCategoryID.Remove(entity.ID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PerkCategory GetByID(int id)
        {
            return (PerkCategory)ByID[id].Clone();
        }

        public IEnumerable<PerkCategory> GetAllByIDs(IEnumerable<int> perkCategoryIDs)
        {
            var list = new List<PerkCategory>();
            foreach (var perkCategoryID in perkCategoryIDs)
            {
                list.Add((PerkCategory)ByCategoryID[perkCategoryID].Clone());
            }

            return list;
        }
    }
}
