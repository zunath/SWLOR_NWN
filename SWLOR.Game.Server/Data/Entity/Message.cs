using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[Message]")]
    public class Message: IEntity
    {
        public Message()
        {
            ID = Guid.NewGuid();
            DatePosted = DateTime.UtcNow;
        }

        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid BoardID { get; set; }
        public Guid PlayerID { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public DateTime DatePosted { get; set; }
        public DateTime DateExpires { get; set; }
        public DateTime? DateRemoved { get; set; }

        public IEntity Clone()
        {
            return new Message
            {
                ID = ID,
                BoardID = BoardID,
                PlayerID = PlayerID,
                Title = Title,
                Text = Text,
                DatePosted = DatePosted,
                DateExpires = DateExpires,
                DateRemoved = DateRemoved
            };
        }
    }
}
