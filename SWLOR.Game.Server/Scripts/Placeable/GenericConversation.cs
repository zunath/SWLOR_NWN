using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.NWN._;

namespace SWLOR.Game.Server.Scripts.Placeable
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
            NWPlaceable placeable = (OBJECT_SELF);
            NWPlayer user = placeable.ObjectType == ObjectType.Placeable ?
                GetLastUsedBy() :
                GetClickingObject();

            if (!user.IsPlayer && !user.IsDM) return;

            string conversation = placeable.GetLocalString("CONVERSATION");
            NWObject target = GetLocalBool(placeable, "TARGET_PC") ? user.Object : placeable.Object;

            if (!string.IsNullOrWhiteSpace(conversation))
            {
                DialogService.StartConversation(user, target, conversation);
            }
            else
            {
                user.AssignCommand(() => ActionStartConversation(target, string.Empty, true, false));
            }

        }
    }
}
