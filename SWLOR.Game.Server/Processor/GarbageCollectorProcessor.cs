using System;
using SWLOR.Game.Server.Processor.Contracts;

namespace SWLOR.Game.Server.Processor
{
    public class GarbageCollectorProcessor: IEventProcessor
    {
        public void Run(object[] args)
        {
            Console.WriteLine("======================================================");
            Console.WriteLine("PlayerDialogs: " + AppCache.PlayerDialogs.Count);
            Console.WriteLine("DialogFilesInUse: " + AppCache.DialogFilesInUse.Count);
            Console.WriteLine("EffectTicks: " + AppCache.EffectTicks.Count);
            Console.WriteLine("CreatureSkillRegistrations: " + AppCache.CreatureSkillRegistrations.Count);
            Console.WriteLine("NPCEffects: " + AppCache.NPCEffects.Count);
            Console.WriteLine("UnregisterProcessingEvents: " + AppCache.UnregisterProcessingEvents.Count);
            Console.WriteLine("NPCEnmityTables: " + AppCache.NPCEnmityTables.Count);
            Console.WriteLine("CustomObjectData: " + AppCache.CustomObjectData.Count);
            Console.WriteLine("NPCBehaviours: " + AppCache.NPCBehaviours.Count);
            Console.WriteLine("AreaSpawns: " + AppCache.AreaSpawns.Count);
            Console.WriteLine("VisibilityObjects: " + AppCache.VisibilityObjects.Count);
            Console.WriteLine("PCEffectsForRemoval: " + AppCache.PCEffectsForRemoval.Count);
            Console.WriteLine("======================================================");
            long memoryInUse = GC.GetTotalMemory(true);
            Console.WriteLine("Memory In Use = " + memoryInUse);

        }
    }
}
