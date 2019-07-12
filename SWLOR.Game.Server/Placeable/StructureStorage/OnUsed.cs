using System;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Placeable.StructureStorage
{
    public class OnUsed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlayer oPC = (_.GetLastUsedBy());
            NWPlaceable container = (NWGameObject.OBJECT_SELF);
            Guid structureID = new Guid(container.GetLocalString("PC_BASE_STRUCTURE_ID"));
            
            if (!BasePermissionService.HasStructurePermission(oPC, structureID, StructurePermission.CanAccessStructureInventory))
            {
                oPC.FloatingText("You do not have permission to access this structure.");
                return false;
            }
            
            DialogService.StartConversation(oPC, container, "StructureStorage");
            return true;
        }
    }
}
