using System.Linq;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class AppStateService : IAppStateService
    {
        private readonly IObjectProcessingService _ops;
        private readonly AppState _state;
        
        public AppStateService(IObjectProcessingService ops,
            AppState state)
        {
            _ops = ops;
            _state = state;
        }

        public void OnModuleLoad()
        {
            _ops.RegisterProcessingEvent(() =>
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
                
            });
        }
    }
}
