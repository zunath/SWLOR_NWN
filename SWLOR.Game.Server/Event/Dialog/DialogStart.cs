using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using static NWN._;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Dialog
{
    public class DialogStart: IRegisteredEvent
    {
        
        private readonly IDialogService _dialog;

        public DialogStart( IDialogService dialogService)
        {
            
            _dialog = dialogService;
        }

        public bool Run(params object[] args)
        {
            NWPlayer pc = (_.GetLastUsedBy());
            if (!pc.IsValid) pc = (_.GetPCSpeaker());

            string conversation = _.GetLocalString(Object.OBJECT_SELF, "CONVERSATION");
            
            if (!string.IsNullOrWhiteSpace(conversation))
            {
                int objectType = _.GetObjectType(Object.OBJECT_SELF);
                if (objectType == OBJECT_TYPE_PLACEABLE)
                {
                    NWPlaceable talkTo = (Object.OBJECT_SELF);
                    _dialog.StartConversation(pc, talkTo, conversation);
                }
                else
                {
                    NWCreature talkTo = (Object.OBJECT_SELF);
                    _dialog.StartConversation(pc, talkTo, conversation);
                }
            }
            else
            {
                _.ActionStartConversation(pc.Object, "", TRUE, FALSE);
            }

            return true;
        }
    }
}
