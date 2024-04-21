namespace SWLOR.Core.Service.SpaceService
{
    public class SpaceObjectDetail
    {
        public string ShipItemTag { get; set; }

        public List<string> HighPoweredModules { get; set; }
        public List<string> LowPowerModules { get; set; }

        public SpaceObjectDetail()
        {
            HighPoweredModules = new List<string>();
            LowPowerModules = new List<string>();
        }
    }
}
