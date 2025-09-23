namespace SWLOR.Component.Space.Contracts
{
    public interface IShipListDefinition
    {
        public Dictionary<string, ShipDetail> BuildShips();
    }
}
