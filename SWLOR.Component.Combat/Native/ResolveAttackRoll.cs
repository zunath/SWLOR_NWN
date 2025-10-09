using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using SWLOR.Component.Combat.Contracts;
using SWLOR.Component.Combat.Enums;
using SWLOR.NWN.API.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Infrastructure;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;

namespace SWLOR.Component.Combat.Native
{
    public static unsafe class ResolveAttackRoll
    {
        private static readonly IScriptExecutor _executor = ServiceContainer.GetService<IScriptExecutor>();
        private static readonly IProfilerPluginService _profilerPlugin = ServiceContainer.GetService<IProfilerPluginService>();
        private static readonly ICombatCalculationService _combatCalculationService = ServiceContainer.GetService<ICombatCalculationService>();
        private static readonly ICombatMessagingService _combatMessagingService = ServiceContainer.GetService<ICombatMessagingService>();

        // Attack result constants
        private const int AttackResultAutomaticHit = 7;
        private const int AttackResultRegularHit = 1;
        private const int AttackResultDeflect = 2;
        private const int AttackResultCriticalHit = 3;
        private const int AttackResultMiss = 4;

        // Default values
        private const int DefaultMissedBy = 1;
        private const int DefaultToHitMod = 1;
        private const int DefaultToHitRoll = 1;

        internal delegate void ResolveAttackRollHook(void* thisPtr, void* pTarget);

        // ReSharper disable once NotAccessedField.Local
        private static ResolveAttackRollHook _callOriginal;

        public static void RegisterHook()
        {
            delegate* unmanaged<void*, void*, void> pHook = &OnResolveAttackRoll;
            var functionPtr = NativeLibrary.GetExport(
                NativeLibrary.GetMainProgramHandle(), "_ZN12CNWSCreature17ResolveAttackRollEP10CNWSObject");
            var hookPtr = NWNXAPI.RequestFunctionHook(
                functionPtr,
                (IntPtr)pHook,
                -1000000);
            _callOriginal = Marshal.GetDelegateForFunctionPointer<ResolveAttackRollHook>((IntPtr)hookPtr);
        }

        [UnmanagedCallersOnly]
        private static void OnResolveAttackRoll(void* thisPtr, void* pTarget)
        {
            _executor.ExecuteInScriptContext(() =>
            {
                var pAttacker = CNWSCreature.FromPointer(thisPtr);
                var attacker = pAttacker.m_idSelf;
                var pDefender = CNWSObject.FromPointer(pTarget);
                var defender = pDefender == null 
                    ? OBJECT_INVALID 
                    : pDefender.m_idSelf;
                var pCombatRound = pAttacker.m_pcCombatRound;
                var weapon = pCombatRound.GetCurrentAttackWeapon() == null 
                    ? OBJECT_INVALID 
                    : pCombatRound.GetCurrentAttackWeapon().m_idSelf;
                var areaTag = GetTag(GetArea(attacker));
                var pAttackData = pCombatRound.GetAttack(pCombatRound.m_nCurrentAttack);

                _profilerPlugin.PushPerfScope("RunScript",
                    "Script", $"NATIVE:{nameof(OnResolveAttackRoll)}",
                    "Area", areaTag,
                    "ObjectType", "Creature");

                if (!GetIsObjectValid(defender))
                {
                    _profilerPlugin.PopPerfScope();
                    return;
                }

                if (GetObjectType(defender) != ObjectType.Creature)
                {
                    pAttackData.m_nAttackResult = AttackResultAutomaticHit;
                    _profilerPlugin.PopPerfScope();
                    return;
                }

                var hitResult = _combatCalculationService.CalculateHitType(attacker, defender, weapon);
                var hitType = hitResult.HitType;
                var isHit = hitType == HitType.Critical || hitType == HitType.Normal;

                switch (hitType)
                {
                    case HitType.Critical:
                        pAttackData.m_nAttackResult = AttackResultCriticalHit;
                        break;
                    case HitType.Normal:
                        pAttackData.m_nAttackResult = AttackResultRegularHit;
                        break;
                    case HitType.Miss:
                        pAttackData.m_nAttackResult = AttackResultMiss;
                        pAttackData.m_nMissedBy = DefaultMissedBy;
                        break;
                    case HitType.Deflect:
                        pAttackData.m_nAttackResult = AttackResultDeflect;
                        pAttackData.m_nMissedBy = DefaultMissedBy;
                        break;
                }

                pAttacker.ResolveDefensiveEffects(pDefender, isHit ? 1 : 0);

                var message = _combatMessagingService.BuildCombatLogMessage(
                    attacker,
                    defender,
                    pAttackData.m_nAttackResult,
                    hitResult.HitRate);

                SendMessageToPC(attacker, message);
                SendMessageToPC(defender, message);

                pAttackData.m_nToHitMod = DefaultToHitMod;
                pAttackData.m_nToHitRoll = DefaultToHitRoll;

                _profilerPlugin.PopPerfScope();
            });
        }
    }
}