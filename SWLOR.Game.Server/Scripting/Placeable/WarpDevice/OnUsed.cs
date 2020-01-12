﻿using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Scripting.Placeable.WarpDevice
{
    public class OnUsed: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlayer oPC = _.GetLastUsedBy();

            if (_.GetIsInCombat(oPC) == true)
            {
                _.SendMessageToPC(oPC, "You are in combat.");
                return;
            }

            NWPlaceable self = NWGameObject.OBJECT_SELF;
            string destination = self.GetLocalString("DESTINATION");
            int visualEffectID = self.GetLocalInt("VISUAL_EFFECT");
            var keyItemID = (KeyItem)self.GetLocalInt("KEY_ITEM_ID");
            string missingKeyItemMessage = self.GetLocalString("MISSING_KEY_ITEM_MESSAGE");
            bool isInstance = self.GetLocalBoolean("INSTANCE") == true;
            bool personalInstanceOnly = self.GetLocalBoolean("PERSONAL_INSTANCE_ONLY") == true;

            if (keyItemID > 0)
            {
                if (!KeyItemService.PlayerHasKeyItem(oPC, keyItemID))
                {
                    if (!string.IsNullOrWhiteSpace(missingKeyItemMessage))
                    {
                        oPC.SendMessage(missingKeyItemMessage);
                    }
                    else
                    {
                        oPC.SendMessage("You don't have the necessary key item to access that object.");
                    }

                    return;
                }
            }

            if (visualEffectID > 0)
            {
                _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect((Vfx)visualEffectID), oPC.Object);
            }

            NWObject entranceWP = _.GetWaypointByTag(destination);
            NWLocation location = _.GetLocation(entranceWP);

            if (!entranceWP.IsValid)
            {
                oPC.SendMessage("Cannot locate entrance waypoint. Inform an admin.");
                return;
            }

            if (isInstance)
            {
                var members = oPC.PartyMembers.Where(x => x.Area.GetLocalString("ORIGINAL_RESREF") == entranceWP.Area.Resref).ToList();

                // A party member is in an instance of this type already.
                // Prompt player to select which instance to enter.
                if (members.Count >= 1 && !personalInstanceOnly)
                {
                    oPC.SetLocalString("INSTANCE_RESREF", entranceWP.Area.Resref);
                    oPC.SetLocalString("INSTANCE_DESTINATION_TAG", destination);
                    DialogService.StartConversation(oPC, self, "InstanceSelection");
                    return;
                }

                // Otherwise no instance exists yet or this instance only allows one player. Make a new one for this player.
                NWArea instance = AreaService.CreateAreaInstance(oPC, entranceWP.Area.Resref, entranceWP.Area.Name, destination);
                location = instance.GetLocalLocation("INSTANCE_ENTRANCE");
                PlayerService.SaveLocation(oPC);
            }

            oPC.AssignCommand(() =>
            {
                _.ActionJumpToLocation(location);
            });
        }
    }
}
