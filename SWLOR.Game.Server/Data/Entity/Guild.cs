using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class Guild: IEntity
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public IEntity Clone()
        {
            return new Guild
            {
                ID = ID,
                Name = Name,
                Description = Description
            };
        }
    }
}
