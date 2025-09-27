using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.NWNX;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Domain.Crafting.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.ValueObjects;

namespace SWLOR.Component.Inventory.Feature.ItemDefinition
{
    public class FishingRodItemDefinition: IItemListDefinition
    {
        private readonly IItemCacheService _itemCache;
        private readonly IServiceProvider _serviceProvider;
        private IItemBuilder Builder => _serviceProvider.GetRequiredService<IItemBuilder>();

        public FishingRodItemDefinition(IItemCacheService itemCache, IServiceProvider serviceProvider)
        {
            _itemCache = itemCache;
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded service to break circular dependency
        private IFishingService FishingService => _serviceProvider.GetRequiredService<IFishingService>();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            FishingRod();

            return Builder.Build();
        }

        private void FishingRod()
        {
            Builder.Create(FishingService.FishingRodTag)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (item != target && 
                        !FishingService.IsItemBait(target))
                        return "Only bait may be selected.";

                    if (GetItemPossessor(target) != user)
                        return "Bait must be located within your inventory.";

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var existingBait = GetLocalString(item, FishingService.ActiveBaitVariable);
                    var remainingBait = GetLocalInt(item, FishingService.RemainingBaitVariable);

                    if (!string.IsNullOrWhiteSpace(existingBait))
                    {
                        if (remainingBait > 0)
                        {
                            var bait = ObjectPlugin.Deserialize(existingBait);
                            ObjectPlugin.AcquireItem(user, bait);

                            SetItemStackSize(bait, remainingBait);
                            SendMessageToPC(user, $"Unloaded bait.");
                        }

                        DeleteLocalString(item, FishingService.ActiveBaitVariable);
                        DeleteLocalInt(item, FishingService.RemainingBaitVariable);
                    }

                    if (FishingService.IsItemBait(target))
                    {
                        var resref = GetResRef(target);
                        var baitType = FishingService.GetBaitByResref(resref);
                        var stackSize = GetItemStackSize(target);
                        var serialized = ObjectPlugin.Serialize(target);
                        SetLocalString(item, FishingService.ActiveBaitVariable, serialized);
                        SetLocalInt(item, FishingService.RemainingBaitVariable, stackSize);
                        SetLocalInt(item, FishingService.LoadedBaitTypeVariable, (int)baitType);

                        DestroyObject(target);

                        var baitName = _itemCache.GetItemNameByResref(GetResRef(target));
                        SendMessageToPC(user, $"Loaded bait: {stackSize}x {baitName}");
                    }
                });
        }
    }
}
