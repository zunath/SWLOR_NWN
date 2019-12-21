using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class ChatChannel: IEntity
    {
        public ChatChannel()
        {
            Name = "";
        }

        [Key]
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
