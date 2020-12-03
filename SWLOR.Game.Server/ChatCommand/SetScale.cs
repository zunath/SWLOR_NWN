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
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {

            float value = float.Parse(args[0]);

            _.SetObjectVisualTransform(target, ObjectVisualTransform.Scale, value);

            user.SendMessage("Object/Person Scale Changed By " + value);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            if (args.Length <= 0)
            {
                return "Please specify an amount to scale.";
            }

            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}
