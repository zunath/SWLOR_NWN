using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using SWLOR.Component.Combat.Contracts;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Inventory.Contracts;
using DamageType = NWN.Native.API.DamageType;

namespace SWLOR.Component.Combat.Native
{
    public static unsafe class GetDamageRoll
    {
        private static readonly IScriptExecutor _scriptExecutor = ServiceContainer.GetService<IScriptExecutor>();
        private static readonly IWeaponStatService _weaponStatService = ServiceContainer.GetService<IWeaponStatService>();
        private static readonly IProfilerPluginService _profilerPlugin = ServiceContainer.GetService<IProfilerPluginService>();
        private static readonly ICombatCalculationService _combatCalculationService = ServiceContainer.GetService<ICombatCalculationService>();

        internal delegate int GetDamageRollHook(void* thisPtr, void* pTarget, int bOffHand, int bCritical, int bSneakAttack, int bDeathAttack, int bForceMax);
        // ReSharper disable once NotAccessedField.Local
        private static GetDamageRollHook _callOriginal;

        public static void RegisterHook()
        {
            delegate* unmanaged<void*, void*, int, int, int, int, int, int> pHook = &OnGetDamageRoll;
            var functionPtr = NativeLibrary.GetExport(
                NativeLibrary.GetMainProgramHandle(), "_ZN17CNWSCreatureStats13GetDamageRollEP10CNWSObjectiiiii");
            var hookPtr = NWNXAPI.RequestFunctionHook(
                functionPtr,
                (IntPtr)pHook,
                -1000000);

            _callOriginal = Marshal.GetDelegateForFunctionPointer<GetDamageRollHook>((IntPtr)hookPtr);
        }

        [UnmanagedCallersOnly]
        private static int OnGetDamageRoll(void* thisPtr, void* pTarget, int bOffHand, int bCritical, int bSneakAttack, int bDeathAttack, int bForceMax)
        {
            return _scriptExecutor.ExecuteInScriptContext(() =>
            {
                var attackerStats = CNWSCreatureStats.FromPointer(thisPtr);
                var pAttacker = CNWSCreature.FromPointer(attackerStats.m_pBaseCreature);
                var defender = CNWSObject.FromPointer(pTarget) == null 
                    ? OBJECT_INVALID 
                    : CNWSObject.FromPointer(pTarget).m_idSelf;
                var pCombatRound = pAttacker.m_pcCombatRound;
                var pAttackData = pCombatRound.GetAttack(pCombatRound.m_nCurrentAttack);
                var attacker = pAttacker.m_idSelf;
                var damageFlags = attackerStats.m_pBaseCreature.GetDamageFlags();
                var pWeapon = pCombatRound.GetCurrentAttackWeapon();
                var weapon = pWeapon == null 
                    ? GetItemInSlot(InventorySlotType.Arms, attacker) // Default to gloves if no weapon
                    : CNWSObject.FromPointer(pWeapon).m_idSelf;
                var isCritical = bCritical == 1;

                var area = GetArea(attacker);
                _profilerPlugin.PushPerfScope("RunScript",
                    "Script", $"NATIVE:{nameof(OnGetDamageRoll)}",
                    "Area", !GetIsObjectValid(area) ? "Unknown" : GetTag(area),
                    "ObjectType", "Creature");

                // Early exit for invalid targets
                if (!GetIsObjectValid(defender))
                {
                    _profilerPlugin.PopPerfScope();
                    return 0;
                }

                var weaponStat = _weaponStatService.LoadWeaponStat(weapon);
                var damage = _combatCalculationService.CalculatePhysicalDamage(
                    attacker,
                    defender,
                    weapon,
                    isCritical);

                AddDamageToAttackData(pAttackData, weaponStat.DamageType, damage);

                _profilerPlugin.PopPerfScope();
                return 0;
            });
        }

        private static void AddDamageToAttackData(
            void* pAttackData, 
            CombatDamageType damageType, 
            int damage)
        {
            if (damage <= 0) 
                return;

            var attackData = CNWSCombatAttackData.FromPointer(pAttackData);
            switch (damageType)
            {
                case CombatDamageType.Force:
                    attackData.AddDamage((ushort)DamageType.Magical, damage);
                    break;
                case CombatDamageType.Fire:
                    attackData.AddDamage((ushort)DamageType.Fire, damage);
                    break;
                case CombatDamageType.Poison:
                    attackData.AddDamage((ushort)DamageType.Acid, damage);
                    break;
                case CombatDamageType.Electrical:
                    attackData.AddDamage((ushort)DamageType.Electrical, damage);
                    break;
                case CombatDamageType.Ice:
                    attackData.AddDamage((ushort)DamageType.Cold, damage);
                    break;
            }
        }

    }
}
