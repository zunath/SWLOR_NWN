using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Legacy.Data.Entity;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Event.Module;
using SWLOR.Game.Server.Legacy.Event.SWLOR;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Legacy.ValueObject;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using PerkType = SWLOR.Game.Server.Legacy.Enumeration.PerkType;
using SkillType = SWLOR.Game.Server.Legacy.Enumeration.SkillType;

namespace SWLOR.Game.Server.Legacy.Service
{
    public static class PlayerStatService
    {
        public const float PrimaryIncrease = 0.1f;
        public const float SecondaryIncrease = 0.05f;
        public const float TertiaryIncrease = 0.025f;
        private const int MaxAttributeBonus = 35;

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleEquipItem>(msg => OnModuleEquipItem());
            MessageHub.Instance.Subscribe<OnModuleUnequipItem>(msg => OnModuleUnequipItem());
            MessageHub.Instance.Subscribe<OnSkillGained>(msg => OnSkillGained(msg.Player));
            MessageHub.Instance.Subscribe<OnSkillDecayed>(msg => OnSkillDecayed(msg.Player));
        }

        private static void OnModuleEquipItem()
        {
            NWPlayer player = GetPCItemLastEquippedBy();
            NWItem item = GetPCItemLastEquipped();

            CalculateEffectiveStats(player, item);
            ApplyStatChanges(player, null);
        }

        private static void OnModuleUnequipItem()
        {
            NWPlayer player = GetPCItemLastUnequippedBy();
            NWItem item = GetPCItemLastUnequipped();

            RemoveCachedEffectiveStats(item);
            ApplyStatChanges(player, null);
        }

        private static void OnSkillGained(NWPlayer player)
        {
            for (var itemSlot = 0; itemSlot < NumberOfInventorySlots; itemSlot++)
            {
                NWItem item = GetItemInSlot((InventorySlot)itemSlot, player);
                CalculateEffectiveStats(player, item);
            }
            ApplyStatChanges(player, null);
        }

        private static void OnSkillDecayed(NWPlayer player)
        {
            for (var itemSlot = 0; itemSlot < NumberOfInventorySlots; itemSlot++)
            {
                NWItem item = GetItemInSlot((InventorySlot)itemSlot, player);
                CalculateEffectiveStats(player, item);
            }
            ApplyStatChanges(player, null);
        }

        public static void ApplyStatChanges(NWPlayer player, NWItem ignoreItem, bool isInitialization = false)
        {
            if (!player.IsPlayer) return;
            if (!player.IsInitializedAsPlayer) return;
            if (player.GetLocalInt("IS_SHIP") == 1) return;

            // Don't fire for ammo as it reapplies bonuses we **just** removed from blasters.
            if (ignoreItem != null &&
                (ignoreItem.BaseItemType == BaseItem.Bolt ||
                 ignoreItem.BaseItemType == BaseItem.Arrow ||
                 ignoreItem.BaseItemType == BaseItem.Bullet)) return;

            var pcEntity = DataService.Player.GetByID(player.GlobalID);
            var skills = DataService.PCSkill
                .GetAllByPlayerID(player.GlobalID)
                .Where(x => x.Rank > 0).ToList();
            var itemBonuses = GetPlayerItemEffectiveStats(player, ignoreItem);

            var strBonus = 0.0f;
            var dexBonus = 0.0f;
            var conBonus = 0.0f;
            var intBonus = 0.0f;
            var wisBonus = 0.0f;
            var chaBonus = 0.0f;

            foreach (var pcSkill in skills)
            {
                var skill = DataService.Skill.GetByID(pcSkill.SkillID);
                var primary = (CustomAttribute)skill.Primary;
                var secondary = (CustomAttribute)skill.Secondary;
                var tertiary = (CustomAttribute)skill.Tertiary;

                // Primary Bonuses
                if (primary == CustomAttribute.STR) strBonus += PrimaryIncrease * pcSkill.Rank;
                else if (primary == CustomAttribute.DEX) dexBonus += PrimaryIncrease * pcSkill.Rank;
                else if (primary == CustomAttribute.CON) conBonus += PrimaryIncrease * pcSkill.Rank;
                else if (primary == CustomAttribute.INT) intBonus += PrimaryIncrease * pcSkill.Rank;
                else if (primary == CustomAttribute.WIS) wisBonus += PrimaryIncrease * pcSkill.Rank;
                else if (primary == CustomAttribute.CHA) chaBonus += PrimaryIncrease * pcSkill.Rank;

                // Secondary Bonuses
                if (secondary == CustomAttribute.STR) strBonus += SecondaryIncrease * pcSkill.Rank;
                else if (secondary == CustomAttribute.DEX) dexBonus += SecondaryIncrease * pcSkill.Rank;
                else if (secondary == CustomAttribute.CON) conBonus += SecondaryIncrease * pcSkill.Rank;
                else if (secondary == CustomAttribute.INT) intBonus += SecondaryIncrease * pcSkill.Rank;
                else if (secondary == CustomAttribute.WIS) wisBonus += SecondaryIncrease * pcSkill.Rank;
                else if (secondary == CustomAttribute.CHA) chaBonus += SecondaryIncrease * pcSkill.Rank;

                // Tertiary Bonuses
                if (tertiary == CustomAttribute.STR) strBonus += TertiaryIncrease * pcSkill.Rank;
                else if (tertiary == CustomAttribute.DEX) dexBonus += TertiaryIncrease * pcSkill.Rank;
                else if (tertiary == CustomAttribute.CON) conBonus += TertiaryIncrease * pcSkill.Rank;
                else if (tertiary == CustomAttribute.INT) intBonus += TertiaryIncrease * pcSkill.Rank;
                else if (tertiary == CustomAttribute.WIS) wisBonus += TertiaryIncrease * pcSkill.Rank;
                else if (tertiary == CustomAttribute.CHA) chaBonus += TertiaryIncrease * pcSkill.Rank;
            }

            // Check caps.
            if (strBonus > MaxAttributeBonus) strBonus = MaxAttributeBonus;
            if (dexBonus > MaxAttributeBonus) dexBonus = MaxAttributeBonus;
            if (conBonus > MaxAttributeBonus) conBonus = MaxAttributeBonus;
            if (intBonus > MaxAttributeBonus) intBonus = MaxAttributeBonus;
            if (wisBonus > MaxAttributeBonus) wisBonus = MaxAttributeBonus;
            if (chaBonus > MaxAttributeBonus) chaBonus = MaxAttributeBonus;

            // Apply item bonuses
            strBonus += itemBonuses.Strength;
            dexBonus += itemBonuses.Dexterity;
            conBonus += itemBonuses.Constitution;
            wisBonus += itemBonuses.Wisdom;
            intBonus += itemBonuses.Intelligence;
            chaBonus += itemBonuses.Charisma;

            // Check final caps
            if (strBonus > 55) strBonus = 55;
            if (dexBonus > 55) dexBonus = 55;
            if (conBonus > 55) conBonus = 55;
            if (intBonus > 55) intBonus = 55;
            if (wisBonus > 55) wisBonus = 55;
            if (chaBonus > 55) chaBonus = 55;

            // Apply attributes
            Creature.SetRawAbilityScore(player, AbilityType.Strength, (int)strBonus + pcEntity.STRBase);
            Creature.SetRawAbilityScore(player, AbilityType.Dexterity, (int)dexBonus + pcEntity.DEXBase);
            Creature.SetRawAbilityScore(player, AbilityType.Constitution, (int)conBonus + pcEntity.CONBase);
            Creature.SetRawAbilityScore(player, AbilityType.Intelligence, (int)intBonus + pcEntity.INTBase);
            Creature.SetRawAbilityScore(player, AbilityType.Wisdom, (int)wisBonus + pcEntity.WISBase);
            Creature.SetRawAbilityScore(player, AbilityType.Charisma, (int)chaBonus + pcEntity.CHABase);

            // Apply AC
            var ac = EffectiveArmorClass(player, ignoreItem, itemBonuses);
            Creature.SetBaseAC(player, ac);

            // Apply BAB
            var bab = CalculateBAB(player, ignoreItem, itemBonuses);
            Creature.SetBaseAttackBonus(player, bab);

            // Apply HP
            var hp = EffectiveMaxHitPoints(player, itemBonuses);
            for (var level = 1; level <= 5; level++)
            {
                hp--;
                Creature.SetMaxHitPointsByLevel(player, level, 1);
            }

            for (var level = 1; level <= 5; level++)
            {
                if (hp > 255) // Levels can only contain a max of 255 HP
                {
                    Creature.SetMaxHitPointsByLevel(player, level, 255);
                    hp = hp - 254;
                }
                else // Remaining value gets set to the level. (<255 hp)
                {
                    Creature.SetMaxHitPointsByLevel(player, level, hp + 1);
                    break;
                }
            }

            if (player.CurrentHP > player.MaxHP)
            {
                var amount = player.CurrentHP - player.MaxHP;
                var damage = EffectDamage(amount);
                ApplyEffectToObject(DurationType.Instant, damage, player.Object);
            }

            // Apply FP
            pcEntity.MaxFP = EffectiveMaxFP(player, itemBonuses);

            if (isInitialization)
            {
                pcEntity.CurrentFP = pcEntity.MaxFP;
            }

            DataService.SubmitDataChange(pcEntity, DatabaseActionType.Update);

            // Attempt a refresh of the character sheet UI in a second.
            DelayCommand(1.0f, () =>
            {
                Core.NWNX.Player.UpdateCharacterSheet(player);
            });
        }


        private static int CalculateAdjustedValue(int baseValue, int recommendedLevel, int skillRank, int minimumValue)
        {
            var adjustedValue = (int)CalculateAdjustedValue((float)baseValue, recommendedLevel, skillRank, minimumValue);
            if (adjustedValue < minimumValue) adjustedValue = minimumValue;
            return adjustedValue;
        }

        private static float CalculateAdjustedValue(float baseValue, int recommendedLevel, int skillRank, float minimumValue)
        {
            var delta = recommendedLevel - skillRank;
            var adjustment = 1.0f - delta * 0.1f;
            if (adjustment <= 0.1f) adjustment = 0.1f;
            else if (adjustment > 1.0f) adjustment = 1.0f;

            var adjustedValue = (float)Math.Round(baseValue * adjustment);
            if (adjustedValue < minimumValue) adjustedValue = minimumValue;
            return adjustedValue;
        }

        private static int EffectiveMaxHitPoints(NWPlayer player, EffectiveItemStats stats)
        {
            var hp = 25 + player.ConstitutionModifier * 5;
            var effectPercentBonus = CustomEffectService.CalculateEffectHPBonusPercent(player);

            hp += PerkService.GetCreaturePerkLevel(player, PerkType.Health) * 5;
            hp += stats.HP;
            hp = hp + (int)(hp * effectPercentBonus);

            if (hp > 1275) hp = 1275;
            if (hp < 20) hp = 20;

            return hp;
        }

        private static int EffectiveMaxFP(NWPlayer player, EffectiveItemStats stats)
        {
            var fp = 20;
            fp += (player.IntelligenceModifier + player.WisdomModifier + player.CharismaModifier) * 5;
            fp += PerkService.GetCreaturePerkLevel(player, PerkType.FP) * 5;
            fp += stats.FP;

            if (fp < 0) fp = 0;

            return fp;
        }

        private static int EffectiveArmorClass(NWPlayer player, NWItem ignoreItem, EffectiveItemStats stats)
        {
            var baseAC = stats.AC + CustomEffectService.CalculateEffectAC(player);

            // Calculate AC bonus granted by skill ranks.
            // Only chest armor is checked for this bonus.

            if (ignoreItem != player.Chest)
            {
                var armorType = player.Chest.CustomItemType;
                var skillRank = 0;
                switch (armorType)
                {
                    case CustomItemType.LightArmor:
                        skillRank = SkillService.GetPCSkillRank(player, SkillType.LightArmor);
                        break;
                    case CustomItemType.HeavyArmor:
                        skillRank = SkillService.GetPCSkillRank(player, SkillType.HeavyArmor);
                        break;
                    case CustomItemType.ForceArmor:
                        skillRank = SkillService.GetPCSkillRank(player, SkillType.ForceArmor);
                        break;
                }

                // +1 AC per 10 skill ranks, while wearing the appropriate armor.
                var skillACBonus = skillRank / 10;
                baseAC += skillACBonus;
            }

            var totalAC = GetAC(player) - baseAC;

            // Shield Oath and Precision Targeting affect a percentage of the TOTAL armor class on a creature.
            var stance = CustomEffectService.GetCurrentStanceType(player);
            if (stance == CustomEffectType.PrecisionTargeting)
            {
                var penalty = (int)(totalAC * 0.3f);
                baseAC -= penalty;
            }

            if (baseAC < 0) baseAC = 0;

            return baseAC;
        }

        public static void CalculateEffectiveStats(NWPlayer player, NWItem item)
        {
            if (item == null || !item.IsValid || !player.IsPlayer || player.IsDMPossessed || player.IsDM || !player.IsInitializedAsPlayer) return;

            // Calculating effective stats can be expensive, so we cache it on the item.
            SkillType skill; 
            
            if(item.BaseItemType == BaseItem.Amulet || item.BaseItemType == BaseItem.Ring)
            {
                var forceArmor = SkillService.GetPCSkill(player, (int)SkillType.ForceArmor);
                var lightArmor = SkillService.GetPCSkill(player, (int)SkillType.LightArmor);
                var heavyArmor = SkillService.GetPCSkill(player, (int)SkillType.HeavyArmor);
                var highest = forceArmor.Rank;
                skill = SkillType.ForceArmor;

                if (lightArmor.Rank > highest)
                {
                    highest = lightArmor.Rank;
                    skill = SkillType.LightArmor;
                }
                if (heavyArmor.Rank > highest)
                {
                    skill = SkillType.HeavyArmor;
                }
            }
            else
            {
                skill = ItemService.GetSkillTypeForItem(item);
            }
                
            var rank = DataService.PCSkill.GetByPlayerIDAndSkillID(player.GlobalID, (int)skill).Rank;
            using (new Profiler("PlayerStatService::ApplyStatChanges::GetPlayerItemEffectiveStats::ItemLoop::CalculateEffectiveStats"))
            {
                // Only scale cooldown recovery if it's a bonus. Penalties remain regardless of skill level difference.
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_COOLDOWN_RECOVERY", item.CooldownRecovery > 0 
                    ? CalculateAdjustedValue(item.CooldownRecovery, item.RecommendedLevel, rank, 1)
                    : item.CooldownRecovery);

                item.SetLocalFloat("STAT_EFFECTIVE_LEVEL_ENMITY_RATE", CalculateAdjustedValue(0.01f * item.EnmityRate, item.RecommendedLevel, rank, 0.00f));

                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_LUCK_BONUS", CalculateAdjustedValue(item.LuckBonus, item.RecommendedLevel, rank, 0));
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_MEDITATE_BONUS", CalculateAdjustedValue(item.MeditateBonus, item.RecommendedLevel, rank, 0));
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_REST_BONUS", CalculateAdjustedValue(item.RestBonus, item.RecommendedLevel, rank, 0));
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_MEDICINE_BONUS", CalculateAdjustedValue(item.MedicineBonus, item.RecommendedLevel, rank, 0));
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_HP_REGEN_BONUS", CalculateAdjustedValue(item.HPRegenBonus, item.RecommendedLevel, rank, 0));
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_FP_REGEN_BONUS", CalculateAdjustedValue(item.FPRegenBonus, item.RecommendedLevel, rank, 0));
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_WEAPONSMITH_BONUS", CalculateAdjustedValue(item.CraftBonusWeaponsmith, item.RecommendedLevel, rank, 0));
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_COOKING_BONUS", CalculateAdjustedValue(item.CraftBonusCooking, item.RecommendedLevel, rank, 0));
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_ENGINEERING_BONUS", CalculateAdjustedValue(item.CraftBonusEngineering, item.RecommendedLevel, rank, 0));
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_FABRICATION_BONUS", CalculateAdjustedValue(item.CraftBonusFabrication, item.RecommendedLevel, rank, 0));
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_ARMORSMITH_BONUS", CalculateAdjustedValue(item.CraftBonusArmorsmith, item.RecommendedLevel, rank, 0));
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_HARVESTING_BONUS", CalculateAdjustedValue(item.HarvestingBonus, item.RecommendedLevel, rank, 0));
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_PILOTING_BONUS", CalculateAdjustedValue(item.PilotingBonus, item.RecommendedLevel, rank, 0));
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_SCAVENGING_BONUS", CalculateAdjustedValue(item.ScavengingBonus, item.RecommendedLevel, rank, 0));
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_SNEAK_ATTACK_BONUS", CalculateAdjustedValue(item.SneakAttackBonus, item.RecommendedLevel, rank, 0));

                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_STRENGTH_BONUS", CalculateAdjustedValue(item.StrengthBonus, item.RecommendedLevel, rank, 0));
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_DEXTERITY_BONUS", CalculateAdjustedValue(item.DexterityBonus, item.RecommendedLevel, rank, 0));
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_CONSTITUTION_BONUS", CalculateAdjustedValue(item.ConstitutionBonus, item.RecommendedLevel, rank, 0));
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_WISDOM_BONUS", CalculateAdjustedValue(item.WisdomBonus, item.RecommendedLevel, rank, 0));
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_INTELLIGENCE_BONUS", CalculateAdjustedValue(item.IntelligenceBonus, item.RecommendedLevel, rank, 0));
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_CHARISMA_BONUS", CalculateAdjustedValue(item.CharismaBonus, item.RecommendedLevel, rank, 0));
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_HP_BONUS", CalculateAdjustedValue(item.HPBonus, item.RecommendedLevel, rank, 0));
                item.SetLocalInt("STAT_EFFECTIVE_LEVEL_FP_BONUS", CalculateAdjustedValue(item.FPBonus, item.RecommendedLevel, rank, 0));

            }
        }

        private static void RemoveCachedEffectiveStats(NWItem item)
        {
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_COOLDOWN_RECOVERY");
            item.DeleteLocalFloat("STAT_EFFECTIVE_LEVEL_ENMITY_RATE");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_LUCK_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_MEDITATE_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_REST_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_MEDICINE_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_HP_REGEN_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_FP_REGEN_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_WEAPONSMITH_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_COOKING_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_ENGINEERING_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_FABRICATION_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_ARMORSMITH_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_HARVESTING_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_PILOTING_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_SCAVENGING_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_SNEAK_ATTACK_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_STRENGTH_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_DEXTERITY_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_CONSTITUTION_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_WISDOM_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_INTELLIGENCE_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_CHARISMA_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_HP_BONUS");
            item.DeleteLocalInt("STAT_EFFECTIVE_LEVEL_FP_BONUS");

        }


        public static EffectiveItemStats GetPlayerItemEffectiveStats(NWPlayer player, NWItem ignoreItem = null)
        {
            var heavyRank = DataService.PCSkill.GetByPlayerIDAndSkillID(player.GlobalID, (int)SkillType.HeavyArmor).Rank;
            var lightRank = DataService.PCSkill.GetByPlayerIDAndSkillID(player.GlobalID, (int)SkillType.LightArmor).Rank;
            var forceRank = DataService.PCSkill.GetByPlayerIDAndSkillID(player.GlobalID, (int)SkillType.ForceArmor).Rank;
            var martialRank = DataService.PCSkill.GetByPlayerIDAndSkillID(player.GlobalID, (int)SkillType.MartialArts).Rank;

            var stats = new EffectiveItemStats();
            stats.EnmityRate = 1.0f;

            var processed = new HashSet<NWItem>();
            for (var itemSlot = 0; itemSlot < NumberOfInventorySlots; itemSlot++)
            {
                NWItem item = GetItemInSlot((InventorySlot)itemSlot, player);

                if (!item.IsValid || item.Equals(ignoreItem)) continue;

                // Have we already processed this particular item? Skip over it.
                // NWN likes to include the same weapon in multiple slots for some reasons, so this works around that.
                // If someone has a better solution to this please feel free to change it.
                if (processed.Contains(item)) continue;
                processed.Add(item);

                var skill = ItemService.GetSkillTypeForItem(item);
                var rank = DataService.PCSkill.GetByPlayerIDAndSkillID(player.GlobalID, (int)skill).Rank;
                stats.CooldownRecovery += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_COOLDOWN_RECOVERY");
                stats.EnmityRate += item.GetLocalFloat("STAT_EFFECTIVE_LEVEL_ENMITY_RATE");
                stats.Luck += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_LUCK_BONUS");
                stats.Meditate += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_MEDITATE_BONUS");
                stats.Rest += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_REST_BONUS");
                stats.Medicine += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_MEDICINE_BONUS");
                stats.HPRegen += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_HP_REGEN_BONUS");
                stats.FPRegen += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_FP_REGEN_BONUS");
                stats.Weaponsmith += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_WEAPONSMITH_BONUS");
                stats.Cooking += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_COOKING_BONUS");
                stats.Engineering += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_ENGINEERING_BONUS");
                stats.Fabrication += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_FABRICATION_BONUS");
                stats.Armorsmith += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_ARMORSMITH_BONUS");
                stats.Harvesting += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_HARVESTING_BONUS");
                stats.Piloting += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_PILOTING_BONUS");
                stats.Scavenging += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_SCAVENGING_BONUS");
                stats.SneakAttack += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_SNEAK_ATTACK_BONUS");
                stats.Strength += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_STRENGTH_BONUS");
                stats.Dexterity += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_DEXTERITY_BONUS");
                stats.Constitution += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_CONSTITUTION_BONUS");
                stats.Wisdom += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_WISDOM_BONUS");
                stats.Intelligence += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_INTELLIGENCE_BONUS");
                stats.Charisma += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_CHARISMA_BONUS");
                stats.HP += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_HP_BONUS");
                stats.FP += item.GetLocalInt("STAT_EFFECTIVE_LEVEL_FP_BONUS");

                // Calculate base attack bonus
                if (ItemService.WeaponBaseItemTypes.Contains(item.BaseItemType))
                {
                    var itemLevel = item.RecommendedLevel;
                    var delta = itemLevel - rank;
                    var itemBAB = item.BaseAttackBonus;
                    if (delta >= 1) itemBAB--;
                    if (delta > 0) itemBAB = itemBAB - delta / 5;

                    if (itemBAB <= 0) itemBAB = 0;
                    stats.BAB += itemBAB;
                }


                // Calculate AC
                if (ItemService.ArmorBaseItemTypes.Contains(item.BaseItemType))
                {
                    int skillRankToUse;
                    var maxAC = 0;

                    if (item.CustomItemType == CustomItemType.HeavyArmor)
                    {
                        skillRankToUse = heavyRank;
                        maxAC = 10;
                    }
                    else if (item.CustomItemType == CustomItemType.LightArmor)
                    {
                        skillRankToUse = lightRank;
                        maxAC = 13;
                    }
                    else if (item.CustomItemType == CustomItemType.ForceArmor)
                    {
                        skillRankToUse = forceRank;
                        maxAC = 11;
                    }
                    else if (item.CustomItemType == CustomItemType.MartialArtWeapon)
                    {
                        skillRankToUse = martialRank;
                    }
                    else continue;

                    var itemAC = item.CustomAC;
                    itemAC = CalculateAdjustedValue(itemAC, item.RecommendedLevel, skillRankToUse, 0);
                    if (itemAC > maxAC)
                    {
                        item.CustomAC = maxAC;
                    }
                    stats.AC += itemAC;

                }

            }

            // Final casting speed adjustments
            if (stats.CooldownRecovery < -99)
                stats.CooldownRecovery = -99;
            else if (stats.CooldownRecovery > 99)
                stats.CooldownRecovery = 99;

            // Final enmity adjustments
            if (stats.EnmityRate < 0.5f) stats.EnmityRate = 0.5f;
            else if (stats.EnmityRate > 1.5f) stats.EnmityRate = 1.5f;

            var stance = CustomEffectService.GetCurrentStanceType(player);
            if (stance == CustomEffectType.ShieldOath)
            {
                stats.EnmityRate = stats.EnmityRate + 0.2f;
            }

            return stats;
        }

        private static int CalculateBAB(NWPlayer oPC, NWItem ignoreItem, EffectiveItemStats stats)
        {
            var weapon = oPC.RightHand;

            // The unequip event fires before the item is actually unequipped, so we need
            // to have additional checks to make sure we're not getting the weapon that's about to be
            // unequipped.
            if (weapon.Equals(ignoreItem))
            {
                weapon = null;
                var offHand = oPC.LeftHand;

                if (offHand.CustomItemType == CustomItemType.Vibroblade ||
                   offHand.CustomItemType == CustomItemType.FinesseVibroblade ||
                   offHand.CustomItemType == CustomItemType.Baton ||
                   offHand.CustomItemType == CustomItemType.HeavyVibroblade ||
                   offHand.CustomItemType == CustomItemType.Saberstaff ||
                   offHand.CustomItemType == CustomItemType.Polearm ||
                   offHand.CustomItemType == CustomItemType.TwinBlade ||
                   offHand.CustomItemType == CustomItemType.MartialArtWeapon ||
                   offHand.CustomItemType == CustomItemType.BlasterPistol ||
                   offHand.CustomItemType == CustomItemType.BlasterRifle ||
                   offHand.CustomItemType == CustomItemType.Throwing)
                {
                    weapon = offHand;
                }
            }

            if (weapon == null || !weapon.IsValid)
            {
                weapon = oPC.Arms;
            }
            if (!weapon.IsValid) return 0;

            var itemSkill = ItemService.GetSkillTypeForItem(weapon);
            if (itemSkill == SkillType.Unknown ||
                itemSkill == SkillType.LightArmor ||
                itemSkill == SkillType.HeavyArmor ||
                itemSkill == SkillType.ForceArmor ||
                itemSkill == SkillType.Shields) return 0;

            var weaponSkillID = (int)itemSkill;
            var skill = DataService.PCSkill.GetByPlayerIDAndSkillID(oPC.GlobalID, weaponSkillID);
            if (skill == null) return 0;
            var skillBAB = skill.Rank / 10;
            var perkBAB = 0;

            return 1 + skillBAB + perkBAB + stats.BAB; // Note: Always add 1 to BAB. 0 will cause a crash in NWNX.
        }
    }
}
