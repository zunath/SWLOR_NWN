using System;
using System.Collections.Generic;
using System.Globalization;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class SystemChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            BugCommand();
            HelpCommand();
            ListEmotesCommand();
            StuckCommand();
            EmotesWindowCommand();

            return _builder.Build();
        }

        private void BugCommand()
        {

            _builder.Create("bug")
                .Description("Toggles the bug report window to submit bugs to the developers. Please include as much detail as possible.")
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) =>
                {
                    var lastSubmission = GetLocalString(user, "BUG_REPORT_LAST_SUBMISSION");
                    if (!string.IsNullOrWhiteSpace(lastSubmission))
                    {
                        var dateTime = DateTime.ParseExact(lastSubmission, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                        if (DateTime.UtcNow <= dateTime)
                        {
                            return "You may only submit one bug report per minute. Please wait and try again.";
                        }                        
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    Gui.TogglePlayerWindow(user, GuiWindowType.BugReport);
                });
        }

        private void HelpCommand()
        {
            _builder.Create("help")
                .Description("Displays all chat commands available to you.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    var appSettings = ApplicationSettings.Get();
                    var authorization = Authorization.GetAuthorizationLevel(user);

                    if (appSettings.ServerEnvironment == ServerEnvironmentType.Test || 
                        authorization == AuthorizationLevel.DM)
                    {
                        SendMessageToPC(user, ChatCommand.HelpTextDM);
                    }
                    else if (authorization == AuthorizationLevel.Admin)
                    {
                        SendMessageToPC(user, ChatCommand.HelpTextAdmin);
                    }
                    else
                    {
                        SendMessageToPC(user, ChatCommand.HelpTextPlayer);
                    }
                });
        }
        private void ListEmotesCommand()
        {
            _builder.Create("emotes")
                .Description("Displays all emotes available to you.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    SendMessageToPC(user, ChatCommand.HelpTextEmote);
                });
        }
        private void StuckCommand()
        {

            _builder.Create("stuck")
                .Description("Emergency Escape Command. Use this if you get stuck on a map.")
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) =>
                {
                    var lastSubmission = GetLocalString(user, "STUCK_REPORT_LAST_SUBMISSION");                    
                    if (!string.IsNullOrWhiteSpace(lastSubmission))
                    {
                        var dateTime = DateTime.ParseExact(lastSubmission, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                        if (DateTime.UtcNow <= dateTime)
                        {
                            return "You may only use the stuck command every five minutes. Please wait and try again.";
                        }                        
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    var nextStuckAllowed = DateTime.UtcNow.AddMinutes(30);
                    var waypoint = GetNearestObject(ObjectType.Waypoint,user);
                    if (GetIsObjectValid(waypoint))
                    {
                        AssignCommand(user, () => { JumpToObject(waypoint); });
                        SetLocalString(user, "STUCK_REPORT_LAST_SUBMISSION", nextStuckAllowed.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                    }
                });
        }
        private void EmotesWindowCommand()
        {
            _builder.Create("emotegui", "emotesgui")
                .Description("Displays the Emotes window.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    Gui.TogglePlayerWindow(user, GuiWindowType.Emotes);
                });
        }
    }
}
