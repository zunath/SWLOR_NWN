﻿using System;
using NWN;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;
using static SWLOR.Game.Server.NWScript._;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Scripting.Delayed
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
            using (new Profiler(nameof(FinishActionItem)))
            {
                IActionItem actionItem = ItemService.GetActionItemHandler(data.ClassName);
                data.Player.IsBusy = false;

                Vector userPosition = data.Player.Position;
                if (userPosition.X != data.UserPosition.X ||
                    userPosition.Y != data.UserPosition.Y ||
                    userPosition.Z != data.UserPosition.Z)
                {
                    data.Player.SendMessage("You move and interrupt your action.");
                    return;
                }

                float maxDistance = actionItem.MaxDistance(data.Player, data.Item, data.Target, data.TargetLocation);
                if (maxDistance > 0.0f)
                {
                    NWObject owner = GetItemPossessor(data.Target);

                    if (data.Target.IsValid && owner.IsValid)
                    {
                        // We are okay - we have targeted an item in our inventory (we can't target someone
                        // else's inventory, so no need to actually check distance).
                    }
                    else if (data.Target.Object == NWGameObject.OBJECT_SELF)
                    {
                        // Also okay.
                    }
                    else if(data.Target.IsValid &&
                        (_.GetDistanceBetween(data.Player, data.Target) > maxDistance ||
                        data.Player.Area.Resref != data.Target.Area.Resref))
                    {
                        data.Player.SendMessage("Your target is too far away.");
                        return;
                    }
                    else if (!data.Target.IsValid &&
                             (_.GetDistanceBetweenLocations(data.Player.Location, data.TargetLocation) > maxDistance ||
                              data.Player.Area.Resref != ((NWArea)_.GetAreaFromLocation(data.TargetLocation)).Resref))
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

                string invalidTargetMessage = actionItem.IsValidTarget(data.Player, data.Item, data.Target, data.TargetLocation);
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
