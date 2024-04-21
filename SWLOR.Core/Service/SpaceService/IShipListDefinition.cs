namespace SWLOR.Core.Service.SpaceService
{
    public interface IShipListDefinition
    {
        public Dictionary<string, ShipDetail> BuildShips();
    }
}
