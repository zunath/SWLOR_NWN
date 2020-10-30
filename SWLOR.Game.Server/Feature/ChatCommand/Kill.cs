using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Kills your target.", CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class Kill : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            var damage = NWScript.EffectDamage(target.MaxHP+11);
            NWScript.ApplyEffectToObject(DurationType.Instant, damage, target);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}
