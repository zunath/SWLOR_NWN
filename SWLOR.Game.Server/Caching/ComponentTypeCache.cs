using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class ComponentTypeCache: CacheBase<ComponentType>
    {
        private const string HasReassembledResrefIndex = "HasReassembledResref";
        private const string HasReassembledResrefValue = "HasValue";

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
            if (ExistsByIndex(HasReassembledResrefIndex, HasReassembledResrefValue) && string.IsNullOrWhiteSpace(entity.ReassembledResref))
                RemoveFromListIndex(HasReassembledResrefIndex, HasReassembledResrefValue, entity);
            // Entity isn't on the list but has a reassembled resref now. Add it to the list.
            else if(!ExistsByIndex(HasReassembledResrefIndex, HasReassembledResrefValue) && !string.IsNullOrWhiteSpace(entity.ReassembledResref))
                SetIntoListIndex(HasReassembledResrefIndex, HasReassembledResrefValue, entity);
        }

        private void RemoveByHasReassembledResref(ComponentType entity)
        {
            if (string.IsNullOrWhiteSpace(entity.ReassembledResref)) return;

            RemoveFromListIndex(HasReassembledResrefIndex, HasReassembledResrefValue, entity);
        }

        public ComponentType GetByID(int id)
        {
            return ByID(id);
        }

        public IEnumerable<ComponentType> GetAllWhereHasReassembledResref()
        {
            return GetFromListIndex(HasReassembledResrefIndex, "");
        }
    }
}
