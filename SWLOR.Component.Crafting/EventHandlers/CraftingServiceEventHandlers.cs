using SWLOR.Component.Crafting.Contracts;
using SWLOR.Component.Crafting.Service;
using SWLOR.Shared.Domain.Crafting.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.Skill;

namespace SWLOR.Component.Crafting.EventHandlers
{
    internal class CraftingServiceEventHandlers
    {
        private readonly ICraftService _craftService;
        private readonly IFishingService _fishingService;

        public CraftingServiceEventHandlers(
            ICraftService craftService,
            IFishingService fishingService)
        {
            _craftService = craftService;
            _fishingService = fishingService;
        }

        /// <summary>
        /// When the skill cache has finished loading, recipe and category data is cached.
        /// </summary>
        [ScriptHandler<OnSkillDataCached>]
        public void CacheCraftData()
        {
            _craftService.CacheData();
        }

        /// <summary>
        /// When the module loads, retrieve and organize all fishing data for quick look-ups.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheFishingData()
        {
            _fishingService.CacheData();
        }
    }
}
