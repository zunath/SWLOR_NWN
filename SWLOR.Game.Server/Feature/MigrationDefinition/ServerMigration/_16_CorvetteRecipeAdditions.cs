using System;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _16_CorvetteRecipeAdditions: ServerMigrationBase, IServerMigration
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        
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
                if (player.UnlockedRecipes.ContainsKey(Service.CraftService.RecipeType.CorvetteNeutThranta))
                {
                    player.UnlockedRecipes[Service.CraftService.RecipeType.CorvetteJehaveyFrigate] = DateTime.UtcNow;
                }
                _db.Set(player);
            }
        }
    }
}
