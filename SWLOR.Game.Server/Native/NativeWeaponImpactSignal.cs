using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Runs the work queued for native weapon rolls when the engine applies their damage. ResolveAttack
    /// rolls when the swing or shot starts and schedules SignalMeleeDamage or SignalRangedDamage for the
    /// moment it connects. The attacks a signal applies end just before the combat round's current
    /// attack, which is how NWNX's attack event reads them.
    /// </summary>
    public static unsafe class NativeWeaponImpactSignal
    {
        private const string SignalMeleeDamageSymbol = "_ZN12CNWSCreature17SignalMeleeDamageEP10CNWSObjecti";
        private const string SignalRangedDamageSymbol = "_ZN12CNWSCreature18SignalRangedDamageEP10CNWSObjecti";

        private static FunctionHook* _meleeHook;
        private static FunctionHook* _rangedHook;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHooks()
        {
            if (_meleeHook != null) return;
            var library = NativeLibrary.GetMainProgramHandle();
            delegate* unmanaged<void*, void*, int, void> melee = &OnSignalMeleeDamage;
            delegate* unmanaged<void*, void*, int, void> ranged = &OnSignalRangedDamage;
            _meleeHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(library, SignalMeleeDamageSymbol),
                (IntPtr)melee, HookOrder.Late);
            _rangedHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(library, SignalRangedDamageSymbol),
                (IntPtr)ranged, HookOrder.Late);
        }

        [UnmanagedCallersOnly]
        private static void OnSignalMeleeDamage(void* creature, void* target, int attacks)
        {
            var original = (delegate* unmanaged<void*, void*, int, void>)_meleeHook->m_trampoline;
            Signal(original, creature, target, attacks);
        }

        [UnmanagedCallersOnly]
        private static void OnSignalRangedDamage(void* creature, void* target, int attacks)
        {
            var original = (delegate* unmanaged<void*, void*, int, void>)_rangedHook->m_trampoline;
            Signal(original, creature, target, attacks);
        }

        private static void Signal(delegate* unmanaged<void*, void*, int, void> original, void* creature, void* target, int attacks)
        {
            if (NativeWeaponImpactQueue.IsEmpty || creature == null || target == null || attacks <= 0)
            {
                original(creature, target, attacks);
                return;
            }

            // Read the slots before the engine applies them: a killing blow runs death scripts inside the call.
            var attacker = OBJECT_INVALID;
            var targetId = OBJECT_INVALID;
            var firstAttack = 0;
            try
            {
                var nativeAttacker = CNWSCreature.FromPointer(creature);
                var round = nativeAttacker.m_pcCombatRound;
                if (round != null)
                {
                    attacker = nativeAttacker.m_idSelf;
                    targetId = CNWSObject.FromPointer(target).m_idSelf;
                    firstAttack = (byte)(round.m_nCurrentAttack - attacks);
                }
            }
            catch (Exception ex)
            {
                attacker = OBJECT_INVALID;
                Log.Write(LogGroup.Error, $"{nameof(NativeWeaponImpactSignal)}: {ex.ToMessageAndCompleteStacktrace()}");
            }

            original(creature, target, attacks);
            if (attacker == OBJECT_INVALID)
                return;

            try
            {
                var impacts = NativeWeaponImpactQueue.Take(attacker, targetId, firstAttack, attacks, DateTime.UtcNow);
                if (impacts.Count == 0)
                    return;
                ServerManager.Executor.ExecuteInScriptContext(() =>
                {
                    foreach (var impact in impacts)
                        impact();
                });
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"{nameof(NativeWeaponImpactSignal)}: {ex.ToMessageAndCompleteStacktrace()}");
            }
        }
    }
}
