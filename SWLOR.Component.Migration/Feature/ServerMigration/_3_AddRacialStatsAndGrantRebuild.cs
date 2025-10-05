using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Enums;
using SWLOR.Component.Migration.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Migration.Feature.ServerMigration
{
    public class _3_AddRacialStatsAndGrantRebuild: ServerMigrationBase, IServerMigration
    {
        // Lazy-loaded services to break circular dependencies
        private IPlayerRepository PlayerRepository => ServiceProvider.GetRequiredService<IPlayerRepository>();
        
        public _3_AddRacialStatsAndGrantRebuild(ILogger logger, IDatabaseService db, IServiceProvider serviceProvider) : base(logger, db, serviceProvider)
        {
        }
        
        public int Version => 3;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;
        public void Migrate()
        {
            var playerCount = (int)PlayerRepository.GetCount();
            var players = PlayerRepository.GetAll();

            foreach (var player in players)
            {
                player.RacialStat = AbilityType.Invalid;

                PlayerRepository.Save(player);
            }
        }
    }
}
