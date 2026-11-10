using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.NWN.API.NWNX;
using SWLOR.Game.Server.Domain.World;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class FishingRodItemDefinition: IItemListDefinition
    {
        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            FishingRod();

            return _builder.Build();
        }

        private void FishingRod()
        {
            _builder.Create(FishingService.FishingRodTag)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (item != target &&
                        !Fishing.IsItemBait(target))
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
                            // Set the remaining count before acquiring: AcquireItem merges into existing stacks.
                            var bait = ObjectPlugin.Deserialize(existingBait);
                            SetItemStackSize(bait, remainingBait);
                            if (!Item.TryAcquireItem(user, bait))
                            {
                                SendMessageToPC(user, ItemService.InventoryFullMessage);
                                return;
                            }

                            SendMessageToPC(user, $"Unloaded bait.");
                        }

                        DeleteLocalString(item, FishingService.ActiveBaitVariable);
                        DeleteLocalInt(item, FishingService.RemainingBaitVariable);
                    }

                    if (Fishing.IsItemBait(target))
                    {
                        var resref = GetResRef(target);
                        var baitType = Fishing.GetBaitByResref(resref);
                        var stackSize = GetItemStackSize(target);
                        var serialized = ObjectPlugin.Serialize(target);
                        SetLocalString(item, FishingService.ActiveBaitVariable, serialized);
                        SetLocalInt(item, FishingService.RemainingBaitVariable, stackSize);
                        SetLocalInt(item, FishingService.LoadedBaitTypeVariable, (int)baitType);

                        DestroyObject(target);

                        var baitName = Cache.GetItemNameByResref(GetResRef(target));
                        SendMessageToPC(user, $"Loaded bait: {stackSize}x {baitName}");
                    }
                });
        }
    }
}
