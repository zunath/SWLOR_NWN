using System;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Displays all chat commands available to you.", CommandPermissionType.DM | CommandPermissionType.Player)]
    public class Help: IChatCommand
    {
        
        private readonly IAuthorizationService _auth;

        public Help(
            IAuthorizationService auth)
        {
            
            _auth = auth;
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
            bool isDM = user.IsDM || _auth.IsPCRegisteredAsDM(user);

            var classes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IChatCommand).IsAssignableFrom(p) && p.IsClass)
                .OrderBy(o => o.Name)
                .ToArray();

            foreach (var @class in classes)
            {
                var attribute = @class.GetCustomAttribute<CommandDetailsAttribute>();
                if (attribute == null) continue;
                
                if (attribute.Permissions.HasFlag(CommandPermissionType.Player) && user.IsPlayer ||
                    attribute.Permissions.HasFlag(CommandPermissionType.DM) && isDM)
                {
                    user.SendMessage(ColorTokenService.Green("/" + @class.Name.ToLower()) + ColorTokenService.White(": " + attribute.Description));
                }
            }
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}
