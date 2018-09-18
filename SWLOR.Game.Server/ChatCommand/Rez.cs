using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Revives & heals you to full.", CommandPermissionType.DM)]
    public class Rez: IChatCommand
    {
        private readonly INWScript _;

        public Rez(INWScript script)
        {
            _ = script;
        }

        /// <summary>
        /// Revives and heals user completely.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="args"></param>
        public void DoAction(NWPlayer user, params string[] args)
        {
            if (user.IsDead)
            {
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, _.EffectResurrection(), user.Object);
            }

            _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, _.EffectHeal(999), user.Object);

        }
    }
}
