using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable
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

            var conversation = placeable.GetLocalString("CONVERSATION");
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
