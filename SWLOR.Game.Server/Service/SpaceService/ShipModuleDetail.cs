namespace SWLOR.Game.Server.Service.SpaceService
{
    public class ShipModuleDetail
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public bool IsPassive { get; set; }

        public ShipModuleDetail()
        {
            IsPassive = true;
        }
    }
}
