

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[Users]")]
    public class User: IEntity
    {
        [Key]
        public long UserID { get; set; }
        public string DiscordUserID { get; set; }
        public string Username { get; set; }
        public string AvatarHash { get; set; }
        public string Discriminator { get; set; }
        public string Email { get; set; }
        public int RoleID { get; set; }
        public System.DateTime DateRegistered { get; set; }
    }
}
