using System;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Returns data cache information.", CommandPermissionType.DM)]
    public class GetDataCache : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            foreach (var item in DataService.Cache)
            {
                user.SendMessage(item.Key + ": " + item.Value.Count);
                Console.WriteLine(item.Key + ": " + item.Value.Count);
            }
        }

        public bool RequiresTarget => false;
        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return null;
        }
    }
}
