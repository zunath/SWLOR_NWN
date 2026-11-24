using System.Collections.Generic;
using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Runs the work queued for native weapon rolls when the engine applies their damage. ResolveAttack
    /// rolls when the swing or shot starts and calls SignalMeleeDamage or SignalRangedDamage at once; those
    /// create each landed slot's combat damage effect and queue its application as an AI event for the moment
    /// the swing connects or the projectile arrives. The signalled attacks end just before the combat round's
    /// current attack, which is how NWNX's attack event reads them. Each slot's work is bound to the ID of its
    /// own scheduled effect and runs when exactly that effect is applied.
    /// </summary>
    public static unsafe class NativeWeaponImpactSignal
    {
        private const string SignalMeleeDamageSymbol = "_ZN12CNWSCreature17SignalMeleeDamageEP10CNWSObjecti";
        private const string SignalRangedDamageSymbol = "_ZN12CNWSCreature18SignalRangedDamageEP10CNWSObjecti";
        private const string ApplyDamageSymbol = "_ZN21CNWSEffectListHandler13OnApplyDamageEP10CNWSObjectP11CGameEffecti";
        // Damage effect integer the engine sets on weapon (combat) damage; NWNX DealDamage documents it.
        private const int CombatDamageInteger = 36;
        // CServerAIMaster event that applies an effect; its event data is the CGameEffect. Observed live: the
        // signal queues each weapon hit's damage as this event, caller the attacker and object the target.
        private const uint ApplyEffectEventId = 5;

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
            int[][] slotDamage = null;
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
                            // Each slot's rolled damage identifies which of the signal's effects is its own.
                            slotDamage = new int[attacks][];
                            for (var offset = 0; offset < attacks; offset++)
                            {
                                var data = round.GetAttack((byte)(firstAttack + offset));
                                slotDamage[offset] = new int[CombatRecordDamageBreakdown.NativeDamageTypeCount];
                                for (var index = 0; index < slotDamage[offset].Length; index++)
                                    slotDamage[offset][index] = data.m_nDamage[index];
                            }
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
                // The signal created its slots' damage effects, whose IDs lie between the two probes, and queued
                // each one's application as an AI event against the target.
                var effects = FindScheduledCombatDamage(attacker, targetId, effectIdBefore, NextEffectId());
                var unbound = NativeWeaponImpactQueue.Bind(attacker, targetId, firstAttack, slotDamage, effects, DateTime.UtcNow);
                if (unbound.Count > 0)
                    RunInScriptContext(unbound);
            }
            catch (Exception ex)
            {
                LogFailure(ex);
            }
        }

        /// <summary>
        /// Lists the combat damage effects with IDs strictly between the probes that the AI event queue holds for
        /// application by <paramref name="attacker"/> to <paramref name="target"/>.
        /// </summary>
        private static List<(ulong Id, int[] Damage)> FindScheduledCombatDamage(uint attacker, uint target,
            ulong effectIdBefore, ulong effectIdAfter)
        {
            var effects = new List<(ulong Id, int[] Damage)>();
            var queue = NWNXLib.g_pAppManager.m_pServerExoApp.GetServerAIMaster().m_lEventQueue;
            for (var position = queue.GetHeadPos(); position != null && position.Pointer != IntPtr.Zero; position = position.pNext)
            {
                var node = queue.GetAtPos(position);
                if (node == null || node.m_nEventId != ApplyEffectEventId || node.m_nCallerObjectId != attacker ||
                    node.m_nObjectId != target || node.m_pEventData == null)
                    continue;
                var effect = CGameEffect.FromPointer(node.m_pEventData);
                if (effect.m_nID <= effectIdBefore || effect.m_nID >= effectIdAfter ||
                    effect.GetInteger(CombatDamageInteger) == 0)
                    continue;
                var damage = new int[CombatRecordDamageBreakdown.NativeDamageTypeCount];
                for (var index = 0; index < damage.Length; index++)
                    damage[index] = effect.GetInteger(index);
                effects.Add((effect.m_nID, damage));
            }

            return effects;
        }

        private static void RunInScriptContext(IReadOnlyList<Action> impacts)
        {
            ServerManager.Executor.ExecuteInScriptContext(() =>
            {
                foreach (var impact in impacts)
                    impact();
            });
        }

        [UnmanagedCallersOnly]
        private static int OnApplyDamage(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame)
        {
            var original = (delegate* unmanaged<void*, void*, void*, int, int>)_applyDamageHook->m_trampoline;
            if (!NativeWeaponImpactQueue.HasBound || bLoadingGame != 0 || pObject == null || pEffect == null)
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
                    RunInScriptContext(impacts);
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
