using System;
using System.Globalization;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using static SWLOR.Game.Server.NWN._;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Jumps you to your last saved location.", CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class Stuck: IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            // Check timestamp
            var lastRun = GetLocalString(user, "STUCK_LOCATION_LAST_RUN");

            if (!string.IsNullOrWhiteSpace(lastRun))
            {
                var dateTime = DateTime.ParseExact(lastRun, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                if (DateTime.UtcNow < dateTime.AddMinutes(30))
                {
                    SendMessageToPC(user, "This command can only be used once every 30 minutes.");
                    return;
                }
            }
            
            var location = GetLocalLocation(user, "STUCK_LOCATION_RESET");

            AssignCommand(user, () =>
            {
                ClearAllActions();
                JumpToLocation(location);
            });

            SetLocalString(user, "STUCK_LOCATION_LAST_RUN", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        }

        public bool RequiresTarget => false;
        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }
    }
}
