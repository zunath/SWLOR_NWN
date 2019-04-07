using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;
/**
 * Pazaak item activation code, used for both individual cards and for card collections.
 * Does some basic policing and then starts the relevant dialogs.
 */
namespace SWLOR.Game.Server.Item
{
    class Pazaak : IActionItem
    {
        public string CustomKey => null;

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            if (item.Tag == "PazaakCard" && target.Tag == "PazaakCollection")
            {
                PazaakService.AddCardToCollection(item, (NWItem)target);
                return;
            }

            if (item.Tag == "PazaakCollection" && target.Tag == "PazaakTable")
            {
                user.SetLocalObject("ACTIVE_COLLECTION", item);
                DialogService.StartConversation(user, target, "PazaakTable");
                return;
            }

            if (item.Tag == "PazaakCollection" &&(!target.IsValid || target == user))
            {
                user.SetLocalObject("ACTIVE_COLLECTION", item);
                DialogService.StartConversation(user, item, "PazaakCollection");
                return;
            }
        }
        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return 0.0f;
        }

        public bool FaceTarget()
        {
            return false;
        }

        public int AnimationID()
        {
            return 0;
        }

        public float MaxDistance(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return 3.0f;
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return false;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            if (item.Tag == "PazaakCard")
            {
                if (target.Tag=="PazaakCollection")
                {
                    return "";
                }
                else
                {
                    return "You may only use cards on a Pazaak Collection.  Buy one from a merchanct if you don't have one.";
                }
            }

            if (item.Tag == "PazaakCollection")
            {
                if (!target.IsValid || target == user)
                {
                    return "";
                }
                else if (target.Tag == "PazaakTable")
                {
                    return "";
                }
                else
                {
                    return "You may only use a Pazaak Collection on yourself to manage your collection, or on a Pazaak Table to play.";
                }
            }

            return "This item is incorrectly configured, please raise a /bug.";
        }

        public bool AllowLocationTarget()
        {
            return false;
        }
    }
}
