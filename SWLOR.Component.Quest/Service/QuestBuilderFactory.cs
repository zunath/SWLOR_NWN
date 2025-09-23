using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Quest.Contracts;

namespace SWLOR.Component.Quest.Service
{
    /// <summary>
    /// Factory implementation for creating QuestBuilder instances.
    /// This ensures proper DI management and stateless operation.
    /// </summary>
    public class QuestBuilderFactory : IQuestBuilderFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public QuestBuilderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Creates a new QuestBuilder instance with all required dependencies injected.
        /// Each call returns a fresh instance suitable for building a single quest.
        /// </summary>
        /// <returns>A new QuestBuilder instance</returns>
        public IQuestBuilder Create()
        {
            return _serviceProvider.GetRequiredService<IQuestBuilder>();
        }
    }
}
