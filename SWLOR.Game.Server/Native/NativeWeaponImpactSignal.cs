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
    /// rolls when the swing or shot starts and calls SignalMeleeDamage or SignalRangedDamage at once; those
    /// create each landed roll's combat damage effect and schedule it for the moment the swing connects or
    /// the projectile arrives. The signalled attacks end just before the combat round's current attack, which
    /// is how NWNX's attack event reads them. The scheduled effect is the roll's impact.
    /// </summary>
    public static unsafe class NativeWeaponImpactSignal
    {
        private const string SignalMeleeDamageSymbol = "_ZN12CNWSCreature17SignalMeleeDamageEP10CNWSObjecti";
        private const string SignalRangedDamageSymbol = "_ZN12CNWSCreature18SignalRangedDamageEP10CNWSObjecti";
        private const string ApplyDamageSymbol = "_ZN21CNWSEffectListHandler13OnApplyDamageEP10CNWSObjectP11CGameEffecti";
        // Damage effect integer the engine sets on weapon (combat) damage; NWNX DealDamage documents it.
        private const int CombatDamageInteger = 36;

        private static FunctionHook* _meleeHook;
        private static FunctionHook* _rangedHook;
        private static FunctionHook* _applyDamageHook;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHooks()
        {
            if (_meleeHook != null) return;
            var library = NativeLibrary.GetMainProgramHandle();
            delegate* unmanaged<void*, void*, int, void> melee = &OnSignalMeleeDamage;
            delegate* unmanaged<void*, void*, int, void> ranged = &OnSignalRangedDamage;
            delegate* unmanaged<void*, void*, void*, int, int> applyDamage = &OnApplyDamage;
            _meleeHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(library, SignalMeleeDamageSymbol),
                (IntPtr)melee, HookOrder.Late);
            _rangedHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(library, SignalRangedDamageSymbol),
                (IntPtr)ranged, HookOrder.Late);
            _applyDamageHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(library, ApplyDamageSymbol),
                (IntPtr)applyDamage, HookOrder.Late);
        }

        /// <summary>
        /// Called as the engine starts a native attack roll, hit or miss: work an earlier roll queued in the
        /// same slot must not ride this roll's signal.
        /// </summary>
        internal static void BeginRoll(void* creature)
        {
            if (NativeWeaponImpactQueue.IsEmpty || creature == null)
                return;
            try
            {
                var attacker = CNWSCreature.FromPointer(creature);
                var round = attacker.m_pcCombatRound;
                if (round != null)
                    NativeWeaponImpactQueue.ForgetSlot(attacker.m_idSelf, round.m_nCurrentAttack);
            }
            catch (Exception ex)
            {
                LogFailure(ex);
            }
        }

        [UnmanagedCallersOnly]
        private static void OnSignalMeleeDamage(void* creature, void* target, int attacks)
        {
            Signal((delegate* unmanaged<void*, void*, int, void>)_meleeHook->m_trampoline, creature, target, attacks);
        }

        [UnmanagedCallersOnly]
        private static void OnSignalRangedDamage(void* creature, void* target, int attacks)
        {
            Signal((delegate* unmanaged<void*, void*, int, void>)_rangedHook->m_trampoline, creature, target, attacks);
        }

        private static void Signal(delegate* unmanaged<void*, void*, int, void> original, void* creature, void* target, int attacks)
        {
            var attacker = OBJECT_INVALID;
            var targetId = OBJECT_INVALID;
            var firstAttack = 0;
            ulong effectIdBefore = 0;
            if (!NativeWeaponImpactQueue.IsEmpty && creature != null && target != null && attacks > 0)
            {
                try
                {
                    var nativeAttacker = CNWSCreature.FromPointer(creature);
                    var round = nativeAttacker.m_pcCombatRound;
                    if (round != null)
                    {
                        firstAttack = (byte)(round.m_nCurrentAttack - attacks);
                        if (NativeWeaponImpactQueue.HasQueued(nativeAttacker.m_idSelf, firstAttack, attacks))
                        {
                            attacker = nativeAttacker.m_idSelf;
                            targetId = CNWSObject.FromPointer(target).m_idSelf;
                            effectIdBefore = NextEffectId();
                        }
                    }
                }
                catch (Exception ex)
                {
                    attacker = OBJECT_INVALID;
                    LogFailure(ex);
                }
            }

            original(creature, target, attacks);
            if (attacker == OBJECT_INVALID)
                return;

            try
            {
                // The signal created this roll's damage effects: their IDs lie between the two probes.
                NativeWeaponImpactQueue.Bind(attacker, targetId, firstAttack, attacks,
                    effectIdBefore, NextEffectId(), DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                LogFailure(ex);
            }
        }

        [UnmanagedCallersOnly]
        private static int OnApplyDamage(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame)
        {
            var original = (delegate* unmanaged<void*, void*, void*, int, int>)_applyDamageHook->m_trampoline;
            if (!NativeWeaponImpactQueue.HasSignalled || bLoadingGame != 0 || pObject == null || pEffect == null)
                return original(effectListHandler, pObject, pEffect, bLoadingGame);

            var attacker = OBJECT_INVALID;
            var targetId = OBJECT_INVALID;
            ulong effectId = 0;
            try
            {
                var effect = CGameEffect.FromPointer(pEffect);
                if (effect.GetInteger(CombatDamageInteger) != 0)
                {
                    attacker = effect.m_oidCreator;
                    targetId = CNWSObject.FromPointer(pObject).m_idSelf;
                    effectId = effect.m_nID;
                }
            }
            catch (Exception ex)
            {
                attacker = OBJECT_INVALID;
                LogFailure(ex);
            }

            var result = original(effectListHandler, pObject, pEffect, bLoadingGame);
            if (attacker == OBJECT_INVALID)
                return result;

            try
            {
                var impacts = NativeWeaponImpactQueue.TakeApplied(attacker, targetId, effectId, DateTime.UtcNow);
                if (impacts.Count > 0)
                {
                    ServerManager.Executor.ExecuteInScriptContext(() =>
                    {
                        foreach (var impact in impacts)
                            impact();
                    });
                }
            }
            catch (Exception ex)
            {
                LogFailure(ex);
            }

            return result;
        }

        /// <summary>The ID the engine gives the next effect it creates; the probe takes it and is freed.</summary>
        private static ulong NextEffectId()
        {
            using var probe = new CGameEffect(1);
            return probe.m_nID;
        }

        private static void LogFailure(Exception ex)
        {
            Log.Write(LogGroup.Error, $"{nameof(NativeWeaponImpactSignal)}: {ex.ToMessageAndCompleteStacktrace()}");
        }
    }
}
