using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.NWN.API.NWNX;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class FishingRodItemDefinition: IItemListDefinition
    {
        private readonly IItemCacheService _itemCache;
        private readonly IFishingService _fishingService;
        private readonly ItemBuilder _builder = new();

        public FishingRodItemDefinition(IItemCacheService itemCache, IFishingService fishingService)
        {
            _itemCache = itemCache;
            _fishingService = fishingService;
        }

        public Dictionary<string, ItemDetail> BuildItems()
        {
            FishingRod();

            return _builder.Build();
        }

        private void FishingRod()
        {
            _builder.Create(_fishingService.FishingRodTag)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (item != target && 
                        !_fishingService.IsItemBait(target))
                        return "Only bait may be selected.";

                    if (GetItemPossessor(target) != user)
                        return "Bait must be located within your inventory.";

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var existingBait = GetLocalString(item, _fishingService.ActiveBaitVariable);
                    var remainingBait = GetLocalInt(item, _fishingService.RemainingBaitVariable);

                    if (!string.IsNullOrWhiteSpace(existingBait))
                    {
                        if (remainingBait > 0)
                        {
                            var bait = ObjectPlugin.Deserialize(existingBait);
                            ObjectPlugin.AcquireItem(user, bait);

                            SetItemStackSize(bait, remainingBait);
                            SendMessageToPC(user, $"Unloaded bait.");
                        }

                        DeleteLocalString(item, _fishingService.ActiveBaitVariable);
                        DeleteLocalInt(item, _fishingService.RemainingBaitVariable);
                    }

                    if (_fishingService.IsItemBait(target))
                    {
                        var resref = GetResRef(target);
                        var baitType = _fishingService.GetBaitByResref(resref);
                        var stackSize = GetItemStackSize(target);
                        var serialized = ObjectPlugin.Serialize(target);
                        SetLocalString(item, _fishingService.ActiveBaitVariable, serialized);
                        SetLocalInt(item, _fishingService.RemainingBaitVariable, stackSize);
                        SetLocalInt(item, _fishingService.LoadedBaitTypeVariable, (int)baitType);

                        DestroyObject(target);

                        var baitName = _itemCache.GetItemNameByResref(GetResRef(target));
                        SendMessageToPC(user, $"Loaded bait: {stackSize}x {baitName}");
                    }
                });
        }
    }
}
