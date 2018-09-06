using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IBasePermissionService
    {
        void GrantBasePermissions(NWPlayer player, int pcBaseID, params BasePermission[] permissions);
        void GrantStructurePermissions(NWPlayer player, int pcBaseStructureID, params StructurePermission[] permissions);
        bool HasBasePermission(NWPlayer player, int pcBaseID, BasePermission permission);
        bool HasStructurePermission(NWPlayer player, int pcBaseStructureID, StructurePermission permission);
    }
}