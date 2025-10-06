using SWLOR.Shared.Domain.Skill.Contracts;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Skill.EventHandlers
{
    /// <summary>
    /// Event handlers for Skill-related game events.
    /// This class handles the infrastructure layer of receiving game events and delegating to the skill service.
    /// </summary>
    public class SkillEventHandlers
    {
        private readonly ISkillService _skillService;

        public SkillEventHandlers(
            ISkillService skillService,
            IEventAggregator eventAggregator)
        {
            _skillService = skillService;

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleEnter>(e => AddMissingSkills());
            eventAggregator.Subscribe<OnModuleLoad>(e => LoadMappings());
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => CacheData());
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => CacheXPChartData());
        }

        /// <summary>
        /// If a player is missing any skills in their DB record, they will be added here.
        /// </summary>
        public void AddMissingSkills()
        {
            _skillService.AddMissingSkills();
        }

        /// <summary>
        /// Handles creating all of the mapping dictionaries used by the skill system on module load.
        /// </summary>
        public void LoadMappings()
        {
            _skillService.LoadMappings();
        }

        /// <summary>
        /// When the module loads, skills and categories are organized into dictionaries for quick look-ups later on.
        /// </summary>
        public void CacheData()
        {
            _skillService.CacheData();
        }

        /// <summary>
        /// When the module loads, cache all XP chart data used for quick access.
        /// </summary>
        public void CacheXPChartData()
        {
            _skillService.CacheXPChartData();
        }
    }
}
