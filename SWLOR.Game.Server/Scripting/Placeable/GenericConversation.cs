using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripting.Placeable
{
    public class GenericConversation: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlaceable placeable = (NWGameObject.OBJECT_SELF);
            NWPlayer user = placeable.ObjectType == ObjectType.Placeable ?
                _.GetLastUsedBy() :
                _.GetClickingObject();

            if (!user.IsPlayer && !user.IsDM) return;

            string conversation = placeable.GetLocalString("CONVERSATION");
            NWObject target = placeable.GetLocalBoolean("TARGET_PC") == true ? user.Object : placeable.Object;

            if (!string.IsNullOrWhiteSpace(conversation))
            {
                DialogService.StartConversation(user, target, conversation);
            }
            else
            {
                user.AssignCommand(() => _.ActionStartConversation(target, string.Empty, true, false));
            }

        }
    }
}
