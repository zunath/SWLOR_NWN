using System;
using System.Linq;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Processor.Contracts;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Processor
{
    public class AppStateProcessor: IEventProcessor
    {
        private readonly AppCache _cache;
        private readonly IObjectProcessingService _ops;

        public AppStateProcessor(AppCache cache, IObjectProcessingService ops)
        {
            _cache = cache;
            _ops = ops;
        }

        public void Run(object[] args)
        {
            foreach (var npcTable in _cache.NPCEnmityTables.ToArray())
            {
                if (!npcTable.Value.NPCObject.IsValid)
                {
                    _cache.NPCEnmityTables.Remove(npcTable.Key);
                }
            }
            for (int x = _cache.NPCBehaviours.Count - 1; x >= 0; x--)
            {
                var npcBehaviour = _cache.NPCBehaviours.ElementAt(x);
                if (!npcBehaviour.Value.IsValid)
                {
                    _cache.NPCBehaviours.Remove(npcBehaviour.Key);
                    _ops.UnregisterProcessingEvent(npcBehaviour.Key);
                }
            }
            foreach (var customData in _cache.CustomObjectData.ToArray())
            {
                NWObject owner = customData.Value.Owner;
                if (!owner.IsValid)
                {
                    _cache.CustomObjectData.Remove(customData.Key);
                }
            }

        }

        
    }
}
