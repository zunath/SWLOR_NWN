using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class EnmityAdjustmentRule: IEntity
    {
        public EnmityAdjustmentRule()
        {
            Name = "";
        }

        [Key]
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
