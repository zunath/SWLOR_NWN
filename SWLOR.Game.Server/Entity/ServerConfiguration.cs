using System;
using System.Collections.Generic;
using System.Numerics;

namespace SWLOR.Game.Server.Entity
{
    public class ServerConfiguration: EntityBase
    {
        public ServerConfiguration()
        {
            MigrationVersion = 0;
            LastRestart = DateTime.MinValue;
            WalkmeshesByArea = new Dictionary<string, List<Vector3>>();
        }

        [Indexed]
        public int MigrationVersion { get; set; }
        public DateTime LastRestart { get; set; }
        public int LastModuleMTime { get; set; }
        public Dictionary<string, List<Vector3>> WalkmeshesByArea { get; set; }
    }
}
