using System.Collections.Generic;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The color doors of the Coxxion Base dungeon: the handler and the door list that used to be static on the Coxxion terminal's
    /// menu, moved here unchanged. The event handler runs on the container's instance.
    /// </summary>
    public class CoxxionBaseDoorsService : ICoxxionBaseDoorsService
    {
        private readonly IAreaService _area;
        private readonly List<uint> _areaDoors = new List<uint>();

        public CoxxionBaseDoorsService(IAreaService area)
        {
            _area = area;
        }

        public IReadOnlyList<uint> Doors => _areaDoors;

        /// <summary>
        /// When the module loads, store the doors for the Coxxion Base dungeon into cache.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public void LoadDoors()
        {
            var area = _area.GetAreaByResref("v_cox_base");
            if (!GetIsObjectValid(area)) return;

            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                var colorId = GetLocalInt(obj, "DOOR_COLOR");

                if (colorId > 0)
                {
                    _areaDoors.Add(obj);
                }
            }
        }
    }
}
