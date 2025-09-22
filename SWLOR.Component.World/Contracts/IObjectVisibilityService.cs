using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.Component.World.Contracts
{
    public interface IObjectVisibilityService
    {
        void LoadVisibilityObjects();
        void LoadPlayerVisibilityObjects();
        void AdjustVisibility(uint player, uint target, VisibilityType type);
        void AdjustVisibilityByObjectId(uint player, string visibilityObjectId, VisibilityType type);
    }
}
