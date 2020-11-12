using System;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.Data.Entity;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Event.Area;
using SWLOR.Game.Server.Legacy.Event.Module;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using PerkType = SWLOR.Game.Server.Legacy.Enumeration.PerkType;
using Player = SWLOR.Game.Server.Legacy.Data.Entity.Player;
using Skill = SWLOR.Game.Server.Core.NWScript.Enum.Skill;

namespace SWLOR.Game.Server.Legacy.Service
{
    public static class PlayerService
    {
        public static Player GetPlayerEntity(Guid playerID)
        {
            if (playerID == null) throw new ArgumentException("Invalid player ID.", nameof(playerID));
            return DataService.Player.GetByID(playerID);
        }

        public static void SaveLocation(NWPlayer player)
        {
            if (!player.IsPlayer) return;
            if (player.GetLocalInt("IS_SHIP") == 1) return;
            if (player.GetLocalInt("IS_GUNNER") == 1) return;

            var area = GetArea(player);
            var areaTag = GetTag(area);
            var areaResref = GetResRef(area);
            var areaIsInstance = AreaService.IsAreaInstance(area);
            if (GetIsObjectValid(area) && areaTag != "ooc_area" && areaTag != "tutorial" && !areaIsInstance)
            {
                LoggingService.Trace(TraceComponent.Space, "Saving location in area " + GetName(area));
                var entity = GetPlayerEntity(player.GlobalID);
                entity.LocationAreaResref = areaResref;
                entity.LocationX = player.Position.X;
                entity.LocationY = player.Position.Y;
                entity.LocationZ = player.Position.Z;
                entity.LocationOrientation = (player.Facing);
                entity.LocationInstanceID = null;

                if (string.IsNullOrWhiteSpace(entity.RespawnAreaResref))
                {
                    NWObject waypoint = GetWaypointByTag("DTH_DEFAULT_RESPAWN_POINT");
                    entity.RespawnAreaResref = GetResRef(waypoint.Area);
                    entity.RespawnLocationOrientation = waypoint.Facing;
                    entity.RespawnLocationX = waypoint.Position.X;
                    entity.RespawnLocationY = waypoint.Position.Y;
                    entity.RespawnLocationZ = waypoint.Position.Z;
                }

                DataService.SubmitDataChange(entity, DatabaseActionType.Update);
            }
            else if (areaIsInstance)
            {
                LoggingService.Trace(TraceComponent.Space, "Saving location in instance area " + GetName(area));
                var instanceID = GetLocalString(area, "PC_BASE_STRUCTURE_ID");
                if (string.IsNullOrWhiteSpace(instanceID))
                {
                    instanceID = GetLocalString(area, "PC_BASE_ID");
                }

                LoggingService.Trace(TraceComponent.Space, "Saving character in instance ID: " + instanceID);

                if (!string.IsNullOrWhiteSpace(instanceID))
                {
                    var entity = GetPlayerEntity(player.GlobalID);
                    entity.LocationAreaResref = areaResref;
                    entity.LocationX = player.Position.X;
                    entity.LocationY = player.Position.Y;
                    entity.LocationZ = player.Position.Z;
                    entity.LocationOrientation = (player.Facing);
                    entity.LocationInstanceID = new Guid(instanceID);

                    DataService.SubmitDataChange(entity, DatabaseActionType.Update);
                }
            }
        }
    }
}
