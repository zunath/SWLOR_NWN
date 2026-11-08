using System.Collections.Generic;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.MusicService;
using static SWLOR.Game.Server.Composition.GameServices;
using SWLOR.Game.Server.Domain.World;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class MusicPlayerItemDefinition : IItemListDefinition
    {
        private readonly ItemBuilder _builder = new ItemBuilder();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            MusicPlayer();

            return _builder.Build();
        }

        private void MusicPlayer()
        {
            _builder.Create(MusicService.MusicPlayerTag)
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var payload = new MusicPickerPayload(MusicPickerMode.Personal, musicPlayer: item);

                    // DMs share one picker window across possession changes, as with jukeboxes and the toolbar.
                    if (GetIsDM(user) || GetIsDMPossessed(user))
                        DMPresence.ToggleWindow(user, GuiWindowType.MusicPicker, payload);
                    else
                        Gui.TogglePlayerWindow(user, GuiWindowType.MusicPicker, payload);
                });
        }
    }
}
