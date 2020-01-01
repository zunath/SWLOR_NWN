using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class ServerConfiguration: IEntity
    {
        public ServerConfiguration()
        {
            ServerName = "";
            MessageOfTheDay = "";
        }

        [Key]
        public int ID { get; set; }
        public string ServerName { get; set; }
        public string MessageOfTheDay { get; set; }
        public int DataVersion { get; set; }
        public DateTime LastGuildTaskUpdate { get; set; }
    }
}
