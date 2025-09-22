using SWLOR.Component.Player.Enums;

namespace SWLOR.Component.Player.Contracts
{
    public interface IFactionService
    {
        void LoadFactions();
        Dictionary<FactionType, FactionAttribute> GetAllFactions();
        FactionAttribute GetFactionDetail(FactionType factionType);
        void AdjustPlayerFactionStanding(uint player, FactionType faction, int adjustBy);
        void AdjustPlayerFactionPoints(uint player, FactionType faction, int adjustBy);
    }
}
