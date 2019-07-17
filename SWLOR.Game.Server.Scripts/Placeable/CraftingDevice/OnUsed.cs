using System;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Placeable.CraftingDevice
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
            NWPlayer player = (_.GetLastUsedBy());
            NWPlaceable device = (NWGameObject.OBJECT_SELF);

            // If a structure ID is defined, we need to make sure the building is set to Workshop mode.
            string structureID = device.GetLocalString("PC_BASE_STRUCTURE_ID");
            if (!string.IsNullOrWhiteSpace(structureID))
            {
                Guid structureGuid = new Guid(structureID);
                var structure = DataService.PCBaseStructure.GetByID(structureGuid);

                // Workbenches and crafting devices can only be used inside 
                // buildings set to "Workshop" mode.
                if(structure.ParentPCBaseStructureID != null)
                {
                    var buildingStructure = DataService.PCBaseStructure.GetByID((Guid)structure.ParentPCBaseStructureID);
                    var modeType = (StructureModeType) buildingStructure.StructureModeID;

                    if (modeType != StructureModeType.Workshop)
                    {
                        player.FloatingText("Workbenches and crafting devices may only be used when the building is set to 'Workshop' mode.");
                        return;
                    }
                }

            }

            if (player.IsBusy)
            {
                player.SendMessage("You are too busy to do that right now.");
                return;
            }
            DialogService.StartConversation(player, device, "CraftingDevice");
        }
    }
}
