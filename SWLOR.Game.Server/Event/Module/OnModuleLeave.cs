using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleLeave : IRegisteredEvent
    {
        
        private readonly IPlayerService _player;
        
        private readonly IMapPinService _mapPin;
        private readonly IMapService _map;
        
        private readonly ISpaceService _space;

        public OnModuleLeave(
            
            IPlayerService player,
            
            IMapPinService mapPin,
            IMapService map,
            
            ISpaceService space)
        {
            
            _player = player;
            
            _mapPin = mapPin;
            _map = map;
            
            _space = space;
        }

        public bool Run(params object[] args)
        {
            NWPlayer pc = (_.GetExitingObject());

            if (pc.IsDM)
            {
                AppCache.ConnectedDMs.Remove(pc);
            }

            if (pc.IsPlayer)
            {
                _.ExportSingleCharacter(pc.Object);
            }

            _player.SaveCharacter(pc);
            _player.SaveLocation(pc);
            ActivityLoggingService.OnModuleClientLeave();
            SkillService.OnModuleClientLeave();
            _mapPin.OnModuleClientLeave();
            _map.OnModuleLeave();
            _space.OnModuleLeave(pc);

            DataService.RemoveCachedPlayerData(pc); // Ensure this is called LAST.
            return true;

        }
    }
}
