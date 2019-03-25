using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Placeable
{
    public class GenericConversation: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable placeable = (Object.OBJECT_SELF);
            NWPlayer user = placeable.ObjectType == OBJECT_TYPE_PLACEABLE ? 
                _.GetLastUsedBy() :
                _.GetClickingObject();
            
            if (!user.IsPlayer && !user.IsDM) return false;

            string conversation = placeable.GetLocalString("CONVERSATION");
            NWObject target = placeable.GetLocalInt("TARGET_PC") == TRUE ? user.Object : placeable.Object;

            if (!string.IsNullOrWhiteSpace(conversation))
            {
                DialogService.StartConversation(user, target, conversation);
            }
            else
            {
                user.AssignCommand(() => _.ActionStartConversation(target, string.Empty, TRUE, FALSE));
            }

            return true;
        }
    }
}
