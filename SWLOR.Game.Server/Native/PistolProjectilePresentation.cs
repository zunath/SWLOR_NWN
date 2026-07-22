using System;
using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using SWLOR.Game.Server.Core;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Canonical pistols use native sling base item 61 so NWN attaches them to the right hand
    /// and permits a shield in the left hand. The client also uses that base item from the
    /// projectile packet to select the sling's arcing, double-emitter bullet presentation.
    /// Keep all server-side sling behavior, but present normal weapon projectiles as native
    /// pistol/bow base item 11 so the client renders the existing straight, single blaster bolt.
    /// </summary>
    public static unsafe class PistolProjectilePresentation
    {
        private const byte HighestWeaponProjectileType = 5;
        private const byte DefaultProjectilePathType = 0;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct NativeVector
        {
            public float X;
            public float Y;
            public float Z;
        }

        private delegate int SendServerToPlayerSafeProjectileHook(
            void* thisPtr,
            void* player,
            uint originator,
            uint target,
            NativeVector originatorPosition,
            NativeVector targetPosition,
            uint deltaTime,
            byte projectileType,
            uint spellId,
            byte baseItemId,
            byte attackResult,
            byte projectilePathType);

        // ReSharper disable once NotAccessedField.Local
        private static SendServerToPlayerSafeProjectileHook _callOriginal;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            delegate* unmanaged<
                void*, void*, uint, uint, NativeVector, NativeVector, uint, byte, uint, byte, byte, byte, int> hook =
                &OnSendServerToPlayerSafeProjectile;
            var functionPtr = NativeLibrary.GetExport(
                NativeLibrary.GetMainProgramHandle(),
                "_ZN11CNWSMessage32SendServerToPlayerSafeProjectileEP10CNWSPlayerjj6VectorS2_jhjhhh");
            var hookPtr = NWNXAPI.RequestFunctionHook(functionPtr, (IntPtr)hook, -1000000);
            _callOriginal = Marshal.GetDelegateForFunctionPointer<SendServerToPlayerSafeProjectileHook>((IntPtr)hookPtr);
        }

        [UnmanagedCallersOnly]
        private static int OnSendServerToPlayerSafeProjectile(
            void* thisPtr,
            void* player,
            uint originator,
            uint target,
            NativeVector originatorPosition,
            NativeVector targetPosition,
            uint deltaTime,
            byte projectileType,
            uint spellId,
            byte baseItemId,
            byte attackResult,
            byte projectilePathType)
        {
            var clientBaseItemId = GetClientBaseItemId(projectileType, baseItemId);
            var clientProjectilePathType = GetClientProjectilePathType(projectileType, baseItemId, projectilePathType);
            return _callOriginal(
                thisPtr,
                player,
                originator,
                target,
                originatorPosition,
                targetPosition,
                deltaTime,
                projectileType,
                spellId,
                clientBaseItemId,
                attackResult,
                clientProjectilePathType);
        }

        private static byte GetClientBaseItemId(byte projectileType, byte serverBaseItemId)
        {
            return ShouldUseStraightPistolPresentation(projectileType, serverBaseItemId)
                ? (byte)BaseItem.Pistol
                : serverBaseItemId;
        }

        private static byte GetClientProjectilePathType(
            byte projectileType,
            byte serverBaseItemId,
            byte serverProjectilePathType)
        {
            return ShouldUseStraightPistolPresentation(projectileType, serverBaseItemId)
                ? DefaultProjectilePathType
                : serverProjectilePathType;
        }

        private static bool ShouldUseStraightPistolPresentation(byte projectileType, byte serverBaseItemId)
        {
            return projectileType <= HighestWeaponProjectileType && serverBaseItemId == (byte)BaseItem.Sling;
        }
    }
}
