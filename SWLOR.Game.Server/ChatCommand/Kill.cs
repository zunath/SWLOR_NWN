using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Kills your target.", CommandPermissionType.DM)]
    public class Kill : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            var damage = _.EffectDamage(target.MaxHP+11);
            _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, damage, target);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}
