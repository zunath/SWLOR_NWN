using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface ILocalVariableService
    {
        void CopyVariables(NWObject source, NWObject copy);
    }
}
