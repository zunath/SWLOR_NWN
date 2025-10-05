using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Enums;
using SWLOR.Component.Migration.Model;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Migration.Feature.ServerMigration
{
    public class _4_ResaveNotes : ServerMigrationBase, IServerMigration
    {
        // Lazy-loaded services to break circular dependencies
        private IPlayerNoteRepository PlayerNoteRepository => ServiceProvider.GetRequiredService<IPlayerNoteRepository>();
        
        public _4_ResaveNotes(ILogger logger, IDatabaseService db, IServiceProvider serviceProvider) : base(logger, db, serviceProvider)
        {
        }
        
        public int Version => 4;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;
        public void Migrate()
        {
            var noteCount = (int)PlayerNoteRepository.GetCount();
            var notes = PlayerNoteRepository.GetAll();

            foreach (var note in notes)
            {
                note.DMCreatorCDKey = string.Empty;
                note.DMCreatorName = string.Empty;
                note.IsDMNote = false;

                PlayerNoteRepository.Save(note);
            }
        }
    }
}
