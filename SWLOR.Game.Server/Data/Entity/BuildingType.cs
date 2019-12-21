using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class BuildingType: IEntity
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }

        public IEntity Clone()
        {
            return new BuildingType
            {
                ID = ID,
                Name = Name
            };
        }
    }
}
