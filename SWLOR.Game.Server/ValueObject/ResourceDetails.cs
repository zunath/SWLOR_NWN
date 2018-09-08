namespace SWLOR.Game.Server.ValueObject
{
    public class ResourceDetails
    {
        public int Quality { get; set; }
        public int ResourceType { get; set; }
        public int Tier { get; set; }

        public ResourceDetails(int quality, int resourceType, int tier)
        {
            Quality = quality;
            ResourceType = resourceType;
            Tier = tier;
        }
    }
}
