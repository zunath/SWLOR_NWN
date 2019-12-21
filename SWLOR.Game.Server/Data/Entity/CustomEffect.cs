using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class CustomEffect: IEntity
    {
        [Key]
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
