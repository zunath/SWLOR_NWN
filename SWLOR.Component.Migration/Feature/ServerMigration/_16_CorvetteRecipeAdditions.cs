using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Enums;
using SWLOR.Component.Migration.Model;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Crafting.Enums;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Migration.Feature.ServerMigration
{
    public class _16_CorvetteRecipeAdditions: ServerMigrationBase, IServerMigration
    {
        // Lazy-loaded services to break circular dependencies
        private IPlayerRepository PlayerRepository => ServiceProvider.GetRequiredService<IPlayerRepository>();
        
        public _16_CorvetteRecipeAdditions(ILogger logger, IDatabaseService db, IServiceProvider serviceProvider) : base(logger, db, serviceProvider)
        {
        }
        
        public int Version => 16;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;
        public void Migrate()
        {
            var count = (int)PlayerRepository.GetCount();
            var dbPlayers = PlayerRepository.GetAll();

            foreach (var player in dbPlayers)
            {
                if (player.UnlockedRecipes.ContainsKey(RecipeType.CorvetteNeutThranta))
                {
                    player.UnlockedRecipes[RecipeType.CorvetteJehaveyFrigate] = DateTime.UtcNow;
                }
                PlayerRepository.Save(player);
            }
        }
    }
}
