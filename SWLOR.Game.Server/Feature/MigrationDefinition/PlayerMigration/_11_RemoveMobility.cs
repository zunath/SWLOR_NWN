﻿using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
{
    public class _11_RemoveMobility : PlayerMigrationBase
    {
        public override int Version => 11;
        public override void Migrate(uint player)
        {
            CreaturePlugin.RemoveFeat(player, FeatType.Mobility);
        }
    }
}
