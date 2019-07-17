using System;
using NWN;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Scripts.Delayed
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
                if (userPosition.m_X != data.UserPosition.m_X ||
                    userPosition.m_Y != data.UserPosition.m_Y ||
                    userPosition.m_Z != data.UserPosition.m_Z)
                {
                    data.Player.SendMessage("You move and interrupt your action.");
                    return;
                }

                float maxDistance = actionItem.MaxDistance(data.Player, data.Item, data.Target, data.TargetLocation);
                if (maxDistance > 0.0f)
                {
                    if (data.Target.IsValid &&
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
                    if (data.Item.Charges > 0) data.Item.ReduceCharges();
                    else data.Item.Destroy();
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
