using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PropertyService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Entity
{
    public class WorldProperty: PropertyBase
    {
        public override void SpawnIntoWorld(uint area)
        {
            uint targetArea;

            // If no interior layout is defined, the provided area will be used.
            if (InteriorLayout == PropertyLayoutType.Invalid)
            {
                targetArea = area;
            }
            // If there is an interior, create an instance and use that as our target.
            else
            {
                var layout = Property.GetLayoutByType(InteriorLayout);
                targetArea = CreateArea(layout.AreaInstanceResref);
                Property.RegisterInstance(Id.ToString(), targetArea);

                SetName(targetArea, CustomName);
            }

            if (ChildPropertyIds.Count > 0)
            {
                var query = new DBQuery<WorldProperty>()
                    .AddFieldSearch(nameof(Id), ChildPropertyIds);
                var children = DB.Search(query);

                foreach (var child in children)
                {
                    child.SpawnIntoWorld(targetArea);
                }
            }
        }
    }
}
