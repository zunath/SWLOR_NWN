using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Service
{
    public class BaseService : IBaseService
    {
        private readonly INWScript _;
        private readonly INWNXEvents _nwnxEvents;
        private readonly IDialogService _dialog;
        private readonly IDataContext _db;

        public BaseService(INWScript script,
            INWNXEvents nwnxEvents,
            IDialogService dialog,
            IDataContext db)
        {
            _ = script;
            _nwnxEvents = nwnxEvents;
            _dialog = dialog;
            _db = db;
        }

        public BaseData GetPlayerTempData(NWPlayer player)
        {
            if (!player.Data.ContainsKey("BASE_SERVICE_DATA"))
            {
                player.Data["BASE_SERVICE_DATA"] = new BaseData();
            }

            return player.Data["BASE_SERVICE_DATA"];
        }

        public void ClearPlayerTempData(NWPlayer player)
        {
            if (player.Data.ContainsKey("BASE_SERVICE_DATA"))
            {
                player.Data.Remove("BASE_SERVICE_DATA");
            }
        }

        public void OnModuleUseFeat()
        {
            NWPlayer player = NWPlayer.Wrap(Object.OBJECT_SELF);
            int featID = _nwnxEvents.OnFeatUsed_GetFeatID();
            Location targetLocation = _nwnxEvents.OnFeatUsed_GetTargetLocation();
            NWArea targetArea = NWArea.Wrap(_.GetAreaFromLocation(targetLocation));

            if (featID != (int)CustomFeatType.BaseManagementTool) return;

            Area dbArea = _db.Areas.Single(x => x.Resref == targetArea.Resref);
            if (!dbArea.IsBuildable)
            {
                player.FloatingText("You cannot manage bases in this area.");
                return;
            }

            var data = GetPlayerTempData(player);
            data.TargetArea = targetArea;
            data.TargetLocation = targetLocation;
            data.TargetObject = _nwnxEvents.OnItemUsed_GetTarget();

            player.ClearAllActions();
            _dialog.StartConversation(player, player, "BaseManagementTool");
        }
    }
}
