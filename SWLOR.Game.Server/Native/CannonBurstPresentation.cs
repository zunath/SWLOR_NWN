using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;

namespace SWLOR.Game.Server.Native;

/// <summary>
/// Presents each cannon shot as a three-bolt burst. Bolts are drawn from the cannon's muzzle anchors
/// in place of the native projectile, which clients draw from the hands; until the anchors are ready
/// the native projectile is replayed instead. The bolts are cosmetic: the attack, its damage and its
/// ammunition resolve once, natively.
/// </summary>
public static unsafe class CannonBurstPresentation
{
    // Types 0-5 are weapon projectiles (plain and elemental); 6 and 7 are spells.
    private const byte LastWeaponProjectileType = 5;
    public static readonly TimeSpan[] FollowUpDelays =
    {
        TimeSpan.FromMilliseconds(100),
        TimeSpan.FromMilliseconds(200),
    };
    private static FunctionHook* _hook;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeVector
    {
        public float X;
        public float Y;
        public float Z;
    }

    private readonly record struct Shot(uint Origin, uint Target, NativeVector From, NativeVector To,
        uint Delta, byte Type, uint Spell, byte Result, byte Path, uint Weapon);

    public static bool IsWeaponProjectile(byte type) => type <= LastWeaponProjectileType;

    public static bool IsBurstWeapon(BaseItem? rightHand) =>
        rightHand.HasValue && Item.CannonBaseItemTypes.Contains(rightHand.Value);

    [NWNEventHandler(ScriptName.OnModuleLoad)]
    public static void RegisterHook()
    {
        if (_hook != null) return;
        var library = NativeLibrary.GetMainProgramHandle();
        delegate* unmanaged<void*, uint, uint, NativeVector, NativeVector, uint, byte, uint, byte, byte, void> broadcast = &Broadcast;
        _hook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(library,
            "_ZN10CNWSObject23BroadcastSafeProjectileEjj6VectorS0_jhjhh"), (IntPtr)broadcast, HookOrder.Late);
    }

    private static void Send(void* self, in Shot shot)
    {
        var original = (delegate* unmanaged<void*, uint, uint, NativeVector, NativeVector, uint, byte, uint, byte, byte, void>)
            _hook->m_trampoline;
        original(self, shot.Origin, shot.Target, shot.From, shot.To, shot.Delta, shot.Type, shot.Spell, shot.Result, shot.Path);
    }

    [UnmanagedCallersOnly]
    private static void Broadcast(void* self, uint origin, uint target, NativeVector from, NativeVector to,
        uint delta, byte type, uint spell, byte result, byte path)
    {
        var shot = new Shot(origin, target, from, to, delta, type, spell, result, path, OBJECT_INVALID);
        var weapon = OBJECT_INVALID;
        var drawnFromMuzzle = false;
        try
        {
            if (IsWeaponProjectile(type) && GetObjectType(origin) == ObjectType.Creature)
            {
                var rightHand = GetItemInSlot(InventorySlot.RightHand, origin);
                if (GetIsObjectValid(rightHand) && IsBurstWeapon(GetBaseItemType(rightHand)))
                {
                    weapon = rightHand;
                    var hit = CannonMuzzleAnchors.IsHitResult(result);
                    drawnFromMuzzle = CannonMuzzleAnchors.Launch(origin, target, hit);
                    if (drawnFromMuzzle)
                        foreach (var delay in FollowUpDelays)
                            Scheduler.Schedule(() => FollowUp(origin, target, hit, rightHand), delay);
                }
            }
        }
        catch (Exception ex) { Log.WriteError(ex, "Could not draw a cannon burst"); }
        if (drawnFromMuzzle) return;

        Send(self, shot);
        if (weapon == OBJECT_INVALID) return;
        var followUp = shot with { Weapon = weapon };
        foreach (var delay in FollowUpDelays)
            Scheduler.Schedule(() => Replay(followUp), delay);
    }

    private static void FollowUp(uint origin, uint target, bool hit, uint weapon)
    {
        if (!GetIsObjectValid(origin) || GetIsDead(origin) ||
            GetItemInSlot(InventorySlot.RightHand, origin) != weapon) return;
        CannonMuzzleAnchors.Launch(origin, target, hit);
    }

    private static void Replay(Shot shot)
    {
        if (!GetIsObjectValid(shot.Origin) || GetIsDead(shot.Origin) ||
            GetItemInSlot(InventorySlot.RightHand, shot.Origin) != shot.Weapon) return;
        if (GetIsObjectValid(shot.Target) && GetArea(shot.Target) != GetArea(shot.Origin)) return;
        var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(shot.Origin)?.AsNWSObject();
        if (native == null) return;
        Send((void*)native.Pointer, shot);
    }
}
