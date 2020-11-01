using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.ChatCommand.Contracts;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.ChatCommand
{
    [CommandDetails("Revives you, heals you to full, and restores all FP.", CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class Rez: IChatCommand
    {
        /// <summary>
        /// Revives and heals user completely.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="target"></param>
        /// <param name="targetLocation"></param>
        /// <param name="args"></param>
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (user.IsDead)
            {
                NWScript.ApplyEffectToObject(DurationType.Instant, NWScript.EffectResurrection(), user.Object);
            }

            NWScript.ApplyEffectToObject(DurationType.Instant, NWScript.EffectHeal(999), user.Object);
            AbilityService.RestorePlayerFP(user, 9999);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}
