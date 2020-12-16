using System;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Sets Scale.", CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class SetScale : IChatCommand
    {
        private const int MaxAmount = 100;

        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (target.IsPlayer) return;

            float value = float.Parse(args[0]);

            _.SetObjectVisualTransform(target, ObjectVisualTransform.Scale, value);

            user.SendMessage("Object Scale Changed To " + value);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            if (args.Length <= 0)
            {
                return "Please specify an amount of RP XP to give. Valid range: 0-" + MaxAmount;
            }

            if (!int.TryParse(args[0], out int value))
            {
                return "Please specify a valid amount between 0 and " + MaxAmount + ".";
            }

            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}
