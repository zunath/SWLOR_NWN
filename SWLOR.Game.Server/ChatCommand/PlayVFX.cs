using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using System;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.NWN.Enum.VisualEffect;
using static SWLOR.Game.Server.NWN._;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Switches the active language. Use /language help for more information.", CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class PlayVFX : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            string command = args[0].ToLower();
            Effect effect = EffectVisualEffect((VisualEffect)Int32.Parse(args[0]));
            _.ApplyEffectAtLocation(DurationType.Instant, effect, targetLocation, 6.0f);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            if (args.Length < 1)
            {
                return ColorTokenService.Red("Enter the ID from visualeffects.2da");
            }

            try
            {
                int result = Int32.Parse(args[0]);
            }
            catch (FormatException)
            {
                return ColorTokenService.Red("Enter the ID from visualeffects.2da");
            }

            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}