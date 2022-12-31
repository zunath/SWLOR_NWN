using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.ItemService;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class DroidControlItemDefinition: IItemListDefinition
    {
        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            DroidControlUnit();

            return _builder.Build();
        }

        private void DroidControlUnit()
        {
            _builder.Create("droid_control")
                .Delay(3f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .ApplyAction((user, item, target, location) =>
                {

                });
        }
    }
}
