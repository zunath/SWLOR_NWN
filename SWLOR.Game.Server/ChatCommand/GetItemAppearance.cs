using System;
using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;


namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Returns item appearance.", CommandPermissionType.DM | CommandPermissionType.Player)]
    public class GetItemAppearance : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            NWItem item = target.Object;

            string appearance = NWNXItem.GetEntireItemAppearance(item);
            Console.WriteLine(appearance);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}
