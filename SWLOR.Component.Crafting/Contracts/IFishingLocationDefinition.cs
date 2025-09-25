using SWLOR.Component.Crafting.Model;
using SWLOR.Shared.Domain.Fishing.Enums;

namespace SWLOR.Component.Crafting.Contracts
{
    public interface IFishingLocationDefinition
    {
        public Dictionary<FishingLocationType, FishingLocationDetail> Build();
    }
}
