using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;

namespace SWLOR.Game.Server.Native;

/// <summary>
/// The engine treats only its native launcher rows (crossbows, bows and slings) as ammunition
/// users. Any other ranged row is handled like a thrown weapon: each shot decrements the weapon's
/// own stack, and the projectile message carries the weapon's base item in one byte. Cannons keep
/// their unlimited ammunition and present as rifle bolts instead.
/// </summary>
public static unsafe class CannonRangedCompatibility
{
    // A native launcher row with the same ammunition as the cannon row, for the client's projectile.
    public const BaseItem ClientProjectileBaseItem = BaseItem.Rifle;
    private static FunctionHook* _ammunitionHook;
    private static FunctionHook* _sendHook;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeVector
    {
        public float X;
        public float Y;
        public float Z;
    }

    public static bool UsesUnlimitedLauncherAmmunition(BaseItem? weapon) =>
        weapon.HasValue && Item.CannonBaseItemTypes.Contains(weapon.Value);

    /// <summary>The one-byte base item clients use to pick the projectile drawn for a weapon shot.</summary>
    public static byte ClientBaseItem(byte sent, BaseItem? weapon) =>
        UsesUnlimitedLauncherAmmunition(weapon) && sent == unchecked((byte)weapon!.Value)
            ? (byte)ClientProjectileBaseItem
            : sent;

    [NWNEventHandler(ScriptName.OnModuleLoad)]
    public static void RegisterHooks()
    {
        if (_ammunitionHook != null) return;
        var library = NativeLibrary.GetMainProgramHandle();
        delegate* unmanaged<void*, uint, void> ammunition = &ResolveAmmunition;
        delegate* unmanaged<void*, void*, uint, uint, NativeVector, NativeVector, uint, byte, uint, byte, byte, byte, int> send =
            &SendSafeProjectile;
        _ammunitionHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(library,
            "_ZN12CNWSCreature17ResolveAmmunitionEj"), (IntPtr)ammunition, HookOrder.Late);
        _sendHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(library,
            "_ZN11CNWSMessage32SendServerToPlayerSafeProjectileEP10CNWSPlayerjj6VectorS2_jhjhhh"),
            (IntPtr)send, HookOrder.Late);
    }

    private static BaseItem? RightHandBaseItem(uint creature)
    {
        if (GetObjectType(creature) != SWLOR.NWN.API.NWScript.Enum.ObjectType.Creature) return null;
        var weapon = GetItemInSlot(InventorySlot.RightHand, creature);
        return GetIsObjectValid(weapon) ? GetBaseItemType(weapon) : null;
    }

    [UnmanagedCallersOnly]
    private static void ResolveAmmunition(void* creature, uint timeIndex)
    {
        try
        {
            if (UsesUnlimitedLauncherAmmunition(RightHandBaseItem(CNWSCreature.FromPointer(creature).m_idSelf)))
                return;
        }
        catch (Exception ex) { Log.WriteError(ex, "Could not resolve cannon ammunition"); }
        ((delegate* unmanaged<void*, uint, void>)_ammunitionHook->m_trampoline)(creature, timeIndex);
    }

    [UnmanagedCallersOnly]
    private static int SendSafeProjectile(void* message, void* player, uint origin, uint target,
        NativeVector from, NativeVector to, uint delta, byte type, uint spell, byte baseItem, byte result, byte path)
    {
        try { baseItem = ClientBaseItem(baseItem, RightHandBaseItem(origin)); }
        catch (Exception ex) { Log.WriteError(ex, "Could not present a cannon projectile"); }
        var original = (delegate* unmanaged<void*, void*, uint, uint, NativeVector, NativeVector, uint, byte, uint, byte, byte, byte, int>)
            _sendHook->m_trampoline;
        return original(message, player, origin, target, from, to, delta, type, spell, baseItem, result, path);
    }
}
