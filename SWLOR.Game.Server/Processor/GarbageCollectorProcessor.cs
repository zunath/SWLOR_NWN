using System;
using SWLOR.Game.Server.Processor.Contracts;

namespace SWLOR.Game.Server.Processor
{
    public class GarbageCollectorProcessor: IEventProcessor
    {
        private readonly AppCache _cache;

        public GarbageCollectorProcessor(AppCache cache)
        {
            _cache = cache;
        }

        public void Run(object[] args)
        {
            Console.WriteLine("======================================================");
            Console.WriteLine("PlayerDialogs: " + _cache.PlayerDialogs.Count);
            Console.WriteLine("DialogFilesInUse: " + _cache.DialogFilesInUse.Count);
            Console.WriteLine("EffectTicks: " + _cache.EffectTicks.Count);
            Console.WriteLine("CreatureSkillRegistrations: " + _cache.CreatureSkillRegistrations.Count);
            Console.WriteLine("NPCEffects: " + _cache.NPCEffects.Count);
            Console.WriteLine("UnregisterProcessingEvents: " + _cache.UnregisterProcessingEvents.Count);
            Console.WriteLine("NPCEnmityTables: " + _cache.NPCEnmityTables.Count);
            Console.WriteLine("CustomObjectData: " + _cache.CustomObjectData.Count);
            Console.WriteLine("NPCBehaviours: " + _cache.NPCBehaviours.Count);
            Console.WriteLine("AreaSpawns: " + _cache.AreaSpawns.Count);
            Console.WriteLine("VisibilityObjects: " + _cache.VisibilityObjects.Count);
            Console.WriteLine("PCEffectsForRemoval: " + _cache.PCEffectsForRemoval.Count);
            Console.WriteLine("======================================================");
            long memoryInUse = GC.GetTotalMemory(true);
            Console.WriteLine("Memory In Use = " + memoryInUse);

        }
    }
}
