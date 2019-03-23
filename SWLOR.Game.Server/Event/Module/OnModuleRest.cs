using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleRest : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlayer player = (_.GetLastPCRested());
            int restType = _.GetLastRestEventType();

            if (restType != _.REST_EVENTTYPE_REST_STARTED ||
                !player.IsValid ||
                player.IsDM) return false;

            player.AssignCommand(() => _.ClearAllActions());

            DialogService.StartConversation(player, player, "RestMenu");
            
            return true;
        }
    }
}
