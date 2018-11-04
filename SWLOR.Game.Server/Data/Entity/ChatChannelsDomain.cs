using System.Collections.Generic;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[ChatChannelsDomain]")]
    public class ChatChannelsDomain: IEntity
    {
        public ChatChannelsDomain()
        {
            Name = "";
        }

        [ExplicitKey]
        public int ChatChannelID { get; set; }
        public string Name { get; set; }
    }
}
