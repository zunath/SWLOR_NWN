using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IEffectTrackerService
    {
        void ProcessPCEffects(NWPlayer oPC);
    }
}