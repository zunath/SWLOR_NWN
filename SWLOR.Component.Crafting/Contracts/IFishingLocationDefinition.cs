using SWLOR.Component.Crafting.Model;

namespace SWLOR.Component.Crafting.Contracts
{
    public interface IFishingLocationDefinition
    {
        public Dictionary<FishingLocationType, FishingLocationDetail> Build();
    }
}
