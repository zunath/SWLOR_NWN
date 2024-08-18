using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _4_ResaveNotes : ServerMigrationBase, IServerMigration
    {
        public int Version => 4;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;
        public void Migrate()
        {
            var query = new DBQuery<PlayerNote>();
            var noteCount = (int)DB.SearchCount(query);
            var notes = DB.Search(query.AddPaging(noteCount, 0));

            foreach (var note in notes)
            {
                note.DMCreatorCDKey = string.Empty;
                note.DMCreatorName = string.Empty;
                note.IsDMNote = false;

                DB.Set(note);
            }
        }
    }
}
