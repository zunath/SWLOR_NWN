using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class ComponentTypeCache: CacheBase<ComponentType>
    {
        private const string ByReassembledResrefIndex = "ByReassembledResref";
        private const string ByReassembledResrefValue = "Active";

        public ComponentTypeCache() 
            : base("ComponentType")
        {
        }

        protected override void OnCacheObjectSet(ComponentType entity)
        {
            SetByHasReassembledResref(entity);
        }

        protected override void OnCacheObjectRemoved(ComponentType entity)
        {
            RemoveByHasReassembledResref(entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        private void SetByHasReassembledResref(ComponentType entity)
        {
            // Entity no longer has a reassembled resref. Remove it from the list.
            if (ExistsInListIndex(ByReassembledResrefIndex, ByReassembledResrefValue, entity) && string.IsNullOrWhiteSpace(entity.ReassembledResref))
                RemoveFromListIndex(ByReassembledResrefIndex, ByReassembledResrefValue, entity);
            // Entity isn't on the list but has a reassembled resref now. Add it to the list.
            else if(!ExistsInListIndex(ByReassembledResrefIndex, ByReassembledResrefValue, entity) && !string.IsNullOrWhiteSpace(entity.ReassembledResref))
                SetIntoListIndex(ByReassembledResrefIndex, ByReassembledResrefValue, entity);
        }

        private void RemoveByHasReassembledResref(ComponentType entity)
        {
            if (string.IsNullOrWhiteSpace(entity.ReassembledResref)) return;

            RemoveFromListIndex(ByReassembledResrefIndex, ByReassembledResrefValue, entity);
        }

        public ComponentType GetByID(int id)
        {
            return ByID(id);
        }

        public IEnumerable<ComponentType> GetAllWhereHasReassembledResref()
        {
            return GetFromListIndex(ByReassembledResrefIndex, ByReassembledResrefValue);
        }
    }
}
