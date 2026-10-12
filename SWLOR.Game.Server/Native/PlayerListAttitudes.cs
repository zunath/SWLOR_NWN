using System;
using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Native
{
    /// <summary>Preserves the client's hostility selections when name overrides rebuild its player list.</summary>
    public static unsafe class PlayerListAttitudes
    {
        private static FunctionHook* _hook;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            if (_hook != null) return;
            delegate* unmanaged<void*, void*, int> refresh = &Refresh;
            _hook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
                "_ZN11CNWSMessage32SendServerToPlayerPlayerList_AllEP10CNWSPlayer"),
                (IntPtr)refresh, HookOrder.Late);
        }

        [UnmanagedCallersOnly]
        private static int Refresh(void* message, void* player)
        {
            var original = (delegate* unmanaged<void*, void*, int>)_hook->m_trampoline;
            var result = original(message, player);
            try
            {
                Restore(CNWSMessage.FromPointer(message), CNWSPlayer.FromPointer(player));
            }
            catch (Exception exception)
            {
                Log.WriteError(exception, "Could not restore player-list hostility selections");
            }
            return result;
        }

        private static void Restore(CNWSMessage message, CNWSPlayer observer)
        {
            var observerCreature = observer.GetGameObject()?.AsNWSCreature();
            if (observerCreature == null) return;

            // The native full-list packet carries names but resets the client's PvP list
            // to Like in both directions. NWNX_Rename sends it on name updates, so replay
            // hostile directions afterward without changing server attitudes or names.
            foreach (var target in NWNXLib.g_pAppManager.m_pServerExoApp.GetPlayerList())
            {
                if (target.m_nPlayerID == observer.m_nPlayerID) continue;
                var targetCreature = target.GetGameObject()?.AsNWSCreature();
                if (targetCreature == null) continue;

                if (targetCreature.GetPVPPlayerLikesMe(observer.m_oidNWSObject) == 0)
                    message.SendServerToPlayerPVP_Attitude_Change(target.m_nPlayerID, observer.m_nPlayerID, 0);
                if (observerCreature.GetPVPPlayerLikesMe(target.m_oidNWSObject) == 0)
                    message.SendServerToPlayerPVP_Attitude_Change(observer.m_nPlayerID, target.m_nPlayerID, 0);
            }
        }
    }
}
