using SWLOR.Shared.Domain.Skill.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Contracts;
using SWLOR.Shared.Events.Events.Module;
namespace SWLOR.Component.Skill.EventHandlers
{
    /// <summary>
    /// Event handlers for Skill-related game events.
    /// This class handles the infrastructure layer of receiving game events and delegating to the skill service.
    /// </summary>
    public class SkillEventHandlers : IEventHandler
    {
        private readonly ISkillService _skillService;

        public SkillEventHandlers(ISkillService skillService)
        {
            _skillService = skillService;
        }

        /// <summary>
        /// If a player is missing any skills in their DB record, they will be added here.
        /// </summary>
        [ScriptHandler<OnModuleEnter>]
        public void AddMissingSkills()
        {
            _skillService.AddMissingSkills();
        }

        /// <summary>
        /// When the module loads, skills and categories are organized into dictionaries for quick look-ups later on.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheData()
        {
            _skillService.CacheData();
        }

        /// <summary>
        /// When the module loads, cache all XP chart data used for quick access.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheXPChartData()
        {
            _skillService.CacheXPChartData();
        }
    }
}


