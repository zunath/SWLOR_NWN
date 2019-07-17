using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[CustomEffect]")]
    public class CustomEffect: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
        public int IconID { get; set; }

        public IEntity Clone()
        {
            return new CustomEffect
            {
                ID = ID,
                Name = Name,
                IconID = IconID
            };
        }
    }
}
