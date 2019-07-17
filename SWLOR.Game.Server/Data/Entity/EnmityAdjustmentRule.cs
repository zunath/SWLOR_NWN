using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[EnmityAdjustmentRule]")]
    public class EnmityAdjustmentRule: IEntity
    {
        public EnmityAdjustmentRule()
        {
            Name = "";
        }

        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }

        public IEntity Clone()
        {
            return new EnmityAdjustmentRule
            {
                ID = ID,
                Name = Name
            };
        }
    }
}
