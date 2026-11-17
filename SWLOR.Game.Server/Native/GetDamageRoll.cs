using NWN.Native.API;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using EquipmentSlot = NWN.Native.API.EquipmentSlot;
using ObjectType = NWN.Native.API.ObjectType;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using SWLOR.Game.Server.Composition;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Native
{
    public static unsafe class GetDamageRoll
    {
        private const int AttributeNegativeThreshold = 128;
        private const int AttributeNegativeOffset = 256;
        private const string ForceConversionAttackIndex = "FORCE_CONVERSION_ATTACK_INDEX";
        private const string ForceConversionAttackTarget = "FORCE_CONVERSION_ATTACK_TARGET";

        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            NativeHookAccess.Hooks.Enable(NativeHookId.GetDamageRoll);
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
                    "Script", "NATIVE:OnGetDamageRoll",
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
                var damageProfile = DamageRollWeapon.ExtractWeaponDamageProfile(weapon);
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
                    : GameServices.Skill.GetWeaponSkill(attacker.m_idSelf, weapon.m_idSelf);

                // Imbuement Stance converts the wearer's hostile weapon auto-attacks to Force damage for an FP cost.
                damageProfile = ApplyForceConversionStance(attacker, defender, damageProfile);

                var attackerStatType = explicitProfile?.DamageAbility ?? DamageRollWeapon.GetWeaponDamageAbilityType(attacker.m_idSelf, weapon);
                var weaponDeltaCap = DamageRollWeapon.GetWeaponDeltaCap(weapon);

                var attackerStat = CombatRating.GetStatValueNative(attacker, attackerStatType);

                // Handle negative attributes
                if (attackerStat > AttributeNegativeThreshold)
                    attackerStat -= AttributeNegativeOffset;

                LogDamageCalculation(attackerStat, damageProfile);

                // Apply combat mode bonuses
                if (weapon != null && damageProfile.HasItemDamage)
                    damageProfile = new WeaponDamageProfile(damageProfile.DamageType,
                        WeaponDamage.GetEffectiveDMG(attacker.m_idSelf, weapon.m_idSelf, damageProfile.Damage));
                damageProfile = DamageRollWeapon.ApplyCombatModeBonus(attacker, damageProfile);
                damageProfile = DamageRollWeapon.ApplyMightModifierDamageBonus(attacker, weapon, damageProfile);

                var critical = bCritical == 1
                    ? CombatRules.StandardCriticalRating
                    : 0;
                // Force-typed swings (e.g. Imbuement Stance) use Force Attack so the attack side lines up
                // with the Force Defense the damage is mitigated against.
                var useForceAttack = damageProfile.DamageType == CombatDamageType.Force;
                var attackerAttack = weapon == null ? 0 : CombatRating.GetAttackNative(attacker, (BaseItem)weapon.m_nBaseItem,
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
                    DamageRollHitEffects.CompleteShot(attacker, defender, shot, damageProfile, weaponSkillType,
                        isLandedAttack, totalDamage, weaponImpactScheduler);
                }

                if (isLandedAttack)
                {
                    DamageRollHitEffects.ApplyLandedHitEffects(attacker, defender, weapon, pAttackData, damageProfile,
                        weaponSkillType, effectiveCritical, totalDamage, weaponImpactScheduler);
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
                GameServices.CombatHitEffects.ConsumeSuppressedAutoAttackDamageBonuses(attacker.m_idSelf, skillType);
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
                    GameServices.CombatDamageTaken.ApplyDamageReflectionEffects(attackerId, defenderId, resolvedDamage, rolledType);
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
                var adjustment = Math.Max(capturedDamagePercentAdjustment, -CombatRules.MaximumNormalDamageReductionPercent);
                adjustedDamage = Math.Max(1, damage + (int)Math.Ceiling(damage * (adjustment / 100f)));
            }
            adjustedDamage = CombatRules.CapOutgoingDamageBonus(damage, adjustedDamage, 0);
            return CombatRules.ApplyOrdinaryAttackDamageMultiplier(adjustedDamage, ordinaryDamagePercentAdjustment);
        }

        private static void AddDamageToAttackData(void* pAttackData, CombatDamageType damageType, int damage)
        {
            if (damage <= 0) return;

            var attackData = CNWSCombatAttackData.FromPointer(pAttackData);
            attackData.AddDamage((ushort)damageType.GetNativeDamageType(), damage);
        }

        // Decide at the attack-roll boundary; SettleForceConversionStanceAttack pays once the result
        // is known. Damage can be queried speculatively or repeatedly for one swing, so it must only
        // read the conversion decision made here.
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

            // Not enough FP to pay the upkeep: the swing stays its normal type and no FP is spent.
            var fpCost = Stat.GetStatAdjustment(attacker.m_idSelf, StatType.StanceHostileAutoAttackFPCost);
            if (fpCost > 0 && Stat.GetCurrentFP(attacker.m_idSelf) < fpCost)
                return;

            // Offset the index so an absent local (zero) cannot match the first attack.
            SetLocalInt(attacker.m_idSelf, ForceConversionAttackIndex, attacker.m_pcCombatRound.m_nCurrentAttack + 1);
            SetLocalObject(attacker.m_idSelf, ForceConversionAttackTarget, defender.m_idSelf);
        }

        // Only a landed swing pays the upkeep. A miss or deflection deals no damage, so its
        // conversion is cleared without charging FP.
        internal static void SettleForceConversionStanceAttack(
            CNWSCreature attacker,
            CNWSObject defender,
            bool landed)
        {
            if (GetLocalInt(attacker.m_idSelf, ForceConversionAttackIndex) != attacker.m_pcCombatRound.m_nCurrentAttack + 1 ||
                GetLocalObject(attacker.m_idSelf, ForceConversionAttackTarget) != defender.m_idSelf)
                return;

            if (!landed)
            {
                DeleteLocalInt(attacker.m_idSelf, ForceConversionAttackIndex);
                DeleteLocalObject(attacker.m_idSelf, ForceConversionAttackTarget);
                return;
            }

            var fpCost = Stat.GetStatAdjustment(attacker.m_idSelf, StatType.StanceHostileAutoAttackFPCost);
            if (fpCost > 0)
                Stat.ReduceFP(attacker.m_idSelf, fpCost);
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
                    return DamageRollCreature.CalculateCreatureDamage(pTarget, attacker, damageProfile, attackerAttack,
                        attackerStat, critical, weaponDeltaCap, attackType, damageFlags, bOffHand, skillType,
                        isLandedAttack, out effectiveCritical, convertedDamageScheduler, damageTakenResolverCapture);

                case (int)ObjectType.Placeable:
                    var plc = CNWSPlaceable.FromPointer(pTarget);
                    return GameServices.Combat.CalculateDamage(attackerAttack, damageProfile.Damage, attackerStat,
                        plc.m_nHardness, plc.m_nHardness, critical);

                case (int)ObjectType.Door:
                    var door = CNWSDoor.FromPointer(pTarget);
                    return GameServices.Combat.CalculateDamage(attackerAttack, damageProfile.Damage, attackerStat,
                        door.m_nHardness, door.m_nHardness, critical);

                default:
                    return damageProfile.Damage;
            }
        }

    }
}
