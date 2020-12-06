using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Feature.DialogDefinition;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Dialog = SWLOR.Game.Server.Service.Dialog;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class DestroyItemDefinition
    {
        [NWNEventHandler("destroy_item")]
        public static void StartDestroyItemConversation()
        {
            var player = OBJECT_SELF;
            var item = StringToObject(Events.GetEventData("ITEM_OBJECT_ID"));

            SetLocalObject(player, "DESTROY_ITEM", item);
            Dialog.StartConversation(player, player, nameof(DestroyItemDialog));
        }
    }
}
