using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NWN.Native.API;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using Vector = NWN.Native.API.Vector;
using Vector2 = System.Numerics.Vector2;

namespace SWLOR.Game.Server.Domain.Administration
{
    /// <summary>
    /// DM avatar visibility and movement: the logic and state that used to be static in <c>DMPresence</c>,
    /// moved here unchanged. Static callers reach the container's instance through <c>GameServices.DMPresence</c>.
    /// </summary>
    public class DMPresenceService : IDMPresenceService
    {
        private readonly Lazy<IDMObjectSearchService> _dmObjectSearch;
        private readonly Lazy<IGuiService> _gui;

        public DMPresenceService(Lazy<IDMObjectSearchService> dmObjectSearch, Lazy<IGuiService> gui)
        {
            _dmObjectSearch = dmObjectSearch;
            _gui = gui;
        }

        // How far from its target the engine's jump action looks for a safe point.
        private const float JumpSearchRadius = 20f;

        private readonly Dictionary<(string DMId, GuiWindowType Type), uint> _windowTargets = new();

        public bool GetIsManifested(uint dm)
        {
            dm = GetDM(dm);
            var creature = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(dm);
            return creature?.m_pStats != null && creature.m_pStats.m_bDMManifested != 0;
        }

        public void SetIsManifested(uint dm, bool isManifested)
        {
            dm = GetDM(dm);
            // A possessing DM's avatar no longer passes GetIsDM.
            if (!IsControlledByDM(dm))
                return;

            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var creature = server.GetCreatureByGameObjectID(dm);
            if (creature?.m_pStats == null)
                return;

            creature.m_pStats.m_bDMManifested = isManifested ? 1 : 0;
            if (!isManifested)
                creature.PacifyCreature();

            RefreshAvatarFade(dm);

            Log.Write(LogGroup.DM, $"{DescribeDM(dm)} {(isManifested ? "appeared" : "disappeared")} in {GetResRef(GetArea(dm))}.");
        }

        public void RefreshAvatarFade(uint dm)
        {
            dm = GetDM(dm);
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            // The server never tells the DM's client about the change itself. The login state message runs the
            // client's own Appear/Disappear handler, which fades the avatar on the DM's screen like the native button.
            // The client applies it to whatever the DM controls, and this lookup finds no client while they possess one.
            var player = server.GetClientObjectByObjectId(dm);
            if (player != null)
                server.GetNWSMessage().SendServerToPlayerDungeonMasterLoginState(player, 1, GetIsManifested(dm) ? 1 : 0);
        }

        public void JumpTo(uint dm, Location location, string destination)
        {
            var area = GetAreaFromLocation(location);
            var position = GetPositionFromLocation(location);
            var facing = GetFacingFromLocation(location);
            var nearby = DMJumpSpots
                .GetSpotsAround(new Vector2(position.X, position.Y))
                .Select(spot => Location(area, new Vector3(spot.X, spot.Y, position.Z), facing));

            JumpToFirstLanding(dm, nearby.Prepend(location), destination);
        }

        public void JumpToArea(uint dm, uint area, string destination)
        {
            JumpToFirstLanding(dm, GetAreaJumpLocations(area), destination);
        }

        public bool CanLandAt(uint dm, Location location)
        {
            return ComputeSafeLanding(dm, location);
        }

        // The engine's own safe-point search, which reads native structures; a unit test subclass answers it.
        protected virtual bool ComputeSafeLanding(uint dm, Location location)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var creature = server.GetCreatureByGameObjectID(GetControlledCreature(dm));
            var area = server.GetAreaByGameObjectID(GetAreaFromLocation(location));
            if (creature?.m_pcPathfindInformation == null || area == null)
                return false;

            // The same search, set up the same way, as the jump action (CNWSCreature::AIActionJumpToPoint).
            var position = GetPositionFromLocation(location);
            var pathfinding = creature.m_pcPathfindInformation;
            pathfinding.ResetInterTileSearchData();
            pathfinding.ResetGridSearchData();
            pathfinding.m_oidSelf = creature.m_idSelf;
            using var target = new Vector { x = position.X, y = position.Y, z = position.Z };
            using var landing = new Vector();
            var isSafe = area.ComputeSafeLocation(target, JumpSearchRadius, pathfinding, 1, landing) == 1;
            pathfinding.ResetInterAreaSearchData();
            return isSafe;
        }

        // The area's jump spots come from the DM object search. That service takes this one, so the call stays in this
        // wrapper and a unit test subclass answers it.
        protected virtual IEnumerable<Location> GetAreaJumpLocations(uint area) => _dmObjectSearch.Value.GetAreaJumpLocations(area);

        // The engine cancels a jump to a spot it cannot land on and tells no one, so the spot is checked first.
        private void JumpToFirstLanding(uint dm, IEnumerable<Location> locations, string destination)
        {
            var landing = locations.FirstOrDefault(location => CanLandAt(dm, location));
            if (landing == null)
            {
                SendMessage(dm, "There is nowhere to land there.");
                Log.Write(LogGroup.DM, $"{DescribeDM(dm)} could not jump to {destination}: nowhere to land.");
                return;
            }

            AssignCommand(GetControlledCreature(dm), () =>
            {
                ClearAllActions();
                JumpToLocation(landing);
            });

            Log.Write(LogGroup.DM, $"{DescribeDM(dm)} jumped to {destination} in {GetResRef(GetAreaFromLocation(landing))}.");
        }

        public string Summon(uint dm, uint target)
        {
            if (!GetIsObjectValid(target))
                return "That target no longer exists.";

            // Moving a possessed creature would drag the DM controlling it along.
            if (GetIsDMPossessed(target))
                return "A DM is possessing that creature.";

            if (GetIsPC(target) && Space.IsPlayerInSpaceMode(target))
                return $"{GetName(target)} is piloting a ship in space and cannot be summoned.";

            var controlled = GetControlledCreature(dm);
            var location = GetLocation(controlled);
            AssignCommand(target, () =>
            {
                ClearAllActions();
                JumpToLocation(location);
            });

            Log.Write(LogGroup.DM, $"{DescribeDM(dm)} summoned {GetName(target)} ({GetObjectUUID(target)}) to {GetResRef(GetArea(controlled))}.");
            return string.Empty;
        }

        public void Boot(uint dm, uint player)
        {
            Log.Write(LogGroup.DM, $"{DescribeDM(dm)} booted {GetName(player)} ({GetPCPlayerName(player)}) [{GetPCPublicCDKey(player)}] ({GetObjectUUID(player)}).");
            BootPC(player, "You have been disconnected by a DM.");
        }

        public string DescribeDM(uint dm)
        {
            dm = GetDM(dm);
            // The account and CD key resolve from what the DM controls, which is the possessed creature while possessing.
            var controlled = GetControlledCreature(dm);
            return $"{GetName(dm)} ({GetPCPlayerName(controlled)}) [{GetPCPublicCDKey(controlled)}]";
        }

        public uint GetDM(uint actor)
        {
            return GetIsDMPossessed(actor) ? GetMaster(actor) : actor;
        }

        public uint GetControlledCreature(uint actor)
        {
            // GetIsPC is true for whatever a player controls, and false for a possessing DM's avatar.
            if (GetIsPC(actor))
                return actor;

            // Possession only changes the object a player controls; GetFirstPC keeps returning the avatar.
            var controlled = PlayerPlugin.GetGameObject(actor);
            return GetIsDMPossessed(controlled) ? controlled : actor;
        }

        public bool IsControlledByDM(uint player)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return true;

            var controlled = PlayerPlugin.GetGameObject(player);
            return GetIsDM(controlled) || GetIsDMPossessed(controlled);
        }

        public bool IsDMPresentInArea(uint area)
        {
            for (var pc = GetFirstPC(); GetIsObjectValid(pc); pc = GetNextPC())
            {
                if (IsControlledByDM(pc) &&
                    (GetArea(pc) == area || GetArea(GetControlledCreature(pc)) == area))
                    return true;
            }

            return false;
        }

        public void ToggleWindow(uint actor, GuiWindowType type, GuiPayloadBase payload = null, uint tetherObject = OBJECT_INVALID)
        {
            var dm = GetDM(actor);
            var controlled = GetControlledCreature(dm);

            // After possession starts or ends, the window can still be open on the object the DM no longer
            // controls. Move it to the controlled creature instead of opening a second copy over the same view model.
            var moved = false;
            foreach (var target in GetWindowTargets(dm, type))
            {
                if (target == controlled || !GetIsObjectValid(target) || !_gui.Value.IsWindowOpen(target, type))
                    continue;

                _gui.Value.ClosePlayerWindow(dm, type, target);
                moved = true;
            }

            if (!moved || !_gui.Value.IsWindowOpen(controlled, type))
                _gui.Value.TogglePlayerWindow(dm, type, payload, tetherObject, controlled);

            _windowTargets[(GetObjectUUID(dm), type)] = controlled;
        }

        public void ShowWindow(uint actor, GuiWindowType type)
        {
            var dm = GetDM(actor);
            var controlled = GetControlledCreature(dm);
            foreach (var target in GetWindowTargets(dm, type))
            {
                if (target != controlled && GetIsObjectValid(target) && _gui.Value.IsWindowOpen(target, type))
                    _gui.Value.ClosePlayerWindow(dm, type, target);
            }

            if (!_gui.Value.IsWindowOpen(controlled, type))
                _gui.Value.TogglePlayerWindow(dm, type, uiTarget: controlled);

            _windowTargets[(GetObjectUUID(dm), type)] = controlled;
        }

        public IEnumerable<uint> GetShownOn(uint actor, GuiWindowType type)
        {
            var dm = GetDM(actor);
            var targets = GetIsDM(dm)
                ? GetWindowTargets(dm, type).Append(actor)
                : new[] { actor };
            return targets.Distinct().Where(target => GetIsObjectValid(target));
        }

        // The DM avatar, and the object a window was last opened on through ToggleWindow.
        private IEnumerable<uint> GetWindowTargets(uint dm, GuiWindowType type)
        {
            yield return dm;
            if (_windowTargets.TryGetValue((GetObjectUUID(dm), type), out var previous) && previous != dm)
                yield return previous;
        }

        public void SendMessage(uint actor, string message)
        {
            SendMessageToPC(GetControlledCreature(actor), message);
        }

        public void CloseWindow(uint actor, GuiWindowType type)
        {
            var dm = GetDM(actor);
            _gui.Value.ClosePlayerWindow(dm, type, GetControlledCreature(dm));
            foreach (var target in GetWindowTargets(dm, type))
            {
                if (GetIsObjectValid(target))
                    _gui.Value.ClosePlayerWindow(dm, type, target);
            }
        }
    }
}
