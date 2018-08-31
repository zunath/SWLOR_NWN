using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;

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
