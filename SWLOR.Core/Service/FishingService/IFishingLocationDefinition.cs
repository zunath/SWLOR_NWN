namespace SWLOR.Core.Service.FishingService
{
    public interface IFishingLocationDefinition
    {
        public Dictionary<FishingLocationType, FishingLocationDetail> Build();
    }
}
