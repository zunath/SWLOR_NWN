using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class DebuggingChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new();
        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            //MoveDoor();
            EnmityDebugger();
            GetObjectId();
            ResetBeast();

            return _builder.Build();
        }

        private static Location GetDoorLocation(uint building, float orientationOverride = 0f, float sqrtValue = 0f)
        {
            var area = GetArea(building);
            var location = GetLocation(building);
            var orientationAdjustment = orientationOverride != 0f ? orientationOverride : 200.31f;
            var sqrtAdjustment = sqrtValue != 0f ? sqrtValue : 13.0f;

            var position = GetPositionFromLocation(location);
            var orientation = GetFacingFromLocation(location);

            orientation = orientation + orientationAdjustment;
            if (orientation > 360.0)
                orientation -= 360.0f;

            var mod = sqrt(sqrtAdjustment) * sin(orientation);
            position.X += mod;

            mod = sqrt(sqrtAdjustment) * cos(orientation);
            position.Y -= mod;
            var doorLocation = Location(area, position, orientation);
            return doorLocation;
        }

        private void MoveDoor()
        {
            _builder.Create("movedoor")
                .Description("Debugging")
                .Permissions(AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    var orientationOverride = float.Parse(args[0]);
                    var sqrtValue = float.Parse(args[1]);
                    var placeable = GetObjectByTag("house1");

                    if (!GetIsObjectValid(placeable))
                    {
                        var waypoint = GetWaypointByTag("DEBUG_HOUSE");
                        placeable = CreateObject(ObjectType.Placeable, "house1", GetLocation(waypoint));
                    }

                    var doorLocation = GetDoorLocation(placeable, orientationOverride, sqrtValue);

                    var door = GetLocalObject(placeable, "PROPERTY_DOOR");
                    if (GetIsObjectValid(door))
                        DestroyObject(door);

                    door = CreateObject(ObjectType.Placeable, "building_ent1", doorLocation);
                    SetLocalObject(placeable, "PROPERTY_DOOR", door);

                    SendMessageToPC(user, $"{orientationOverride} {sqrtValue}");
                });
        }

        private void EnmityDebugger()
        {
            _builder.Create("enmitydebugger")
                .Description("Opens enmity debugger window")
                .Permissions(AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    Gui.TogglePlayerWindow(user, GuiWindowType.DebugEnmity);
                });
        }

        private void GetObjectId()
        {
            _builder.Create("objectid", "oid")
                .Description("Gets the object Id of a target")
                .Permissions(AuthorizationLevel.Admin)
                .RequiresTarget()
                .Action((user, target, location, args) =>
                {
                    SendMessageToPC(user, $"{GetName(target)} oid = {target}");
                });
        }

        private void ResetBeast()
        {
            _builder.Create("resetbeast")
                .Description("")
                .Permissions(AuthorizationLevel.Admin)
                .RequiresTarget()
                .Action((user, target, location, args) =>
                {
                    var playerId = GetObjectUUID(user);
                    var dbPlayer = DB.Get<Player>(playerId);
                    
                    dbPlayer.ActiveBeastId = string.Empty;
                    DB.Set(dbPlayer);

                    DB.Delete<Beast>(dbPlayer.ActiveBeastId);

                });
        }
    }
}
