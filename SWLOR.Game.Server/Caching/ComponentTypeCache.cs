using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class ComponentTypeCache: CacheBase<ComponentType>
    {
        private Dictionary<int, ComponentType> ByHasReassembledResref { get; } = new Dictionary<int, ComponentType>();

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
            if (ByHasReassembledResref.ContainsKey(entity.ID) && string.IsNullOrWhiteSpace(entity.ReassembledResref))
                ByHasReassembledResref.Remove(entity.ID);
            // Entity isn't on the list but has a reassembled resref now. Add it to the list.
            else if(!ByHasReassembledResref.ContainsKey(entity.ID) && !string.IsNullOrWhiteSpace(entity.ReassembledResref))
                ByHasReassembledResref[entity.ID] = (ComponentType)entity.Clone();
        }

        private void RemoveByHasReassembledResref(ComponentType entity)
        {
            ByHasReassembledResref.Remove(entity.ID);
        }

        public ComponentType GetByID(int id)
        {
            return (ComponentType)ByID[id].Clone();
        }

        public IEnumerable<ComponentType> GetAllWhereHasReassembledResref()
        {
            var list = new List<ComponentType>();
            foreach (var record in ByHasReassembledResref.Values)
            {
                list.Add( (ComponentType)record.Clone());
            }

            return list;
        }
    }
}
