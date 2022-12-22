﻿using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    public abstract class ServerMigrationBase
    {
        protected void GrantRebuildTokenToAllPlayers()
        {
            var query = new DBQuery<Player>();
            var count = (int)DB.SearchCount(query);
            var dbPlayers = DB.Search(query
                .AddPaging(count, 0));

            foreach (var dbPlayer in dbPlayers)
            {
                dbPlayer.NumberRebuildsAvailable++;
                DB.Set(dbPlayer);
            }
        }
    }
}
