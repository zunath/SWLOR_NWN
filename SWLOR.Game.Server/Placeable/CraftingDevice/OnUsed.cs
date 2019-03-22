using System;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.CraftingDevice
{
    public class OnUsed: IRegisteredEvent
    {
        
        private readonly IDialogService _dialog;
        private readonly IDataService _data;

        public OnUsed(
            
            IDialogService dialog,
            IDataService data)
        {
            
            _dialog = dialog;
            _data = data;
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = (_.GetLastUsedBy());
            NWPlaceable device = (Object.OBJECT_SELF);

            // If a structure ID is defined, we need to make sure the building is set to Workshop mode.
            string structureID = device.GetLocalString("PC_BASE_STRUCTURE_ID");
            if (!string.IsNullOrWhiteSpace(structureID))
            {
                Guid structureGuid = new Guid(structureID);
                var structure = _data.Get<PCBaseStructure>(structureGuid);

                // Workbenches and crafting devices can only be used inside 
                // buildings set to "Workshop" mode.
                if(structure.ParentPCBaseStructureID != null)
                {
                    var buildingStructure = _data.Get<PCBaseStructure>(structure.ParentPCBaseStructureID);
                    var modeType = (StructureModeType) buildingStructure.StructureModeID;

                    if (modeType != StructureModeType.Workshop)
                    {
                        player.FloatingText("Workbenches and crafting devices may only be used when the building is set to 'Workshop' mode.");
                        return false;
                    }
                }

            }

            if (player.IsBusy)
            {
                player.SendMessage("You are too busy to do that right now.");
                return false;
            }
            _dialog.StartConversation(player, device, "CraftingDevice");
            return true;
        }
    }
}
