using SWLOR.Shared.Domain.Crafting.Contracts;
using SWLOR.Shared.Domain.Crafting.Events;
using SWLOR.Shared.Domain.Properties.Events;
using SWLOR.Shared.Domain.Skill.Events;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Crafting.EventHandlers
{
    internal class CraftingServiceEventHandlers
    {
        private readonly ICraftService _craftService;
        private readonly IFishingService _fishingService;

        public CraftingServiceEventHandlers(
            ICraftService craftService,
            IFishingService fishingService,
            IEventAggregator eventAggregator)
        {
            _craftService = craftService;
            _fishingService = fishingService;

            // Subscribe to events
            eventAggregator.Subscribe<OnSkillDataCached>(e => CacheCraftData());
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => CacheFishingData());
            eventAggregator.Subscribe<OnCraftUsed>(e => UseCraftingDevice());
            eventAggregator.Subscribe<OnRefineryUsed>(e => UseRefinery());
            eventAggregator.Subscribe<OnResearchTerminal>(e => UseResearchTerminal());
            eventAggregator.Subscribe<OnDeleteProperty>(e => OnRemoveProperty());
            eventAggregator.Subscribe<OnFishPoint>(e => ClickFishingPoint());
            eventAggregator.Subscribe<OnFinishFishing>(e => FinishFishing());
        }

        /// <summary>
        /// When the skill cache has finished loading, recipe and category data is cached.
        /// </summary>
        public void CacheCraftData()
        {
            _craftService.CacheData();
        }

        /// <summary>
        /// When the module loads, retrieve and organize all fishing data for quick look-ups.
        /// </summary>
        public void CacheFishingData()
        {
            _fishingService.CacheData();
        }

        /// <summary>
        /// When a crafting device is used, display the recipe menu.
        /// </summary>
        public void UseCraftingDevice()
        {
            _craftService.UseCraftingDevice();
        }
        public void UseRefinery()
        {
            _craftService.UseRefinery();
        }
        public void UseResearchTerminal()
        {
            _craftService.UseResearchTerminal();
        }

        /// <summary>
        /// When a property is removed, also remove any associated research jobs.
        /// </summary>
        public void OnRemoveProperty()
        {
            _craftService.OnRemoveProperty();
        }

        /// <summary>
        /// Runs when a player interacts with a fishing point.
        /// </summary>
        public void ClickFishingPoint()
        {
            _fishingService.ClickFishingPoint();
        }

        /// <summary>
        /// Runs when the fishing process completes.
        /// </summary>
        public void FinishFishing()
        {
            _fishingService.FinishFishing();
        }
    }
}
