using System;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _16_CorvetteRecipeAdditions: ServerMigrationBase, IServerMigration
    {
        public _16_CorvetteRecipeAdditions(ILogger logger, IDatabaseService db) : base(logger, db)
        {
        }
        
        public int Version => 16;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;
        public void Migrate()
        {
            var query = new DBQuery<Player>();
            var count = (int)_db.SearchCount(query);
            var dbPlayers = _db.Search(query
                .AddPaging(count, 0));

            foreach (var player in dbPlayers)
            {
                if (player.UnlockedRecipes.ContainsKey(RecipeType.CorvetteNeutThranta))
                {
                    player.UnlockedRecipes[RecipeType.CorvetteJehaveyFrigate] = DateTime.UtcNow;
                }
                _db.Set(player);
            }
        }
    }
}
