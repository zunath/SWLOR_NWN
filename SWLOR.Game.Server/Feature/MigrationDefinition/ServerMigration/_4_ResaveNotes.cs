using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _4_ResaveNotes : ServerMigrationBase, IServerMigration
    {
        public _4_ResaveNotes(ILogger logger, IDatabaseService db) : base(logger, db)
        {
        }
        
        public int Version => 4;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;
        public void Migrate()
        {
            var query = new DBQuery<PlayerNote>();
            var noteCount = (int)_db.SearchCount(query);
            var notes = _db.Search(query.AddPaging(noteCount, 0));

            foreach (var note in notes)
            {
                note.DMCreatorCDKey = string.Empty;
                note.DMCreatorName = string.Empty;
                note.IsDMNote = false;

                _db.Set(note);
            }
        }
    }
}
