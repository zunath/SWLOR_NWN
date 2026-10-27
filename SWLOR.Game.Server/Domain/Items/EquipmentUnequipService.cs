using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Domain.Items
{
    /// <summary>
    /// Lets equipment owners reserve safe removal before any shared equipment hooks run: the logic and state that used
    /// to be static in <c>Service.EquipmentUnequip</c>, moved here unchanged. The facade still carries the event handler
    /// and forwards every call to the container's instance.
    /// </summary>
    public class EquipmentUnequipService : IEquipmentUnequipService
    {
        private readonly Dictionary<string, Func<uint, uint, bool>> _guards = new(StringComparer.Ordinal);

        public void RegisterGuard(string key, Func<uint, uint, bool> guard)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(guard);
            _guards[key] = guard;
        }

        public bool TryHandle(uint creature, uint item)
        {
            foreach (var guard in _guards.Values)
                if (guard(creature, item)) return true;
            return false;
        }

        public void BeforeUnequip()
        {
            var creature = OBJECT_SELF;
            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            if (TryHandle(creature, item))
            {
                EventsPlugin.SkipEvent();
                return;
            }
            ExecuteScript(ScriptName.OnItemUnequipBefore, creature);
        }
    }
}
