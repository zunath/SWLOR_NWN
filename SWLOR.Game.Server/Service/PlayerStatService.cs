
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
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
        private readonly IDataService _data;
        private readonly IPerkService _perk;
        private readonly INWNXCreature _nwnxCreature;

        public PlayerStatService(
            INWScript script,
            ICustomEffectService customEffect,
            INWNXCreature nwnxCreature,
            IItemService item,
            IDataService data,
            IPerkService perk)
        {
            _ = script;
            _customEffect = customEffect;
            _item = item;
            _data = data;
            _perk = perk;
            _nwnxCreature = nwnxCreature;
        }

        public void ApplyStatChanges(NWPlayer player, NWItem ignoreItem, bool isInitialization = false)
        {
            if (!player.IsPlayer) return;
            if (!player.IsInitializedAsPlayer) return;

            Player pcEntity = _data.Get<Player>(player.GlobalID);
            List<PCSkill> skills = _data.Where<PCSkill>(x => x.PlayerID == player.GlobalID && x.Rank > 0).ToList();
            EffectiveItemStats itemBonuses = GetPlayerItemEffectiveStats(player, ignoreItem);
            
            float strBonus = 0.0f;
            float dexBonus = 0.0f;
            float conBonus = 0.0f;
            float intBonus = 0.0f;
            float wisBonus = 0.0f;
            float chaBonus = 0.0f;

            using (new Profiler("PlayerStatService::ApplyStatChanges::AttributeApplication"))
            {
                foreach (PCSkill pcSkill in skills)
                {
                    Skill skill = _data.Get<Skill>(pcSkill.SkillID);
                    CustomAttribute primary = (CustomAttribute) skill.Primary;
                    CustomAttribute secondary = (CustomAttribute) skill.Secondary;
                    CustomAttribute tertiary = (CustomAttribute) skill.Tertiary;

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
            _nwnxCreature.SetRawAbilityScore(player, ABILITY_STRENGTH, (int) strBonus + pcEntity.STRBase);
            _nwnxCreature.SetRawAbilityScore(player, ABILITY_DEXTERITY, (int) dexBonus + pcEntity.DEXBase);
            _nwnxCreature.SetRawAbilityScore(player, ABILITY_CONSTITUTION, (int) conBonus + pcEntity.CONBase);
            _nwnxCreature.SetRawAbilityScore(player, ABILITY_INTELLIGENCE, (int) intBonus + pcEntity.INTBase);
            _nwnxCreature.SetRawAbilityScore(player, ABILITY_WISDOM, (int) wisBonus + pcEntity.WISBase);
            _nwnxCreature.SetRawAbilityScore(player, ABILITY_CHARISMA, (int) chaBonus + pcEntity.CHABase);

            // Apply AC

            using (new Profiler("PlayerStatService::ApplyStatChanges::CalcAC"))
            {
                int ac = EffectiveArmorClass(itemBonuses, player);
                _nwnxCreature.SetBaseAC(player, ac);
            }


            // Apply BAB

            using (new Profiler("PlayerStatService::ApplyStatChanges::CalcBAB"))
            {
                int bab = CalculateBAB(player, ignoreItem, itemBonuses);
                _nwnxCreature.SetBaseAttackBonus(player, bab);
            }

            // Apply HP

            using (new Profiler("PlayerStatService::ApplyStatChanges::CalcHP"))
            {
                int hp = EffectiveMaxHitPoints(player, itemBonuses);
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
            }



            if (player.CurrentHP > player.MaxHP)
            {
                int amount = player.CurrentHP - player.MaxHP;
                Effect damage = _.EffectDamage(amount);
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, damage, player.Object);
            }

            // Apply FP
            using (new Profiler("PlayerStatService::ApplyStatChanges::CalcFP"))
            {
                pcEntity.MaxFP = EffectiveMaxFP(player, itemBonuses);

                if (isInitialization)
                {
                    pcEntity.CurrentFP = pcEntity.MaxFP;
                }

                _data.SubmitDataChange(pcEntity, DatabaseActionType.Update);
            }
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

        private int EffectiveMaxHitPoints(NWPlayer player, EffectiveItemStats stats)
        {
            int hp = 25 + player.ConstitutionModifier * 5;
            float effectPercentBonus = _customEffect.CalculateEffectHPBonusPercent(player);
            
            hp += _perk.GetPCPerkLevel(player, PerkType.Health) * 5;
            hp += stats.HP;
            hp = hp + (int)(hp * effectPercentBonus);

            if (hp > 1275) hp = 1275;
            if (hp < 20) hp = 20;

            return hp;
        }

        private int EffectiveMaxFP(NWPlayer player, EffectiveItemStats stats)
        {
            int fp = 20;
            fp += (player.IntelligenceModifier + player.WisdomModifier + player.CharismaModifier) * 5;
            fp += _perk.GetPCPerkLevel(player, PerkType.FP) * 5;
            fp += stats.FP;

            if (fp < 0) fp = 0;

            return fp;
        }

        private int EffectiveArmorClass(EffectiveItemStats stats, NWPlayer player)
        {
            int baseAC = stats.AC + _customEffect.CalculateEffectAC(player);
            int totalAC = _.GetAC(player) - baseAC;
            
            // Shield Oath and Precision Targeting affect a percentage of the TOTAL armor class on a creature.
            var stance = _customEffect.GetCurrentStanceType(player);
            if (stance == CustomEffectType.ShieldOath)
            {
                int bonus = (int) (totalAC * 0.2f);
                baseAC = baseAC + bonus;
            }
            else if (stance == CustomEffectType.PrecisionTargeting)
            {
                int penalty = (int)(totalAC * 0.3f);
                baseAC = baseAC - penalty;
            }
            
            return baseAC;
        }
        
        public EffectiveItemStats GetPlayerItemEffectiveStats(NWPlayer player, NWItem ignoreItem = null)
        {
            using (new Profiler("PlayerStatService::ApplyStatChanges::GetPlayerItemEffectiveStats"))
            {
                int[] armorSkills =
                {
                    (int)SkillType.HeavyArmor, 
                    (int)SkillType.LightArmor, 
                    (int)SkillType.ForceArmor
                };
                var pcArmorSkills = _data.Where<PCSkill>(x => x.PlayerID == player.GlobalID && 
                                                              armorSkills.Contains(x.SkillID)).ToList();

                int heavyRank = pcArmorSkills.Single(x => x.SkillID == (int)SkillType.HeavyArmor).Rank;
                int lightRank = pcArmorSkills.Single(x => x.SkillID == (int)SkillType.LightArmor).Rank;
                int forceRank = pcArmorSkills.Single(x => x.SkillID == (int)SkillType.ForceArmor).Rank;

                EffectiveItemStats stats = new EffectiveItemStats();
                stats.EnmityRate = 1.0f;

                using (new Profiler("PlayerStatService::ApplyStatChanges::GetPlayerItemEffectiveStats::ItemLoop"))
                {

                    for (int itemSlot = 0; itemSlot < NUM_INVENTORY_SLOTS; itemSlot++)
                    {
                        NWItem item = _.GetItemInSlot(itemSlot, player);
                        if (!item.IsValid || item.Equals(ignoreItem)) continue;
                        SkillType skill = _item.GetSkillTypeForItem(item);
                        int rank = _data.Single<PCSkill>(x => x.PlayerID == player.GlobalID && x.SkillID == (int) skill).Rank;

                        using(new Profiler("PlayerStatService::ApplyStatChanges::GetPlayerItemEffectiveStats::ItemLoop::StatAdjustments"))
                        {
                            // Only scale casting speed if it's a bonus. Penalties remain regardless of skill level difference.
                            if (item.CastingSpeed > 0)
                            {
                                stats.CastingSpeed += CalculateAdjustedValue(item.CastingSpeed, item.RecommendedLevel, rank, 1);
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
                            stats.HP += CalculateAdjustedValue(item.HPBonus, item.RecommendedLevel, rank, 0);
                            stats.FP += CalculateAdjustedValue(item.FPBonus, item.RecommendedLevel, rank, 0);

                        }

                        // Calculate base attack bonus
                        int itemLevel = item.RecommendedLevel;
                        int delta = itemLevel - rank;
                        int itemBAB = item.BaseAttackBonus;
                        if (delta >= 1) itemBAB--;
                        if (delta > 0) itemBAB = itemBAB - delta / 5;

                        if (itemBAB <= 0) itemBAB = 0;
                        stats.BAB += itemBAB;

                        // Calculate AC
                        if (ItemService.ArmorBaseItemTypes.Contains(item.BaseItemType))
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
                            else continue;

                            int itemAC = item.CustomAC;
                            itemAC = CalculateAdjustedValue(itemAC, item.RecommendedLevel, skillRankToUse, 0);
                            stats.AC += itemAC;
                        }
                    }
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
            
        }


        public float EffectiveResidencyBonus(NWPlayer player)
        {
            var dbPlayer = _data.Get<Player>(player.GlobalID);

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
                var primaryResidencePCBase = _data.Get<PCBase>(dbPlayer.PrimaryResidencePCBaseID);
                structures = _data.Where<PCBaseStructure>(x => x.PCBaseID == primaryResidencePCBase.ID).ToList();
                
            }
            // Buildings - Get the building's PCBaseID and then grab its children
            else
            {
                var primaryResidenceStructure = _data.Get<PCBaseStructure>(dbPlayer.PrimaryResidencePCBaseStructureID);
                structures = _data.Where<PCBaseStructure>(x => x.ParentPCBaseStructureID == primaryResidenceStructure.ParentPCBaseStructureID).ToList();
            }

            var atmoStructures = structures.Where(x =>
            {
                var baseStructure = _data.Get<BaseStructure>(x.BaseStructureID);
                return baseStructure.HasAtmosphere;
            }).ToList();
            
            float bonus = atmoStructures.Sum(x => (x.StructureBonus * 0.02f) + 0.02f);

            if (bonus >= 0.5f) bonus = 0.5f;
            return bonus;
        }
        
        private int CalculateBAB(NWPlayer oPC, NWItem ignoreItem, EffectiveItemStats stats)
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
            PCSkill skill = _data.Single<PCSkill>(x => x.PlayerID == oPC.GlobalID && x.SkillID == weaponSkillID);
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
                    proficiencySkill = SkillType.Lightsaber;
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
                    receivesBackgroundBonus = background == BackgroundType.Sharpshooter || background == BackgroundType.Mandalorian;
                    break;
                case CustomItemType.Throwing:
                    proficiencyPerk = PerkType.ThrowingProficiency;
                    proficiencySkill = SkillType.Throwing;
                    break;
                case CustomItemType.Lightsaber:
                    proficiencyPerk = PerkType.LightsaberProficiency;
                    proficiencySkill = SkillType.Lightsaber;
                    break;
            }

            if (proficiencyPerk != PerkType.Unknown &&
                proficiencySkill != SkillType.Unknown)
            {
                perkBAB += _perk.GetPCPerkLevel(oPC, proficiencyPerk);
            }

            if (receivesBackgroundBonus)
            {
                backgroundBAB = background == BackgroundType.Mandalorian ? 1 : 2;
            }
            
            return 1 + skillBAB + perkBAB + stats.BAB + backgroundBAB; // Note: Always add 1 to BAB. 0 will cause a crash in NWNX.
        }
    }
}
