using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleRest : IRegisteredEvent
    {
        
        private readonly IDialogService _dialog;

        public OnModuleRest( IDialogService dialog)
        {
            
            _dialog = dialog;
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = (_.GetLastPCRested());
            int restType = _.GetLastRestEventType();

            if (restType != _.REST_EVENTTYPE_REST_STARTED ||
                !player.IsValid ||
                player.IsDM) return false;

            player.AssignCommand(() => _.ClearAllActions());

            _dialog.StartConversation(player, player, "RestMenu");
            
            return true;
        }
    }
}
