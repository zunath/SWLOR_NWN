using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IImpoundService
    {
        void Impound(PCBaseStructureItem pcBaseStructureItem);
        void Impound(string playerID, NWItem item);
    }
}