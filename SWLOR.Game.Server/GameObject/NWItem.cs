using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Enumeration;

using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;

namespace SWLOR.Game.Server.GameObject
{
    public class NWItem : NWObject
    {
        public NWItem(uint o)
            : base(o)
        {
        }

        public virtual NWCreature Possessor => NWScript.GetItemPossessor(Object);

        public virtual BaseItem BaseItemType => NWScript.GetBaseItemType(Object);

        public virtual bool IsDroppable
        {
            get => NWScript.GetDroppableFlag(Object);
            set => NWScript.SetDroppableFlag(Object, value);
        }

        public virtual bool IsCursed
        {
            get => NWScript.GetItemCursedFlag(Object);
            set => NWScript.SetItemCursedFlag(Object, value);
        }

        public virtual bool IsStolen
        {
            get => NWScript.GetStolenFlag(Object);
            set => NWScript.SetStolenFlag(Object, value);
        }

        public virtual bool IsIdentified
        {
            get => NWScript.GetIdentified(Object);
            set => NWScript.SetIdentified(Object, value);
        }
        public virtual int AC => NWScript.GetItemACValue(Object);

        public virtual int Charges
        {
            get => NWScript.GetItemCharges(Object);
            set => NWScript.SetItemCharges(Object, value);
        }

        public virtual int ReduceCharges(int reduceBy = 1)
        {
            Charges = Charges - reduceBy;
            if (Charges < 0) Charges = 0;
            return Charges;
        }

        public virtual int StackSize
        {
            get => NWScript.GetItemStackSize(Object);
            set => NWScript.SetItemStackSize(Object, value);
        }

        public virtual float Weight => NWScript.GetWeight(Object) * 0.1f;

        public virtual IEnumerable<ItemProperty> ItemProperties
        {
            get
            {
                for (ItemProperty ip = NWScript.GetFirstItemProperty(Object); NWScript.GetIsItemPropertyValid(ip) == true; ip = NWScript.GetNextItemProperty(Object))
                {
                    yield return ip;
                }
            }
        }

        // For the custom item properties:
        // If a toolset-placed item property exists, mark the value and remove the item property.
        // Otherwise, return the local variable stored on the item.
        

        public virtual int CustomAC
        {
            get
            {
                int armorClass = GetItemPropertyValueAndRemove(ItemPropertyType.ArmorClass);
                if (armorClass <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_AC");
                CustomAC = armorClass;
                return armorClass;
            }
            set => NWScript.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_AC", value);
        }
        public virtual CustomItemType CustomItemType
        {
            get
            {
                // Item property takes precedence, followed by local int on the item, 
                // followed by hard-calculating it based on base item type.
                int itemType = GetItemPropertyValueAndRemove(ItemPropertyType.ItemType);
                CustomItemType storedItemType = (CustomItemType)NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_TYPE");

                if (itemType > -1)
                {
                    // Found a valid item property
                    CustomItemType = (CustomItemType)itemType;
                    return (CustomItemType)itemType;
                }

                if (storedItemType != CustomItemType.None)
                {
                    // Found a valid stored item type
                    return storedItemType;
                }

                // Attempt to get the item type by base item type.
                // Will fail for armor as there's no determining factor for light versus heavy.
                // Need to assign the local variable or give the armor the item property in order to mark it.
                CustomItemType type = GetCustomItemType(this);
                CustomItemType = type;

                return type;
            }
            set => NWScript.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_TYPE", (int)value);
        }


        private CustomItemType GetCustomItemType(NWItem item)
        {
            if (GetLocalBool("LIGHTSABER"))
            {
                return CustomItemType.Lightsaber;
            }

            BaseItem[] vibroblades =
            {
                BaseItem.BastardSword,
                BaseItem.Longsword,
                BaseItem.Katana,
                BaseItem.Scimitar,
                BaseItem.BattleAxe
            };

            BaseItem[] finesseVibroblades =
            {
                BaseItem.Dagger,
                BaseItem.Rapier,
                BaseItem.ShortSword,
                BaseItem.Kukri,
                BaseItem.Sickle,
                BaseItem.Whip,
                BaseItem.HandAxe
            };

            BaseItem[] batons =
            {
                BaseItem.LightFlail,
                BaseItem.LightHammer,
                BaseItem.LightMace,
                BaseItem.MorningStar
            };

            BaseItem[] lightsabers =
            {
                BaseItem.Lightsaber
            };

            BaseItem[] heavyVibroblades =
            {
                BaseItem.GreatAxe,
                BaseItem.GreatSword,
                BaseItem.DwarvenWarAxe
            };


            BaseItem[] polearms =
            {
                BaseItem.Halberd,
                BaseItem.Scythe,
                BaseItem.ShortSpear,
                BaseItem.Trident
            };

            BaseItem[] twinVibroblades =
            {
                BaseItem.DoubleAxe,
                BaseItem.TwoBladedSword
            };

            BaseItem[] saberstaffs =
            {
                BaseItem.Saberstaff
            };

            BaseItem[] martialArts =
            {
                BaseItem.Club,
                BaseItem.Gloves,
                BaseItem.Bracer,
                BaseItem.Kama,
                BaseItem.QuarterStaff
            };

            BaseItem[] blasterRifles =
            {
                BaseItem.LightCrossbow,
                BaseItem.HeavyCrossbow
            };

            BaseItem[] blasterPistol =
            {
                BaseItem.ShortBow,
                BaseItem.Longbow,
                BaseItem.Sling
            };

            BaseItem[] throwing =
            {
                BaseItem.Dart,
                BaseItem.Shuriken,
                BaseItem.ThrowingAxe
            };



            if (vibroblades.Contains(item.BaseItemType)) return CustomItemType.Vibroblade;
            if (finesseVibroblades.Contains(item.BaseItemType)) return CustomItemType.FinesseVibroblade;
            if (batons.Contains(item.BaseItemType)) return CustomItemType.Baton;
            if (heavyVibroblades.Contains(item.BaseItemType)) return CustomItemType.HeavyVibroblade;
            if (saberstaffs.Contains(item.BaseItemType)) return CustomItemType.Saberstaff;
            if (polearms.Contains(item.BaseItemType)) return CustomItemType.Polearm;
            if (twinVibroblades.Contains(item.BaseItemType)) return CustomItemType.TwinBlade;
            if (martialArts.Contains(item.BaseItemType)) return CustomItemType.MartialArtWeapon;
            if (blasterRifles.Contains(item.BaseItemType)) return CustomItemType.BlasterRifle;
            if (blasterPistol.Contains(item.BaseItemType)) return CustomItemType.BlasterPistol;
            if (throwing.Contains(item.BaseItemType)) return CustomItemType.Throwing;
            if (lightsabers.Contains(item.BaseItemType)) return CustomItemType.Lightsaber;
            // Armor is deliberately left out here because we don't have a way to determine the type of armor it should be
            // based on base item type.

            return CustomItemType.None;
        }

        private void SetCustomProperty(string propertyName, ItemPropertyType type, int value)
        {
            GetItemPropertyValueAndRemove(type); // We're setting, so just remove it and ignore the return value.
            NWScript.SetLocalInt(Object, propertyName, value);
        }

        public virtual int RecommendedLevel
        {
            get
            {
                int recommendedLevel = GetItemPropertyValueAndRemove(ItemPropertyType.RecommendedLevel);
                if (recommendedLevel <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_TYPE_RECOMMENDED_LEVEL");
                RecommendedLevel = recommendedLevel;
                return recommendedLevel;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_TYPE_RECOMMENDED_LEVEL", ItemPropertyType.RecommendedLevel, value);
        }

        public virtual int LevelIncrease
        {
            get
            {
                int levelIncrease = GetItemPropertyValueAndRemove(ItemPropertyType.LevelIncrease);
                if (levelIncrease <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_TYPE_LEVEL_INCREASE");
                LevelIncrease = levelIncrease;
                return levelIncrease;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_TYPE_LEVEL_INCREASE", ItemPropertyType.LevelIncrease, value);
        }

        public virtual int HarvestingBonus
        {
            get
            {
                int craftBonus = GetItemPropertyValueAndRemove(ItemPropertyType.HarvestingBonus);
                if (craftBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_HARVESTING_BONUS");
                HarvestingBonus = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_HARVESTING_BONUS", ItemPropertyType.HarvestingBonus, value);
        }

        public virtual int PilotingBonus
        {
            get
            {
                int craftBonus = GetItemPropertyValueAndRemove(ItemPropertyType.PilotingBonus);
                if (craftBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_PILOTING_BONUS");
                PilotingBonus = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_PILOTING_BONUS", ItemPropertyType.PilotingBonus, value);
        }

        public virtual int ScanningBonus
        {
            get
            {
                int craftBonus = GetItemPropertyValueAndRemove(ItemPropertyType.ScanningBonus);
                if (craftBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_SCANNING_BONUS");
                ScanningBonus = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_SCANNING_BONUS", ItemPropertyType.ScanningBonus, value);
        }

        public virtual int ScavengingBonus
        {
            get
            {
                int craftBonus = GetItemPropertyValueAndRemove(ItemPropertyType.ScavengingBonus);
                if (craftBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_SCAVENGING_BONUS");
                ScavengingBonus = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_SCAVENGING_BONUS", ItemPropertyType.ScavengingBonus, value);
        }

        public virtual int CooldownRecovery
        {
            get
            {
                int cooldownRecovery = GetItemPropertyValueAndRemove(ItemPropertyType.CastingSpeed);
                // Variable name is kept as-is for backwards compatibility.
                if (cooldownRecovery <= 0) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CASTING_SPEED");

                if (cooldownRecovery <= 99) cooldownRecovery = -cooldownRecovery;
                else cooldownRecovery = cooldownRecovery - 99;
                CooldownRecovery = cooldownRecovery;
                return cooldownRecovery;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_CASTING_SPEED", ItemPropertyType.CastingSpeed, value);
        }

        public virtual int CraftBonusArmorsmith
        {
            get
            {
                int craftBonus = GetItemPropertyValueAndRemove(ItemPropertyType.CraftBonusArmorsmith);
                if (craftBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_ARMORSMITH");
                CraftBonusArmorsmith = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_ARMORSMITH", ItemPropertyType.CraftBonusArmorsmith, value);
        }
        public virtual int CraftBonusWeaponsmith
        {
            get
            {
                int craftBonus = GetItemPropertyValueAndRemove(ItemPropertyType.CraftBonusWeaponsmith);
                if (craftBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_WEAPONSMITH");
                CraftBonusWeaponsmith = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_WEAPONSMITH", ItemPropertyType.CraftBonusWeaponsmith, value);
        }
        public virtual int CraftBonusCooking
        {
            get
            {
                int craftBonus = GetItemPropertyValueAndRemove(ItemPropertyType.CraftBonusCooking);
                if (craftBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_COOKING");
                CraftBonusCooking = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_COOKING", ItemPropertyType.CraftBonusCooking, value);
        }
        public virtual int CraftBonusEngineering
        {
            get
            {
                int craftBonus = GetItemPropertyValueAndRemove(ItemPropertyType.CraftBonusEngineering);
                if (craftBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_ENGINEERING");
                CraftBonusEngineering = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_ENGINEERING", ItemPropertyType.CraftBonusEngineering, value);
        }
        public virtual int CraftBonusFabrication
        {
            get
            {
                int craftBonus = GetItemPropertyValueAndRemove(ItemPropertyType.CraftBonusFabrication);
                if (craftBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_FABRICATION");
                CraftBonusFabrication = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_FABRICATION", ItemPropertyType.CraftBonusFabrication, value);
        }
        public virtual SkillType AssociatedSkillType
        {
            get
            {
                int skillType = GetItemPropertyValueAndRemove(ItemPropertyType.AssociatedSkill);
                if (skillType <= -1) return (SkillType)NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_ASSOCIATED_SKILL_ID");
                AssociatedSkillType = (SkillType)skillType;
                return (SkillType)skillType;
            }
            set 
            {
                GetItemPropertyValueAndRemove(ItemPropertyType.AssociatedSkill);
                NWScript.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_ASSOCIATED_SKILL_ID", (int)value);
            }
        }
        public virtual int CraftTierLevel
        {
            get
            {
                int craftTier = GetItemPropertyValueAndRemove(ItemPropertyType.CraftTierLevel);
                if (craftTier <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_TIER_LEVEL");
                CraftTierLevel = craftTier;
                return craftTier;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_CRAFT_TIER_LEVEL", ItemPropertyType.CraftTierLevel, value);
        }
        public virtual int HPBonus
        {
            get
            {
                int hpBonus = GetItemPropertyValueAndRemove(ItemPropertyType.HPBonus);
                if (hpBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_HP_BONUS");
                HPBonus = hpBonus;
                return hpBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_HP_BONUS", ItemPropertyType.HPBonus, value);
        }
        public virtual int FPBonus
        {
            get
            {
                int fpBonus = GetItemPropertyValueAndRemove(ItemPropertyType.FPBonus);
                if (fpBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_FP_BONUS");
                FPBonus = fpBonus;
                return fpBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_FP_BONUS", ItemPropertyType.FPBonus, value);
        }

        public virtual int EnmityRate
        {
            get
            {
                int enmityRate = GetItemPropertyValueAndRemove(ItemPropertyType.EnmityRate);
                if (enmityRate <= 0) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_ENMITY_RATE");

                if (enmityRate <= 50) enmityRate = -enmityRate;
                else enmityRate = enmityRate - 50;
                EnmityRate = enmityRate;
                return enmityRate;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_ENMITY_RATE", ItemPropertyType.EnmityRate, value);
        }

        
        public virtual int LuckBonus
        {
            get
            {
                int luckBonus = GetItemPropertyValueAndRemove(ItemPropertyType.LuckBonus);
                if (luckBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_LUCK_BONUS");
                LuckBonus = luckBonus;
                return luckBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_LUCK_BONUS", ItemPropertyType.LuckBonus, value);
        }
        public virtual int MeditateBonus
        {
            get
            {
                int meditateBonus = GetItemPropertyValueAndRemove(ItemPropertyType.MeditateBonus);
                if (meditateBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_MEDITATE_BONUS");
                MeditateBonus = meditateBonus;
                return meditateBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_MEDITATE_BONUS", ItemPropertyType.MeditateBonus, value);
        }
        public virtual int RestBonus
        {
            get
            {
                int restBonus = GetItemPropertyValueAndRemove(ItemPropertyType.RestBonus);
                if (restBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_REST_BONUS");
                RestBonus = restBonus;
                return restBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_REST_BONUS", ItemPropertyType.RestBonus, value);
        }
        public virtual int MedicineBonus
        {
            get
            {
                int medicineBonus = GetItemPropertyValueAndRemove(ItemPropertyType.MedicineBonus);
                if (medicineBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_MEDICINE_BONUS");
                MedicineBonus = medicineBonus;
                return medicineBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_MEDICINE_BONUS", ItemPropertyType.MedicineBonus, value);
        }
        public virtual int HPRegenBonus
        {
            get
            {
                int hpRegenBonus = GetItemPropertyValueAndRemove(ItemPropertyType.HPRegenBonus);
                if (hpRegenBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_HP_REGEN_BONUS");
                HPRegenBonus = hpRegenBonus;
                return hpRegenBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_HP_REGEN_BONUS", ItemPropertyType.HPRegenBonus, value);
        }
        public virtual int FPRegenBonus
        {
            get
            {
                int fpRegenBonus = GetItemPropertyValueAndRemove(ItemPropertyType.FPRegenBonus);
                if (fpRegenBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_FP_REGEN_BONUS");
                FPRegenBonus = fpRegenBonus;
                return fpRegenBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_FP_REGEN_BONUS", ItemPropertyType.FPRegenBonus, value);
        }
        public virtual int BaseAttackBonus
        {
            get
            {
                int baseAttackBonus = GetItemPropertyValueAndRemove(ItemPropertyType.BaseAttackBonus);
                if (baseAttackBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_BASE_ATTACK_BONUS");
                BaseAttackBonus = baseAttackBonus;
                return baseAttackBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_BASE_ATTACK_BONUS", ItemPropertyType.BaseAttackBonus, value);
        }
        public virtual int StructureBonus
        {
            get
            {
                int structureBonus = GetItemPropertyValueAndRemove(ItemPropertyType.StructureBonus);
                if (structureBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_STRUCTURE_BONUS");
                StructureBonus = structureBonus;
                return structureBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_STRUCTURE_BONUS", ItemPropertyType.StructureBonus, value);
        }

        public virtual int SneakAttackBonus
        {
            get => NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_SNEAK_ATTACK_BONUS");
            set => NWScript.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_SNEAK_ATTACK_BONUS", value);
        }

        public virtual int DamageBonus
        {
            get => NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_DAMAGE_BONUS");
            set => NWScript.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_DAMAGE_BONUS", value);
        }

        public virtual int StrengthBonus
        {
            get => NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_STRENGTH_BONUS");
            set => NWScript.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_STRENGTH_BONUS", value);
        }
        public virtual int DexterityBonus
        {
            get => NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_DEXTERITY_BONUS");
            set => NWScript.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_DEXTERITY_BONUS", value);
        }
        public virtual int ConstitutionBonus
        {
            get => NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CONSTITUTION_BONUS");
            set => NWScript.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CONSTITUTION_BONUS", value);
        }
        public virtual int WisdomBonus
        {
            get => NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_WISDOM_BONUS");
            set => NWScript.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_WISDOM_BONUS", value);
        }
        public virtual int IntelligenceBonus
        {
            get => NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_INTELLIGENCE_BONUS");
            set => NWScript.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_INTELLIGENCE_BONUS", value);
        }
        public virtual int CharismaBonus
        {
            get => NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CHARISMA_BONUS");
            set => NWScript.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CHARISMA_BONUS", value);
        }
        public virtual int DurationBonus
        {
            get => NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_DURATION_BONUS");
            set => NWScript.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_DURATION_BONUS", value);
        }

        public virtual void ReduceItemStack()
        {
            int stackSize = NWScript.GetItemStackSize(Object);
            if (stackSize > 1)
            {
                NWScript.SetItemStackSize(Object, stackSize - 1);
            }
            else
            {
                NWScript.DestroyObject(Object);
            }
        }

        public virtual bool IsRanged => NWScript.GetWeaponRanged(Object);


        public int GetItemPropertyValueAndRemove(ItemPropertyType itemPropertyID)
        {
            ItemProperty ip = NWScript.GetFirstItemProperty(Object);
            while (NWScript.GetIsItemPropertyValid(ip) == true)
            {
                if (NWScript.GetItemPropertyType(ip) == itemPropertyID)
                {
                    var result = NWScript.GetItemPropertyCostTableValue(ip);
                    NWScript.RemoveItemProperty(Object, ip);
                    return result;
                }

                ip = NWScript.GetNextItemProperty(Object);
            }

            return -1;
        }

        //
        // -- BELOW THIS POINT IS JUNK TO MAKE THE API FRIENDLIER!
        //

        public static bool operator ==(NWItem lhs, NWItem rhs)
        {
            bool lhsNull = lhs is null;
            bool rhsNull = rhs is null;
            return (lhsNull && rhsNull) || (!lhsNull && !rhsNull && lhs.Object == rhs.Object);
        }

        public static bool operator !=(NWItem lhs, NWItem rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object o)
        {
            NWItem other = o as NWItem;
            return other != null && other == this;
        }

        public override int GetHashCode()
        {
            return Object.GetHashCode();
        }

        public static implicit operator uint(NWItem o)
        {
            return o.Object;
        }
        public static implicit operator NWItem(uint o)
        {
            return new NWItem(o);
        }
    }
}
