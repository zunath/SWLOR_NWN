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
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Skill.Contracts;
using SWLOR.Shared.UI.Service;
using FeatType = SWLOR.NWN.API.NWScript.Enum.FeatType;
using ILogger = SWLOR.Shared.Abstractions.Contracts.ILogger;
using ImmunityType = SWLOR.NWN.API.NWScript.Enum.ImmunityType;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;

namespace SWLOR.Component.Combat.Native
{
    public static unsafe class ResolveAttackRoll
    {
        private static readonly IScriptExecutor _executor = ServiceContainer.GetService<IScriptExecutor>();
        private static readonly IItemService _itemService = ServiceContainer.GetService<IItemService>();
        private static readonly ISkillService _skillService = ServiceContainer.GetService<ISkillService>();
        private static readonly IStatCalculationService _statCalculation = ServiceContainer.GetService<IStatCalculationService>();
        private static readonly IRandomService _random = ServiceContainer.GetService<IRandomService>();
        private static readonly ICombatService _combatService = ServiceContainer.GetService<ICombatService>();
        private static readonly IAbilityService _abilityService = ServiceContainer.GetService<IAbilityService>();
        private static readonly ILogger _logger = ServiceContainer.GetService<ILogger>();
        private static readonly IProfilerPluginService _profilerPlugin = ServiceContainer.GetService<IProfilerPluginService>();

        // Attack result constants
        private const int AttackResultAutomaticHit = 7;
        private const int AttackResultRegularHit = 1;
        private const int AttackResultDeflect = 2;
        private const int AttackResultCriticalHit = 3;
        private const int AttackResultMiss = 4;

        // Combat mode constants
        private const int PowerAttackMode = 2;
        private const int ImprovedPowerAttackMode = 3;

        // Modifier constants
        private const int CircumstanceBonus = 5;
        private const int WeaponFocusBonus = 5;
        private const int SuperiorWeaponFocusBonus = 5;
        private const int PointBlankShotBonus = 5;
        private const int BackstabBonus = 30;
        private const int PowerAttackPenalty = -5;
        private const int ImprovedPowerAttackPenalty = -10;
        private const int CloseRangePenalty = -20;
        private const int LongRangePenalty = -20;
        private const int MediumRangePenalty = -10;
        private const int ShortRangePenalty = -5;
        private const int TwoWeaponPenalty = -10;
        private const int FlurryStylePenalty = -10;
        private const int DuelistBonus = 5;

        // Range constants
        private const float CloseRange = 5.0f;
        private const float ShortRange = 20.0f;
        private const float MediumRange = 30.0f;
        private const float LongRange = 40.0f;

        // Backstab constants
        private const float BackstabAngleThreshold = 0.5f;

        // Critical constants
        private const int ImprovedCriticalBonus = 5;
        private const int PrecisionAim1Bonus = 2;
        private const int PrecisionAim2Bonus = 4;
        private const int CrushingStyleBonus = 15;
        private const int CrushingMasteryBonus = 15;

        // Deflection constants
        private const int SaberDeflectChance = 5;
        private const int ShieldDeflectChance = 10;

        // NPC object ID constant
        private const uint NpcActionTargetId = 2130706432;

        // Default values
        private const int DefaultMissedBy = 1;
        private const int DefaultToHitMod = 1;
        private const int DefaultToHitRoll = 1;

        // Optimized weapon feat lookups
        private static readonly Dictionary<BaseItemType, FeatType> _weaponFocusLookup = CreateWeaponFocusLookup();
        private static readonly Dictionary<BaseItemType, FeatType> _improvedCriticalLookup = CreateImprovedCriticalLookup();

        private static Dictionary<BaseItemType, FeatType> CreateWeaponFocusLookup()
        {
            var lookup = new Dictionary<BaseItemType, FeatType>();

            void AddItems(IEnumerable<BaseItemType> items, FeatType feat)
            {
                foreach (var item in items)
                    lookup[item] = feat;
            }

            lookup[BaseItemType.Gloves] = FeatType.WeaponFocus_UnarmedStrike;
            AddItems(_itemService.CreatureBaseItemTypes, FeatType.WeaponFocus_Creature);
            AddItems(_itemService.VibrobladeBaseItemTypes, FeatType.WeaponFocusVibroblades);
            AddItems(_itemService.FinesseVibrobladeBaseItemTypes, FeatType.WeaponFocusFinesseVibroblades);
            AddItems(_itemService.LightsaberBaseItemTypes, FeatType.WeaponFocusLightsabers);
            AddItems(_itemService.HeavyVibrobladeBaseItemTypes, FeatType.WeaponFocusHeavyVibroblades);
            AddItems(_itemService.PolearmBaseItemTypes, FeatType.WeaponFocusPolearms);
            AddItems(_itemService.TwinBladeBaseItemTypes, FeatType.WeaponFocusTwinBlades);
            AddItems(_itemService.SaberstaffBaseItemTypes, FeatType.WeaponFocusSaberstaffs);
            AddItems(_itemService.KatarBaseItemTypes, FeatType.WeaponFocusKatars);
            AddItems(_itemService.StaffBaseItemTypes, FeatType.WeaponFocus_Staff);
            AddItems(_itemService.PistolBaseItemTypes, FeatType.WeaponFocusPistol);
            AddItems(_itemService.ThrowingWeaponBaseItemTypes, FeatType.WeaponFocusThrowingWeapons);
            AddItems(_itemService.RifleBaseItemTypes, FeatType.WeaponFocusRifles);

            return lookup;
        }

        private static Dictionary<BaseItemType, FeatType> CreateImprovedCriticalLookup()
        {
            var lookup = new Dictionary<BaseItemType, FeatType>();

            void AddItems(IEnumerable<BaseItemType> items, FeatType feat)
            {
                foreach (var item in items)
                    lookup[item] = feat;
            }

            lookup[BaseItemType.Gloves] = FeatType.ImprovedCritical_UnarmedStrike;
            AddItems(_itemService.CreatureBaseItemTypes, FeatType.ImprovedCritical_Creature);
            AddItems(_itemService.VibrobladeBaseItemTypes, FeatType.ImprovedCriticalVibroblades);
            AddItems(_itemService.FinesseVibrobladeBaseItemTypes, FeatType.ImprovedCriticalFinesseVibroblades);
            AddItems(_itemService.LightsaberBaseItemTypes, FeatType.ImprovedCriticalLightsabers);
            AddItems(_itemService.HeavyVibrobladeBaseItemTypes, FeatType.ImprovedCriticalHeavyVibroblades);
            AddItems(_itemService.PolearmBaseItemTypes, FeatType.ImprovedCriticalPolearms);
            AddItems(_itemService.TwinBladeBaseItemTypes, FeatType.ImprovedCriticalTwinBlades);
            AddItems(_itemService.SaberstaffBaseItemTypes, FeatType.ImprovedCriticalSaberstaffs);
            AddItems(_itemService.KatarBaseItemTypes, FeatType.ImprovedCriticalKatars);
            AddItems(_itemService.StaffBaseItemTypes, FeatType.ImprovedCritical_Staff);
            AddItems(_itemService.PistolBaseItemTypes, FeatType.ImprovedCriticalPistol);
            AddItems(_itemService.ThrowingWeaponBaseItemTypes, FeatType.ImprovedCriticalThrowingWeapons);
            AddItems(_itemService.RifleBaseItemTypes, FeatType.ImprovedCriticalRifles);

            return lookup;
        }
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
                var pDefender = CNWSCreature.FromPointer(pTarget);
                var pCombatRound = pAttacker.m_pcCombatRound;
                var pWeapon = pCombatRound.GetCurrentAttackWeapon();
                var pTargetObject = CNWSObject.FromPointer(pTarget);

                var attacker = pAttacker.m_idSelf;
                var defender = pDefender == null ? OBJECT_INVALID : pDefender.m_idSelf;
                var target = pTargetObject.m_idSelf;
                var area = pAttacker.GetArea();
                var attackerStats = pAttacker.m_pStats;
                var weapon = pWeapon.m_idSelf;
                var baseItemType = GetBaseItemType(weapon);
                var weaponStyleAbilityOverride = GetWeaponStyleAbilityType(weapon, attacker);
                var ability = weaponStyleAbilityOverride != AbilityType.Invalid
                    ? weaponStyleAbilityOverride
                    : _itemService.GetWeaponAccuracyAbilityType(baseItemType);
                var skillType = _skillService.GetSkillTypeByBaseItem(baseItemType);

                _profilerPlugin.PushPerfScope("RunScript",
                    "Script", $"NATIVE:{nameof(OnResolveAttackRoll)}",
                    "Area", area.m_sTag.ToString(),
                    "ObjectType", "Creature");

                _logger.Write<AttackLogGroup>("Running OnResolveAttackRoll");
                if (!GetIsObjectValid(target))
                {
                    _profilerPlugin.PopPerfScope();
                    return;
                }

                _logger.Write<AttackLogGroup>("Attacker: " + GetName(attacker) + ", defender " + GetName(target));

                var pAttackData = pCombatRound.GetAttack(pCombatRound.m_nCurrentAttack);

                if (GetObjectType(target) != ObjectType.Creature)
                {
                    // Automatically hit non-creature targets.  Do not apply criticals.
                    _logger.Write<AttackLogGroup>("Placeable target.  Auto hit.");
                    pAttackData.m_nAttackResult = AttackResultAutomaticHit;
                    _profilerPlugin.PopPerfScope();
                    return;
                }

                var attackType = (uint)AttackType.Melee;

                // Check whether this is a ranged weapon. 
                if (GetIsObjectValid(weapon) && 
                    pAttackData.m_bRangedAttack == 1 && 
                    pAttacker.GetRangeWeaponEquipped() == 1)
                {
                    attackType = (uint)AttackType.Ranged;
                }

                _logger.Write<AttackLogGroup>("Selected attack type " + attackType + ", weapon " + (!GetIsObjectValid(weapon) ? "none" : GetName(weapon)));

                var attackerAccuracy = _statCalculation.CalculateAccuracy(attacker, ability, skillType);
                var defenderEvasion = _statCalculation.CalculateEvasion(defender);

                var accuracyModifiers = 0;

                // Weapon focus feats.
                accuracyModifiers += WeaponFocusBonus * (HasWeaponFocus(attacker, weapon) ? 1 : 0);
                accuracyModifiers += SuperiorWeaponFocusBonus * (HasSuperiorWeaponFocus(attacker, weapon) ? 1 : 0);

                // Range bonuses and penalties
                accuracyModifiers += CalculateRangeModifiers(attackType, attacker, defender, weapon);

                // Dual wield and weapon style modifiers
                var percentageModifier = CalculateDualWieldAndStyleModifiers(attacker, weapon);

                var attackRoll = _random.Next(1, 100);
                var hitRate = _combatService.CalculateHitRate(attackerAccuracy + accuracyModifiers, defenderEvasion, percentageModifier);
                var isHit = attackRoll <= hitRate;

                _logger.Write<AttackLogGroup>($"attackerAccuracy = {attackerAccuracy}, modifiers = {accuracyModifiers}, defenderEvasion = {defenderEvasion}");
                _logger.Write<AttackLogGroup>($"Hit Rate: {hitRate}, Roll = {attackRoll}");

                // Check for deflection
                var deflected = CheckDeflection(attackType, isHit, attacker, defender);
                if (deflected)
                    isHit = false;

                // Hit
                if (isHit)
                {
                    var defenderMGT = GetAbilityScore(defender, AbilityType.Might);
                    var criticalStat = attackerStats.GetDEXStat();
                    var criticalRoll = _random.Next(1, 100);
                    var criticalBonus = _statCalculation.CalculateCriticalRate(attacker);
                    var criticalRate = _combatService.CalculateCriticalRate(criticalStat, defenderMGT, criticalBonus);

                    // Critical
                    if (criticalRoll <= criticalRate)
                    {
                        _logger.Write<AttackLogGroup>($"Critical hit");

                        // Critical Hit - populate variables for feedback
                        pAttackData.m_bCriticalThreat = 1;
                        pAttackData.m_nThreatRoll = 1;

                        if (GetIsImmune(defender, ImmunityType.CriticalHit, attacker))
                        {
                            _logger.Write<AttackLogGroup>($"Immune to critical hits");
                            // Immune!
                            var defenderName = GetName(defender);
                            SendMessageToPC(attacker, $"{defenderName} is immune to critical hits!");
                            pAttackData.m_nAttackResult = AttackResultRegularHit;
                        }
                        else
                        {
                            _logger.Write<AttackLogGroup>($"Not immune to critical hits - dealing crit damage");
                            pAttackData.m_nAttackResult = AttackResultCriticalHit;
                        }
                    }
                    // Regular Hit
                    else
                    {
                        _logger.Write<AttackLogGroup>($"Regular hit - attack result 1");
                        pAttackData.m_nAttackResult = AttackResultRegularHit;
                    }
                }
                // Miss
                else
                {
                    if (deflected)
                    {
                        _logger.Write<AttackLogGroup>($"Deflected - setting attack result to 2");
                        pAttackData.m_nAttackResult = AttackResultDeflect;
                    }
                    else
                    {
                        _logger.Write<AttackLogGroup>($"Miss - setting attack result to 4, missed by 0");
                        pAttackData.m_nAttackResult = AttackResultMiss;
                    }
                    pAttackData.m_nMissedBy = DefaultMissedBy;
                }

                _logger.Write<AttackLogGroup>($"Resolving NWN defensive effects");
                // Resolve any defensive effects (like concealment).  Do this after all the above so that the attack data is 
                // accurate.
                pAttacker.ResolveDefensiveEffects(pDefender, isHit ? 1 : 0);

                _logger.Write<AttackLogGroup>($"Building combat log message");
                var message = _combatService.BuildCombatLogMessage(
                    attacker,
                    defender,
                    pAttackData.m_nAttackResult,
                    hitRate);

                SendMessageToPC(attacker, message);
                SendMessageToPC(defender, message);

                _logger.Write<AttackLogGroup>($"Setting pAttackData results");
                pAttackData.m_nToHitMod = DefaultToHitMod;
                pAttackData.m_nToHitRoll = DefaultToHitRoll;

                _logger.Write<AttackLogGroup>($"Finished ResolveAttackRoll");

                _profilerPlugin.PopPerfScope();
            });
        }

        private static bool HasWeaponFocus(uint attacker, uint weapon)
        {
            if (!GetIsObjectValid(weapon))
            {
                return GetHasFeat(FeatType.WeaponFocus_UnarmedStrike, attacker);
            }

            var baseItemType = GetBaseItemType(weapon);
            if (_weaponFocusLookup.TryGetValue(baseItemType, out var feat))
            {
                return GetHasFeat(feat, attacker);
            }

            _logger.Write<AttackLogGroup>("No weapon focus feat found.");
            return false;
        }

        private static int HasImprovedCritical(CNWSCreature attacker, CNWSItem weapon)
        {
            if (weapon == null)
            {
                return attacker.m_pStats.HasFeat((ushort)FeatType.ImprovedCritical_UnarmedStrike);
            }

            var baseItemType = (BaseItemType)weapon.m_nBaseItem;
            if (_improvedCriticalLookup.TryGetValue(baseItemType, out var feat))
            {
                return attacker.m_pStats.HasFeat((ushort)feat);
            }

            _logger.Write<AttackLogGroup>("No improved critical feat found.");
            return 0;
        }

        private static bool HasSuperiorWeaponFocus(uint attacker, uint weapon)
        {
            if (!GetIsObjectValid(weapon)) 
                return false;
            
            if (!GetHasFeat(FeatType.SuperiorWeaponFocus, attacker))
                return false;

            var baseItemType = GetBaseItemType(weapon);

            if (_itemService.StaffBaseItemTypes.Contains(baseItemType)) return true;
            if (_itemService.PolearmBaseItemTypes.Contains(baseItemType)) return true;
            if (_itemService.HeavyVibrobladeBaseItemTypes.Contains(baseItemType)) return true;

            return false;
        }

        private static int CalculateRangeModifiers(uint attackType, uint attacker, uint defender, uint weapon)
        {
            if (attackType != (uint)AttackType.Ranged)
                return 0;

            var isValidWeapon = GetIsObjectValid(weapon);
            var attackerPos = GetPosition(attacker);
            var defenderPos = GetPosition(defender);
            var baseItemType = GetBaseItemType(weapon);

            // Calculate distance using X/Y coordinates only
            var range = Math.Sqrt(Math.Pow(attackerPos.X - defenderPos.X, 2) + Math.Pow(attackerPos.X - defenderPos.Y, 2));

            _logger.Write<AttackLogGroup>($"Ranged attack at range {range}");

            // Close range (under 5.0)
            if (range < CloseRange)
            {
                if (GetHasFeat(FeatType.PointBlankShot, attacker))
                    return PointBlankShotBonus;
                else if (isValidWeapon)
                    return CloseRangePenalty;
            }
            // Long range (over 40.0)
            else if (range > LongRange)
            {
                if (isValidWeapon && !_itemService.RifleBaseItemTypes.Contains(baseItemType))
                    return LongRangePenalty;
                else
                    return MediumRangePenalty;
            }
            // Medium range (30.0 - 40.0)
            else if (range > MediumRange)
            {
                if (isValidWeapon && !_itemService.RifleBaseItemTypes.Contains(baseItemType))
                    return MediumRangePenalty;
                else
                    return ShortRangePenalty;
            }
            // Short range (20.0 - 30.0)
            else if (isValidWeapon && range > ShortRange && !_itemService.RifleBaseItemTypes.Contains(baseItemType))
            {
                return ShortRangePenalty;
            }

            return 0;
        }

        private static int CalculateDualWieldAndStyleModifiers(uint attacker, uint weapon)
        {
            var percentageModifier = 0;
            var offHand = GetItemInSlot(InventorySlotType.LeftHand, attacker);
            var offHandType = GetBaseItemType(offHand);
            var weaponType = GetBaseItemType(weapon);

            if (!GetIsObjectValid(weapon)) 
                return percentageModifier;

            var bDoubleWeapon = _itemService.TwinBladeBaseItemTypes.Contains(weaponType) ||
                               _itemService.SaberstaffBaseItemTypes.Contains(weaponType);
            var hasImprovedTwoWeaponFighting = GetHasFeat(FeatType.ImprovedTwoWeaponFighting);
            var isShieldEquipped = GetIsObjectValid(offHand) && _itemService.ShieldBaseItemTypes.Contains(offHandType);
            var isDualKatarsEquipped = _itemService.KatarBaseItemTypes.Contains(weaponType) &&
                                      GetIsObjectValid(offHand) && _itemService.KatarBaseItemTypes.Contains(offHandType);

            // Apply dual wield penalty
            if (bDoubleWeapon || !isShieldEquipped || !isDualKatarsEquipped)
            {
                if (!hasImprovedTwoWeaponFighting || weapon == offHand)
                    percentageModifier += TwoWeaponPenalty;
            }

            // Staff Flurry penalty
            if (_itemService.StaffBaseItemTypes.Contains(weaponType) &&
                GetHasFeat(FeatType.FlurryStyle, attacker) &&
                !GetHasFeat(FeatType.FlurryMastery, attacker))
            {
                percentageModifier += FlurryStylePenalty;
                _logger.Write<AttackLogGroup>($"Applying Flurry Style I penalty: {FlurryStylePenalty}%");
            }

            // Duelist bonus
            if (GetHasFeat(FeatType.Duelist, attacker) &&
                (_itemService.OneHandedMeleeItemTypes.Contains(weaponType) ||
                 _itemService.ThrowingWeaponBaseItemTypes.Contains(weaponType)))
            {
                var isDuelistValid = !GetIsObjectValid(offHand) || _itemService.ShieldBaseItemTypes.Contains(offHandType);
                if (isDuelistValid)
                {
                    percentageModifier += DuelistBonus;
                    _logger.Write<AttackLogGroup>($"Applying Duelist bonus: +{DuelistBonus}%");
                }
            }

            return percentageModifier;
        }

        private static bool CheckDeflection(uint attackType, bool isHit, uint attacker, uint defender)
        {
            var hasDeflected = GetLocalInt(defender, "RESOLVE_ATTACK_ROLL_DEFLECT_BLASTER");

            if (attackType != (uint)AttackType.Ranged || !isHit || hasDeflected != 0)
                return false;

            var defenderWeapon = GetItemInSlot(InventorySlotType.RightHand, defender);
            var defenderOffhand = GetItemInSlot(InventorySlotType.LeftHand, defender);
            var saberBlock = GetIsObjectValid(defenderWeapon) && 
                             _itemService.LightsaberBaseItemTypes.Contains(GetBaseItemType(defenderWeapon));
            var shieldBlock = GetIsObjectValid(defenderOffhand) &&
                              GetHasFeat(FeatType.Bulwark, defender) &&
                             _itemService.ShieldBaseItemTypes.Contains(GetBaseItemType(defenderOffhand));

            if (!saberBlock && !shieldBlock)
                return false;

            SetLocalInt(defender, "RESOLVE_ATTACK_ROLL_DEFLECT_BLASTER", 1);

            var deflectRoll = _random.Next(1, 100);
            var deflectChance = 0;

            if (saberBlock) deflectChance += SaberDeflectChance;
            if (shieldBlock) deflectChance += ShieldDeflectChance;

            var deflected = deflectRoll <= deflectChance;

            var feedbackString = deflected ? "*success*" : "*failure*";
            var attackerName = ColorToken.GetNameColor(attacker);
            var defenderName = ColorToken.GetNameColor(defender);
            feedbackString = ColorToken.Combat($"{defenderName} attempts to deflect {attackerName}'s ranged attack: {feedbackString}");

            SendMessageToPC(attacker, feedbackString);
            SendMessageToPC(defender, feedbackString);
            _logger.Write<AttackLogGroup>($"Deflect roll: {deflectRoll}, Hit: {!deflected}");

            return deflected;
        }

        private static AbilityType GetWeaponStyleAbilityType(uint weapon, uint attacker)
        {
            if (!GetIsPC(attacker))
                return AbilityType.Invalid;

            if (!GetIsObjectValid(weapon))
                return AbilityType.Invalid;

            var baseItemType = GetBaseItemType(weapon);
            var playerId = GetObjectUUID(attacker);
            if (_itemService.LightsaberBaseItemTypes.Contains(baseItemType))
            {
                if (_abilityService.IsAbilityToggled(playerId, AbilityToggleType.StrongStyleLightsaber))
                    return AbilityType.Perception;
            }
            else if (_itemService.SaberstaffBaseItemTypes.Contains(baseItemType))
            {
                if (_abilityService.IsAbilityToggled(playerId, AbilityToggleType.StrongStyleSaberstaff))
                    return AbilityType.Perception;
            }
            else if (_itemService.StaffBaseItemTypes.Contains(baseItemType))
            {
                if (GetHasFeat(FeatType.FlurryStyle, attacker))
                    return AbilityType.Agility;
            }

            return AbilityType.Invalid;
        }
    }
}