using System;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.ControlTower
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
            NWPlayer clicker = (NWScript.GetPlaceableLastClickedBy());
            NWPlaceable tower = (NWScript.OBJECT_SELF);

            clicker.ClearAllActions();
            if (!clicker.IsPlayer) return;

            // Check the distance.
            if (NWScript.GetDistanceBetween(clicker.Object, tower.Object) > 15.0f)
            {
                clicker.SendMessage("You are too far away to interact with that control tower.");
                return;
            }
            var structureID = new Guid(tower.GetLocalString("PC_BASE_STRUCTURE_ID"));
            var structure = DataService.PCBaseStructure.GetByID(structureID);

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
