using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class ComponentTypeCache: CacheBase<ComponentType>
    {
        private List<ComponentType> ByHasReassembledResref { get; } = new List<ComponentType>();

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
            if (ByHasReassembledResref.Contains(entity) && string.IsNullOrWhiteSpace(entity.ReassembledResref))
                ByHasReassembledResref.Remove(entity);
            // Entity isn't on the list but has a reassembled resref now. Add it to the list.
            else if(!ByHasReassembledResref.Contains(entity) && !string.IsNullOrWhiteSpace(entity.ReassembledResref))
                ByHasReassembledResref.Add(entity);
        }

        private void RemoveByHasReassembledResref(ComponentType entity)
        {
            ByHasReassembledResref.Remove(entity);
        }

        public ComponentType GetByID(int id)
        {
            return ByID[id];
        }

        public IEnumerable<ComponentType> GetAllWhereHasReassembledResref()
        {
            return ByHasReassembledResref;
        }
    }
}
