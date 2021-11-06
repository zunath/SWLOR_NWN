﻿using System;

namespace SWLOR.Game.Server.Entity
{
    public class ServerConfiguration: EntityBase
    {
        public ServerConfiguration()
        {
            MigrationVersion = 0;
            LastRestart = DateTime.MinValue;
        }

        [Indexed]
        public int MigrationVersion { get; set; }
        public DateTime LastRestart { get; set; }
    }
}
