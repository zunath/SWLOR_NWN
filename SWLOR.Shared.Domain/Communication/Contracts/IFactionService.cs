using SWLOR.Shared.Domain.Social.Enums;

namespace SWLOR.Shared.Domain.Social.Contracts
{
    public interface IFactionService
    {
        public int MinimumFaction { get; }
        public int MaximumFaction { get; }
        void LoadFactions();
        Dictionary<FactionType, FactionAttribute> GetAllFactions();
        FactionAttribute GetFactionDetail(FactionType factionType);
        void AdjustPlayerFactionStanding(uint player, FactionType faction, int adjustBy);
        void AdjustPlayerFactionPoints(uint player, FactionType faction, int adjustBy);
    }
}
