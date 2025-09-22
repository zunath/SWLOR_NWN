using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Quest.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Dialog.Contracts;
using SWLOR.Shared.Dialog.Service;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Component.Quest.Service
{
    /// <summary>
    /// Factory implementation for creating QuestDetail instances.
    /// This ensures proper DI management and dependency injection.
    /// </summary>
    public class QuestDetailFactory : IQuestDetailFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public QuestDetailFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public QuestDetail Create(string questId, string name)
        {
            var db = _serviceProvider.GetRequiredService<IDatabaseService>();
            var gui = _serviceProvider.GetRequiredService<IGuiService>();
            var dialog = _serviceProvider.GetRequiredService<IDialogService>();
            var quest = _serviceProvider.GetRequiredService<IQuestService>();
            
            var questDetail = new QuestDetail(db, gui, dialog, quest)
            {
                QuestId = questId,
                Name = name
            };

            return questDetail;
        }
    }
}
