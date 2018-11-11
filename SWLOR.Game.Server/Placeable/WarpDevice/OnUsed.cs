using System.Linq;
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
        private readonly IAreaService _area;
        private readonly IPlayerService _player;
        private readonly IDialogService _dialog;

        public OnUsed(
            INWScript script,
            IKeyItemService keyItem,
            IAreaService area,
            IPlayerService player,
            IDialogService dialog)
        {
            _ = script;
            _keyItem = keyItem;
            _area = area;
            _player = player;
            _dialog = dialog;
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
            bool isInstance = self.GetLocalInt("INSTANCE") == TRUE;
            bool personalInstanceOnly = self.GetLocalInt("PERSONAL_INSTANCE_ONLY") == TRUE;

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

            NWObject entranceWP = _.GetWaypointByTag(destination);
            NWLocation location = _.GetLocation(entranceWP);

            if (!entranceWP.IsValid)
            {
                oPC.SendMessage("Cannot locate entrance waypoint. Inform an admin.");
                return false;
            }

            if (isInstance)
            {
                var members = oPC.PartyMembers.Where(x => x.GetLocalString("ORIGINAL_RESREF") == entranceWP.Area.Resref).ToList();

                // A party member is in an instance of this type already.
                // Prompt player to select which instance to enter.
                if (members.Count >= 1 && !personalInstanceOnly)
                {
                    oPC.SetLocalString("INSTANCE_RESREF", entranceWP.Resref);
                    oPC.SetLocalString("INSTANCE_DESTINATION_TAG", destination);
                    _dialog.StartConversation(oPC, self, "InstanceSelection");
                    return false;
                }

                // Otherwise no instance exists yet or this instance only allows one player. Make a new one for this player.
                NWArea instance = _area.CreateAreaInstance(oPC, entranceWP.Area.Resref, entranceWP.Area.Name, destination);
                location = instance.GetLocalLocation("INSTANCE_ENTRANCE");
                _player.SaveLocation(oPC);
            }

            oPC.AssignCommand(() =>
            {
                _.ActionJumpToLocation(location);
            });

            return true;
        }
    }
}
