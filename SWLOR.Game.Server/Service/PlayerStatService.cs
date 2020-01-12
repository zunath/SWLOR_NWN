﻿
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;
using static SWLOR.Game.Server.NWScript._;
using SWLOR.Game.Server.ValueObject;
using BaseItemType = SWLOR.Game.Server.NWScript.Enumerations.BaseItemType;

namespace SWLOR.Game.Server.Service
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
            for (int itemSlot = 0; itemSlot < NWNConstants.NumberOfInventorySlots; itemSlot++)
            {
                NWItem item = GetItemInSlot((InventorySlot)itemSlot, player);
                CalculateEffectiveStats(player, item);
            }
            ApplyStatChanges(player, null);
        }

        private static void OnSkillDecayed(NWPlayer player)
        {
            for (int itemSlot = 0; itemSlot < NWNConstants.NumberOfInventorySlots; itemSlot++)
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
                (ignoreItem.BaseItemType == BaseItemType.Bolt ||
                 ignoreItem.BaseItemType == BaseItemType.Arrow ||
                 ignoreItem.BaseItemType == BaseItemType.Bullet)) return;

            Player pcEntity = DataService.Player.GetByID(player.GlobalID);
            var skills = pcEntity.Skills
                .Where(x => x.Value.Rank > 0).ToList();
            EffectiveItemStats itemBonuses = GetPlayerItemEffectiveStats(player, ignoreItem);

            float strBonus = 0.0f;
            float dexBonus = 0.0f;
            float conBonus = 0.0f;
            float intBonus = 0.0f;
            float wisBonus = 0.0f;
            float chaBonus = 0.0f;

            foreach (var pcSkill in skills)
            {
                var skill = SkillService.GetSkill(pcSkill.Key);
                CustomAttribute primary = (CustomAttribute)skill.Primary;
                CustomAttribute secondary = (CustomAttribute)skill.Secondary;
                CustomAttribute tertiary = (CustomAttribute)skill.Tertiary;

                // Primary Bonuses
                if (primary == CustomAttribute.STR) strBonus += PrimaryIncrease * pcSkill.Value.Rank;
                else if (primary == CustomAttribute.DEX) dexBonus += PrimaryIncrease * pcSkill.Value.Rank;
                else if (primary == CustomAttribute.CON) conBonus += PrimaryIncrease * pcSkill.Value.Rank;
                else if (primary == CustomAttribute.INT) intBonus += PrimaryIncrease * pcSkill.Value.Rank;
                else if (primary == CustomAttribute.WIS) wisBonus += PrimaryIncrease * pcSkill.Value.Rank;
                else if (primary == CustomAttribute.CHA) chaBonus += PrimaryIncrease * pcSkill.Value.Rank;

                // Secondary Bonuses
                if (secondary == CustomAttribute.STR) strBonus += SecondaryIncrease * pcSkill.Value.Rank;
                else if (secondary == CustomAttribute.DEX) dexBonus += SecondaryIncrease * pcSkill.Value.Rank;
                else if (secondary == CustomAttribute.CON) conBonus += SecondaryIncrease * pcSkill.Value.Rank;
                else if (secondary == CustomAttribute.INT) intBonus += SecondaryIncrease * pcSkill.Value.Rank;
                else if (secondary == CustomAttribute.WIS) wisBonus += SecondaryIncrease * pcSkill.Value.Rank;
                else if (secondary == CustomAttribute.CHA) chaBonus += SecondaryIncrease * pcSkill.Value.Rank;

                // Tertiary Bonuses
                if (tertiary == CustomAttribute.STR) strBonus += TertiaryIncrease * pcSkill.Value.Rank;
                else if (tertiary == CustomAttribute.DEX) dexBonus += TertiaryIncrease * pcSkill.Value.Rank;
                else if (tertiary == CustomAttribute.CON) conBonus += TertiaryIncrease * pcSkill.Value.Rank;
                else if (tertiary == CustomAttribute.INT) intBonus += TertiaryIncrease * pcSkill.Value.Rank;
                else if (tertiary == CustomAttribute.WIS) wisBonus += TertiaryIncrease * pcSkill.Value.Rank;
                else if (tertiary == CustomAttribute.CHA) chaBonus += TertiaryIncrease * pcSkill.Value.Rank;
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
            NWNXCreature.SetRawAbilityScore(player, Ability.Strength, (int)strBonus + pcEntity.STRBase);
            NWNXCreature.SetRawAbilityScore(player, Ability.Dexterity, (int)dexBonus + pcEntity.DEXBase);
            NWNXCreature.SetRawAbilityScore(player, Ability.Constitution, (int)conBonus + pcEntity.CONBase);
            NWNXCreature.SetRawAbilityScore(player, Ability.Intelligence, (int)intBonus + pcEntity.INTBase);
            NWNXCreature.SetRawAbilityScore(player, Ability.Wisdom, (int)wisBonus + pcEntity.WISBase);
            NWNXCreature.SetRawAbilityScore(player, Ability.Charisma, (int)chaBonus + pcEntity.CHABase);

            // Apply AC
            int ac = EffectiveArmorClass(player, ignoreItem, itemBonuses);
            NWNXCreature.SetBaseAC(player, ac);

            // Apply BAB
            int bab = CalculateBAB(player, ignoreItem, itemBonuses);
            NWNXCreature.SetBaseAttackBonus(player, bab);

            // Apply HP
            int hp = EffectiveMaxHitPoints(player, itemBonuses);
            for (int level = 1; level <= 5; level++)
            {
                hp--;
                NWNXCreature.SetMaxHitPointsByLevel(player, level, 1);
            }

            for (int level = 1; level <= 5; level++)
            {
                if (hp > 255) // Levels can only contain a max of 255 HP
                {
                    NWNXCreature.SetMaxHitPointsByLevel(player, level, 255);
                    hp = hp - 254;
                }
                else // Remaining value gets set to the level. (<255 hp)
                {
                    NWNXCreature.SetMaxHitPointsByLevel(player, level, hp + 1);
                    break;
                }
            }

            if (player.CurrentHP > player.MaxHP)
            {
                int amount = player.CurrentHP - player.MaxHP;
                Effect damage = EffectDamage(amount);
                ApplyEffectToObject(DurationType.Instant, damage, player.Object);
            }

            // Apply FP
            pcEntity.MaxFP = EffectiveMaxFP(player, itemBonuses);

            if (isInitialization)
            {
                pcEntity.CurrentFP = pcEntity.MaxFP;
            }

            DataService.Set(pcEntity);

            // Attempt a refresh of the character sheet UI in a second.
            DelayCommand(1.0f, () =>
            {
                NWNXPlayer.UpdateCharacterSheet(player);
            });
        }


        private static int CalculateAdjustedValue(int baseValue, int recommendedLevel, int skillRank, int minimumValue)
        {
            int adjustedValue = (int)CalculateAdjustedValue((float)baseValue, recommendedLevel, skillRank, minimumValue);
            if (adjustedValue < minimumValue) adjustedValue = minimumValue;
            return adjustedValue;
        }

        private static float CalculateAdjustedValue(float baseValue, int recommendedLevel, int skillRank, float minimumValue)
        {
            int delta = recommendedLevel - skillRank;
            float adjustment = 1.0f - delta * 0.1f;
            if (adjustment <= 0.1f) adjustment = 0.1f;
            else if (adjustment > 1.0f) adjustment = 1.0f;

            float adjustedValue = (float)Math.Round(baseValue * adjustment);
            if (adjustedValue < minimumValue) adjustedValue = minimumValue;
            return adjustedValue;
        }

        private static int EffectiveMaxHitPoints(NWPlayer player, EffectiveItemStats stats)
        {
            int hp = 25 + player.ConstitutionModifier * 5;
            float effectPercentBonus = CustomEffectService.CalculateEffectHPBonusPercent(player);

            hp += PerkService.GetCreaturePerkLevel(player, PerkType.Health) * 5;
            hp += stats.HP;
            hp = hp + (int)(hp * effectPercentBonus);

            if (hp > 1275) hp = 1275;
            if (hp < 20) hp = 20;

            return hp;
        }

        private static int EffectiveMaxFP(NWPlayer player, EffectiveItemStats stats)
        {
            int fp = 20;
            fp += (player.IntelligenceModifier + player.WisdomModifier + player.CharismaModifier) * 5;
            fp += PerkService.GetCreaturePerkLevel(player, PerkType.FP) * 5;
            fp += stats.FP;

            if (fp < 0) fp = 0;

            return fp;
        }

        private static int EffectiveArmorClass(NWPlayer player, NWItem ignoreItem, EffectiveItemStats stats)
        {
            int baseAC = stats.AC + CustomEffectService.CalculateEffectAC(player);

            // Calculate AC bonus granted by skill ranks.
            // Only chest armor is checked for this bonus.

            if (ignoreItem != player.Chest)
            {
                CustomItemType armorType = player.Chest.CustomItemType;
                int skillRank = 0;
                switch (armorType)
                {
                    case CustomItemType.LightArmor:
                        skillRank = SkillService.GetPCSkillRank(player, Skill.LightArmor);
                        break;
                    case CustomItemType.HeavyArmor:
                        skillRank = SkillService.GetPCSkillRank(player, Skill.HeavyArmor);
                        break;
                    case CustomItemType.ForceArmor:
                        skillRank = SkillService.GetPCSkillRank(player, Skill.ForceArmor);
                        break;
                }

                // +1 AC per 10 skill ranks, while wearing the appropriate armor.
                int skillACBonus = skillRank / 10;
                baseAC += skillACBonus;
            }

            int totalAC = GetAC(player) - baseAC;

            // Shield Oath and Precision Targeting affect a percentage of the TOTAL armor class on a creature.
            var stance = CustomEffectService.GetCurrentStanceType(player);
            if (stance == CustomEffectType.ShieldOath)
            {
                int bonus = (int)(totalAC * 0.2f);
                baseAC += bonus;
            }
            else if (stance == CustomEffectType.PrecisionTargeting)
            {
                int penalty = (int)(totalAC * 0.3f);
                baseAC -= penalty;
            }

            if (baseAC < 0) baseAC = 0;

            return baseAC;
        }

        private static void CalculateEffectiveStats(NWPlayer player, NWItem item)
        {
            if (item == null || !item.IsValid || !player.IsPlayer || player.IsDMPossessed || player.IsDM || !player.IsInitializedAsPlayer) return;

            // Calculating effective stats can be expensive, so we cache it on the item.
            Skill skill;
            var dbPlayer = DataService.Player.GetByID(player.GlobalID);

            if(item.BaseItemType == BaseItemType.Amulet || item.BaseItemType == BaseItemType.Ring)
            {
                var forceArmor = dbPlayer.Skills[Skill.ForceArmor];
                var lightArmor = dbPlayer.Skills[Skill.LightArmor];
                var heavyArmor = dbPlayer.Skills[Skill.HeavyArmor];
                var highest = forceArmor.Rank;
                skill = Skill.ForceArmor;

                if (lightArmor.Rank > highest)
                {
                    highest = lightArmor.Rank;
                    skill = Skill.LightArmor;
                }
                if (heavyArmor.Rank > highest)
                {
                    skill = Skill.HeavyArmor;
                }
            }
            else
            {
                skill = ItemService.GetSkillTypeForItem(item);
            }

            var rank = dbPlayer.Skills[skill].Rank;
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
            var dbPlayer = DataService.Player.GetByID(player.GlobalID);
            int heavyRank = dbPlayer.Skills[Skill.HeavyArmor].Rank;
            int lightRank = dbPlayer.Skills[Skill.LightArmor].Rank;
            int forceRank = dbPlayer.Skills[Skill.ForceArmor].Rank;
            int martialRank = dbPlayer.Skills[Skill.MartialArts].Rank;

            EffectiveItemStats stats = new EffectiveItemStats();
            stats.EnmityRate = 1.0f;

            HashSet<NWItem> processed = new HashSet<NWItem>();
            for (int itemSlot = 0; itemSlot < NWNConstants.NumberOfInventorySlots; itemSlot++)
            {
                NWItem item = GetItemInSlot((InventorySlot)itemSlot, player);

                if (!item.IsValid || item.Equals(ignoreItem)) continue;

                // Have we already processed this particular item? Skip over it.
                // NWN likes to include the same weapon in multiple slots for some reasons, so this works around that.
                // If someone has a better solution to this please feel free to change it.
                if (processed.Contains(item)) continue;
                processed.Add(item);

                Skill skill = ItemService.GetSkillTypeForItem(item);
                var rank = dbPlayer.Skills[skill].Rank;
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
                    int itemLevel = item.RecommendedLevel;
                    int delta = itemLevel - rank;
                    int itemBAB = item.BaseAttackBonus;
                    if (delta >= 1) itemBAB--;
                    if (delta > 0) itemBAB = itemBAB - delta / 5;

                    if (itemBAB <= 0) itemBAB = 0;
                    stats.BAB += itemBAB;
                }


                // Calculate AC
                if (ItemService.ArmorBaseItemTypes.Contains(item.BaseItemType) ||
                    ItemService.ShieldBaseItemTypes.Contains(item.BaseItemType))
                {
                    int skillRankToUse;
                    if (item.CustomItemType == CustomItemType.HeavyArmor)
                    {
                        skillRankToUse = heavyRank;
                    }
                    else if (item.CustomItemType == CustomItemType.LightArmor)
                    {
                        skillRankToUse = lightRank;
                    }
                    else if (item.CustomItemType == CustomItemType.ForceArmor)
                    {
                        skillRankToUse = forceRank;
                    }
                    else if (item.CustomItemType == CustomItemType.MartialArtWeapon)
                    {
                        skillRankToUse = martialRank;
                    }
                    else continue;

                    int itemAC = item.CustomAC;
                    itemAC = CalculateAdjustedValue(itemAC, item.RecommendedLevel, skillRankToUse, 0);
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

        public static float EffectiveResidencyBonus(NWPlayer player)
        {
            var dbPlayer = DataService.Player.GetByID(player.GlobalID);

            // Player doesn't have either kind of residence. Return 0f
            if (dbPlayer.PrimaryResidencePCBaseID == null &&
                dbPlayer.PrimaryResidencePCBaseStructureID == null) return 0.0f;

            // Two paths for this. Players can either have a primary residence in an apartment which is considered a "PCBase".
            // Or they can have a primary residence in a building which is a child structure contained in an actual PCBase.
            // We grab the furniture objects differently based on the type.

            List<PCBaseStructure> structures;

            // Apartments - Pull structures directly from the table based on the PCBaseID
            if (dbPlayer.PrimaryResidencePCBaseID != null)
            {
                structures = DataService.PCBaseStructure.GetAllByPCBaseID((Guid)dbPlayer.PrimaryResidencePCBaseID).ToList();

            }
            // Buildings - Get the building's PCBaseID and then grab its children
            else if (dbPlayer.PrimaryResidencePCBaseStructureID != null)
            {
                structures = DataService.PCBaseStructure.GetAllByParentPCBaseStructureID((Guid)dbPlayer.PrimaryResidencePCBaseStructureID).ToList();
            }
            else return 0.0f;

            var atmoStructures = structures.Where(x =>
            {
                var baseStructure = BaseService.GetBaseStructure(x.BaseStructureID);
                return baseStructure.HasAtmosphere;
            }).ToList();

            float bonus = atmoStructures.Sum(x => (x.StructureBonus * 0.02f) + 0.02f);

            if (bonus >= 1.5f) bonus = 1.5f; // Maximum = 250% XP (+150% bonus from residency)
            return bonus;
        }

        private static int CalculateBAB(NWPlayer oPC, NWItem ignoreItem, EffectiveItemStats stats)
        {
            NWItem weapon = oPC.RightHand;

            // The unequip event fires before the item is actually unequipped, so we need
            // to have additional checks to make sure we're not getting the weapon that's about to be
            // unequipped.
            if (weapon.Equals(ignoreItem))
            {
                weapon = null;
                NWItem offHand = oPC.LeftHand;

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

            Skill itemSkill = ItemService.GetSkillTypeForItem(weapon);
            if (itemSkill == Skill.Unknown ||
                itemSkill == Skill.LightArmor ||
                itemSkill == Skill.HeavyArmor ||
                itemSkill == Skill.ForceArmor ||
                itemSkill == Skill.Shields) return 0;

            var player = DataService.Player.GetByID(oPC.GlobalID);
            PCSkill skill = player.Skills[itemSkill];
            if (skill == null) return 0;
            int skillBAB = skill.Rank / 10;
            int perkBAB = 0;
            int backgroundBAB = 0;
            ClassType background = (ClassType)oPC.Class1;
            bool receivesBackgroundBonus = false;

            switch (weapon.CustomItemType)
            {
                case CustomItemType.FinesseVibroblade:
                    receivesBackgroundBonus = background == ClassType.Duelist;
                    break;
                case CustomItemType.Baton:
                    receivesBackgroundBonus = background == ClassType.SecurityOfficer;
                    break;
                case CustomItemType.HeavyVibroblade:
                    receivesBackgroundBonus = background == ClassType.Soldier;
                    break;
                case CustomItemType.TwinBlade:
                    receivesBackgroundBonus = background == ClassType.Berserker;
                    break;
                case CustomItemType.MartialArtWeapon:
                    receivesBackgroundBonus = background == ClassType.TerasKasi;
                    break;
                case CustomItemType.BlasterPistol:
                    receivesBackgroundBonus = background == ClassType.Smuggler;
                    break;
                case CustomItemType.BlasterRifle:
                    receivesBackgroundBonus = background == ClassType.Sharpshooter || background == ClassType.Mandalorian;
                    break;
            }

            if (receivesBackgroundBonus)
            {
                backgroundBAB = background == ClassType.Mandalorian ? 1 : 2;
            }

            return 1 + skillBAB + perkBAB + stats.BAB + backgroundBAB; // Note: Always add 1 to BAB. 0 will cause a crash in NWNX.
        }
    }
}
