using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Placeable.WarpDevice
{
    public class OnUsed: IRegisteredEvent
    {
        private readonly INWScript _;

        public OnUsed(INWScript script)
        {
            _ = script;
        }

        public bool Run(params object[] args)
        {
            NWObject oPC = (_.GetLastUsedBy());

            if (_.GetIsInCombat(oPC) == TRUE)
            {
                _.SendMessageToPC(oPC, "You are in combat.");
                return false;
            }

            NWPlaceable self = (Object.OBJECT_SELF);
            string destination = self.GetLocalString("DESTINATION");
            int visualEffectID = self.GetLocalInt("VISUAL_EFFECT");

            if (visualEffectID > 0)
            {
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectVisualEffect(visualEffectID), oPC.Object);
            }

            oPC.AssignCommand(() =>
            {
                Location location = _.GetLocation(_.GetWaypointByTag(destination));
                _.ActionJumpToLocation(location);
            });

            return true;
        }
    }
}
