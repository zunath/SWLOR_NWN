using System.Linq;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Processor.Contracts;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Processor
{
    public class AppStateProcessor: IEventProcessor
    {
        public void Run()
        {
            foreach (var npcTable in AppCache.NPCEnmityTables.ToArray())
            {
                if (!npcTable.Value.NPCObject.IsValid)
                {
                    AppCache.NPCEnmityTables.Remove(npcTable.Key);
                }
            }
            for (int x = AppCache.NPCBehaviours.Count - 1; x >= 0; x--)
            {
                var npcBehaviour = AppCache.NPCBehaviours.ElementAt(x);
                if (!npcBehaviour.Value.IsValid)
                {
                    ObjectProcessingService.UnregisterProcessingEvent(npcBehaviour.Key);
                    AppCache.NPCBehaviours.Remove(npcBehaviour.Key);
                }
            }
            foreach (var customData in AppCache.CustomObjectData.ToArray())
            {
                NWObject owner = customData.Value.Owner;
                if (!owner.IsValid)
                {
                    AppCache.CustomObjectData.Remove(customData.Key);
                }
            }

        }

        
    }
}
