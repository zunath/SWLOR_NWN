

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[User]")]
    public class User: IEntity
    {
        [Key]
        public int ID { get; set; }
        public string DiscordUserID { get; set; }
        public string Username { get; set; }
        public string AvatarHash { get; set; }
        public string Discriminator { get; set; }
        public string Email { get; set; }
        public int RoleID { get; set; }
        public DateTime DateRegistered { get; set; }


        public IEntity Clone()
        {
            return new User
            {
                ID = ID,
                DiscordUserID = DiscordUserID,
                Username = Username,
                AvatarHash = AvatarHash,
                Discriminator = Discriminator,
                Email = Email,
                RoleID = RoleID,
                DateRegistered = DateRegistered
            };
        }
    }
}
