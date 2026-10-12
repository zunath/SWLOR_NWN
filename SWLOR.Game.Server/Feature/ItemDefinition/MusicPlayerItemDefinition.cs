using System.Collections.Generic;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.MusicService;

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
            _builder.Create("MUSIC_PLAYER")
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    Gui.TogglePlayerWindow(user, GuiWindowType.MusicPicker, new MusicPickerPayload(MusicPickerMode.Personal));
                });
        }
    }
}
