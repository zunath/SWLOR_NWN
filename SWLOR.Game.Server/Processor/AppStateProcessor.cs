using System.Linq;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Processor.Contracts;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Processor
{
    public class AppStateProcessor: IEventProcessor
    {
        private readonly AppState _state;
        private readonly IObjectProcessingService _ops;

        public AppStateProcessor(AppState state, IObjectProcessingService ops)
        {
            _state = state;
            _ops = ops;
        }

        public void Run(object[] args)
        {
            foreach (var npcTable in _state.NPCEnmityTables.ToArray())
            {
                if (!npcTable.Value.NPCObject.IsValid)
                {
                    _state.NPCEnmityTables.Remove(npcTable.Key);
                }
            }
            for (int x = _state.NPCBehaviours.Count - 1; x >= 0; x--)
            {
                var npcBehaviour = _state.NPCBehaviours.ElementAt(x);
                if (!npcBehaviour.Value.IsValid)
                {
                    _state.NPCBehaviours.Remove(npcBehaviour.Key);
                    _ops.UnregisterProcessingEvent(npcBehaviour.Key);
                }
            }
            foreach (var customData in _state.CustomObjectData.ToArray())
            {
                NWObject owner = customData.Value.Owner;
                if (!owner.IsValid)
                {
                    _state.CustomObjectData.Remove(customData.Key);
                }
            }

        }


    }
}
