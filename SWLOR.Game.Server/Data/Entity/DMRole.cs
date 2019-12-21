using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class DMRole: IEntity
    {
        [Key]
        public int ID { get; set; }
        public string Description { get; set; }

        public IEntity Clone()
        {
            return new DMRole
            {
                ID = ID,
                Description = Description
            };
        }
    }
}
