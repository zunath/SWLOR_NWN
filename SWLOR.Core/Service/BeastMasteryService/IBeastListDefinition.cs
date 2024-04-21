namespace SWLOR.Core.Service.BeastMasteryService
{
    public interface IBeastListDefinition
    {
        public Dictionary<BeastType, BeastDetail> Build();
    }
}
