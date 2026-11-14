using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Native;

/// <summary>Changes a held blaster's mesh without deleting the client's currently attached item.</summary>
internal static unsafe class WeaponAppearanceIdentity
{
    private static readonly WeaponAppearanceIdentityCache Cache = new();
    private static readonly HashSet<uint> Retiring = new();
    private static FunctionHook* _write;
    private static FunctionHook* _read;
    [ThreadStatic] private static bool _canonical;

    [NWNEventHandler(ScriptName.OnModuleLoad)]
    public static void Register()
    {
        if (_write != null) return;
        var library = NativeLibrary.GetMainProgramHandle();
        delegate* unmanaged<void*, uint, void> write = &Write;
        delegate* unmanaged<void*, uint> read = &Read;
        _write = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(library,
            "_ZN11CNWSMessage19WriteOBJECTIDServerEj"), (IntPtr)write, HookOrder.Early);
        _read = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(library,
            "_ZN11CNWSMessage18ReadOBJECTIDServerEv"), (IntPtr)read, HookOrder.Early);
    }

    internal static bool Select(uint creature, uint item, BaseItem type)
    {
        if (_write == null || type is not (BaseItem.Pistol or BaseItem.Sling or BaseItem.LegacyPistol or
                BaseItem.Rifle or BaseItem.CompanionPistol)) return false;
        try
        {
            var appearance = new WeaponAppearanceKey((int)type,
                WeaponFit.GetPart(item, AppearanceWeapon.Top), WeaponFit.GetPart(item, AppearanceWeapon.Middle),
                WeaponFit.GetPart(item, AppearanceWeapon.Bottom));
            Cache.Select(item, appearance, Reserve, out var changed);
            if (changed) WeaponAppearanceDraw.Begin(creature);
            return true;
        }
        catch (Exception exception)
        {
            Log.WriteError(exception, "Could not prepare the blaster appearance cache");
            return false;
        }
    }

    private static uint Reserve()
    {
        var identity = CreateObject(ObjectType.Item, "b_rifle", GetStartingLocation());
        if (!GetIsObjectValid(identity)) throw new InvalidOperationException("Could not reserve an appearance identity.");
        NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(identity).AsNWSItem().RemoveFromArea();
        return identity;
    }

    [UnmanagedCallersOnly]
    private static void Write(void* message, uint item) =>
        ((delegate* unmanaged<void*, uint, void>)_write->m_trampoline)(message, _canonical ? item : Cache.Write(item));

    [UnmanagedCallersOnly]
    private static uint Read(void* message) =>
        Cache.Read(((delegate* unmanaged<void*, uint>)_read->m_trampoline)(message));

    [NWNEventHandler(ScriptName.OnModuleHeartbeat)]
    public static void Prune()
    {
        var server = NWNXLib.g_pAppManager.m_pServerExoApp;
        foreach (var item in Cache.Items)
        {
            var identities = Cache.Identities(item);
            if (Retiring.Contains(item))
            {
                // Keep inbound aliases pointed at the destroyed real item until their reserved objects are gone.
                if (identities.Any(GetIsObjectValid)) continue;
                Cache.Forget(item);
                Retiring.Remove(item);
                continue;
            }
            if (GetIsObjectValid(item)) continue;
            try
            {
                _canonical = true;
                foreach (var player in server.GetPlayerList())
                foreach (var identity in identities.Prepend(item))
                    server.GetNWSMessage().SendServerPlayerItemUpdate_DestroyItem(player, identity);
            }
            finally { _canonical = false; }
            foreach (var identity in identities.Where(GetIsObjectValid)) DestroyObject(identity);
            Retiring.Add(item);
        }
    }
}
