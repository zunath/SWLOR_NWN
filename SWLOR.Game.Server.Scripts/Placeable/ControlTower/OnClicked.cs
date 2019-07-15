using System;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Placeable.ControlTower
{
    public class OnClicked: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlayer clicker = (_.GetPlaceableLastClickedBy());
            NWPlaceable tower = (NWGameObject.OBJECT_SELF);

            clicker.ClearAllActions();
            if (!clicker.IsPlayer) return;

            // Check the distance.
            if (_.GetDistanceBetween(clicker.Object, tower.Object) > 15.0f)
            {
                clicker.SendMessage("You are too far away to interact with that control tower.");
                return;
            }
            Guid structureID = new Guid(tower.GetLocalString("PC_BASE_STRUCTURE_ID"));
            PCBaseStructure structure = DataService.PCBaseStructure.GetByID(structureID);

            // Does the player have permission to access the fuel bays?
            if (BasePermissionService.HasBasePermission(clicker, structure.PCBaseID, BasePermission.CanManageBaseFuel))
            {
                // Is the tower in reinforced mode? If so, fuel cannot be accessed.
                var pcBase = DataService.PCBase.GetByID(structure.PCBaseID);
                if (pcBase.IsInReinforcedMode)
                {
                    clicker.SendMessage("This tower is currently in reinforced mode and cannot be accessed.");
                }
                else
                {
                    DialogService.StartConversation(clicker, tower, "ControlTower");
                }
            }
            else
            {
                clicker.SendMessage("You don't have permission to interact with this control tower.");
            }

        }
    }
}
