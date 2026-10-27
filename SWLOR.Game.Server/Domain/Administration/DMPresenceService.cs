using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Domain.Administration
{
    /// <summary>
    /// DM avatar visibility and movement: the logic and state that used to be static in <c>Service.DMPresence</c>,
    /// moved here unchanged. The facade forwards every call to the container's instance.
    /// </summary>
    public class DMPresenceService : IDMPresenceService
    {
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
            AssignCommand(GetControlledCreature(dm), () =>
            {
                ClearAllActions();
                JumpToLocation(location);
            });

            Log.Write(LogGroup.DM, $"{DescribeDM(dm)} jumped to {destination} in {GetResRef(GetAreaFromLocation(location))}.");
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
                if (target == controlled || !GetIsObjectValid(target) || !Gui.IsWindowOpen(target, type))
                    continue;

                Gui.ClosePlayerWindow(dm, type, target);
                moved = true;
            }

            if (!moved || !Gui.IsWindowOpen(controlled, type))
                Gui.TogglePlayerWindow(dm, type, payload, tetherObject, controlled);

            _windowTargets[(GetObjectUUID(dm), type)] = controlled;
        }

        public void ShowWindow(uint actor, GuiWindowType type)
        {
            var dm = GetDM(actor);
            var controlled = GetControlledCreature(dm);
            foreach (var target in GetWindowTargets(dm, type))
            {
                if (target != controlled && GetIsObjectValid(target) && Gui.IsWindowOpen(target, type))
                    Gui.ClosePlayerWindow(dm, type, target);
            }

            if (!Gui.IsWindowOpen(controlled, type))
                Gui.TogglePlayerWindow(dm, type, uiTarget: controlled);

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
            Gui.ClosePlayerWindow(dm, type, GetControlledCreature(dm));
            foreach (var target in GetWindowTargets(dm, type))
            {
                if (GetIsObjectValid(target))
                    Gui.ClosePlayerWindow(dm, type, target);
            }
        }
    }
}
