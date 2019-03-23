using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleLeave : IRegisteredEvent
    {
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

            PlayerService.SaveCharacter(pc);
            PlayerService.SaveLocation(pc);
            ActivityLoggingService.OnModuleClientLeave();
            SkillService.OnModuleClientLeave();
            MapPinService.OnModuleClientLeave();
            MapService.OnModuleLeave();
            SpaceService.OnModuleLeave(pc);

            DataService.RemoveCachedPlayerData(pc); // Ensure this is called LAST.
            return true;

        }
    }
}
