using SWLOR.Core.Entity;
using SWLOR.Core.Service;
using SWLOR.Core.Service.DBService;

namespace SWLOR.Core.Feature.MigrationDefinition.ServerMigration
{
    public class _4_ResaveNotes : ServerMigrationBase
    {
        public int Version => 4;
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
