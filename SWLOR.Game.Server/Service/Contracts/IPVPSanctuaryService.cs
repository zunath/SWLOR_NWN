using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IPVPSanctuaryService
    {
        bool PlayerHasPVPSanctuary(NWPlayer player);
        void SetPlayerPVPSanctuaryOverride(NWPlayer player, bool overrideStatus);
        bool IsPVPAttackAllowed(NWPlayer attacker, NWPlayer target);
    }
}
