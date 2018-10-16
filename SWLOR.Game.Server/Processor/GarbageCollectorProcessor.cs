using System;
using SWLOR.Game.Server.Processor.Contracts;

namespace SWLOR.Game.Server.Processor
{
    public class GarbageCollectorProcessor: IEventProcessor
    {
        private readonly AppState _state;

        public GarbageCollectorProcessor(AppState state)
        {
            _state = state;
        }

        public void Run(object[] args)
        {
            Console.WriteLine("======================================================");
            Console.WriteLine("PlayerDialogs: " + _state.PlayerDialogs.Count);
            Console.WriteLine("DialogFilesInUse: " + _state.DialogFilesInUse.Count);
            Console.WriteLine("EffectTicks: " + _state.EffectTicks.Count);
            Console.WriteLine("CreatureSkillRegistrations: " + _state.CreatureSkillRegistrations.Count);
            Console.WriteLine("NPCEffects: " + _state.NPCEffects.Count);
            Console.WriteLine("UnregisterProcessingEvents: " + _state.UnregisterProcessingEvents.Count);
            Console.WriteLine("NPCEnmityTables: " + _state.NPCEnmityTables.Count);
            Console.WriteLine("CustomObjectData: " + _state.CustomObjectData.Count);
            Console.WriteLine("NPCBehaviours: " + _state.NPCBehaviours.Count);
            Console.WriteLine("AreaSpawns: " + _state.AreaSpawns.Count);
            Console.WriteLine("VisibilityObjects: " + _state.VisibilityObjects.Count);
            Console.WriteLine("PCEffectsForRemoval: " + _state.PCEffectsForRemoval.Count);
            Console.WriteLine("======================================================");
            long memoryInUse = GC.GetTotalMemory(true);
            Console.WriteLine("Memory In Use = " + memoryInUse);

        }
    }
}
