using System;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Processor;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Gets the amount of time before the next restart.", CommandPermissionType.Player | CommandPermissionType.DM)]
    public class RestartTime : IChatCommand
    {
        private readonly ITimeService _time;

        public RestartTime(ITimeService time)
        {
            _time = time;
        }
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (ServerRestartProcessor.IsDisabled)
            {
                user.FloatingText("Server auto-restarts are currently disabled.");
            }
            else
            {
                DateTime now = DateTime.UtcNow;
                var delta = ServerRestartProcessor.RestartTime - now;
                string rebootString = _time.GetTimeLongIntervals(delta.Days, delta.Hours, delta.Minutes, delta.Seconds, false);
                string message = "Server will automatically reboot in " + rebootString;
                user.FloatingText(message);
            }

        }

        public bool RequiresTarget => false;
        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return null;
        }
    }
}
