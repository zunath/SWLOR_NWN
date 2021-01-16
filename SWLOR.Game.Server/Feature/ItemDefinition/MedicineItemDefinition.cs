using System.Collections.Generic;
using SWLOR.Game.Server.Service.ItemService;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class MedicineItemDefinition: IItemListDefinition
    {
        public Dictionary<string, ItemDetail> BuildItems()
        {
            var builder = new ItemBuilder();


            return builder.Build();
        }
    }
}
