using System;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class ResourceService : IResourceService
    {
        private readonly IItemService _item;

        public ResourceService(IItemService item)
        {
            _item = item;
        }

        public string GetOreQualityName(ResourceQuality quality)
        {
            switch (quality)
            {
                case ResourceQuality.Low:
                    return "Low Quality";
                case ResourceQuality.Normal:
                    return "Normal Quality";
                case ResourceQuality.High:
                    return "High Quality";
                case ResourceQuality.VeryHigh:
                    return "Very High Quality";
                default:
                    return "Unknown Quality";
            }
        }

        public string GetResourceTypeName(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Ore:
                    return "Ore";
                case ResourceType.Organic:
                    return "Organic Material";
                default:
                    return "Unknown Resource";
            }
        }

        public string GetResourcePlaceableResref(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Ore: return "ore_vein";
                case ResourceType.Organic: return "tree";
                default:
                    throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, null);
            }
        }

        public string GetResourceItemResref(ResourceType resourceType, int tier)
        {
            // Raw Ore
            if (resourceType == ResourceType.Ore)
            {
                switch (tier)
                {
                    case 1: return "raw_veldite";
                    case 2: return "raw_scordspar";
                    case 3: return "raw_plagionite";
                    case 4: return "raw_keromber";
                    case 5: return "raw_jasioclase";
                    case 6: return "raw_hemorgite";
                    case 7: return "raw_ochne";
                    case 8: return "raw_croknor";
                    case 9: return "raw_arkoxit";
                    case 10: return "raw_bisteiss";
                    default: return string.Empty;
                }
            }
            // Organic Material
            else if (resourceType ==  ResourceType.Organic)
            {
                switch (tier)
                {
                    case 1: return "elm_wood";
                    case 2: return "ash_wood";
                    case 3: return "walnut_wood";
                    case 4: return "arrowwood_wood";
                    case 5: return "rosewood_wood";
                    case 6: return "mahogany_wood";
                    case 7: return "maple_wood";
                    case 8: return "willow_wood";
                    case 9: return "lauan_wood";
                    case 10: return "ebony_wood";
                    default: return string.Empty;
                }
            }

            return string.Empty;
        }

        public string GetResourceName(ResourceType resourceType, int tier)
        {
            string resref = GetResourceItemResref(resourceType, tier);
            return _item.GetNameByResref(resref);
        }

        public string GetResourceDescription(ResourceType resourceType, ResourceQuality quality, int tier)
        {
            string description = GetOreQualityName(quality) + " " +
                                 GetResourceName(resourceType, tier) + " (" +
                                 GetResourceTypeName(resourceType) + ")";

            return description;
        }

    }
}
