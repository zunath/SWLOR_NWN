using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class RestMenu
    {
        [NWNEventHandler("mod_rest")]
        public static void OpenRestMenu()
        {
            var player = GetLastPCRested();
            var restType = GetLastRestEventType();

            if (restType != RestEventType.Started ||
                !GetIsObjectValid(player) ||
                GetIsDM(player)) return;

            AssignCommand(player, () => ClearAllActions());

            Dialog.StartConversation(player, player, nameof(RestMenuDialog));
        }
    }
}
