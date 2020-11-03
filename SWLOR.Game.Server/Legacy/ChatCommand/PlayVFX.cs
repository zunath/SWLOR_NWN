using System;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Legacy.ChatCommand.Contracts;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.ChatCommand
{
    [CommandDetails("Plays a VFX from visualeffects.2da. Only parameter is ID from the 2da.", CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class PlayVFX : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            var command = args[0].ToLower();
            var effect = EffectVisualEffect((VisualEffect)Int32.Parse(args[0]));
            ApplyEffectAtLocation(DurationType.Instant, effect, targetLocation, 6.0f);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            if (args.Length < 1)
            {
                return ColorToken.Red("Enter the ID from visualeffects.2da");
            }

            try
            {
                var result = Int32.Parse(args[0]);
            }
            catch (FormatException)
            {
                return ColorToken.Red("Enter the ID from visualeffects.2da");
            }

            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}