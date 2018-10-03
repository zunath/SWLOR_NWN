using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Placeable.WarpDevice
{
    public class OnUsed: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IKeyItemService _keyItem;

        public OnUsed(
            INWScript script,
            IKeyItemService keyItem)
        {
            _ = script;
            _keyItem = keyItem;
        }

        public bool Run(params object[] args)
        {
            NWPlayer oPC = _.GetLastUsedBy();

            if (_.GetIsInCombat(oPC) == TRUE)
            {
                _.SendMessageToPC(oPC, "You are in combat.");
                return false;
            }

            NWPlaceable self = Object.OBJECT_SELF;
            string destination = self.GetLocalString("DESTINATION");
            int visualEffectID = self.GetLocalInt("VISUAL_EFFECT");
            int keyItemID = self.GetLocalInt("KEY_ITEM_ID");
            string missingKeyItemMessage = self.GetLocalString("MISSING_KEY_ITEM_MESSAGE");

            if (keyItemID > 0)
            {
                if (!_keyItem.PlayerHasKeyItem(oPC, keyItemID))
                {
                    if (!string.IsNullOrWhiteSpace(missingKeyItemMessage))
                    {
                        oPC.SendMessage(missingKeyItemMessage);
                    }
                    else
                    {
                        oPC.SendMessage("You don't have the necessary key item to access that object.");
                    }

                    return false;
                }
            }

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
