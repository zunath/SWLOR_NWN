using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    /// <summary>
    /// Rift Keys (plan section 31): used in space, out of combat, a key opens a private rift pocket for the user's ship and
    /// up to two party ships nearby, and is spent.
    /// </summary>
    public class RiftKeyItemDefinition : IItemListDefinition
    {
        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            foreach (var key in RiftRunTuning.Keys)
            {
                _builder.Create(key.Resref)
                    .ValidationAction((user, item, _, _, _) => Space.GetRiftKeyRefusal(user, item))
                    .ApplyAction((user, item, _, _, _) => Space.StartRiftRun(user, item));
            }

            return _builder.Build();
        }
    }
}
