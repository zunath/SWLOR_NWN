using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[ApartmentBuilding]")]
    public class ApartmentBuilding: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }

        public IEntity Clone()
        {
            return new ApartmentBuilding
            {
                ID = ID,
                Name = Name
            };
        }
    }
}
