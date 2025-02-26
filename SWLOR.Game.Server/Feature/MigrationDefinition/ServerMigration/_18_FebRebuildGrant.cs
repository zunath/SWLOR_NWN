﻿using System.Collections.Generic;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _18_FebRebuildGrant : ServerMigrationBase, IServerMigration
    {
    public int Version => 18;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;
    public void Migrate()
    {
            GrantRebuildTokenToAllPlayers();
    }
}
}
