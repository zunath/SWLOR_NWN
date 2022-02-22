using System;
using System.Globalization;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.NWNX;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Restarts the server.", CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class RestartServer: IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            var lastSubmission = user.GetLocalString("RESTART_SERVER_LAST_SUBMISSION");
            var isFirstSubmission = true;

            // Check for the last submission, if any.
            if (!string.IsNullOrWhiteSpace(lastSubmission))
            {
                // Found one, parse it.
                var dateTime = DateTime.Parse(lastSubmission);
                if (DateTime.UtcNow <= dateTime.AddSeconds(15))
                {
                    // Player submitted a second request within 15 seconds of the last one. 
                    // This is a confirmation they want to restart.
                    isFirstSubmission = false;
                }
            }

            // Player hasn't submitted or time has elapsed
            if (isFirstSubmission)
            {
                user.SetLocalString("RESTART_SERVER_LAST_SUBMISSION", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
                user.FloatingText("Please confirm server reset by entering another \"/restartserver <CD Key>\" command within 15 seconds.");
            }
            else
            {
                foreach (var player in NWModule.Get().Players)
                {
                    _.BootPC(player, $"A DM has restarted the server. Please reconnect shortly.");
                }

                NWNXAdmin.ShutdownServer();
            }
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            var cdKey = _.GetPCPublicCDKey(user);
            var enteredCDKey = args.Length > 0 ? args[0] : string.Empty;

            if (cdKey != enteredCDKey)
            {
                return "Invalid CD key entered. Please enter the command as follows: \"/restartserver <CD Key>\". You can retrieve your CD key with the /CDKey chat command.";
            }

            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}
