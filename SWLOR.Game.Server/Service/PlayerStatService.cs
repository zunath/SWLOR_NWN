
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.NWNX.Contracts;
using static NWN.NWScript;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public class PlayerStatService : IPlayerStatService
    {
        private const float PrimaryIncrease = 0.2f;
        private const float SecondaryIncrease = 0.1f;
        private const float TertiaryIncrease = 0.05f;
        private const int MaxAttributeBonus = 70;

        private readonly INWScript _;
        private readonly ICustomEffectService _customEffect;
        private readonly IItemService _item;
        private readonly IDataContext _db;
        private readonly IPerkService _perk;
        private readonly INWNXCreature _nwnxCreature;

        public PlayerStatService(
            INWScript script,
            ICustomEffectService customEffect,
            INWNXCreature nwnxCreature,
            IItemService item,
            IDataContext db,
            IPerkService perk)
        {
            _ = script;
            _customEffect = customEffect;
            _item = item;
            _db = db;
            _perk = perk;
            _nwnxCreature = nwnxCreature;

        }
        
        public void ApplyStatChanges(NWPlayer player, NWItem ignoreItem, bool isInitialization = false)
        {
            if (!player.IsPlayer) return;
            if (!player.IsInitializedAsPlayer) return;

            PlayerCharacter pcEntity = _db.PlayerCharacters.Single(x => x.PlayerID == player.GlobalID);
            List<PCSkill> skills = _db.PCSkills.Where(x => x.PlayerID == player.GlobalID && x.Skill.IsActive && x.Rank > 0).ToList();
            var itemBonuses = GetPlayerItemEffectiveStats(player, ignoreItem);

            float strBonus = 0.0f;
            float dexBonus = 0.0f;
            float conBonus = 0.0f;
            float intBonus = 0.0f;
            float wisBonus = 0.0f;
            float chaBonus = 0.0f;

            foreach (PCSkill pcSkill in skills)
            {
                Skill skill = pcSkill.Skill;
                CustomAttribute primary = (CustomAttribute)skill.Primary;
                CustomAttribute secondary = (CustomAttribute)skill.Secondary;
                CustomAttribute tertiary = (CustomAttribute)skill.Tertiary;

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
            if (strBonus > 100) strBonus = 100;
            if (dexBonus > 100) dexBonus = 100;
            if (conBonus > 100) conBonus = 100;
            if (intBonus > 100) intBonus = 100;
            if (wisBonus > 100) wisBonus = 100;
            if (chaBonus > 100) chaBonus = 100;
            
            // Apply attributes
            _nwnxCreature.SetRawAbilityScore(player, ABILITY_STRENGTH, (int)strBonus + pcEntity.STRBase);
            _nwnxCreature.SetRawAbilityScore(player, ABILITY_DEXTERITY, (int)dexBonus + pcEntity.DEXBase);
            _nwnxCreature.SetRawAbilityScore(player, ABILITY_CONSTITUTION, (int)conBonus + pcEntity.CONBase);
            _nwnxCreature.SetRawAbilityScore(player, ABILITY_INTELLIGENCE, (int)intBonus + pcEntity.INTBase);
            _nwnxCreature.SetRawAbilityScore(player, ABILITY_WISDOM, (int)wisBonus + pcEntity.WISBase);
            _nwnxCreature.SetRawAbilityScore(player, ABILITY_CHARISMA, (int)chaBonus + pcEntity.CHABase);

            // Apply AC
            int ac = EffectiveArmorClass(player, ignoreItem);
            _nwnxCreature.SetBaseAC(player, ac);

            // Apply BAB
            int bab = CalculateBAB(player, ignoreItem);
            _nwnxCreature.SetBaseAttackBonus(player, bab);

            // Apply HP
            int hp = EffectiveMaxHitPoints(player, ignoreItem);
            for (int level = 1; level <= 5; level++)
            {
                hp--;
                _nwnxCreature.SetMaxHitPointsByLevel(player, level, 1);
            }

            for (int level = 1; level <= 5; level++)
            {
                if (hp > 255) // Levels can only contain a max of 255 HP
                {
                    _nwnxCreature.SetMaxHitPointsByLevel(player, level, 255);
                    hp = hp - 255;
                }
                else // Remaining value gets set to the level. (<255 hp)
                {
                    _nwnxCreature.SetMaxHitPointsByLevel(player, level, hp + 1);
                    break;
                }
            }

            if (player.CurrentHP > player.MaxHP)
            {
                int amount = player.CurrentHP - player.MaxHP;
                Effect damage = _.EffectDamage(amount);
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, damage, player.Object);
            }

            // Apply FP
            pcEntity.MaxFP = EffectiveMaxFP(player, ignoreItem);

            if (isInitialization)
            {
                pcEntity.CurrentFP = pcEntity.MaxFP;
            }

            _db.SaveChanges();
        }


        private int CalculateAdjustedValue(int baseValue, int recommendedLevel, int skillRank, int minimumValue = 1)
        {
            int adjustedValue = (int)CalculateAdjustedValue((float)baseValue, recommendedLevel, skillRank, minimumValue);
            if (adjustedValue < minimumValue) adjustedValue = minimumValue;
            return adjustedValue;
        }

        private float CalculateAdjustedValue(float baseValue, int recommendedLevel, int skillRank, float minimumValue = 0.01f)
        {
            int delta = recommendedLevel - skillRank;
            float adjustment = 1.0f - delta * 0.1f;
            if (adjustment <= 0.1f) adjustment = 0.1f;

            float adjustedValue = (float)Math.Round(baseValue * adjustment);
            if (adjustedValue < minimumValue) adjustedValue = minimumValue;
            return adjustedValue;
        }

        public int EffectiveMaxHitPoints(NWPlayer player, NWItem ignoreItem)
        {
            int hp = 25 + player.ConstitutionModifier * 5;
            int equippedItemHPBonus = 0;
            var skills = _db.PCSkills.Where(x => x.PlayerID == player.GlobalID)
                .Select(x => new
                {
                    x.SkillID,
                    x.Rank
                }).ToDictionary(x => x.SkillID, x => x.Rank);
            float effectPercentBonus = _customEffect.CalculateEffectHPBonusPercent(player);

            for (int slot = 0; slot < NUM_INVENTORY_SLOTS; slot++)
            {
                NWItem item = _.GetItemInSlot(slot, player);
                if (item.Equals(ignoreItem)) continue;

                var skillType = _item.GetSkillTypeForItem(item);
                int rank = skills[(int)skillType];
                equippedItemHPBonus += CalculateAdjustedValue(item.HPBonus, item.RecommendedLevel, rank, 0);
            }

            hp += _perk.GetPCPerkLevel(player, PerkType.Health) * 5;
            hp += equippedItemHPBonus;
            hp = hp + (int)(hp * effectPercentBonus);

            if (hp > 1275) hp = 1275;
            if (hp < 20) hp = 20;

            return hp;
        }

        public int EffectiveMaxFP(NWPlayer player, NWItem ignoreItem)
        {
            int equippedItemFPBonus = 0;
            var skills = _db.PCSkills.Where(x => x.PlayerID == player.GlobalID)
                .Select(x => new
                {
                    x.SkillID,
                    x.Rank
                }).ToDictionary(x => x.SkillID, x => x.Rank);


            for (int slot = 0; slot < NUM_INVENTORY_SLOTS; slot++)
            {
                NWItem item = _.GetItemInSlot(slot, player.Object);
                if (item.Equals(ignoreItem)) continue;

                var skillType = _item.GetSkillTypeForItem(item);
                int rank = skills[(int)skillType];
                equippedItemFPBonus += CalculateAdjustedValue(item.FPBonus, item.RecommendedLevel, rank, 0);
            }

            int fp = 20;
            fp += (player.IntelligenceModifier + player.WisdomModifier + player.CharismaModifier) * 5;
            fp += _perk.GetPCPerkLevel(player, PerkType.FP) * 5;
            fp += equippedItemFPBonus;

            if (fp < 0) fp = 0;

            return fp;
        }

        public int EffectiveArmorClass(NWPlayer player, NWItem ignoreItem)
        {
            int[] skills = {(int)SkillType.HeavyArmor, (int)SkillType.LightArmor, (int)SkillType.ForceArmor};
            var armorSkills = _db.PCSkills.Where(x => x.PlayerID == player.GlobalID && skills.Contains(x.SkillID)).ToList();

            int heavyRank = armorSkills.Single(x => x.SkillID == (int) SkillType.HeavyArmor).Rank;
            int lightRank = armorSkills.Single(x => x.SkillID == (int) SkillType.LightArmor).Rank;
            int forceRank = armorSkills.Single(x => x.SkillID == (int)SkillType.ForceArmor).Rank;

            int baseAC = 0;
            for (int slot = 0; slot < NUM_INVENTORY_SLOTS; slot++)
            {
                NWItem oItem = _.GetItemInSlot(slot, player.Object);
                if (oItem.Equals(ignoreItem))
                    continue;

                if (!oItem.IsValid) continue;

                if (!_item.ArmorBaseItemTypes.Contains(oItem.BaseItemType))
                    continue;

                if (oItem.CustomItemType != CustomItemType.HeavyArmor &&
                    oItem.CustomItemType != CustomItemType.LightArmor &&
                    oItem.CustomItemType != CustomItemType.ForceArmor)
                    continue;

                int skillRankToUse = 0;
                if (oItem.CustomItemType == CustomItemType.HeavyArmor &&
                    oItem.RecommendedLevel > heavyRank)
                {
                    skillRankToUse = heavyRank;
                }
                else if (oItem.CustomItemType == CustomItemType.LightArmor &&
                    oItem.RecommendedLevel > lightRank)
                {
                    skillRankToUse = lightRank;
                }
                else if (oItem.CustomItemType == CustomItemType.ForceArmor &&
                         oItem.RecommendedLevel > forceRank)
                {
                    skillRankToUse = forceRank;
                }

                int itemAC = oItem.CustomAC;
                itemAC = CalculateAdjustedValue(itemAC, oItem.RecommendedLevel, skillRankToUse, 0);
                baseAC += itemAC;
            }
            baseAC = baseAC + _customEffect.CalculateEffectAC(player);
            int totalAC = _.GetAC(player) - baseAC;
            
            // Shield Oath and Sword Oath affect a percentage of the TOTAL armor class on a creature.
            var stance = _customEffect.GetCurrentStanceType(player);
            if (stance == CustomEffectType.ShieldOath)
            {
                int bonus = (int) (totalAC * 0.2f);
                baseAC = baseAC + bonus;
            }
            else if (stance == CustomEffectType.SwordOath)
            {
                int penalty = (int)(totalAC * 0.3f);
                baseAC = baseAC - penalty;
            }
            
            return baseAC;
        }
        
        public EffectiveItemStats GetPlayerItemEffectiveStats(NWPlayer player, NWItem ignoreItem = null)
        {
            EffectiveItemStats stats = new EffectiveItemStats();
            stats.EnmityRate = 1.0f;

            for (int itemSlot = 0; itemSlot < NUM_INVENTORY_SLOTS; itemSlot++)
            {
                NWItem item = _.GetItemInSlot(itemSlot, player);
                if (!item.IsValid || item.Equals(ignoreItem)) continue;
                SkillType skill = _item.GetSkillTypeForItem(item);
                int rank = _db.PCSkills.Single(x => x.PlayerID == player.GlobalID && x.SkillID == (int)skill).Rank;

                // Only scale casting speed if it's a bonus. Penalties remain regardless of skill level difference.
                if (item.CastingSpeed > 0)
                {
                    stats.CastingSpeed += CalculateAdjustedValue(item.CastingSpeed, item.RecommendedLevel, rank);
                }
                else stats.CastingSpeed += item.CastingSpeed;

                stats.EnmityRate += CalculateAdjustedValue(0.01f * item.EnmityRate, item.RecommendedLevel, rank, 0.00f);
                stats.DarkAbility += CalculateAdjustedValue(item.DarkAbilityBonus, item.RecommendedLevel, rank, 0);
                stats.LightAbility += CalculateAdjustedValue(item.LightAbilityBonus, item.RecommendedLevel, rank, 0);
                stats.Luck += CalculateAdjustedValue(item.LuckBonus, item.RecommendedLevel, rank, 0);
                stats.Meditate += CalculateAdjustedValue(item.MeditateBonus, item.RecommendedLevel, rank, 0);
                stats.Rest += CalculateAdjustedValue(item.RestBonus, item.RecommendedLevel, rank, 0);
                stats.Medicine += CalculateAdjustedValue(item.MedicineBonus, item.RecommendedLevel, rank, 0);
                stats.HPRegen += CalculateAdjustedValue(item.HPRegenBonus, item.RecommendedLevel, rank, 0);
                stats.FPRegen += CalculateAdjustedValue(item.FPRegenBonus, item.RecommendedLevel, rank, 0);
                stats.Weaponsmith += CalculateAdjustedValue(item.CraftBonusWeaponsmith, item.RecommendedLevel, rank, 0);
                stats.Cooking += CalculateAdjustedValue(item.CraftBonusCooking, item.RecommendedLevel, rank, 0);
                stats.Engineering += CalculateAdjustedValue(item.CraftBonusEngineering, item.RecommendedLevel, rank, 0);
                stats.Fabrication += CalculateAdjustedValue(item.CraftBonusFabrication, item.RecommendedLevel, rank, 0);
                stats.Armorsmith += CalculateAdjustedValue(item.CraftBonusArmorsmith, item.RecommendedLevel, rank, 0);
                stats.Harvesting += CalculateAdjustedValue(item.HarvestingBonus, item.RecommendedLevel, rank, 0);
                stats.SneakAttack += CalculateAdjustedValue(item.SneakAttackBonus, item.RecommendedLevel, rank, 0);

                stats.Strength += CalculateAdjustedValue(item.StrengthBonus, item.RecommendedLevel, rank, 0);
                stats.Dexterity += CalculateAdjustedValue(item.DexterityBonus, item.RecommendedLevel, rank, 0);
                stats.Constitution += CalculateAdjustedValue(item.ConstitutionBonus, item.RecommendedLevel, rank, 0);
                stats.Wisdom += CalculateAdjustedValue(item.WisdomBonus, item.RecommendedLevel, rank, 0);
                stats.Intelligence += CalculateAdjustedValue(item.IntelligenceBonus, item.RecommendedLevel, rank, 0);
                stats.Charisma += CalculateAdjustedValue(item.CharismaBonus, item.RecommendedLevel, rank, 0);
            }

            // Final casting speed adjustments
            if (stats.CastingSpeed < -99)
                stats.CastingSpeed = -99;
            else if (stats.CastingSpeed > 99)
                stats.CastingSpeed = 99;

            // Final enmity adjustments
            if (stats.EnmityRate < 0.5f) stats.EnmityRate = 0.5f;
            else if (stats.EnmityRate > 1.5f) stats.EnmityRate = 1.5f;

            var stance = _customEffect.GetCurrentStanceType(player);
            if (stance == CustomEffectType.ShieldOath)
            {
                stats.EnmityRate = stats.EnmityRate + 0.2f;
            }

            return stats;
        }


        public float EffectiveResidencyBonus(NWPlayer player)
        {
            var dbPlayer = _db.PlayerCharacters.Single(x => x.PlayerID == player.GlobalID);
            if (dbPlayer.PrimaryResidencePCBaseStructureID == null &&
                dbPlayer.PrimaryResidencePCBaseID == null) return 0.0f;

            var atmoStructures = dbPlayer.PrimaryResidencePCBaseID != null ? 
                dbPlayer.PrimaryResidencePCBase.PCBaseStructures.Where(x => x.BaseStructure.HasAtmosphere).ToList() : 
                dbPlayer.PrimaryResidencePCBaseStructure.ChildStructures.Where(x => x.BaseStructure.HasAtmosphere).ToList();

            float bonus = atmoStructures.Sum(x => (x.StructureBonus * 0.02f) + 0.02f);

            if (bonus >= 1f) bonus = 1f;

            return bonus;
        }




        private int CalculateBAB(NWPlayer oPC, NWItem ignoreItem)
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

            SkillType itemSkill = _item.GetSkillTypeForItem(weapon);
            if (itemSkill == SkillType.Unknown ||
                itemSkill == SkillType.LightArmor ||
                itemSkill == SkillType.HeavyArmor ||
                itemSkill == SkillType.ForceArmor ||
                itemSkill == SkillType.Shields) return 0;

            int weaponSkillID = (int)itemSkill;
            PCSkill skill = _db.PCSkills.Single(x => x.PlayerID == oPC.GlobalID && x.SkillID == weaponSkillID);
            if (skill == null) return 0;
            int skillBAB = skill.Rank / 10;
            int perkBAB = 0;
            int backgroundBAB = 0;
            BackgroundType background = (BackgroundType)oPC.Class1;
            bool receivesBackgroundBonus = false;

            // Apply increased BAB if player is using a weapon for which they have a proficiency.
            PerkType proficiencyPerk = PerkType.Unknown;
            SkillType proficiencySkill = SkillType.Unknown;
            switch (weapon.CustomItemType)
            {
                case CustomItemType.Vibroblade:
                    proficiencyPerk = PerkType.VibrobladeProficiency;
                    proficiencySkill = SkillType.OneHanded;
                    break;
                case CustomItemType.FinesseVibroblade:
                    proficiencyPerk = PerkType.FinesseVibrobladeProficiency;
                    proficiencySkill = SkillType.OneHanded;
                    receivesBackgroundBonus = background == BackgroundType.Duelist;
                    break;
                case CustomItemType.Baton:
                    proficiencyPerk = PerkType.BatonProficiency;
                    proficiencySkill = SkillType.OneHanded;
                    receivesBackgroundBonus = background == BackgroundType.SecurityOfficer;
                    break;
                case CustomItemType.HeavyVibroblade:
                    proficiencyPerk = PerkType.HeavyVibrobladeProficiency;
                    proficiencySkill = SkillType.TwoHanded;
                    receivesBackgroundBonus = background == BackgroundType.Soldier;
                    break;
                case CustomItemType.Saberstaff:
                    proficiencyPerk = PerkType.SaberstaffProficiency;
                    proficiencySkill = SkillType.TwoHanded;
                    break;
                case CustomItemType.Polearm:
                    proficiencyPerk = PerkType.PolearmProficiency;
                    proficiencySkill = SkillType.TwoHanded;
                    break;
                case CustomItemType.TwinBlade:
                    proficiencyPerk = PerkType.TwinVibrobladeProficiency;
                    proficiencySkill = SkillType.TwinBlades;
                    receivesBackgroundBonus = background == BackgroundType.Berserker;
                    break;
                case CustomItemType.MartialArtWeapon:
                    proficiencyPerk = PerkType.MartialArtsProficiency;
                    proficiencySkill = SkillType.MartialArts;
                    receivesBackgroundBonus = background == BackgroundType.TerasKasi;
                    break;
                case CustomItemType.BlasterPistol:
                    proficiencyPerk = PerkType.BlasterPistolProficiency;
                    proficiencySkill = SkillType.Firearms;
                    receivesBackgroundBonus = background == BackgroundType.Smuggler;
                    break;
                case CustomItemType.BlasterRifle:
                    proficiencyPerk = PerkType.BlasterRifleProficiency;
                    proficiencySkill = SkillType.Firearms;
                    receivesBackgroundBonus = background == BackgroundType.Sharpshooter;
                    break;
                case CustomItemType.Throwing:
                    proficiencyPerk = PerkType.ThrowingProficiency;
                    proficiencySkill = SkillType.Throwing;
                    break;
                case CustomItemType.Lightsaber:
                    proficiencyPerk = PerkType.LightsaberProficiency;
                    proficiencySkill = SkillType.OneHanded;
                    break;
            }

            if (proficiencyPerk != PerkType.Unknown &&
                proficiencySkill != SkillType.Unknown)
            {
                perkBAB += _perk.GetPCPerkLevel(oPC, proficiencyPerk);
            }

            if (receivesBackgroundBonus)
            {
                backgroundBAB = 2;
            }

            int equipmentBAB = 0;
            for (int x = 0; x < NUM_INVENTORY_SLOTS; x++)
            {
                NWItem equipped = (_.GetItemInSlot(x, oPC.Object));

                int itemLevel = equipped.RecommendedLevel;
                SkillType equippedSkill = _item.GetSkillTypeForItem(equipped);
                int rank = _db.PCSkills.Single(s => s.PlayerID == oPC.GlobalID && s.SkillID == (int) equippedSkill).Rank;
                int delta = itemLevel - rank; // -20
                int itemBAB = equipped.BaseAttackBonus;

                if (delta >= 1) itemBAB--;
                if (delta > 0) itemBAB = itemBAB - delta / 5;

                if (itemBAB <= 0) itemBAB = 0;

                equipmentBAB += itemBAB;
            }

            return 1 + skillBAB + perkBAB + equipmentBAB + backgroundBAB; // Note: Always add 1 to BAB. 0 will cause a crash in NWNX.
        }
    }
}
