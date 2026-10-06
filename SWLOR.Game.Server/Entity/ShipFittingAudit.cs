namespace SWLOR.Game.Server.Entity
{
    public sealed class ShipFittingAudit : EntityBase
    {
        [Indexed] public string ShipId {get;set;}
        public string OriginalShip {get;set;}
        public bool Completed {get;set;}
        public string Error {get;set;}
    }
}
