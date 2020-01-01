using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class AuthorizedDM: IEntity
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public string CDKey { get; set; }
        public int DMRole { get; set; }
        public bool IsActive { get; set; }
    }
}
