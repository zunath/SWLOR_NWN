using SWLOR.Shared.Domain.Crafting.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Crafting;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.Properties;
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

        /// <summary>
        /// When a crafting device is used, display the recipe menu.
        /// </summary>
        [ScriptHandler<OnCraftUsed>]
        public void UseCraftingDevice()
        {
            _craftService.UseCraftingDevice();
        }

        [ScriptHandler<OnRefineryUsed>]
        public void UseRefinery()
        {
            _craftService.UseRefinery();
        }

        [ScriptHandler<OnResearchTerminal>]
        public void UseResearchTerminal()
        {
            _craftService.UseResearchTerminal();
        }

        /// <summary>
        /// When a property is removed, also remove any associated research jobs.
        /// </summary>
        [ScriptHandler<OnDeleteProperty>]
        public void OnRemoveProperty()
        {
            _craftService.OnRemoveProperty();
        }

        /// <summary>
        /// Runs when a player interacts with a fishing point.
        /// </summary>
        [ScriptHandler<OnFishPoint>]
        public void ClickFishingPoint()
        {
            _fishingService.ClickFishingPoint();
        }

        /// <summary>
        /// Runs when the fishing process completes.
        /// </summary>
        [ScriptHandler<OnFinishFishing>]
        public void FinishFishing()
        {
            _fishingService.FinishFishing();
        }
    }
}
