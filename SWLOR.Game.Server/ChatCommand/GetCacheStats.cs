using System;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Returns cache stats information.", CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class GetCacheStats: IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            user.SendMessage("======================================================");

            MessageHub.Instance.Publish(new OnRequestCacheStats(user));

            user.SendMessage("======================================================");
            long memoryInUse = GC.GetTotalMemory(true);
            user.SendMessage("Memory In Use = " + memoryInUse);
        }

        public bool RequiresTarget => false;
        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return null;
        }
    }
}
