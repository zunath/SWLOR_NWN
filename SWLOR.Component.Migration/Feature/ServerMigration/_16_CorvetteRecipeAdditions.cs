using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Enums;
using SWLOR.Component.Migration.Model;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.Crafting.Enums;
using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Component.Migration.Feature.ServerMigration
{
    public class _16_CorvetteRecipeAdditions: ServerMigrationBase, IServerMigration
    {
        public _16_CorvetteRecipeAdditions(ILogger logger, IDatabaseService db, ISpaceService spaceService) : base(logger, db, spaceService)
        {
        }
        
        public int Version => 16;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;
        public void Migrate()
        {
            var query = new DBQuery<Player>();
            var count = (int)DB.SearchCount(query);
            var dbPlayers = DB.Search(query
                .AddPaging(count, 0));

            foreach (var player in dbPlayers)
            {
                if (player.UnlockedRecipes.ContainsKey(RecipeType.CorvetteNeutThranta))
                {
                    player.UnlockedRecipes[RecipeType.CorvetteJehaveyFrigate] = DateTime.UtcNow;
                }
                DB.Set(player);
            }
        }
    }
}
