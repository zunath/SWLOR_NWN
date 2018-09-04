using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using System;
using System.Collections.Generic;
using System.Linq;
using static NWN.NWScript;
using Object = NWN.Object;

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

        public PCTempBaseData GetPlayerTempData(NWPlayer player)
        {
            if (!player.Data.ContainsKey("BASE_SERVICE_DATA"))
            {
                player.Data["BASE_SERVICE_DATA"] = new PCTempBaseData();
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

            var data = GetPlayerTempData(player);
            data.TargetArea = targetArea;
            data.TargetLocation = targetLocation;
            data.TargetObject = _nwnxEvents.OnItemUsed_GetTarget();

            player.ClearAllActions();
            _dialog.StartConversation(player, player, "BaseManagementTool");
        }


        public void PurchaseArea(NWPlayer player, NWArea area, string sector)
        {
            if (sector != AreaSector.Northwest && sector != AreaSector.Northeast &&
                sector != AreaSector.Southwest && sector != AreaSector.Southeast)
                throw new ArgumentException(nameof(sector) + " must match one of the valid sector values: NE, NW, SE, SW");

            if (area.Width < 32) throw new Exception("Area must be at least 32 tiles wide.");
            if (area.Height < 32) throw new Exception("Area must be at least 32 tiles high.");


            var dbArea = _db.Areas.Single(x => x.Resref == area.Resref);
            string existingOwner = string.Empty;
            switch (sector)
            {
                case AreaSector.Northwest: existingOwner = dbArea.NorthwestOwner; break;
                case AreaSector.Northeast: existingOwner = dbArea.NortheastOwner; break;
                case AreaSector.Southwest: existingOwner = dbArea.SouthwestOwner; break;
                case AreaSector.Southeast: existingOwner = dbArea.SoutheastOwner; break;
            }

            if (!string.IsNullOrWhiteSpace(existingOwner))
            {
                player.SendMessage("Another player already owns that sector.");
                return;
            }

            if (player.Gold < dbArea.PurchasePrice)
            {
                player.SendMessage("You do not have enough credits to purchase that sector.");
                return;
            }

            player.AssignCommand(() => _.TakeGoldFromCreature(dbArea.PurchasePrice, player.Object, TRUE));

            switch (sector)
            {
                case AreaSector.Northwest: dbArea.NorthwestOwner = player.GlobalID; break;
                case AreaSector.Northeast: dbArea.NortheastOwner = player.GlobalID; break;
                case AreaSector.Southwest: dbArea.SouthwestOwner = player.GlobalID; break;
                case AreaSector.Southeast: dbArea.SoutheastOwner = player.GlobalID; break;
            }

            PCBase pcBase = new PCBase
            {
                AreaResref = dbArea.Resref,
                PlayerID = player.GlobalID,
                DateInitialPurchase = DateTime.UtcNow,
                DateRentDue = DateTime.UtcNow.AddDays(7),
                Sector = sector
            };

            _db.PCBases.Add(pcBase);
            _db.SaveChanges();

            player.FloatingText("You purchase " + area.Name + " (" + sector + ") for " + dbArea.PurchasePrice + " credits.");
        }

        public void OnModuleHeartbeat()
        {
            NWModule module = NWModule.Get();
            int ticks = module.GetLocalInt("BASE_SERVICE_TICKS") + 1;


            if (ticks >= 10)
            {
                List<Tuple<string, string>> playerIDs = new List<Tuple<string, string>>();
                var pcBases = _db.PCBases.Where(x => x.DateRentDue <= DateTime.UtcNow).ToList();
                
                foreach (var pcBase in pcBases)
                {
                    Area dbArea = _db.Areas.Single(x => x.Resref == pcBase.AreaResref);

                    if (pcBase.Sector == AreaSector.Northeast) dbArea.NortheastOwner = null;
                    else if (pcBase.Sector == AreaSector.Northwest) dbArea.NorthwestOwner = null;
                    else if (pcBase.Sector == AreaSector.Southeast) dbArea.SoutheastOwner = null;
                    else if (pcBase.Sector == AreaSector.Southwest) dbArea.SouthwestOwner = null;

                    playerIDs.Add(new Tuple<string, string>(pcBase.PlayerID, dbArea.Name + " (" + pcBase.Sector + ")"));
                    _db.PCBases.Remove(pcBase);
                }

                var players = module.Players;
                foreach(var removed in playerIDs)
                {
                    var existing = players.FirstOrDefault(x => x.GlobalID == removed.Item1);
                    if(existing != null)
                    {
                        existing.FloatingText("Your lease on " + removed.Item2 + " has ended. All structures and items have been impounded by the planetary government. Speak with them to pay a fee and retrieve your goods.");
                    }
                }

                _db.SaveChanges();
                ticks = 0;
            }

            module.SetLocalInt("BASE_SERVICE_TICKS", ticks);
        }

    }
}
