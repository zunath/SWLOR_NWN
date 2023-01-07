using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;

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
            _builder.Create(Fishing.FishingRodTag)
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
                    var existingBait = GetLocalString(item, Fishing.ActiveBaitVariable);
                    var remainingBait = GetLocalInt(item, Fishing.RemainingBaitVariable);

                    if (!string.IsNullOrWhiteSpace(existingBait))
                    {
                        if (remainingBait > 0)
                        {
                            var bait = ObjectPlugin.Deserialize(existingBait);
                            ObjectPlugin.AcquireItem(user, bait);

                            SetItemStackSize(bait, remainingBait);
                            SendMessageToPC(user, $"Unloaded bait.");
                        }

                        DeleteLocalString(item, Fishing.ActiveBaitVariable);
                        DeleteLocalInt(item, Fishing.RemainingBaitVariable);
                    }

                    if (Fishing.IsItemBait(target))
                    {
                        var resref = GetResRef(target);
                        var baitType = Fishing.GetBaitByResref(resref);
                        var stackSize = GetItemStackSize(target);
                        var serialized = ObjectPlugin.Serialize(target);
                        SetLocalString(item, Fishing.ActiveBaitVariable, serialized);
                        SetLocalInt(item, Fishing.RemainingBaitVariable, stackSize);
                        SetLocalInt(item, Fishing.LoadedBaitTypeVariable, (int)baitType);

                        DestroyObject(target);

                        var baitName = Cache.GetItemNameByResref(GetResRef(target));
                        SendMessageToPC(user, $"Loaded bait: {stackSize}x {baitName}");
                    }
                });
        }
    }
}
