using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleRest : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDialogService _dialog;

        public OnModuleRest(INWScript script, IDialogService dialog)
        {
            _ = script;
            _dialog = dialog;
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = (_.GetLastPCRested());
            int restType = _.GetLastRestEventType();

            if (restType != NWScript.REST_EVENTTYPE_REST_STARTED ||
                !player.IsValid ||
                player.IsDM) return false;

            player.AssignCommand(() => _.ClearAllActions());

            _dialog.StartConversation(player, player, "RestMenu");
            
            return true;
        }
    }
}
