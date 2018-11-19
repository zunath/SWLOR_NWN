namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[DMActionType]")]
    public class DMActionType
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
    }
}
