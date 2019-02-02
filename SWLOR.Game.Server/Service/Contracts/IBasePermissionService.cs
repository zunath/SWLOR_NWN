using System;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IBasePermissionService
    {
        void GrantBasePermissions(Guid player, Guid pcBaseID, params BasePermission[] permissions);
        void GrantBasePermissions(NWPlayer player, Guid pcBaseID, params BasePermission[] permissions);
        void GrantStructurePermissions(NWPlayer player, Guid pcBaseStructureID, params StructurePermission[] permissions);
        bool HasBasePermission(Guid player, Guid pcBaseID, BasePermission permission);
        bool HasBasePermission(NWPlayer player, Guid pcBaseID, BasePermission permission);
        bool HasStructurePermission(NWPlayer player, Guid pcBaseStructureID, StructurePermission permission);
    }
}