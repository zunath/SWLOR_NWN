using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IExaminationService
    {
        bool OnModuleExamine(NWPlayer examiner, NWObject target);
    }
}
