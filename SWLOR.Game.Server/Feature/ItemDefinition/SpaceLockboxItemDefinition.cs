using System.Collections.Generic;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.SlicingService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    /// <summary>
    /// Space lockboxes (plan section 12): dropped by destroyed NPC ships and sliced later by their owner, out of combat,
    /// the way a ground lockbox is. They pay from the space lockbox table: components, ammo, credits and parts with rare
    /// traits, never cargo.
    /// </summary>
    public class SpaceLockboxItemDefinition : IItemListDefinition
    {
        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            for (var tier = 1; tier <= SpaceSlicingItems.Lockboxes.Count; tier++)
                Lockbox(SpaceSlicingItems.Lockboxes[tier - 1], tier);

            return _builder.Build();
        }

        private void Lockbox(string resref, int tier)
        {
            _builder.Create(resref)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                    SlicingSession.ValidateStart(user, item, SlicingSourceType.SpaceLockbox, tier))
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (!SlicingSession.TryStart(user, item, SlicingSourceType.SpaceLockbox, tier, out var error))
                    {
                        SendMessageToPC(user, error);
                        return;
                    }

                    var payload = new SlicingPayload(item, SlicingSourceType.SpaceLockbox, tier);
                    Gui.TogglePlayerWindow(user, GuiWindowType.Slicing, payload);
                });
        }
    }
}
