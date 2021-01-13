using System;
using SWLOR.Game.Server.Legacy.Event.SWLOR;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.ValueObject;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.Scripts.Delayed
{
    public class FinishActionItem: IScript
    {
        private Guid _eventID;

        public void SubscribeEvents()
        {
            _eventID = MessageHub.Instance.Subscribe<OnFinishActionItem>(OnFinishActionItem);
        }

        private void OnFinishActionItem(OnFinishActionItem data)
        {
            //using (new Profiler(nameof(FinishActionItem)))
            {
                var actionItem = ItemService.GetActionItemHandler(data.ClassName);
                data.Player.IsBusy = false;

                var userPosition = data.Player.Position;
                if (userPosition.X != data.UserPosition.X ||
                    userPosition.Y != data.UserPosition.Y ||
                    userPosition.Z != data.UserPosition.Z)
                {
                    data.Player.SendMessage("You move and interrupt your action.");
                    return;
                }

                var maxDistance = actionItem.MaxDistance(data.Player, data.Item, data.Target, data.TargetLocation);
                if (maxDistance > 0.0f)
                {
                    NWObject owner = GetItemPossessor(data.Target);

                    if (data.Target.IsValid && owner.IsValid)
                    {
                        // We are okay - we have targeted an item in our inventory (we can't target someone
                        // else's inventory, so no need to actually check distance).
                    }
                    else if (data.Target.Object == OBJECT_SELF)
                    {
                        // Also okay.
                    }
                    else if(data.Target.IsValid &&
                        (GetDistanceBetween(data.Player, data.Target) > maxDistance ||
                        GetResRef(data.Player.Area) != GetResRef(data.Target.Area)))
                    {
                        data.Player.SendMessage("Your target is too far away.");
                        return;
                    }
                    else if (!data.Target.IsValid &&
                             (GetDistanceBetweenLocations(data.Player.Location, data.TargetLocation) > maxDistance ||
                              GetResRef(data.Player.Area) != GetResRef(GetAreaFromLocation(data.TargetLocation))))
                    {
                        data.Player.SendMessage("That location is too far away.");
                        return;
                    }
                }

                if (!data.Target.IsValid && !actionItem.AllowLocationTarget())
                {
                    data.Player.SendMessage("Unable to locate target.");
                    return;
                }

                var invalidTargetMessage = actionItem.IsValidTarget(data.Player, data.Item, data.Target, data.TargetLocation);
                if (!string.IsNullOrWhiteSpace(invalidTargetMessage))
                {
                    data.Player.SendMessage(invalidTargetMessage);
                    return;
                }

                actionItem.ApplyEffects(data.Player, data.Item, data.Target, data.TargetLocation, data.CustomData);

                if (actionItem.ReducesItemCharge(data.Player, data.Item, data.Target, data.TargetLocation, data.CustomData))
                {
                    if (data.Item.Charges > 0)
                        data.Item.ReduceCharges();

                    if (data.Item.Charges <= 0)
                        data.Item.Destroy();
                }
            }
        }

        public void UnsubscribeEvents()
        {
            MessageHub.Instance.Unsubscribe(_eventID);
        }

        public void Main()
        {
        }
    }
}
