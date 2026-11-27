using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Player = SWLOR.Game.Server.Entity.Player;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using EquipmentSlot = NWN.Native.API.EquipmentSlot;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The Attack, Accuracy, Defense and Evasion ratings and the deflection and Guard chances, scripted and native: part 5 of
    /// the stat split (<c>Readmes/MaintainabilityRefactorStatSplit.md</c>), moved from <c>StatService</c> unchanged except
    /// for how it reaches its dependencies. The stat read, the NPC stat skin reads and the pools it read on its own instance are
    /// <see cref="IStatService"/> calls, and the Combat services it reached through <c>Lazy&lt;&gt;</c> are plain parameters.
    /// Static callers reach it through <c>GameServices.CombatRating</c>. Calls into <c>Skill</c>, the combat queries and the
    /// damage modifiers sit behind one-line <c>protected virtual</c> methods whose bodies are the original calls.
    /// </summary>
    public class CombatRatingService : ICombatRatingService
    {
        public const int DefaultMeleeDeflectionChanceCap = 50;
        public const int DefaultRangedDeflectionChanceCap = 50;
        public const int MaximumDeflectionChanceCap = 100;
        public const int MaximumShieldDeflectionChance = 75;
        public const int MaximumGuardChance = 100;

        private readonly IDBService _db;
        private readonly ISkillService _skill;
        private readonly IItemService _item;
        private readonly ITwinPistolEquipmentService _twinPistolEquipment;
        private readonly ICombatQueriesService _queries;
        private readonly ICombatDamageModifiersService _damageModifiers;
        private readonly IStatService _stat;

        public CombatRatingService(
            IDBService db,
            ISkillService skill,
            IItemService item,
            ITwinPistolEquipmentService twinPistolEquipment,
            ICombatQueriesService queries,
            ICombatDamageModifiersService damageModifiers,
            IStatService stat)
        {
            _db = db;
            _skill = skill;
            _item = item;
            _twinPistolEquipment = twinPistolEquipment;
            _queries = queries;
            _damageModifiers = damageModifiers;
            _stat = stat;
        }

        private int CalculateEffectAttack(uint creature, int attack)
        {
            return attack + _stat.GetStatAdjustment(creature, StatType.Attack);
        }

        /// <summary>
        /// Calculates the attack for a given creature.
        /// </summary>
        /// <param name="creature">The creature to calculate.</param>
        /// <param name="abilityType">The type of ability to use.</param>
        /// <param name="skillType">The type of skill to use.</param>
        /// <param name="attackBonusOverride">Overrides the attack bonus granted by equipment. Usually only used by Space combat.</param>
        /// <returns>The total Attack value of a creature.</returns>
        public int GetAttack(uint creature, AbilityType abilityType, SkillType skillType, int attackBonusOverride = 0)
        {
            if (attackBonusOverride < 0)
                attackBonusOverride = 0;

            var attackBonus = 0 + attackBonusOverride;
            var skillLevel = 0;
            var stat = GetAbilityScore(creature, abilityType);

            if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

                if (skillType != SkillType.Invalid)
                    skillLevel = dbPlayer.Skills[skillType].Rank;

                if (attackBonusOverride <= 0)
                {
                    if (skillType == SkillType.Force)
                        attackBonus += dbPlayer.ForceAttack;
                    else
                        attackBonus += dbPlayer.Attack;
                }
            }
            else
            {
                // If a skill value is assigned for this item type, use it.
                // Otherwise fallback to the NPC's level.
                var npcStats = _stat.GetNPCStats(creature);

                skillLevel = npcStats.Skills.ContainsKey(skillType)
                    ? npcStats.Skills[skillType]
                    : npcStats.Level;

                if (attackBonusOverride <= 0)
                {
                    if (skillType == SkillType.Force)
                        attackBonus += npcStats.ForceAttack;
                    else
                        attackBonus += npcStats.Attack;
                }
            }

            attackBonus = CalculateEffectAttack(creature, attackBonus);

            var attack = GetAttack(skillLevel, stat, attackBonus);
            return ApplyPostAttackStatusModifiers(creature, attack, skillType);
        }

        public int GetAttackNative(CNWSCreature creature, BaseItem itemType, AbilityType statOverride = AbilityType.Invalid,
            bool useForceAttack = false, SkillType skillOverride = SkillType.Invalid)
        {
            var attackBonus = 0;
            var skillLevel = 0;
            var statType = statOverride != AbilityType.Invalid
                ? statOverride
                : GetWeaponDamageAbilityType(creature.m_idSelf, itemType);
            var stat = GetStatValueNative(creature, statType);
            var skillType = skillOverride != SkillType.Invalid ? skillOverride :
                _twinPistolEquipment.IsActivePair(creature.m_idSelf)
                    ? _twinPistolEquipment.ResolveSkill(itemType,
                        GetBaseItemType(GetItemInSlot(InventorySlot.RightHand, creature.m_idSelf)),
                        GetBaseItemType(GetItemInSlot(InventorySlot.LeftHand, creature.m_idSelf)))
                    : GetSkillTypeByBaseItem(itemType);

            // Force-typed attacks (e.g. Imbuement Stance retyping a weapon swing to Force) use the
            // wearer's Force Attack in place of physical Attack so the attack side matches the Force
            // Defense the hit is mitigated against. The weapon's own skill rank still governs the roll.
            var usesForceAttack = useForceAttack || skillType == SkillType.Force;

            if (creature.m_bPlayerCharacter == 1 && !GetIsDMPossessed(creature.m_idSelf))
            {
                var playerId = creature.m_pUUID.GetOrAssignRandom().ToString();
                var dbPlayer = _db.Get<Player>(playerId);

                if (dbPlayer != null)
                {
                    if(skillType != SkillType.Invalid)
                        skillLevel = dbPlayer.Skills[skillType].Rank;

                    if (usesForceAttack)
                        attackBonus += dbPlayer.ForceAttack;
                    else
                        attackBonus += dbPlayer.Attack;
                }
            }
            else
            {
                // If a skill value is assigned for this item type, use it.
                // Otherwise fallback to the NPC's level.
                var npcStats = _stat.GetNPCStatsNative(creature);

                skillLevel = npcStats.Skills.ContainsKey(skillType)
                    ? npcStats.Skills[skillType]
                    : npcStats.Level;

                if (usesForceAttack)
                    attackBonus += npcStats.ForceAttack;
                else
                    attackBonus += npcStats.Attack;
            }

            attackBonus = CalculateEffectAttack(creature.m_idSelf, attackBonus);

            var attack = GetAttack(skillLevel, stat, attackBonus);
            return ApplyPostAttackStatusModifiers(creature.m_idSelf, attack, usesForceAttack ? SkillType.Force : skillType);
        }

        /// <summary>
        /// Retrieves the raw attack based on the level, stat, and any bonuses.
        /// </summary>
        /// <param name="level">The level (NPC or skill)</param>
        /// <param name="stat">The raw stat points</param>
        /// <param name="bonus">The amount of bonus attack or force attack</param>
        /// <returns></returns>
        public int GetAttack(int level, int stat, int bonus) => StatRules.GetAttack(level, stat, bonus);

        /// <summary>
        /// Retrieves the defense value used by the attack-vs-defense damage roll.
        /// Physical and Force equipment bonuses live here; elemental/status mitigation lives in resistance.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="type">The type of damage to retrieve.</param>
        /// <param name="abilityType"></param>
        /// <param name="defenseBonusOverride">Overrides the defense bonus granted by equipment. Usually only used for Space combat.</param>
        /// <returns>The defense value toward a given damage type.</returns>
        public int GetDefense(uint creature, CombatDamageType type, AbilityType abilityType, int defenseBonusOverride = 0)
        {
            if (defenseBonusOverride < 0)
                defenseBonusOverride = 0;

            var defenseType = type.GetDefenseDamageType();
            var defenderStat = GetAbilityScore(creature, abilityType);
            int skillLevel;
            var defenseBonus = 0;
            var equipmentDefense = defenseBonusOverride;

            if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

                skillLevel = dbPlayer.Skills[SkillType.Armor].Rank;
                if (defenseBonusOverride <= 0 &&
                    dbPlayer.Defenses != null &&
                    dbPlayer.Defenses.TryGetValue(defenseType, out var playerDefense))
                {
                    equipmentDefense += playerDefense;
                }
            }
            else
            {
                var npcStats = _stat.GetNPCStats(creature);
                skillLevel = npcStats.Level;
                if (defenseBonusOverride <= 0 &&
                    npcStats.Defenses.TryGetValue(defenseType, out var npcDefense))
                {
                    equipmentDefense += npcDefense;
                }
            }

            defenseBonus = CalculateEffectDefense(creature, defenseBonus, defenseType);
            defenseBonus += equipmentDefense;
            var defense = CalculateDefense(defenderStat, skillLevel, defenseBonus);
            return ApplyPostDefenseStatusModifiers(creature, defenseType, defense);
        }

        public int CalculateDefense(int defenderStat, int skillLevel, int defenseBonus) =>
            StatRules.CalculateDefense(defenderStat, skillLevel, defenseBonus);

        /// <summary>
        /// Retrieves the native stat value of a given type on a particular creature.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <param name="statType">The type of stat to check</param>
        /// <returns>The stat value of a creature based on the ability type</returns>
        public int GetStatValueNative(CNWSCreature creature, AbilityType statType)
        {
            var stat = 0;
            switch (statType)
            {
                case AbilityType.Might:
                    stat = creature.m_pStats.GetSTRStat();
                    break;
                case AbilityType.Perception:
                    stat = creature.m_pStats.GetDEXStat();
                    break;
                case AbilityType.Vitality:
                    stat = creature.m_pStats.GetCONStat();
                    break;
                case AbilityType.Willpower:
                    stat = creature.m_pStats.GetWISStat();
                    break;
                case AbilityType.Agility:
                    stat = creature.m_pStats.GetINTStat();
                    break;
                case AbilityType.Social:
                    stat = creature.m_pStats.GetCHAStat();
                    break;
                default:
                    stat = 0;
                    break;
            }

            // Check for negative modifiers.  A modifier of -2 is represented as 254.
            if (stat > 128) stat -= 256;

            return stat;
        }

        /// <summary>
        /// Retrieves the defense value used by the attack-vs-defense damage roll.
        /// This is specifically for use with Native code and should not be referenced outside of there.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="type">The type of damage to retrieve.</param>
        /// <param name="abilityType"></param>
        /// <returns>The defense value toward a given damage type.</returns>
        public int GetDefenseNative(CNWSCreature creature, CombatDamageType type, AbilityType abilityType)
        {
            var defenseType = type.GetDefenseDamageType();
            var defenderStat = GetStatValueNative(creature, abilityType);
            var skillLevel = 0;
            var defenseBonus = 0;
            var equipmentDefense = 0;

            if (creature.m_bPlayerCharacter == 1 && !GetIsDMPossessed(creature.m_idSelf))
            {
                var playerId = creature.m_pUUID.GetOrAssignRandom().ToString();
                var dbPlayer = _db.Get<Player>(playerId);

                if (dbPlayer != null)
                {
                    skillLevel = dbPlayer.Skills[SkillType.Armor].Rank;
                    if (dbPlayer.Defenses != null &&
                        dbPlayer.Defenses.TryGetValue(defenseType, out var playerDefense))
                    {
                        equipmentDefense += playerDefense;
                    }
                }
            }
            else
            {
                var npcStats = _stat.GetNPCStatsNative(creature);
                skillLevel = npcStats.Level;
                if (npcStats.Defenses.TryGetValue(defenseType, out var npcDefense))
                {
                    equipmentDefense += npcDefense;
                }
            }

            defenseBonus = CalculateEffectDefense(creature.m_idSelf, defenseBonus, defenseType);
            defenseBonus += equipmentDefense;
            var defense = CalculateDefense(defenderStat, skillLevel, defenseBonus);
            return ApplyPostDefenseStatusModifiers(creature.m_idSelf, defenseType, defense);
        }

        private int ApplyPostAttackStatusModifiers(uint creature, int attack, SkillType skillType)
        {
            var adjustment = GetAttackPercentAdjustment(creature, skillType);

            adjustment += GetHighFPAndStaminaAttackAdjustment(creature);
            adjustment += GetNearbyStatusTargetAttackAdjustment(creature);
            adjustment += GetLowHPAttackAdjustment(creature);
            adjustment += GetLowFPAttackAdjustment(creature);
            return Math.Max(1, StatRules.ApplyPercentAdjustment(attack, adjustment));
        }

        public int GetAttackPercentAdjustment(uint creature, SkillType skillType)
        {
            return _stat.GetStatAdjustment(creature, StatType.AttackPercentAdjustment) +
                   (skillType == SkillType.Force
                       ? _stat.GetStatAdjustment(creature, StatType.ForceAttackPercentAdjustment)
                       : 0);
        }

        private int GetHighFPAndStaminaAttackAdjustment(uint creature)
        {
            var threshold = _stat.GetStatAdjustment(creature, StatType.HighFPAndStaminaAttackThresholdPercent);
            var adjustment = _stat.GetStatAdjustment(creature, StatType.HighFPAndStaminaAttackPercentAdjustment);

            if (threshold <= 0 || adjustment == 0)
                return 0;

            var currentFP = _stat.GetCurrentFP(creature);
            var maxFP = _stat.GetMaxFP(creature);
            var currentStamina = _stat.GetCurrentStamina(creature);
            var maxStamina = _stat.GetMaxStamina(creature);

            if (maxFP <= 0 || maxStamina <= 0)
                return 0;

            return currentFP >= maxFP * (threshold / 100f) &&
                   currentStamina >= maxStamina * (threshold / 100f)
                ? adjustment
                : 0;
        }

        /// <summary>
        /// Retrieves the accuracy rating of a creature.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="weapon">The weapon being used.</param>
        /// <param name="statOverride">The stat override used to calculate accuracy. This stat will be used instead of whatever stat is defined for the weapon type.</param>
        /// <param name="skillOverride">The skill override used to calculate accuracy. This skill will be used instead of whatever skill is defined for the weapon type.</param>
        /// <param name="skillLevelOverride">Overrides the skill rank or NPC level used in the accuracy calculation.</param>
        /// <param name="ignoreWeaponAccuracyStatOverride">When true, Accuracy Stat item properties do not replace <paramref name="statOverride"/>.</param>
        /// <returns>The accuracy rating for a creature using a specific weapon.</returns>
        public int GetAccuracy(
            uint creature,
            uint weapon,
            AbilityType statOverride,
            SkillType skillOverride,
            int skillLevelOverride = -1,
            bool ignoreWeaponAccuracyStatOverride = false)
        {
            var accuracyBonus = 0;

            for (var ip = GetFirstItemProperty(weapon); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(weapon))
            {
                var type = GetItemPropertyType(ip);
                if (type != ItemPropertyType.Accuracy &&
                    type != ItemPropertyType.AccuracyStat)
                    continue;

                var value = type == ItemPropertyType.AccuracyStat
                    ? GetItemPropertySubType(ip)
                    : GetItemPropertyCostTableValue(ip);
                (statOverride, accuracyBonus) = ApplyAccuracyItemProperty(
                    statOverride,
                    accuracyBonus,
                    type,
                    value,
                    ignoreWeaponAccuracyStatOverride);
            }


            var baseItemType = GetBaseItemType(weapon);
            var statType = statOverride == AbilityType.Invalid ?
                GetWeaponAccuracyAbilityType(creature, baseItemType) :
                statOverride;
            var stat = statType == AbilityType.Invalid ? 0 : GetAbilityScore(creature, statType);
            var skillType = skillOverride == SkillType.Invalid ? GetWeaponSkill(creature, weapon) : skillOverride;
            var skillLevel = 0;


            // Creature skill level / NPC level, plus Accuracy granted by non-weapon equipment.
            if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

                if (skillType != SkillType.Invalid)
                    skillLevel = dbPlayer.Skills[skillType].Rank;

                accuracyBonus += dbPlayer.Accuracy;
            }
            else
            {
                var npcStats = _stat.GetNPCStats(creature);
                skillLevel = npcStats.Level;
                accuracyBonus += npcStats.Accuracy;
            }

            if (skillLevelOverride >= 0)
                skillLevel = skillLevelOverride;

            // Accuracy increases granted by effects
            accuracyBonus = CalculateEffectAccuracy(creature, accuracyBonus);

            // Power Attack to-hit penalty
            if (GetActionMode(creature, ActionMode.PowerAttack))
                accuracyBonus -= 5;
            else if (GetActionMode(creature, ActionMode.ImprovedPowerAttack))
                accuracyBonus -= 10;

            var accuracy = GetAccuracy(skillLevel, stat, accuracyBonus);
            return ApplyPostAccuracyStatusModifiers(creature, accuracy);
        }

        private (AbilityType StatOverride, int AccuracyBonus) ApplyAccuracyItemProperty(
            AbilityType statOverride,
            int accuracyBonus,
            ItemPropertyType type,
            int value,
            bool ignoreWeaponAccuracyStatOverride)
        {
            if (type == ItemPropertyType.Accuracy)
                return (statOverride, accuracyBonus + value);

            if (type == ItemPropertyType.AccuracyStat && !ignoreWeaponAccuracyStatOverride)
                return ((AbilityType)value, accuracyBonus);

            return (statOverride, accuracyBonus);
        }

        /// <summary>
        /// Retrieves the accuracy rating of a creature from a native context.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="weapon">The weapon being used.</param>
        /// <returns>The accuracy rating for a creature using a specific weapon.</returns>
        public int GetAccuracyNative(CNWSCreature creature, CNWSItem weapon, SkillType skillOverride = SkillType.Invalid,
            AbilityType accuracyAbilityOverride = AbilityType.Invalid)
        {
            var accuracyBonus = 0;
            var statOverride = AbilityType.Invalid;

            if (weapon != null)
            {
                foreach (var ip in weapon.m_lstPassiveProperties)
                {
                    // Accuracy found on the weapon.
                    if (ip.m_nPropertyName == (ushort)ItemPropertyType.Accuracy)
                    {
                        accuracyBonus += ip.m_nCostTableValue;
                    }
                    // Accuracy Stat Override - Always "wins" even if another override was passed in.
                    else if (ip.m_nPropertyName == (ushort)ItemPropertyType.AccuracyStat)
                    {
                        statOverride = (AbilityType)ip.m_nSubType;
                    }
                }
            }

            var baseItemType = weapon == null ? BaseItem.Invalid : (BaseItem)weapon.m_nBaseItem;
            var statType = accuracyAbilityOverride != AbilityType.Invalid
                ? accuracyAbilityOverride
                : statOverride == AbilityType.Invalid
                    ? GetWeaponAccuracyAbilityType(creature.m_idSelf, baseItemType)
                    : statOverride;
            var skillType = skillOverride != SkillType.Invalid ? skillOverride :
                weapon == null ? SkillType.Invalid : GetWeaponSkill(creature.m_idSelf, weapon.m_idSelf);
            var stat = GetStatValueNative(creature, statType);
            var skillLevel = 0;


            // Creature skill level / NPC level, plus Accuracy granted by non-weapon equipment.
            if (creature.m_bPlayerCharacter == 1 && !GetIsDMPossessed(creature.m_idSelf))
            {
                var playerId = creature.m_pUUID.GetOrAssignRandom().ToString();
                var dbPlayer = _db.Get<Player>(playerId);

                if (dbPlayer != null)
                {
                    if (skillType != SkillType.Invalid)
                        skillLevel = dbPlayer.Skills[skillType].Rank;

                    accuracyBonus += dbPlayer.Accuracy;
                }
            }
            else
            {
                var npcStats = _stat.GetNPCStatsNative(creature);
                skillLevel = npcStats.Level;
                accuracyBonus += npcStats.Accuracy;
            }

            accuracyBonus = CalculateEffectAccuracyNative(creature, accuracyBonus);

            var accuracy = GetAccuracy(skillLevel, stat, accuracyBonus);
            return ApplyPostAccuracyStatusModifiers(creature.m_idSelf, accuracy);
        }

        /// <summary>
        /// Gets the calculated accuracy for a given level, stat, and bonus.
        /// </summary>
        /// <param name="level">The level (skill/NPC)</param>
        /// <param name="stat">The raw accuracy stat amount</param>
        /// <param name="bonus">The amount of bonus accuracy.</param>
        /// <returns>The calculated accuracy result.</returns>
        public int GetAccuracy(int level, int stat, int bonus) => StatRules.GetAccuracy(level, stat, bonus);

        private int CalculateEffectAccuracy(uint creature, int accuracy)
        {
            accuracy += _stat.GetStatAdjustment(creature, StatType.Accuracy);

            Log.Write(LogGroup.Attack, $"Effect Accuracy: {accuracy}");

            return accuracy;
        }

        private int CalculateEffectAccuracyNative(CNWSCreature creature, int accuracy)
        {
            accuracy += _stat.GetStatAdjustment(creature.m_idSelf, StatType.Accuracy);

            Log.Write(LogGroup.Attack, $"Native Effect Accuracy: {accuracy}");

            return accuracy;
        }

        private int CalculateEffectEvasion(uint creature)
        {
            return _stat.GetStatAdjustment(creature, StatType.Evasion);
        }

        /// <summary>
        /// Retrieves a creature's evasion.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="skillOverride">The skill override to use instead of Armor for the purposes of calculating evasion.</param>
        /// <param name="incomingSkillType">The skill type of the incoming attack for conditional evasion modifiers.</param>
        /// <returns>The evasion rating of a creature.</returns>
        public int GetEvasion(uint creature, SkillType skillOverride, SkillType incomingSkillType = SkillType.Invalid)
        {
            var stat = GetAbilityScore(creature, AbilityType.Agility);
            int skillLevel;
            int evasionBonus;
            var hasNpcStatBudget = false;

            // Base NWN applies an AC bonus based on the DEX stat. The Perception stat is based upon this.
            // Perception should not increase AC in SWLOR, so this is subtracted from the AC.
            var dexOffset = GetAbilityModifier(AbilityType.Perception, creature);
            var ac = GetAC(creature) - dexOffset - 10; // Offset by natural 10 AC granted to all characters.
            var skillType = skillOverride == SkillType.Invalid ? SkillType.Armor : skillOverride;

            Log.Write(LogGroup.Attack, $"Evasion regular AC = {ac}");

            if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

                skillLevel = dbPlayer.Skills[skillType].Rank;
                evasionBonus = dbPlayer.Evasion;
            }
            else
            {
                var npcStats = _stat.GetNPCStats(creature);
                skillLevel = npcStats.Level;
                evasionBonus = npcStats.Evasion;
                hasNpcStatBudget = npcStats.Level > 0;
            }

            evasionBonus += CalculateEffectEvasion(creature);

            Log.Write(LogGroup.Attack, $"Effect Evasion: {evasionBonus}");

            var evasion = GetEvasion(skillLevel, stat, evasionBonus, ac, hasNpcStatBudget);
            return ApplyPostEvasionStatusModifiers(creature, evasion, incomingSkillType);
        }

        /// <summary>
        /// Retrieves a creature's evasion rating from a native context.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>The evasion rating of a creature.</returns>
        public int GetEvasionNative(CNWSCreature creature, SkillType incomingSkillType = SkillType.Invalid)
        {
            var stat = GetStatValueNative(creature, AbilityType.Agility);
            var skillLevel = 0;
            var evasionBonus = 0;
            var hasNpcStatBudget = false;

            // Note: The DEX offset is unnecessary for the native call.
            var ac = creature.m_pStats.m_nACArmorBase +
                     creature.m_pStats.m_nACNaturalBase +
                     creature.m_pStats.m_nACArmorMod -
                     creature.m_pStats.m_nACArmorNeg +
                     creature.m_pStats.m_nACDeflectionMod -
                     creature.m_pStats.m_nACDeflectionNeg +
                     creature.m_pStats.m_nACDodgeMod -
                     creature.m_pStats.m_nACDodgeNeg +
                     creature.m_pStats.m_nACNaturalMod -
                     creature.m_pStats.m_nACNaturalNeg +
                     creature.m_pStats.m_nACShieldMod -
                     creature.m_pStats.m_nACShieldNeg;

            Log.Write(LogGroup.Attack, $"Native Evasion AC = {ac}");

            if (creature.m_bPlayerCharacter == 1 && !GetIsDMPossessed(creature.m_idSelf))
            {
                var playerId = creature.m_pUUID.GetOrAssignRandom().ToString();
                var dbPlayer = _db.Get<Player>(playerId);

                if (dbPlayer != null)
                {
                    skillLevel = dbPlayer.Skills[SkillType.Armor].Rank;
                    evasionBonus = dbPlayer.Evasion;
                }
            }
            else
            {
                var npcStats = _stat.GetNPCStatsNative(creature);
                skillLevel = npcStats.Level;
                evasionBonus = npcStats.Evasion;
                hasNpcStatBudget = npcStats.Level > 0;
            }

            evasionBonus += CalculateEffectEvasion(creature.m_idSelf);

            var evasion = GetEvasion(skillLevel, stat, evasionBonus, ac, hasNpcStatBudget);
            return ApplyPostEvasionStatusModifiers(creature.m_idSelf, evasion, incomingSkillType);
        }

        public int GetMeleeDeflectionChanceNative(CNWSCreature creature)
        {
            if (!HasWeaponEquippedForWeaponDeflectionNative(creature) || HasShieldEquippedNative(creature))
                return 0;

            var chance = _stat.GetStatAdjustment(creature.m_idSelf, StatType.MeleeDeflection);
            return Math.Clamp(chance, 0, GetMeleeDeflectionChanceCap(creature.m_idSelf));
        }

        public int GetMeleeDeflectionChance(uint creature)
        {
            if (!HasWeaponEquippedForWeaponDeflection(creature) || HasShieldEquipped(creature))
                return 0;

            var chance = _stat.GetStatAdjustment(creature, StatType.MeleeDeflection);
            return Math.Clamp(chance, 0, GetMeleeDeflectionChanceCap(creature));
        }

        public int GetRangedDeflectionChanceNative(CNWSCreature creature)
        {
            if (!HasWeaponEquippedForWeaponDeflectionNative(creature) || HasShieldEquippedNative(creature))
                return 0;

            var chance = _stat.GetStatAdjustment(creature.m_idSelf, StatType.RangedDeflection);
            return Math.Clamp(chance, 0, GetRangedDeflectionChanceCap(creature.m_idSelf));
        }

        public int GetRangedDeflectionChance(uint creature)
        {
            if (!HasWeaponEquippedForWeaponDeflection(creature) || HasShieldEquipped(creature))
                return 0;

            var chance = _stat.GetStatAdjustment(creature, StatType.RangedDeflection);
            return Math.Clamp(chance, 0, GetRangedDeflectionChanceCap(creature));
        }

        public int GetShieldDeflectionChanceNative(CNWSCreature creature)
        {
            var shield = GetEquippedShieldNative(creature);
            if (shield == null)
                return 0;

            var chance = GetShieldDeflectionItemPropertyBonusNative(shield) +
                         _stat.GetStatAdjustment(creature.m_idSelf, StatType.ShieldDeflection);
            return Math.Clamp(chance, 0, MaximumShieldDeflectionChance);
        }

        public int GetShieldDeflectionChance(uint creature)
        {
            var shield = GetEquippedShield(creature);
            if (!GetIsObjectValid(shield))
                return 0;

            var chance = GetShieldDeflectionItemPropertyBonus(shield) +
                         _stat.GetStatAdjustment(creature, StatType.ShieldDeflection);
            return Math.Clamp(chance, 0, MaximumShieldDeflectionChance);
        }

        public int GetGuardChance(uint creature)
        {
            return Math.Clamp(_stat.GetStatAdjustment(creature, StatType.Guard), 0, MaximumGuardChance);
        }

        private int GetMeleeDeflectionChanceCap(uint creature)
        {
            return GetDeflectionChanceCap(
                creature,
                StatType.MeleeDeflectionChanceCap,
                DefaultMeleeDeflectionChanceCap);
        }

        private int GetRangedDeflectionChanceCap(uint creature)
        {
            return GetDeflectionChanceCap(
                creature,
                StatType.RangedDeflectionChanceCap,
                DefaultRangedDeflectionChanceCap);
        }

        private int GetDeflectionChanceCap(uint creature, StatType capStat, int defaultCap)
        {
            var cap = defaultCap + _stat.GetStatAdjustment(creature, capStat);
            return Math.Clamp(cap, defaultCap, MaximumDeflectionChanceCap);
        }

        private bool HasWeaponEquippedForWeaponDeflectionNative(CNWSCreature creature)
        {
            return HasWeaponEquippedForWeaponDeflectionNative(creature, EquipmentSlot.RightHand) ||
                   HasWeaponEquippedForWeaponDeflectionNative(creature, EquipmentSlot.LeftHand);
        }

        private bool HasWeaponEquippedForWeaponDeflectionNative(CNWSCreature creature, EquipmentSlot slot)
        {
            var item = creature.m_pInventory.GetItemInSlot((uint)slot);
            return item != null &&
                   GetWeaponSkill(creature.m_idSelf, item.m_idSelf) != SkillType.Invalid;
        }

        private bool HasWeaponEquippedForWeaponDeflection(uint creature)
        {
            return HasWeaponEquippedForWeaponDeflection(creature, InventorySlot.RightHand) ||
                   HasWeaponEquippedForWeaponDeflection(creature, InventorySlot.LeftHand);
        }

        private bool HasWeaponEquippedForWeaponDeflection(uint creature, InventorySlot slot)
        {
            var item = GetItemInSlot(slot, creature);
            return GetIsObjectValid(item) &&
                   GetWeaponSkill(creature, item) != SkillType.Invalid;
        }

        private SkillType GetMainHandSkillTypeNative(CNWSCreature creature)
        {
            var rightHandItem = creature.m_pInventory.GetItemInSlot((uint)EquipmentSlot.RightHand);
            if (rightHandItem == null)
                return SkillType.Invalid;

            return GetWeaponSkill(creature.m_idSelf, rightHandItem.m_idSelf);
        }

        private bool HasShieldEquippedNative(CNWSCreature creature)
        {
            return GetEquippedShieldNative(creature) != null;
        }

        private bool HasShieldEquipped(uint creature)
        {
            return GetIsObjectValid(GetEquippedShield(creature));
        }

        private CNWSItem GetEquippedShieldNative(CNWSCreature creature)
        {
            var leftHandItem = creature.m_pInventory.GetItemInSlot((uint)EquipmentSlot.LeftHand);
            return _item.IsBaseItemType(leftHandItem, _item.ShieldBaseItemTypes)
                ? leftHandItem
                : null;
        }

        private uint GetEquippedShield(uint creature)
        {
            var leftHandItem = GetItemInSlot(InventorySlot.LeftHand, creature);
            return _item.IsBaseItemType(leftHandItem, _item.ShieldBaseItemTypes)
                ? leftHandItem
                : OBJECT_INVALID;
        }

        private int GetShieldDeflectionItemPropertyBonusNative(CNWSItem shield)
        {
            var bonus = 0;
            for (var index = 0; index < shield.m_lstPassiveProperties.Count; index++)
            {
                var ip = shield.GetPassiveProperty(index);
                if (ip?.m_nPropertyName == (ushort)ItemPropertyType.ShieldDeflection)
                    bonus += ip.m_nCostTableValue;
            }

            return bonus;
        }

        private int GetShieldDeflectionItemPropertyBonus(uint shield)
        {
            var bonus = 0;
            for (var ip = GetFirstItemProperty(shield); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(shield))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.ShieldDeflection)
                    bonus += GetItemPropertyCostTableValue(ip);
            }

            return bonus;
        }

        private int ApplyPostAccuracyStatusModifiers(uint creature, int accuracy)
        {
            var adjustment = _stat.GetStatAdjustment(creature, StatType.AccuracyPercentAdjustment);
            return Math.Max(1, StatRules.ApplyPercentAdjustment(accuracy, adjustment));
        }

        private int ApplyPostEvasionStatusModifiers(uint creature, int evasion, SkillType incomingSkillType)
        {
            var adjustment = _stat.GetStatAdjustment(creature, StatType.EvasionPercentAdjustment);
            if (IsRangedDamageSkill(incomingSkillType))
            {
                adjustment += _stat.GetStatAdjustment(creature, StatType.RangedEvasionPercentAdjustment);
            }

            return Math.Max(1, StatRules.ApplyPercentAdjustment(evasion, adjustment));
        }

        /// <summary>
        /// Calculates defense bonuses granted by status effects, perks, and temporary stat modifiers.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="defense">The base bonus to adjust.</param>
        /// <param name="type">The damage type.</param>
        /// <returns>A modified defense bonus.</returns>
        private int CalculateEffectDefense(uint creature, int defense, CombatDamageType type)
        {
            return defense + GetDefenseAdjustment(creature, type);
        }

        private int ApplyPostDefenseStatusModifiers(uint creature, CombatDamageType type, int defense)
        {
            var adjustment = GetDefensePercentAdjustment(creature, type);
            return Math.Max(1, StatRules.ApplyPercentAdjustment(defense, adjustment));
        }

        /// <summary>
        /// Retrieves the total percentage adjustment applied to a creature's defense for a damage type.
        /// This combines the general defense adjustment with the type-specific adjustment, including
        /// the shield-only bonus when a shield is equipped.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="type">The damage type.</param>
        /// <returns>The percentage adjustment applied to defense.</returns>
        public int GetDefensePercentAdjustment(uint creature, CombatDamageType type)
        {
            return _stat.GetStatAdjustment(creature, StatType.DefensePercentAdjustment) + (type switch
            {
                CombatDamageType.Physical => _stat.GetStatAdjustment(creature, StatType.PhysicalDefensePercentAdjustment) +
                                             _stat.GetStatAdjustment(creature, StatType.PhysicalAndForceDefenseAuraPercentAdjustment) +
                                             GetShieldEquippedPhysicalDefensePercentAdjustment(creature),
                CombatDamageType.Force => _stat.GetStatAdjustment(creature, StatType.ForceDefensePercentAdjustment) +
                                          _stat.GetStatAdjustment(creature, StatType.PhysicalAndForceDefenseAuraPercentAdjustment),
                _ => 0
            });
        }

        private int GetShieldEquippedPhysicalDefensePercentAdjustment(uint creature)
        {
            return HasShieldEquipped(creature)
                ? _stat.GetStatAdjustment(creature, StatType.ShieldEquippedPhysicalDefensePercentAdjustment)
                : 0;
        }

        private int GetDefenseAdjustment(uint creature, CombatDamageType type)
        {
            return _stat.GetStatAdjustment(creature, StatType.Defense) + (type switch
            {
                CombatDamageType.Physical => _stat.GetStatAdjustment(creature, StatType.PhysicalDefense),
                CombatDamageType.Force => _stat.GetStatAdjustment(creature, StatType.ForceDefense),
                _ => 0
            });
        }

        /// <summary>
        /// Gets the evasion based on level, stat, and bonuses.
        /// </summary>
        /// <param name="level">The level (skill/NPC)</param>
        /// <param name="stat">The raw agility stat</param>
        /// <param name="bonus">The amount of bonus evasion</param>
        /// <returns></returns>
        public int GetEvasion(int level, int stat, int bonus) => StatRules.GetEvasion(level, stat, bonus);

        /// <summary>
        /// NPC stat skins carry the complete Evasion budget. Native armor must
        /// not add another budget through inherited equipment, feats or effects.
        /// Stat-based Evasion adjustments remain part of bonus for all creatures.
        /// </summary>
        public int GetEvasion(int level, int stat, int bonus, int nativeArmorClass, bool hasNpcStatBudget)
        {
            return GetEvasion(level, stat, bonus + (hasNpcStatBudget ? 0 : nativeArmorClass * 5));
        }

        // The calls that are not injected, one line each, so a test can answer them. The bodies are the original calls.

        protected virtual SkillType GetSkillTypeByBaseItem(BaseItem baseItem) => _skill.GetSkillTypeByBaseItem(baseItem);

        protected virtual SkillType GetWeaponSkill(uint creature, uint weapon) => _skill.GetWeaponSkill(creature, weapon);

        protected virtual AbilityType GetWeaponDamageAbilityType(uint creature, BaseItem weaponType) =>
            _queries.GetWeaponDamageAbilityType(creature, weaponType);

        protected virtual AbilityType GetWeaponAccuracyAbilityType(uint creature, BaseItem weaponType) =>
            _queries.GetWeaponAccuracyAbilityType(creature, weaponType);

        protected virtual int GetNearbyStatusTargetAttackAdjustment(uint creature) =>
            _damageModifiers.GetNearbyStatusTargetAttackAdjustment(creature);

        protected virtual int GetLowHPAttackAdjustment(uint creature) => _damageModifiers.GetLowHPAttackAdjustment(creature);

        protected virtual int GetLowFPAttackAdjustment(uint creature) => _damageModifiers.GetLowFPAttackAdjustment(creature);

        protected virtual bool IsRangedDamageSkill(SkillType skillType) => CombatRules.IsRangedDamageSkill(skillType);
    }
}
