using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Native
{
    public static unsafe class ChatChannelLabels
    {
        private static FunctionHook* _loginConfirmHook;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            if (_loginConfirmHook != null) return;
            delegate* unmanaged<void*, uint, int> confirm = &ConfirmLogin;
            _loginConfirmHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
                "_ZN11CNWSMessage31SendServerToPlayerLogin_ConfirmEj"), (IntPtr)confirm, HookOrder.Early);
        }

        [UnmanagedCallersOnly]
        private static int ConfirmLogin(void* message, uint playerId)
        {
            var original = (delegate* unmanaged<void*, uint, int>)_loginConfirmHook->m_trampoline;
            try
            {
                // The client constructs CTL_LST_CHATMODE on Login/Confirm (major 2, minor 5),
                // not on the area packet. There may be no character object at this point.
                var info = NWNXLib.g_pAppManager.m_pServerExoApp.GetNetLayer().GetPlayerInfo(playerId);
                if (info == null) throw new InvalidOperationException("Login confirmation has no player information.");
                SendLabel(playerId, info.m_bGameMasterPrivileges != 0, (id, strRef, text) =>
                {
                    using var label = new CExoString(text);
                    CNWSMessage.FromPointer(message).SendServerToPlayerSetTlkOverride(id, strRef, label);
                });
            }
            catch (Exception exception)
            {
                Log.WriteError(exception, "Could not set chat channel labels before login confirmation");
            }
            return original(message, playerId);
        }

        internal static void SendLabel(uint playerId, bool isDM, Action<uint, int, string> send)
        {
            send(playerId, 66751, isDM ? "Shout" : "Disabled");
        }
    }
}
