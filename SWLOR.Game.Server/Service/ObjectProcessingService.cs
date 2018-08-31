using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class ObjectProcessingService : IObjectProcessingService
    {
        private readonly INWScript _;
        private readonly AppState _state;
        private readonly IErrorService _error;

        public ObjectProcessingService(INWScript script,
            AppState state,
            IErrorService error)
        {
            _ = script;
            _state = state;
            _error = error;
        }

        public void OnModuleLoad()
        {
            NWArea area = NWArea.Wrap(_.GetFirstArea());
            while (area.IsValid)
            {
                NWObject @object = NWObject.Wrap(_.GetFirstObjectInArea(area.Object));
                while (@object.IsValid)
                {
                    HandleSpawnWaypointRename(@object);

                    @object = NWObject.Wrap(_.GetNextObjectInArea(area.Object));
                }

                area = NWArea.Wrap(_.GetNextArea());
            }

            RunProcessor();
        }

        public float ProcessingTickInterval => 1f;

        private void RunProcessor()
        {
            foreach (var toUnregister in _state.UnregisterProcessingEvents)
            {
                _state.ProcessingEvents.Remove(toUnregister);
            }
            _state.UnregisterProcessingEvents.Clear();

            foreach (var @event in _state.ProcessingEvents)
            {
                try
                {
                    @event.Value.Invoke();
                }
                catch (Exception ex)
                {
                    _error.LogError(ex, "ObjectProcessingService. EventID = " + @event.Key);
                }
                
            }

            _.DelayCommand(ProcessingTickInterval, RunProcessor);
        }

        public string RegisterProcessingEvent(Action action)
        {
            string globalID = Guid.NewGuid().ToString("N");
            _state.ProcessingEvents.Add(globalID, action);
            return globalID;
        }

        public void UnregisterProcessingEvent(string globalID)
        {
            if (_state.ProcessingEvents.ContainsKey(globalID))
            {
                _state.UnregisterProcessingEvents.Enqueue(globalID);
            }
        }

        // It's difficult to see what waypoint represents what in the toolset.
        // To fix this, we rename the waypoints on module load so that they function in-game.
        private void HandleSpawnWaypointRename(NWObject obj)
        {
            if (obj.ObjectType != NWScript.OBJECT_TYPE_WAYPOINT) return;

            string name = obj.Name;
            
            
            string[] split = name.Split(new[] {"SP_"}, StringSplitOptions.None);
            
            if (split.Length <= 1) return;

            name = "SP_" + split[split.Length - 1];
            name = name.Trim();
            
            obj.Name = name;
        }
    }
}
