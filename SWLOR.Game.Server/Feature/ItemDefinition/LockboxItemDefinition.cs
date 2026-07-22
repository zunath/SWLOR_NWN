using System.Collections.Generic;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.SlicingService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    /// <summary>
    /// Opens the shared, turn-based slicing interface for portable lockboxes. Puzzle
    /// seed, failures, and integrity live on the item, so trading it never rerolls or
    /// repairs the lock.
    /// </summary>
    public class LockboxItemDefinition : IItemListDefinition
    {
        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            for (var tier = 1; tier <= 5; tier++)
                Lockbox($"lockbox_t{tier}", tier);

            return _builder.Build();
        }

        private void Lockbox(string resref, int tier)
        {
            _builder.Create(resref)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                    SlicingSession.ValidateStart(user, item, SlicingSourceType.Lockbox, tier))
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (!SlicingSession.TryStart(user, item, SlicingSourceType.Lockbox, tier, out var error))
                    {
                        SendMessageToPC(user, error);
                        return;
                    }

                    var payload = new SlicingPayload(item, SlicingSourceType.Lockbox, tier);
                    Gui.TogglePlayerWindow(user, GuiWindowType.Slicing, payload);
                });
        }
    }
}
