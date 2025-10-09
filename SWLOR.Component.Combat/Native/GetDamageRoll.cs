using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using SWLOR.Component.Combat.Enums;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Inventory.Contracts;
using DamageType = NWN.Native.API.DamageType;
using EquipmentSlot = NWN.Native.API.EquipmentSlot;
using ILogger = SWLOR.Shared.Abstractions.Contracts.ILogger;
using ObjectType = NWN.Native.API.ObjectType;
using RacialType = SWLOR.NWN.API.NWScript.Enum.RacialType;

namespace SWLOR.Component.Combat.Native
{
    public static unsafe class GetDamageRoll
    {
        private static readonly IItemService _itemService = ServiceContainer.GetService<IItemService>();
        private static readonly IScriptExecutor _scriptExecutor = ServiceContainer.GetService<IScriptExecutor>();
        private static readonly IStatCalculationService _statCalculationService = ServiceContainer.GetService<IStatCalculationService>();
        private static readonly IAbilityService _abilityService = ServiceContainer.GetService<IAbilityService>();
        private static readonly ICombatService _combatService = ServiceContainer.GetService<ICombatService>();
        private static readonly IWeaponStatService _weaponStatService = ServiceContainer.GetService<IWeaponStatService>();
        private static readonly ILogger _logger = ServiceContainer.GetService<ILogger>();
        private static readonly IProfilerPluginService _profilerPlugin = ServiceContainer.GetService<IProfilerPluginService>();

        private const int PowerAttackDamageBonus = 3;
        private const int ImprovedPowerAttackDamageBonus = 6;
        private const int DefaultPhysicalDamage = 1;
        private const int ElectricalDroidMultiplier = 2;
        private const int CrushingMasteryMultiplier = 2;
        private const int WeaponSpecializationUnarmedDamage = 2;
        private const int WeaponSpecializationCreatureDamage = 2;
        private const int WeaponSpecializationOtherDamage = 1;
        private const int PowerAttackMode = 2;
        private const int ImprovedPowerAttackMode = 3;
        private const int AttributeNegativeThreshold = 128;
        private const int AttributeNegativeOffset = 256;
        private const int MaxValidDamageType = 6;
        private const int MinValidDamageType = 1;

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
                var targetObject = CNWSObject.FromPointer(pTarget) == null ? OBJECT_INVALID : CNWSObject.FromPointer(pTarget).m_idSelf;
                var attackerStats = CNWSCreatureStats.FromPointer(thisPtr);
                var pAttacker = CNWSCreature.FromPointer(attackerStats.m_pBaseCreature);
                var pCombatRound = pAttacker.m_pcCombatRound;
                var pAttackData = pCombatRound.GetAttack(pCombatRound.m_nCurrentAttack);
                var attacker = pAttacker.m_idSelf;
                var damageFlags = attackerStats.m_pBaseCreature.GetDamageFlags();
                var pWeapon = pCombatRound.GetCurrentAttackWeapon();
                var weapon = CNWSObject.FromPointer(pWeapon) == null ? OBJECT_INVALID : CNWSObject.FromPointer(pWeapon).m_idSelf;

                var area = GetArea(attacker);
                _profilerPlugin.PushPerfScope("RunScript",
                    "Script", $"NATIVE:{nameof(OnGetDamageRoll)}",
                    "Area", !GetIsObjectValid(area) ? "Unknown" : GetTag(area),
                    "ObjectType", "Creature");


                // Early exit for invalid targets
                if (!GetIsObjectValid(targetObject))
                {
                    _profilerPlugin.PopPerfScope();
                    return 0;
                }

                var attackType = pAttacker.GetRangeWeaponEquipped() == 1 
                    ? (uint)AttackType.Ranged 
                    : (uint)AttackType.Melee;

                // Nothing equipped - check gloves
                if (!GetIsObjectValid(weapon))
                {
                    weapon = GetItemInSlot(InventorySlotType.Arms, attacker);
                }

                // Extract weapon damage properties and get ability stats
                var weaponStat = _weaponStatService.LoadWeaponStat(weapon);
                var attackerStat = GetAbilityScore(attacker, weaponStat.DamageStat);

                // Calculate critical multiplier
                var critical = false; 
                var attackerAttack = 0; 

                var physicalDamage = ProcessDamageTypes(
                    pTarget,
                    attacker,
                    weapon,
                    dmgValues,
                    pAttackData,
                    attackerAttack,
                    attackerStat,
                    critical,
                    weaponStat.Tier,
                    attackType,
                    damageFlags,
                    bOffHand,
                    targetObject);

                _profilerPlugin.PopPerfScope();
                return physicalDamage;
            });
        }


        private static int ProcessDamageTypes(
            void* pTarget, 
            CNWSCreature attacker, 
            CNWSItem weapon,
            Dictionary<CombatDamageType, int> dmgValues, 
            void* pAttackData, 
            int attackerAttack,
            int attackerStat, 
            int critical, 
            int weaponPerkLevel, 
            uint attackType, 
            uint damageFlags,
            int bOffHand, 
            CNWSObject targetObject)
        {
            var physicalDamage = 0;

            foreach (var damageType in dmgValues.Keys)
            {
                var damage = CalculateTargetSpecificDamage(pTarget, attacker, weapon, dmgValues, damageType,
                    attackerAttack, attackerStat, critical, weaponPerkLevel, attackType, damageFlags, bOffHand);

                // Plot target takes no damage
                if (targetObject.m_bPlotObject == 1)
                    damage = 0;

                // Ensure damage is never negative
                if (damage < 0)
                    damage = 0;

                if (damageType == CombatDamageType.Physical)
                {
                    physicalDamage = damage;
                }
                else
                {
                    AddDamageToAttackData(pAttackData, damageType, damage);
                }
            }

            return physicalDamage;
        }

        private static void AddDamageToAttackData(
            void* pAttackData, 
            CombatDamageType damageType, 
            int damage)
        {
            if (damage <= 0) return;

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

        private static int CalculateTargetSpecificDamage(
            void* pTarget, 
            CNWSCreature attacker, 
            CNWSItem weapon,
            Dictionary<CombatDamageType, int> dmgValues, 
            CombatDamageType damageType, 
            int attackerAttack,
            int attackerStat, 
            int critical, 
            int weaponPerkLevel, 
            uint attackType, 
            uint damageFlags, 
            int bOffHand)
        {
            var targetObject = CNWSObject.FromPointer(pTarget);

            switch (targetObject.m_nObjectType)
            {
                case (int)ObjectType.Creature:
                    return CalculateCreatureDamage(pTarget, attacker, dmgValues, damageType, attackerAttack,
                        attackerStat, critical, weaponPerkLevel, attackType, damageFlags, bOffHand);

                case (int)ObjectType.Placeable:
                    var plc = CNWSPlaceable.FromPointer(pTarget);
                    return _combatService.CalculateDamage(attackerAttack, dmgValues[damageType], attackerStat,
                        plc.m_nHardness, plc.m_nHardness, critical);

                case (int)ObjectType.Door:
                    var door = CNWSDoor.FromPointer(pTarget);
                    return _combatService.CalculateDamage(attackerAttack, dmgValues[damageType], attackerStat,
                        door.m_nHardness, door.m_nHardness, critical);

                default:
                    return dmgValues[damageType];
            }
        }

        private static int CalculateCreatureDamage(void* pTarget, CNWSCreature attacker, Dictionary<CombatDamageType, int> dmgValues,
            CombatDamageType damageType, int attackerAttack, int attackerStat, int critical, int weaponPerkLevel,
            uint attackType, uint damageFlags, int bOffHand)
        {
            var target = CNWSCreature.FromPointer(pTarget);
            var defenderStat = target.m_pStats.GetCONStat();
            var damagePower = attacker.CalculateDamagePower(target, bOffHand);
            var defense = _statCalculationService.CalculateDefense(target.m_idSelf);

            _logger.Write<AttackLogGroup>($"DAMAGE: attacker damage attribute: {dmgValues[damageType]} defender defense attribute: {defense}, defender racial type {target.m_pStats.m_nRace}");

            var damage = _combatService.CalculateDamage(attackerAttack, dmgValues[damageType], attackerStat,
                defense, defenderStat, critical, weaponPerkLevel);

            // Apply droid electrical damage bonus
            if (target.m_pStats.m_nRace == (ushort)RacialType.Robot && damageType == CombatDamageType.Electrical)
            {
                damage *= ElectricalDroidMultiplier;
            }

            // Apply NWN damage mechanics for physical damage only
            if (damageType == CombatDamageType.Physical)
            {
                var bRangedAttack = attackType == (uint)AttackType.Ranged ? 1 : 0;
                damage = target.DoDamageImmunity(attacker, damage, damageFlags, 0, 1);
                damage = target.DoDamageResistance(attacker, damage, damageFlags, 0, 1, 1, bRangedAttack);
                damage = target.DoDamageReduction(attacker, damage, damagePower, 0, 1, bRangedAttack);
            }

            return damage;
        }
    }
}
