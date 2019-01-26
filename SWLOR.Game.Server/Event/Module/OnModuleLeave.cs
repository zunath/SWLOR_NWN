using NWN;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleLeave : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IPlayerService _player;
        private readonly IActivityLoggingService _activityLogging;
        private readonly ISkillService _skill;
        private readonly IMapPinService _mapPin;
        private readonly IMapService _map;
        private readonly IDataService _data;

        public OnModuleLeave(
            INWScript script,
            IPlayerService player,
            IActivityLoggingService activityLogging,
            ISkillService skill,
            IMapPinService mapPin,
            IMapService map,
            IDataService data)
        {
            _ = script;
            _player = player;
            _activityLogging = activityLogging;
            _skill = skill;
            _mapPin = mapPin;
            _map = map;
            _data = data;
        }

        public bool Run(params object[] args)
        {
            NWPlayer pc = (_.GetExitingObject());

            if (pc.IsDM)
            {
                App.GetAppState().ConnectedDMs.Remove(pc);
            }

            if (pc.IsPlayer)
            {
                _.ExportSingleCharacter(pc.Object);
            }

            _player.SaveCharacter(pc);
            _player.SaveLocation(pc);
            _activityLogging.OnModuleClientLeave();
            _skill.OnModuleClientLeave();
            _mapPin.OnModuleClientLeave();
            _map.OnModuleLeave();

            _data.RemoveCachedPlayerData(pc); // Ensure this is called LAST.
            return true;

        }
    }
}
