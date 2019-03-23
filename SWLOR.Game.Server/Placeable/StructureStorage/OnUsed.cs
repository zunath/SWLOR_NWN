using System;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.StructureStorage
{
    public class OnUsed: IRegisteredEvent
    {
        
        
        private readonly IDialogService _dialog;
        

        public OnUsed(
            
            IDialogService dialog)
            
        {
            
            
            _dialog = dialog;
        }

        public bool Run(params object[] args)
        {
            NWPlayer oPC = (_.GetLastUsedBy());
            NWPlaceable container = (Object.OBJECT_SELF);
            Guid structureID = new Guid(container.GetLocalString("PC_BASE_STRUCTURE_ID"));
            
            if (!BasePermissionService.HasStructurePermission(oPC, structureID, StructurePermission.CanAccessStructureInventory))
            {
                oPC.FloatingText("You do not have permission to access this structure.");
                return false;
            }
            
            _dialog.StartConversation(oPC, container, "StructureStorage");
            return true;
        }
    }
}
