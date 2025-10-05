using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Enums;
using SWLOR.Component.Migration.Model;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Migration.Feature.ServerMigration
{
    public class _8_EnableServerResetReminders: ServerMigrationBase, IServerMigration
    {
        // Lazy-loaded services to break circular dependencies
        private IPlayerRepository PlayerRepository => ServiceProvider.GetRequiredService<IPlayerRepository>();
        
        public _8_EnableServerResetReminders(ILogger logger, IDatabaseService db, IServiceProvider serviceProvider) : base(logger, db, serviceProvider)
        {
        }
        
        public int Version => 8;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;
        public void Migrate()
        {
            var playerCount = (int)PlayerRepository.GetCount();
            var dbPlayers = PlayerRepository.GetAll();

            foreach (var dbPlayer in dbPlayers)
            {
                dbPlayer.Settings.DisplayServerResetReminders = true;
                PlayerRepository.Save(dbPlayer);
            }
        }
    }
}
