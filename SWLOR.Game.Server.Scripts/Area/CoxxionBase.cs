using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Scripting.Contracts;

namespace SWLOR.Game.Server.Scripts.Area
{
    public class CoxxionBase: IScript
    {
        private Guid _moduleLoadID;

        public void SubscribeEvents()
        {
            _moduleLoadID = MessageHub.Instance.Subscribe<OnModuleLoad>(msg => LoadDoors());
        }

        public void UnsubscribeEvents()
        {
            MessageHub.Instance.Unsubscribe(_moduleLoadID);
        }

        public void Main()
        {
        }

        /// <summary>
        /// The Coxxion Base has a set of doors which are opened based on terminals used by the player.
        /// We store these doors in the area's custom data for later use.
        /// </summary>
        public void LoadDoors()
        {
            // This area used to be an instance so this code made sense then.
            // Now, however, it would make more sense to refactor and store them as local objects.
            var area = NWModule.Get().Areas.SingleOrDefault(x => x.Resref == "v_cox_base");
            if (area == null) return;

            var doors = new List<NWObject>();

            var obj = _.GetFirstObjectInArea(area);
            while (_.GetIsObjectValid(obj) == _.TRUE)
            {
                int colorID = _.GetLocalInt(obj, "DOOR_COLOR");

                if (colorID > 0)
                {
                    doors.Add(obj);
                }

                obj = _.GetNextObjectInArea(area);
            }

            area.Data["DOORS"] = doors;
        }
    }
}
