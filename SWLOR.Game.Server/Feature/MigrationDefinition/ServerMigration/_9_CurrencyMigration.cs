using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CurrencyService;

using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _9_CurrencyMigration : ServerMigrationBase, IServerMigration
    {
        private ILogger _logger = ServiceContainer.GetService<ILogger>();
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        public int Version => 9;

        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

        public void Migrate()
        {
            var dbQuery = new DBQuery<Player>();
            var playerCount = (int)_db.SearchCount(dbQuery);

            var dbPlayers = _db.Search(dbQuery
                .AddPaging(playerCount, 0));

            foreach (var dbPlayer in dbPlayers)
            {
                var json = _db.GetRawJson<Player>(dbPlayer.Id);
                var jObject = JObject.Parse(json);

                var perkResets = jObject["NumberPerkResetsAvailable"]?.Value<int>() ?? 0;
                var rebuildCount = jObject["NumberRebuildsAvailable"]?.Value<int>() ?? 0;

                dbPlayer.Currencies[CurrencyType.PerkRefundToken] = perkResets;
                dbPlayer.Currencies[CurrencyType.RebuildToken] = rebuildCount;

                _db.Set(dbPlayer);

                _logger.Write<MigrationLogGroup>($"Migrated {perkResets} perk resets and {rebuildCount} rebuild tokens for player {dbPlayer.Name} ({dbPlayer.Id})");
            }
        }
    }
}
