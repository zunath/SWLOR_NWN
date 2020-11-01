using System;
using SWLOR.Game.Server.Legacy.ChatCommand.Contracts;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Event.SWLOR;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;

namespace SWLOR.Game.Server.Legacy.ChatCommand
{
    [CommandDetails("Returns cache stats information.", CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class GetCacheStats: IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            user.SendMessage("======================================================");

            MessageHub.Instance.Publish(new OnRequestCacheStats(user));

            user.SendMessage("======================================================");
            var memoryInUse = GC.GetTotalMemory(true);
            user.SendMessage("Memory In Use = " + memoryInUse);
        }

        public bool RequiresTarget => false;
        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return null;
        }
    }
}
