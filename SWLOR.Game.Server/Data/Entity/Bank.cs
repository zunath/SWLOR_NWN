using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[Bank]")]
    public class Bank: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string AreaName { get; set; }
        public string AreaTag { get; set; }
        public string AreaResref { get; set; }

        public IEntity Clone()
        {
            return new Bank
            {
                ID = ID,
                AreaName = AreaName,
                AreaTag = AreaTag,
                AreaResref = AreaResref
            };
        }
    }
}
