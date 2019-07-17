using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[ServerConfiguration]")]
    public class ServerConfiguration: IEntity
    {
        public ServerConfiguration()
        {
            ServerName = "";
            MessageOfTheDay = "";
        }

        [ExplicitKey]
        public int ID { get; set; }
        public string ServerName { get; set; }
        public string MessageOfTheDay { get; set; }
        public int AreaBakeStep { get; set; }
        public int ModuleVersion { get; set; }
        public DateTime LastGuildTaskUpdate { get; set; }

        public IEntity Clone()
        {
            return new ServerConfiguration
            {
                ID = ID,
                ServerName = ServerName,
                MessageOfTheDay = MessageOfTheDay,
                AreaBakeStep = AreaBakeStep,
                ModuleVersion = ModuleVersion,
                LastGuildTaskUpdate = LastGuildTaskUpdate
            };
        }
    }
}
