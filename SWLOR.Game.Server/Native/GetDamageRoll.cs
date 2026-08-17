using NWN.Native.API;
using NWNX.NET;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using System.Runtime.InteropServices;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using EquipmentSlot = NWN.Native.API.EquipmentSlot;
using ObjectType = NWN.Native.API.ObjectType;
using RacialType = SWLOR.NWN.API.NWScript.Enum.RacialType;

namespace SWLOR.Game.Server.Native
{
    public static unsafe class GetDamageRoll
    {
        private const int PowerAttackDamageBonus = 3;
        private const int ImprovedPowerAttackDamageBonus = 6;
        private const int DefaultPhysicalDamage = 1;
        private const int ElectricalDroidMultiplier = 2;
        private const int PowerAttackMode = 2;
        private const int ImprovedPowerAttackMode = 3;
        private const int AttributeNegativeThreshold = 128;
        private const int AttributeNegativeOffset = 256;
        private const int MaxValidDamageType = (int)CombatDamageType.Sonic;
        private const int MinValidDamageType = 1;

        internal delegate int GetDamageRollHook(void* thisPtr, void* pTarget, int bOffHand, int bCritical, int bSneakAttack, int bDeathAttack, int bForceMax);
        // ReSharper disable once NotAccessedField.Local
        private static GetDamageRollHook _callOriginal;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
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
        private static int OnGetDamageRoll(
            void* thisPtr,
            void* pTarget,
            int bOffHand,
            int bCritical,
            int bSneakAttack,
            int bDeathAttack,
            int bForceMax)
        {
            return ServerManager.Executor.ExecuteInScriptContext(() =>
            {
                var attackerStats = CNWSCreatureStats.FromPointer(thisPtr);
                var attacker = CNWSCreature.FromPointer(attackerStats.m_pBaseCreature);

                var area = attacker.GetArea();
                ProfilerPlugin.PushPerfScope("RunScript",
                    "Script", $"NATIVE:{nameof(OnGetDamageRoll)}",
                    "Area", area == null ? "Unknown" : area.m_sTag.ToString(),
                    "ObjectType", "Creature");

                var defender = CNWSObject.FromPointer(pTarget);

                // Early exit for invalid targets
                if (defender == null || defender.m_idSelf == OBJECT_INVALID)
                {
                    ProfilerPlugin.PopPerfScope();
                    return 0;
                }

                var damageFlags = attackerStats.m_pBaseCreature.GetDamageFlags();
                var pCombatRound = attacker.m_pcCombatRound;
                var pAttackData = pCombatRound.GetAttack(pCombatRound.m_nCurrentAttack);
                var weapon = pCombatRound.GetCurrentAttackWeapon(bOffHand);

                var attackType = attacker.GetRangeWeaponEquipped() == 1 ? (uint)AttackType.Ranged : (uint)AttackType.Melee;

                // CurrentAttackWeapon can be null on the opening swing for creature natural weapons.
                if (weapon == null)
                {
                    weapon = GetFallbackAttackWeapon(attacker);
                }

                LogAttackInfo(attacker, defender, attackType, weapon);

                // Extract weapon damage properties and get ability stats
                // ResolveAttack already emits distinct main-hand and off-hand rolls. Keep the
                // damage profile tied to that roll's current weapon so an elemental weapon cannot
                // re-type or absorb the other hand's DMG.
                var damageProfile = ExtractWeaponDamageProfile(weapon);
                var weaponSkillType = weapon == null
                    ? SkillType.Invalid
                    : SWLOR.Game.Server.Service.Skill.GetSkillTypeByBaseItem((BaseItem)weapon.m_nBaseItem);

                // Imbuement Stance converts the wearer's hostile weapon auto-attacks to Force damage for an FP cost.
                damageProfile = ApplyForceConversionStance(attacker, defender, damageProfile, weaponSkillType);

                var attackerStatType = GetWeaponDamageAbilityType(attacker.m_idSelf, weapon);
                var weaponDeltaCap = GetWeaponDeltaCap(weapon);

                var attackerStat = Stat.GetStatValueNative(attacker, attackerStatType);

                // Handle negative attributes
                if (attackerStat > AttributeNegativeThreshold)
                    attackerStat -= AttributeNegativeOffset;

                LogDamageCalculation(attackerStat, damageProfile);

                // Apply combat mode bonuses
                damageProfile = ApplyCombatModeBonus(attacker, damageProfile);
                damageProfile = ApplyMightModifierDamageBonus(attacker, weapon, damageProfile);

                var critical = bCritical == 1
                    ? Combat.StandardCriticalRating
                    : 0;
                // Force-typed swings (e.g. Imbuement Stance) use Force Attack so the attack side lines up
                // with the Force Defense the damage is mitigated against.
                var useForceAttack = damageProfile.DamageType == CombatDamageType.Force;
                var attackerAttack = weapon == null ? 0 : Stat.GetAttackNative(attacker, (BaseItem)weapon.m_nBaseItem, attackerStatType, useForceAttack);
                var totalDamage = 0;

                // The engine calls this hook for swings it later discards. On-hit riders such as
                // Guard must only fire when the attack roll actually landed against a target that
                // can take damage.
                var isLandedAttack = IsLandedAttackOnDamageableTarget(pAttackData, defender);

                var physicalDamage = ProcessDamage(pTarget, attacker, damageProfile, pAttackData,
                    attackerAttack, attackerStat, critical, weaponDeltaCap, attackType, damageFlags, bOffHand, defender, weaponSkillType,
                    isLandedAttack,
                    out totalDamage,
                    out var effectiveCritical);

                if (isLandedAttack && totalDamage > 0)
                {
                    using var damageDerivedHealing = Combat.BeginDamageDerivedHealing(attacker.m_idSelf);

                    if (defender.m_nObjectType == (int)ObjectType.Creature)
                    {
                        Combat.SendTemporaryHitPointDamageFeedback(attacker.m_idSelf, defender.m_idSelf, totalDamage);
                        Combat.ApplyCriticalHitEffects(attacker.m_idSelf, defender.m_idSelf, totalDamage, effectiveCritical, true, weaponSkillType);
                    }

                    if (defender.m_bPlotObject == 0)
                    {
                        var weaponId = weapon?.m_idSelf ?? OBJECT_INVALID;
                        PublishDamageDealtEvent(attacker.m_idSelf, defender.m_idSelf, weaponId, totalDamage, weaponSkillType, damageProfile.DamageType);
                    }
                }

                ProfilerPlugin.PopPerfScope();
                return physicalDamage;
            });
        }

        private static void LogAttackInfo(CNWSCreature attacker, CNWSObject targetObject, uint attackType, CNWSItem weapon)
        {
            Log.Write(LogGroup.Attack, $"DAMAGE: Attacker: {attacker.GetFirstName().GetSimple()}, PC?: {attacker.m_bPlayerCharacter}, " +
                                      $"Defender {targetObject.GetFirstName().GetSimple()}, object type {targetObject.m_nObjectType}, " +
                                      $"Attack type: {attackType}, weapon {(weapon == null ? "None" : weapon.GetFirstName().GetSimple())}");
        }

        private static CNWSItem GetFallbackAttackWeapon(CNWSCreature attacker)
        {
            var arms = attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.Arms);
            return arms ?? GetCreatureNaturalWeapon(attacker);
        }

        private static CNWSItem GetCreatureNaturalWeapon(CNWSCreature attacker)
        {
            var creatureRight = attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.CreatureWeaponRight);
            if (creatureRight != null)
                return creatureRight;

            var creatureLeft = attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.CreatureWeaponLeft);
            if (creatureLeft != null)
                return creatureLeft;

            return attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.CreatureWeaponBite);
        }

        private static void LogDamageCalculation(int attackerStat, WeaponDamageProfile damageProfile)
        {
            Log.Write(LogGroup.Attack,
                $"DAMAGE: attacker attribute modifier: {attackerStat}, weapon damage rating {damageProfile.DamageType}: {damageProfile.Damage}");
        }

        private static bool IsLandedAttackOnDamageableTarget(void* pAttackData, CNWSObject targetObject)
        {
            if (targetObject == null || targetObject.m_bPlotObject == 1 || pAttackData == null)
                return false;

            var attackData = CNWSCombatAttackData.FromPointer(pAttackData);
            return attackData != null &&
                   ResolveAttackRoll.IsSuccessfulAttackResult(attackData.m_nAttackResult);
        }

        private static int ProcessDamage(void* pTarget, CNWSCreature attacker,
            WeaponDamageProfile damageProfile, void* pAttackData, int attackerAttack,
            int attackerStat, int critical, int weaponDeltaCap, uint attackType, uint damageFlags,
            int bOffHand, CNWSObject targetObject, SkillType skillType, bool isLandedAttack,
            out int totalDamage, out int effectiveCritical)
        {
            var physicalDamage = 0;
            effectiveCritical = critical;
            totalDamage = 0;

            if (targetObject.m_nObjectType == (int)ObjectType.Creature &&
                UsePerkFeat.HasQueuedWeaponAbility(attacker.m_idSelf, skillType))
            {
                Combat.ConsumeSuppressedAutoAttackDamageBonuses(attacker.m_idSelf, skillType);
                return physicalDamage;
            }

            var damage = CalculateTargetSpecificDamage(pTarget, attacker, damageProfile,
                attackerAttack, attackerStat, critical, weaponDeltaCap, attackType, damageFlags, bOffHand, skillType,
                isLandedAttack, out effectiveCritical);

            // Plot target takes no damage
            if (targetObject.m_bPlotObject == 1)
                damage = 0;

            // Ensure damage is never negative
            if (damage < 0)
                damage = 0;

            if (isLandedAttack && damage > 0 && targetObject.m_nObjectType == (int)ObjectType.Creature)
            {
                Combat.ApplyDamageReflectionEffects(
                    attacker.m_idSelf,
                    targetObject.m_idSelf,
                    damage,
                    damageProfile.DamageType);
            }

            if (damageProfile.DamageType.IsPhysicalDamageType())
            {
                physicalDamage = damage;
            }
            else
            {
                AddDamageToAttackData(pAttackData, damageProfile.DamageType, damage);
            }

            totalDamage = damage;

            return physicalDamage;
        }

        private static void AddDamageToAttackData(void* pAttackData, CombatDamageType damageType, int damage)
        {
            if (damage <= 0) return;

            var attackData = CNWSCombatAttackData.FromPointer(pAttackData);
            attackData.AddDamage((ushort)damageType.GetNativeDamageType(), damage);
        }

        // While Imbuement Stance is active, the wearer's hostile weapon auto-attacks deal Force damage instead of
        // their normal type and cost FP per swing. This only affects real auto-attacks against creatures; queued
        // weapon abilities apply their own damage and are excluded so they are neither converted nor charged.
        private static WeaponDamageProfile ApplyForceConversionStance(
            CNWSCreature attacker,
            CNWSObject defender,
            WeaponDamageProfile damageProfile,
            SkillType weaponSkillType)
        {
            if (defender.m_nObjectType != (int)ObjectType.Creature)
                return damageProfile;

            // Only physical auto-attacks are converted; anything already non-physical is left untouched.
            if (!damageProfile.DamageType.IsPhysicalDamageType())
                return damageProfile;

            var conversion = Stat.GetStatAdjustment(attacker.m_idSelf, StatType.StanceHostileAutoAttackForceConversion);
            if (conversion <= 0)
                return damageProfile;

            // Weapon abilities apply their own combat impact and suppress the auto-attack; do not convert/charge them.
            if (UsePerkFeat.HasQueuedWeaponAbility(attacker.m_idSelf, weaponSkillType))
                return damageProfile;

            var fpCost = Stat.GetStatAdjustment(attacker.m_idSelf, StatType.StanceHostileAutoAttackFPCost);
            if (fpCost > 0)
            {
                // Not enough FP to pay the upkeep: the swing stays its normal type and no FP is spent.
                if (Stat.GetCurrentFP(attacker.m_idSelf) < fpCost)
                    return damageProfile;

                Stat.ReduceFP(attacker.m_idSelf, fpCost);
            }

            return new WeaponDamageProfile(CombatDamageType.Force, damageProfile.Damage);
        }

        private static WeaponDamageProfile ExtractWeaponDamageProfile(CNWSItem weapon)
        {
            var damageType = CombatDamageType.Physical;
            var damage = 0;
            var hasDamageProperty = false;

            if (weapon != null)
            {
                for (var index = 0; index < weapon.m_lstPassiveProperties.Count; index++)
                {
                    var ip = weapon.GetPassiveProperty(index);
                    if (ip == null)
                        continue;

                    if (ip.m_nPropertyName == (ushort)ItemPropertyType.DMG)
                    {
                        damage += ip.m_nCostTableValue;
                        hasDamageProperty = true;
                    }
                    else if (ip.m_nPropertyName == (ushort)ItemPropertyType.WeaponDamageType)
                    {
                        damageType = ResolveWeaponDamageType(damageType, ip.m_nSubType);
                    }
                }
            }

            // A damage type only selects the type of a real DMG property. Items without DMG use
            // the unarmed/default physical fallback instead of manufacturing elemental damage.
            if (!hasDamageProperty)
            {
                return new WeaponDamageProfile(CombatDamageType.Physical, DefaultPhysicalDamage);
            }

            return new WeaponDamageProfile(damageType, damage);
        }

        private static CombatDamageType ResolveWeaponDamageType(CombatDamageType current, int damageTypeId)
        {
            if (damageTypeId > MaxValidDamageType || damageTypeId < MinValidDamageType)
                return current;

            var candidate = (CombatDamageType)damageTypeId;
            if (!candidate.IsCharacterDamageType())
                return current;

            if (current.IsElementalDamageType())
                return current;

            if (candidate.IsElementalDamageType())
                return candidate;

            if (current.IsPhysicalDamageType() && candidate == CombatDamageType.Force)
                return CombatDamageType.Force;

            return current;
        }

        private static AbilityType GetWeaponDamageAbilityType(uint attacker, CNWSItem weapon)
        {
            if (weapon == null) return AbilityType.Might;

            for (var index = 0; index < weapon.m_lstPassiveProperties.Count; index++)
            {
                var ip = weapon.GetPassiveProperty(index);
                if (ip?.m_nPropertyName == (ushort)ItemPropertyType.DamageStat)
                {
                    return (AbilityType)ip.m_nSubType;
                }
            }

            return Combat.GetWeaponDamageAbilityType(attacker, (BaseItem)weapon.m_nBaseItem);
        }

        private static int GetWeaponDeltaCap(CNWSItem weapon)
        {
            var requiredSkillRank = GetWeaponRequiredSkillRank(weapon);
            return requiredSkillRank < 0
                ? 0
                : GetWeaponDeltaCapFromRequiredSkillRank(requiredSkillRank);
        }

        private static int GetWeaponRequiredSkillRank(CNWSItem weapon)
        {
            if (weapon == null) return -1;

            var requiredSkillRank = -1;
            for (var index = 0; index < weapon.m_lstPassiveProperties.Count; index++)
            {
                var ip = weapon.GetPassiveProperty(index);
                if (ip?.m_nPropertyName == (ushort)ItemPropertyType.RequiresSkill)
                {
                    requiredSkillRank = Math.Max(requiredSkillRank, ip.m_nCostTableValue);
                }
            }

            return requiredSkillRank;
        }

        private static int GetWeaponDeltaCapFromRequiredSkillRank(int requiredSkillRank)
        {
            return requiredSkillRank <= 0
                ? 1
                : Math.Clamp((requiredSkillRank / 10) + 1, 1, 6);
        }

        private static WeaponDamageProfile ApplyCombatModeBonus(CNWSCreature attacker, WeaponDamageProfile damageProfile)
        {
            switch (attacker?.m_nCombatMode)
            {
                case PowerAttackMode:
                    return new WeaponDamageProfile(damageProfile.DamageType, damageProfile.Damage + PowerAttackDamageBonus);
                case ImprovedPowerAttackMode:
                    return new WeaponDamageProfile(damageProfile.DamageType, damageProfile.Damage + ImprovedPowerAttackDamageBonus);
                default:
                    return damageProfile;
            }
        }

        private static WeaponDamageProfile ApplyMightModifierDamageBonus(CNWSCreature attacker, CNWSItem weapon, WeaponDamageProfile damageProfile)
        {
            if (attacker == null)
                return damageProfile;

            var mightModifier = Math.Max(0, Stat.GetStatValueNative(attacker, AbilityType.Might));
            if (mightModifier <= 0)
                return damageProfile;

            var multiplier = Stat.GetStatAdjustment(attacker.m_idSelf, StatType.WeaponMightModifierDamageMultiplier);
            if (weapon != null && Item.StaffBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem))
            {
                multiplier += Stat.GetStatAdjustment(attacker.m_idSelf, StatType.StaffMightModifierDamageMultiplier);
            }

            if (multiplier <= 0)
                return damageProfile;

            return new WeaponDamageProfile(damageProfile.DamageType, damageProfile.Damage + mightModifier * multiplier);
        }

        private static int CalculateTargetSpecificDamage(void* pTarget, CNWSCreature attacker,
            WeaponDamageProfile damageProfile, int attackerAttack,
            int attackerStat, int critical, int weaponDeltaCap, uint attackType, uint damageFlags, int bOffHand, SkillType skillType,
            bool isLandedAttack, out int effectiveCritical)
        {
            effectiveCritical = critical;
            var targetObject = CNWSObject.FromPointer(pTarget);

            switch (targetObject.m_nObjectType)
            {
                case (int)ObjectType.Creature:
                    return CalculateCreatureDamage(pTarget, attacker, damageProfile, attackerAttack,
                        attackerStat, critical, weaponDeltaCap, attackType, damageFlags, bOffHand, skillType,
                        isLandedAttack, out effectiveCritical);

                case (int)ObjectType.Placeable:
                    var plc = CNWSPlaceable.FromPointer(pTarget);
                    return Combat.CalculateDamage(attackerAttack, damageProfile.Damage, attackerStat,
                        plc.m_nHardness, plc.m_nHardness, critical);

                case (int)ObjectType.Door:
                    var door = CNWSDoor.FromPointer(pTarget);
                    return Combat.CalculateDamage(attackerAttack, damageProfile.Damage, attackerStat,
                        door.m_nHardness, door.m_nHardness, critical);

                default:
                    return damageProfile.Damage;
            }
        }

        private static int CalculateCreatureDamage(void* pTarget, CNWSCreature attacker, WeaponDamageProfile damageProfile,
            int attackerAttack, int attackerStat, int critical, int weaponDeltaCap,
            uint attackType, uint damageFlags, int bOffHand, SkillType skillType,
            bool isLandedAttack, out int effectiveCritical)
        {
            effectiveCritical = critical;
            var target = CNWSCreature.FromPointer(pTarget);
            var damageType = damageProfile.DamageType;
            var defenderAbility = damageType.GetDefenseAbilityType();
            var defenderStat = Stat.GetStatValueNative(target, defenderAbility);
            var damagePower = attacker.CalculateDamagePower(target, bOffHand);
            var defense = Stat.GetDefenseNative(target, damageType, defenderAbility);
            defense = Combat.ApplyStatusSourceDefenseModifiers(attacker.m_idSelf, target.m_idSelf, defense);
            defense = Combat.ApplyIncomingPhysicalToForceDefenseConversion(
                target.m_idSelf,
                damageType,
                defense,
                () => Combat.ApplyStatusSourceDefenseModifiers(
                    attacker.m_idSelf,
                    target.m_idSelf,
                    Stat.GetDefenseNative(target, CombatDamageType.Force, CombatDamageType.Force.GetDefenseAbilityType())));
            defense = Combat.ApplyRangedAttackDefenseIgnore(attacker.m_idSelf, defense, skillType);
            // Discarded swings must not burn one-shot buffs or advance cycle counters — the engine
            // rolls damage for attacks it then throws away, so every consuming rider gates on the
            // attack having actually landed.
            var guardedHitBonuses = isLandedAttack
                ? Combat.ConsumeNextAttackGuardedHitAutoAttackBonuses(attacker.m_idSelf)
                : default;
            var statusAppliedNextAttackDamageBonus = isLandedAttack
                ? Combat.ConsumeStatusAppliedNextAttackDamageBonus(attacker.m_idSelf)
                : 0;
            var cycleDamageBonus = isLandedAttack
                ? Combat.ConsumeAutoAttackCycleDamageBonus(attacker.m_idSelf, skillType)
                : 0;
            var attackDamage = damageProfile.Damage +
                               Combat.GetRangedAttackDamageFlatAdjustment(attacker.m_idSelf, skillType) +
                               cycleDamageBonus +
                               guardedHitBonuses.DMGBonus +
                               statusAppliedNextAttackDamageBonus;

            Log.Write(LogGroup.Attack, $"DAMAGE: attacker damage attribute: {damageProfile.Damage} defender defense attribute: {defense}, defender racial type {target.m_pStats.m_nRace}");

            attackerAttack = Combat.ApplyTargetStatusAttackModifiers(attacker.m_idSelf, target.m_idSelf, attackerAttack, skillType);

            var damageRoll = Combat.CalculateDamageWithCriticalMitigation(
                target.m_idSelf,
                attackerAttack,
                attackDamage,
                attackerStat,
                defense,
                defenderStat,
                critical,
                weaponDeltaCap);
            var damage = damageRoll.Damage;
            effectiveCritical = damageRoll.CriticalRating;
            if (damageRoll.WasCriticalDowngraded)
            {
                Combat.SendIncomingCriticalHitDowngradeFeedback(attacker.m_idSelf, target.m_idSelf);
            }

            damage = Combat.ApplyCriticalDamageModifier(attacker.m_idSelf, damage, effectiveCritical, skillType, target.m_idSelf);

            if (isLandedAttack)
            {
                damage = Combat.ApplyAutoAttackDamageModifiers(attacker.m_idSelf, target.m_idSelf, damage, skillType);
            }
            damage = Combat.ApplySideAttackDamageModifier(attacker.m_idSelf, target.m_idSelf, skillType, damage);
            if (isLandedAttack)
            {
                // Unlike its pure side-attack sibling, the back-attack modifier also consumes
                // Ghost Protocol's primed Exposed rider - a discarded swing must not burn it.
                damage = Combat.ApplyBackAttackDamageModifier(attacker.m_idSelf, target.m_idSelf, skillType, damage);
            }

            var canApplyRandomFlatBonusesThisDamage = damage > 0;

            damage = Combat.ApplyDamageDealtModifiers(
                attacker.m_idSelf,
                target.m_idSelf,
                damage,
                skillType,
                damageType,
                false,
                canApplyRandomFlatBonusesThisDamage,
                isLandedAttack,
                out var damageBeforeTargetStatusStage);

            // Saber Ward / Aegis Eternal: re-type a share of the physical hit into a real Force
            // instance (mitigated by Force resistance, shown as Force) before physical resistance.
            if (isLandedAttack)
            {
                Combat.ApplyIncomingPhysicalToForceConversion(attacker.m_idSelf, target.m_idSelf, damageType, ref damage);
            }

            damage = Resistance.ApplyResistanceToDamageNative(target, damageType, damage);

            // Apply droid electrical damage bonus
            if (target.m_pStats.m_nRace == (ushort)RacialType.Robot &&
                damageType.TryGetElementalResistanceType(out var elementalResistanceType) &&
                elementalResistanceType == ResistanceType.Electrical)
            {
                damage *= ElectricalDroidMultiplier;
            }

            // Apply NWN damage mechanics for physical damage only
            if (damageType.IsPhysicalDamageType())
            {
                var bRangedAttack = attackType == (uint)AttackType.Ranged ? 1 : 0;
                damage = target.DoDamageImmunity(attacker, damage, damageFlags, 0, 1);
                damage = target.DoDamageResistance(attacker, damage, damageFlags, 0, 1, 1, bRangedAttack);
                damage = target.DoDamageReduction(attacker, damage, damagePower, 0, 1, bRangedAttack);
            }

            damage = Combat.ApplyGuardedHitModifiers(
                target.m_idSelf,
                attacker.m_idSelf,
                damage,
                damageType,
                isLandedAttack);
            if (isLandedAttack && damage > 0 && attackType == (uint)AttackType.Melee)
            {
                Combat.ApplyMeleeDamageTakenEffects(target.m_idSelf, attacker.m_idSelf);
            }

            damage = Combat.ApplyDamageTakenModifiers(
                target.m_idSelf,
                damage,
                attacker.m_idSelf,
                damageType,
                preTargetStatusStageDamage: damageBeforeTargetStatusStage,
                isLandedAttack: isLandedAttack);
            if (isLandedAttack)
            {
                Combat.ApplyNextAttackGuardedHitEnmityBonus(
                    attacker.m_idSelf,
                    target.m_idSelf,
                    guardedHitBonuses.EnmityBonus);
            }
            return damage;
        }

        private readonly struct WeaponDamageProfile
        {
            public CombatDamageType DamageType { get; }
            public int Damage { get; }

            public WeaponDamageProfile(CombatDamageType damageType, int damage)
            {
                DamageType = damageType;
                Damage = damage;
            }
        }

        private static void PublishDamageDealtEvent(uint attacker, uint defender, uint weapon, int damage, SkillType skillType, CombatDamageType damageType)
        {
            Combat.ApplyDamageDealtEffects(attacker, defender, damage, skillType, damageType);

            EventsPlugin.PushEventData("DEFENDER", ObjectToString(defender));
            EventsPlugin.PushEventData("WEAPON", ObjectToString(weapon));
            EventsPlugin.PushEventData("DAMAGE", damage.ToString());
            EventsPlugin.PushEventData("DAMAGE_TYPE", ((int)damageType).ToString());

            EventsPlugin.SignalEvent("SWLOR_ON_DAMAGE", attacker);
        }
    }
}
