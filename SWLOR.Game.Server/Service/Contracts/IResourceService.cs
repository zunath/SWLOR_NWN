using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IResourceService
    {
        string GetOreQualityName(ResourceQuality quality);
        string GetResourcePlaceableResref(ResourceType resourceType);
        string GetResourceItemResref(ResourceType resourceType, int tier);
        string GetResourceTypeName(ResourceType resourceType);
        string GetResourceName(ResourceType resourceType, int tier);
        string GetResourceDescription(ResourceType resourceType, ResourceQuality quality, int tier);
    }
}