using System;
using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Native
{
    public static unsafe class NeckAppearanceSerialization
    {
        private static FunctionHook* _hook;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            if (_hook != null) return;
            delegate* unmanaged<void*, void*, void*, void> write = &Write;
            _hook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
                "_ZN11CNWSMessage26AddItemAppearanceToMessageEP10CNWSPlayerP8CNWSItem"),
                (IntPtr)write, HookOrder.Late);
        }

        [UnmanagedCallersOnly]
        private static void Write(void* message, void* player, void* itemPointer)
        {
            var original = (delegate* unmanaged<void*, void*, void*, void>)_hook->m_trampoline;
            var item = CNWSItem.FromPointer(itemPointer);
            var source = item.m_nArmorModelPart[(int)AppearanceArmor.Neck];
            try
            {
                try { item.m_nArmorModelPart[(int)AppearanceArmor.Neck] = NeckModelRenderer.GetItemProjection(item); }
                catch (Exception ex) { Log.WriteError(ex, "Could not project the equipped neck model"); }
                original(message, player, itemPointer);
            }
            finally
            {
                // The synchronous native writer only serializes appearance. Inventory,
                // saves, outfit copies, stats, and subsequent scripts see the authored ID.
                item.m_nArmorModelPart[(int)AppearanceArmor.Neck] = source;
            }
        }
    }
}
