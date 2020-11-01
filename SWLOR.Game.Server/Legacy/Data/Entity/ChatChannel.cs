using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Legacy.Data.Contracts;

namespace SWLOR.Game.Server.Legacy.Data.Entity
{
    [Table("ChatChannel")]
    public class ChatChannel: IEntity
    {
        public ChatChannel()
        {
            Name = "";
        }

        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }

        public IEntity Clone()
        {
            return new ChatChannel
            {
                ID = ID,
                Name = Name
            };
        }
    }
}
