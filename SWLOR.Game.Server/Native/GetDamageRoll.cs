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
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;

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
        private const string ForceConversionAttackIndex = "FORCE_CONVERSION_ATTACK_INDEX";
        private const string ForceConversionAttackTarget = "FORCE_CONVERSION_ATTACK_TARGET";

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
            return ResolveWeaponDamage(thisPtr, pTarget, bOffHand, bCritical, bSneakAttack, bDeathAttack, bForceMax);
        }

        internal static int ResolveWeaponDamage(void* thisPtr, void* pTarget, int bOffHand,
            int bCritical, int bSneakAttack = 0, int bDeathAttack = 0, int bForceMax = 0,
            Action<Action> convertedDamageScheduler = null,
            Action<Action<int>> weaponImpactScheduler = null,
            Action<Func<int>> damageTakenResolverCapture = null)
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
                // GetDamageRoll receives a boolean; GetCurrentAttackWeapon expects a weapon
                // attack type (2 = off-hand, 0 = infer the current attack, including natural weapons).
                var weapon = WeaponAttackCycle.GetAttackWeapon(attacker, bOffHand != 0 ? 2 : 0);
                weapon = RangedWeaponAttackContext.GetWeapon(attacker.m_idSelf, weapon);

                var attackType = attacker.GetRangeWeaponEquipped() == 1 ? (uint)AttackType.Ranged : (uint)AttackType.Melee;

                // CurrentAttackWeapon can be null on the opening swing for creature natural weapons.
                if (weapon == null)
                {
                    weapon = GetFallbackAttackWeapon(attacker);
                }

                // A restricted melee item never falls back to natural damage or triggers procs.
                // Ranged attacks already in flight are unaffected by equipment changes here.
                if (pAttackData.m_bRangedAttack == 0 && !WeaponAttackEligibility.IsAllowed(
                        attacker.m_idSelf, weapon?.m_idSelf ?? OBJECT_INVALID, WeaponAttackPhase.Resolve))
                {
                    ProfilerPlugin.PopPerfScope();
                    return 0;
                }

                LogAttackInfo(attacker, defender, attackType, weapon);

                // Extract weapon damage properties and get ability stats
                // ResolveAttack already emits distinct main-hand and off-hand rolls. Keep the
                // damage profile tied to that roll's current weapon so an elemental weapon cannot
                // re-type or absorb the other hand's DMG.
                var damageProfile = ExtractWeaponDamageProfile(weapon);
                var explicitProfile = RangedWeaponAttackContext.GetProfile(attacker.m_idSelf);
                if (explicitProfile != null)
                {
                    damageProfile = new WeaponDamageProfile(explicitProfile.DamageType, explicitProfile.Damage);
                    // The companion's carrier is deliberately inert. Its captured shot type,
                    // rather than the real launcher's native physical flags, owns mitigation.
                    damageFlags = (uint)explicitProfile.DamageType.GetNativeDamageType();
                }
                var weaponSkillType = weapon == null
                    ? SkillType.Invalid
                    : SWLOR.Game.Server.Service.Skill.GetWeaponSkill(attacker.m_idSelf, weapon.m_idSelf);

                // Imbuement Stance converts the wearer's hostile weapon auto-attacks to Force damage for an FP cost.
                damageProfile = ApplyForceConversionStance(attacker, defender, damageProfile);

                var attackerStatType = explicitProfile?.DamageAbility ?? GetWeaponDamageAbilityType(attacker.m_idSelf, weapon);
                var weaponDeltaCap = GetWeaponDeltaCap(weapon);

                var attackerStat = Stat.GetStatValueNative(attacker, attackerStatType);

                // Handle negative attributes
                if (attackerStat > AttributeNegativeThreshold)
                    attackerStat -= AttributeNegativeOffset;

                LogDamageCalculation(attackerStat, damageProfile);

                // Apply combat mode bonuses
                if (weapon != null && damageProfile.HasItemDamage)
                    damageProfile = new WeaponDamageProfile(damageProfile.DamageType,
                        WeaponDamage.GetEffectiveDMG(attacker.m_idSelf, weapon.m_idSelf, damageProfile.Damage));
                damageProfile = ApplyCombatModeBonus(attacker, damageProfile);
                damageProfile = ApplyMightModifierDamageBonus(attacker, weapon, damageProfile);

                var critical = bCritical == 1
                    ? Combat.StandardCriticalRating
                    : 0;
                // Force-typed swings (e.g. Imbuement Stance) use Force Attack so the attack side lines up
                // with the Force Defense the damage is mitigated against.
                var useForceAttack = damageProfile.DamageType == CombatDamageType.Force;
                var attackerAttack = weapon == null ? 0 : Stat.GetAttackNative(attacker, (BaseItem)weapon.m_nBaseItem,
                    attackerStatType, useForceAttack, weaponSkillType);
                var totalDamage = 0;

                // The engine calls this hook for swings it later discards. On-hit riders such as
                // Guard must only fire when the attack roll actually landed against a target that
                // can take damage.
                var isLandedAttack = IsLandedAttackOnDamageableTarget(pAttackData, defender);
                var shot = RangedWeaponAttackContext.GetRoll(attacker.m_idSelf, defender.m_idSelf, pCombatRound.m_nCurrentAttack);
                if (shot is { Completed: false } && weaponSkillType == SkillType.TwinPistol && isLandedAttack &&
                    !RangedWeaponAttackContext.IsSimulated(attacker.m_idSelf))
                    CombatPoint.CaptureNativeWeaponAttackPoint(attacker.m_idSelf, defender.m_idSelf,
                        weapon.m_idSelf, weaponSkillType, shot.Id,
                        GetItemInSlot(InventorySlot.Bullets, attacker.m_idSelf), awardPoint: shot.Ordinary);

                var physicalDamage = ProcessDamage(pTarget, attacker, damageProfile, pAttackData,
                    attackerAttack, attackerStat, critical, weaponDeltaCap, attackType, damageFlags, bOffHand, defender, weaponSkillType,
                    isLandedAttack,
                    out totalDamage,
                    out var effectiveCritical, convertedDamageScheduler, weaponImpactScheduler, damageTakenResolverCapture);

                if (shot != null)
                {
                    shot.Damage = totalDamage;
                    shot.DamageType = damageProfile.DamageType;
                    if (shot.Ordinary && !shot.Completed && isLandedAttack)
                    {
                        var attackerId = attacker.m_idSelf;
                        var defenderId = defender.m_idSelf;
                        var rollId = shot.Id;
                        var nativeTwinPistol = weaponSkillType == SkillType.TwinPistol &&
                                               !RangedWeaponAttackContext.IsSimulated(attackerId);
                        void RecordOrdinaryHit(int _)
                        {
                            PairedRangedTraits.RecordOrdinaryHit(attackerId, defenderId, rollId);
                            OrdinaryAttackTraits.RecordHit(attackerId, defenderId, rollId);
                            if (nativeTwinPistol)
                                TwinPistolMastery.RecordOrdinaryHit(attackerId, defenderId, false);
                        }
                        if (weaponImpactScheduler == null) RecordOrdinaryHit(totalDamage);
                        else weaponImpactScheduler(RecordOrdinaryHit);
                    }
                    shot.Completed = true;
                }

                if (isLandedAttack)
                {
                    var attackerId = attacker.m_idSelf;
                    var defenderId = defender.m_idSelf;
                    var weaponId = weapon?.m_idSelf ?? OBJECT_INVALID;
                    var creatureTarget = defender.m_nObjectType == (int)ObjectType.Creature;
                    var damageableTarget = defender.m_bPlotObject == 0;
                    var rolledType = damageProfile.DamageType;
                    var nativeDamageApplication = weaponImpactScheduler == null;

                    void ApplyWeaponHitEffects(int rolledDamage)
                    {
                        if (!GetIsObjectValid(attackerId) || !GetIsObjectValid(defenderId)) return;
                        using var damageDerivedHealing = Combat.BeginDamageDerivedHealing(attackerId, defenderId);
                        if (creatureTarget)
                        {
                            if (rolledDamage > 0)
                                Combat.SendTemporaryHitPointDamageFeedback(attackerId, defenderId, rolledDamage);
                            Combat.ApplyCriticalHitEffects(attackerId, defenderId, rolledDamage, effectiveCritical, true, weaponSkillType);
                        }
                        if (rolledDamage > 0 && damageableTarget)
                        {
                            // Native weapon damage needs a pending registration; managed projectile
                            // effects restore the captured attribution around their actual application.
                            if (nativeDamageApplication && CombatRecorder.IsRecording)
                                CombatRecorder.ExpectWeaponDamage(attackerId, defenderId, rolledDamage, weaponSkillType);
                            PublishDamageDealtEvent(attackerId, defenderId, weaponId, rolledDamage, weaponSkillType, rolledType);
                        }
                    }
                    if (weaponImpactScheduler == null) ApplyWeaponHitEffects(totalDamage);
                    else weaponImpactScheduler(ApplyWeaponHitEffects);
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
            out int totalDamage, out int effectiveCritical, Action<Action> convertedDamageScheduler,
            Action<Action<int>> weaponImpactScheduler, Action<Func<int>> damageTakenResolverCapture)
        {
            var physicalDamage = 0;
            effectiveCritical = critical;
            totalDamage = 0;

            if (UsePerkFeat.HasQueuedWeaponAbility(attacker.m_idSelf, skillType, attacker.m_pcCombatRound.m_nCurrentAttack))
            {
                Combat.ConsumeSuppressedAutoAttackDamageBonuses(attacker.m_idSelf, skillType);
                return physicalDamage;
            }

            var damage = CalculateTargetSpecificDamage(pTarget, attacker, damageProfile,
                attackerAttack, attackerStat, critical, weaponDeltaCap, attackType, damageFlags, bOffHand, skillType,
                isLandedAttack, out effectiveCritical, convertedDamageScheduler, damageTakenResolverCapture);

            if (targetObject.m_nObjectType != (int)ObjectType.Creature)
            {
                var pairedShot = RangedWeaponAttackContext.GetRoll(attacker.m_idSelf, targetObject.m_idSelf,
                    attacker.m_pcCombatRound.m_nCurrentAttack);
                damage = ApplyNonCreatureOrdinaryAttackDamage(damage,
                    pairedShot is { Ordinary: true } ? pairedShot.Bonuses.DamagePercentAdjustment : 0,
                    Stat.GetStatAdjustment(attacker.m_idSelf, StatType.OrdinaryAttackDamagePercentAdjustment));
            }

            // Plot target takes no damage
            if (targetObject.m_bPlotObject == 1)
                damage = 0;

            // Ensure damage is never negative
            if (damage < 0)
                damage = 0;

            if (isLandedAttack && damage > 0 && targetObject.m_nObjectType == (int)ObjectType.Creature)
            {
                var attackerId = attacker.m_idSelf;
                var defenderId = targetObject.m_idSelf;
                var rolledType = damageProfile.DamageType;
                void ApplyPreDamageEffects(int resolvedDamage)
                {
                    if (resolvedDamage <= 0 || !GetIsObjectValid(attackerId) || !GetIsObjectValid(defenderId)) return;
                    StatusEffect.NotifyPreDamageStatusEffects(attackerId, defenderId, resolvedDamage, rolledType);
                    Combat.ApplyDamageReflectionEffects(attackerId, defenderId, resolvedDamage, rolledType);
                }
                if (weaponImpactScheduler == null) ApplyPreDamageEffects(damage);
                else weaponImpactScheduler(ApplyPreDamageEffects);
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

        /// <summary>Matches creature-hit bonus rounding and caps before the global ordinary-attack multiplier.</summary>
        public static int ApplyNonCreatureOrdinaryAttackDamage(int damage, int capturedDamagePercentAdjustment,
            int ordinaryDamagePercentAdjustment)
        {
            var adjustedDamage = damage;
            if (damage > 0 && capturedDamagePercentAdjustment != 0)
            {
                var adjustment = Math.Max(capturedDamagePercentAdjustment, -Combat.MaximumNormalDamageReductionPercent);
                adjustedDamage = Math.Max(1, damage + (int)Math.Ceiling(damage * (adjustment / 100f)));
            }
            adjustedDamage = Combat.CapOutgoingDamageBonus(damage, adjustedDamage, 0);
            return Combat.ApplyOrdinaryAttackDamageMultiplier(adjustedDamage, ordinaryDamagePercentAdjustment);
        }

        private static void AddDamageToAttackData(void* pAttackData, CombatDamageType damageType, int damage)
        {
            if (damage <= 0) return;

            var attackData = CNWSCombatAttackData.FromPointer(pAttackData);
            attackData.AddDamage((ushort)damageType.GetNativeDamageType(), damage);
        }

        // Pay at the attack-roll boundary, including misses. Damage can be queried speculatively
        // or repeatedly for one swing, so it must only read the conversion decision made here.
        internal static void PrepareForceConversionStanceAttack(
            CNWSCreature attacker,
            CNWSObject defender,
            SkillType weaponSkillType)
        {
            DeleteLocalInt(attacker.m_idSelf, ForceConversionAttackIndex);
            DeleteLocalObject(attacker.m_idSelf, ForceConversionAttackTarget);

            if (defender.m_nObjectType != (int)ObjectType.Creature)
                return;

            var conversion = Stat.GetStatAdjustment(attacker.m_idSelf, StatType.StanceHostileAutoAttackForceConversion);
            if (conversion <= 0)
                return;

            // Weapon abilities apply their own combat impact and suppress the auto-attack; do not convert/charge them.
            if (UsePerkFeat.HasQueuedWeaponAbility(attacker.m_idSelf, weaponSkillType, attacker.m_pcCombatRound.m_nCurrentAttack))
                return;

            var fpCost = Stat.GetStatAdjustment(attacker.m_idSelf, StatType.StanceHostileAutoAttackFPCost);
            if (fpCost > 0)
            {
                // Not enough FP to pay the upkeep: the swing stays its normal type and no FP is spent.
                if (Stat.GetCurrentFP(attacker.m_idSelf) < fpCost)
                    return;

                Stat.ReduceFP(attacker.m_idSelf, fpCost);
            }

            // Offset the index so an absent local (zero) cannot match the first attack.
            SetLocalInt(attacker.m_idSelf, ForceConversionAttackIndex, attacker.m_pcCombatRound.m_nCurrentAttack + 1);
            SetLocalObject(attacker.m_idSelf, ForceConversionAttackTarget, defender.m_idSelf);
        }

        private static WeaponDamageProfile ApplyForceConversionStance(
            CNWSCreature attacker,
            CNWSObject defender,
            WeaponDamageProfile damageProfile)
        {
            if (GetLocalInt(attacker.m_idSelf, ForceConversionAttackIndex) != attacker.m_pcCombatRound.m_nCurrentAttack + 1 ||
                GetLocalObject(attacker.m_idSelf, ForceConversionAttackTarget) != defender.m_idSelf)
                return damageProfile;

            return new WeaponDamageProfile(CombatDamageType.Force, damageProfile.Damage, damageProfile.HasItemDamage);
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
                return new WeaponDamageProfile(CombatDamageType.Physical, DefaultPhysicalDamage, false);
            }

            return new WeaponDamageProfile(damageType, damage);
        }

        private static CombatDamageType ResolveWeaponDamageType(CombatDamageType current, int damageTypeId)
        {
            return WeaponDamage.ResolveDamageType(current, damageTypeId);
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
                    return new WeaponDamageProfile(damageProfile.DamageType, damageProfile.Damage + PowerAttackDamageBonus, damageProfile.HasItemDamage);
                case ImprovedPowerAttackMode:
                    return new WeaponDamageProfile(damageProfile.DamageType, damageProfile.Damage + ImprovedPowerAttackDamageBonus, damageProfile.HasItemDamage);
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

            return new WeaponDamageProfile(damageProfile.DamageType, damageProfile.Damage + mightModifier * multiplier, damageProfile.HasItemDamage);
        }

        private static int CalculateTargetSpecificDamage(void* pTarget, CNWSCreature attacker,
            WeaponDamageProfile damageProfile, int attackerAttack,
            int attackerStat, int critical, int weaponDeltaCap, uint attackType, uint damageFlags, int bOffHand, SkillType skillType,
            bool isLandedAttack, out int effectiveCritical, Action<Action> convertedDamageScheduler,
            Action<Func<int>> damageTakenResolverCapture)
        {
            effectiveCritical = critical;
            var targetObject = CNWSObject.FromPointer(pTarget);

            switch (targetObject.m_nObjectType)
            {
                case (int)ObjectType.Creature:
                    return CalculateCreatureDamage(pTarget, attacker, damageProfile, attackerAttack,
                        attackerStat, critical, weaponDeltaCap, attackType, damageFlags, bOffHand, skillType,
                        isLandedAttack, out effectiveCritical, convertedDamageScheduler, damageTakenResolverCapture);

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
            bool isLandedAttack, out int effectiveCritical, Action<Action> convertedDamageScheduler,
            Action<Func<int>> damageTakenResolverCapture)
        {
            effectiveCritical = critical;
            var target = CNWSCreature.FromPointer(pTarget);
            var damageType = damageProfile.DamageType;
            var defenderAbility = damageType.GetDefenseAbilityType();
            var defenderStat = Stat.GetStatValueNative(target, defenderAbility);
            var damagePower = ResolveDamagePower(attacker, target, bOffHand);
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
                               MeleeTechnique.TakeOrdinaryPower(attacker.m_idSelf, target.m_idSelf, skillType,
                                   attacker.m_pcCombatRound.m_nCurrentAttack, isLandedAttack) +
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

            var openingCriticalDamageAdjustment = isLandedAttack
                ? Combat.ConsumeOpeningAutoAttackCriticalDamageAdjustment(attacker.m_idSelf)
                : 0;
            damage = Combat.ApplyCriticalDamageModifier(
                attacker.m_idSelf,
                damage,
                effectiveCritical,
                skillType,
                target.m_idSelf,
                openingCriticalDamageAdjustment);

            if (isLandedAttack)
            {
                damage = Combat.ApplyAutoAttackDamageModifiers(attacker.m_idSelf, target.m_idSelf, damage, skillType);
            }
            var canApplyRandomFlatBonusesThisDamage = damage > 0;
            var pairedShot = RangedWeaponAttackContext.GetRoll(attacker.m_idSelf, target.m_idSelf,
                attacker.m_pcCombatRound.m_nCurrentAttack);

            damage = Combat.ApplyDamageDealtModifiers(
                attacker.m_idSelf,
                target.m_idSelf,
                damage,
                skillType,
                damageType,
                false,
                canApplyRandomFlatBonusesThisDamage,
                isLandedAttack,
                null,
                out var targetStatusDamageAdjustment,
                abilityDamagePercentAdjustment: pairedShot is { Ordinary: true }
                    ? pairedShot.Bonuses.DamagePercentAdjustment : 0,
                isOrdinaryWeaponAttack: pairedShot is { Ordinary: true });

            damage = MeleeTechnique.ApplyOrdinaryDamageModifiers(attacker.m_idSelf, damage, damageType);

            // Saber Ward / Aegis Eternal: re-type a share of the physical hit into a real Force
            // instance (mitigated by Force resistance, shown as Force) before physical resistance.
            if (isLandedAttack)
            {
                Combat.ApplyIncomingPhysicalToForceConversion(attacker.m_idSelf, target.m_idSelf, damageType, ref damage, targetStatusDamageAdjustment, convertedDamageScheduler);
            }

            // Conversion must split first so each portion receives only its own typed Leadership channel.
            damage = Combat.ApplyTypedLeadershipDamageTakenModifier(target.m_idSelf, damage, damageType);

            damage = Resistance.ApplyResistanceToDamageNative(target, damageType, damage);

            // Apply droid electrical damage bonus
            if (target.m_pStats.m_nRace == (ushort)RacialType.Robot &&
                damageType.TryGetElementalResistanceType(out var elementalResistanceType) &&
                elementalResistanceType == ResistanceType.Electrical)
            {
                damage *= ElectricalDroidMultiplier;
            }

            if (damageTakenResolverCapture != null)
            {
                damageTakenResolverCapture(CaptureDamageTakenResolution(attacker.m_idSelf, target.m_idSelf,
                    damage, damageType, damageFlags, damagePower, attackType, isLandedAttack,
                    targetStatusDamageAdjustment, guardedHitBonuses.EnmityBonus));
                return damage;
            }

            return ApplyCreatureDamageTaken(attacker, target, damage, damageType, damageFlags, damagePower,
                attackType, isLandedAttack, targetStatusDamageAdjustment, guardedHitBonuses.EnmityBonus);
        }

        // Only managed IDs and launch values enter the delayed closure. Native wrappers are
        // reacquired at impact, after the caller has validated the projectile's target and area.
        private static Func<int> CaptureDamageTakenResolution(uint attackerId, uint defenderId,
            int damage, CombatDamageType damageType, uint damageFlags, byte damagePower, uint attackType,
            bool isLandedAttack, int targetStatusDamageAdjustment, int guardedHitEnmityBonus)
        {
            var resolved = false;
            var result = 0;
            return () =>
            {
                if (resolved) return result;
                resolved = true;
                var attacker = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(attackerId)?.AsNWSCreature();
                var defender = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(defenderId)?.AsNWSCreature();
                if (attacker == null || defender == null) return result;
                result = ApplyCreatureDamageTaken(attacker, defender, damage, damageType, damageFlags, damagePower,
                    attackType, isLandedAttack, targetStatusDamageAdjustment, guardedHitEnmityBonus);
                return result;
            };
        }

        private static int ApplyCreatureDamageTaken(CNWSCreature attacker, CNWSCreature target,
            int damage, CombatDamageType damageType, uint damageFlags, byte damagePower, uint attackType,
            bool isLandedAttack, int targetStatusDamageAdjustment, int guardedHitEnmityBonus)
        {
            // Native physical effects can have finite absorption budgets. Resolve those,
            // Guard reactions and the shared damage-taken pipeline at the same impact boundary.
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
                targetStatusDamagePercentAdjustment: targetStatusDamageAdjustment,
                isLandedAttack: isLandedAttack,
                typedLeadershipReductionAlreadyApplied: true);
            if (isLandedAttack)
            {
                Combat.ApplyNextAttackGuardedHitEnmityBonus(
                    attacker.m_idSelf,
                    target.m_idSelf,
                    guardedHitEnmityBonus);
            }
            return damage;
        }

        private static byte ResolveDamagePower(CNWSCreature attacker, CNWSObject target, int offHand)
        {
            var attack = attacker.m_pcCombatRound.GetAttack(attacker.m_pcCombatRound.m_nCurrentAttack);
            var attackTypeBeforePower = attack.m_nWeaponAttackType;
            if (offHand != 0 && PowerGloveEquipment.IsActivePair(attacker.m_idSelf))
            {
                // The cosmetic left glove carries no enhancement; penetrate with the owned glove's main-hand scope.
                try
                {
                    attack.m_nWeaponAttackType = 1;
                    return attacker.CalculateDamagePower(target, 0);
                }
                finally { attack.m_nWeaponAttackType = attackTypeBeforePower; }
            }

            if (!RangedWeaponAttackContext.IsSimulated(attacker.m_idSelf))
                return attacker.CalculateDamagePower(target, offHand);

            try
            {
                // GetWeaponPower prefers the attack-data hand over its boolean argument.
                // Select the companion's native effect scope only for penetration; the
                // borrowed roll remains a ranged launcher everywhere else.
                attack.m_nWeaponAttackType = 2;
                return attacker.CalculateDamagePower(target, 1);
            }
            finally { attack.m_nWeaponAttackType = attackTypeBeforePower; }
        }

        private readonly struct WeaponDamageProfile
        {
            public CombatDamageType DamageType { get; }
            public int Damage { get; }
            public bool HasItemDamage { get; }

            public WeaponDamageProfile(CombatDamageType damageType, int damage, bool hasItemDamage = true)
            {
                DamageType = damageType;
                Damage = damage;
                HasItemDamage = hasItemDamage;
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
