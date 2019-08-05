using System;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Displays all chat commands available to you.", CommandPermissionType.DM | CommandPermissionType.Player)]
    public class Help: IChatCommand
    {
        private static string _helpTextPlayer = string.Empty;
        private static string _helpTextDM = string.Empty;

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleLoad>(x =>
            {
                var classes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => typeof(IChatCommand).IsAssignableFrom(p) && p.IsClass)
                    .OrderBy(o => o.Name)
                    .ToArray();

                foreach (var @class in classes)
                {
                    var attribute = @class.GetCustomAttribute<CommandDetailsAttribute>();
                    if (attribute == null) continue;

                    if (attribute.Permissions.HasFlag(CommandPermissionType.Player))
                    {
                        _helpTextPlayer += ColorTokenService.Green("/" + @class.Name.ToLower()) + ColorTokenService.White(": " + attribute.Description) + "\n";
                    }

                    if (attribute.Permissions.HasFlag(CommandPermissionType.DM))
                    {
                        _helpTextDM += ColorTokenService.Green("/" + @class.Name.ToLower()) + ColorTokenService.White(": " + attribute.Description) + "\n";
                    }
                }
            });
        }

        /// <summary>
        /// Displays all the chat commands available to a user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="target"></param>
        /// <param name="targetLocation"></param>
        /// <param name="args"></param>
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            bool isDM = user.IsDM || AuthorizationService.IsPCRegisteredAsDM(user);

            if(isDM)
            {
                user.SendMessage(_helpTextDM);
            }
            else
            {
                user.SendMessage(_helpTextPlayer);
            }
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}
