using NWN;
using SWLOR.Game.Server.Enumeration;

using System.Collections.Generic;
using System.Linq;
using static NWN._;

namespace SWLOR.Game.Server.GameObject
{
    public class NWItem : NWObject
    {
        public NWItem(NWGameObject o)
            : base(o)
        {
        }

        public virtual NWCreature Possessor => _.GetItemPossessor(Object);

        public virtual int BaseItemType => _.GetBaseItemType(Object);

        public virtual bool IsDroppable
        {
            get => _.GetDroppableFlag(Object) == 1;
            set => _.SetDroppableFlag(Object, value ? 1 : 0);
        }

        public virtual bool IsCursed
        {
            get => _.GetItemCursedFlag(Object) == 1;
            set => _.SetItemCursedFlag(Object, value ? 1 : 0);
        }

        public virtual bool IsStolen
        {
            get => _.GetStolenFlag(Object) == 1;
            set => _.SetStolenFlag(Object, value ? 1 : 0);
        }

        public virtual bool IsIdentified
        {
            get => _.GetIdentified(Object) == 1;
            set => _.SetIdentified(Object, value ? 1 : 0);
        }
        public virtual int AC => _.GetItemACValue(Object);

        public virtual int Charges
        {
            get => _.GetItemCharges(Object);
            set => _.SetItemCharges(Object, value);
        }

        public virtual int ReduceCharges(int reduceBy = 1)
        {
            Charges = Charges - reduceBy;
            if (Charges < 0) Charges = 0;
            return Charges;
        }

        public virtual int StackSize
        {
            get => _.GetItemStackSize(Object);
            set => _.SetItemStackSize(Object, value);
        }

        public virtual float Weight => _.GetWeight(Object) * 0.1f;

        public virtual IEnumerable<ItemProperty> ItemProperties
        {
            get
            {
                for (ItemProperty ip = _.GetFirstItemProperty(Object); _.GetIsItemPropertyValid(ip) == TRUE; ip = _.GetNextItemProperty(Object))
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
                int armorClass = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.ArmorClass);
                if (armorClass <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_AC");
                CustomAC = armorClass;
                return armorClass;
            }
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_AC", value);
        }
        public virtual CustomItemType CustomItemType
        {
            get
            {
                // Item property takes precedence, followed by local int on the item, 
                // followed by hard-calculating it based on base item type.
                int itemType = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.ItemType);
                CustomItemType storedItemType = (CustomItemType)_.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_TYPE");

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
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_TYPE", (int)value);
        }


        private CustomItemType GetCustomItemType(NWItem item)
        {
            if (item.GetLocalInt("LIGHTSABER") == TRUE)
            {
                return CustomItemType.Lightsaber;
            }

            int[] vibroblades =
            {
                BASE_ITEM_BASTARDSWORD,
                BASE_ITEM_LONGSWORD,
                BASE_ITEM_KATANA,
                BASE_ITEM_SCIMITAR,
                BASE_ITEM_BATTLEAXE
            };

            int[] finesseVibroblades =
            {
                BASE_ITEM_DAGGER,
                BASE_ITEM_RAPIER,
                BASE_ITEM_SHORTSWORD,
                BASE_ITEM_KUKRI,
                BASE_ITEM_SICKLE,
                BASE_ITEM_WHIP,
                BASE_ITEM_HANDAXE
            };

            int[] batons =
            {
                BASE_ITEM_CLUB,
                BASE_ITEM_LIGHTFLAIL,
                BASE_ITEM_LIGHTHAMMER,
                BASE_ITEM_LIGHTMACE,
                BASE_ITEM_MORNINGSTAR
            };

            int[] lightsabers =
            {
                CustomBaseItemType.Lightsaber
            };

            int[] heavyVibroblades =
            {
                BASE_ITEM_GREATAXE,
                BASE_ITEM_GREATSWORD,
                BASE_ITEM_DWARVENWARAXE
            };


            int[] polearms =
            {
                BASE_ITEM_HALBERD,
                BASE_ITEM_SCYTHE,
                BASE_ITEM_SHORTSPEAR,
                BASE_ITEM_TRIDENT
            };

            int[] twinVibroblades =
            {
                BASE_ITEM_DOUBLEAXE,
                BASE_ITEM_TWOBLADEDSWORD
            };

            int[] saberstaffs =
            {
                CustomBaseItemType.Saberstaff
            };

            int[] martialArts =
            {
                BASE_ITEM_GLOVES,
                BASE_ITEM_BRACER,
                BASE_ITEM_KAMA,
                BASE_ITEM_QUARTERSTAFF
            };

            int[] blasterRifles =
            {
                BASE_ITEM_LIGHTCROSSBOW,
                BASE_ITEM_HEAVYCROSSBOW
            };

            int[] blasterPistol =
            {
                BASE_ITEM_SHORTBOW,
                BASE_ITEM_LONGBOW,
            };

            int[] throwing =
            {
                BASE_ITEM_SLING,
                BASE_ITEM_DART,
                BASE_ITEM_SHURIKEN,
                BASE_ITEM_THROWINGAXE
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

        private void SetCustomProperty(string propertyName, CustomItemPropertyType type, int value)
        {
            GetItemPropertyValueAndRemove((int)type); // We're setting, so just remove it and ignore the return value.
            _.SetLocalInt(Object, propertyName, value);
        }

        public virtual int RecommendedLevel
        {
            get
            {
                int recommendedLevel = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.RecommendedLevel);
                if (recommendedLevel <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_TYPE_RECOMMENDED_LEVEL");
                RecommendedLevel = recommendedLevel;
                return recommendedLevel;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_TYPE_RECOMMENDED_LEVEL", CustomItemPropertyType.RecommendedLevel, value);
        }

        public virtual int LevelIncrease
        {
            get
            {
                int levelIncrease = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.LevelIncrease);
                if (levelIncrease <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_TYPE_LEVEL_INCREASE");
                LevelIncrease = levelIncrease;
                return levelIncrease;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_TYPE_LEVEL_INCREASE", CustomItemPropertyType.LevelIncrease, value);
        }

        public virtual int HarvestingBonus
        {
            get
            {
                int craftBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.HarvestingBonus);
                if (craftBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_HARVESTING_BONUS");
                HarvestingBonus = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_HARVESTING_BONUS", CustomItemPropertyType.HarvestingBonus, value);
        }

        public virtual int PilotingBonus
        {
            get
            {
                int craftBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.PilotingBonus);
                if (craftBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_PILOTING_BONUS");
                PilotingBonus = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_PILOTING_BONUS", CustomItemPropertyType.PilotingBonus, value);
        }

        public virtual int ScanningBonus
        {
            get
            {
                int craftBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.ScanningBonus);
                if (craftBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_SCANNING_BONUS");
                ScanningBonus = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_SCANNING_BONUS", CustomItemPropertyType.ScanningBonus, value);
        }

        public virtual int ScavengingBonus
        {
            get
            {
                int craftBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.ScavengingBonus);
                if (craftBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_SCAVENGING_BONUS");
                ScavengingBonus = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_SCAVENGING_BONUS", CustomItemPropertyType.ScavengingBonus, value);
        }

        public virtual int CooldownRecovery
        {
            get
            {
                int cooldownRecovery = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.CastingSpeed);
                // Variable name is kept as-is for backwards compatibility.
                if (cooldownRecovery <= 0) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CASTING_SPEED");

                if (cooldownRecovery <= 99) cooldownRecovery = -cooldownRecovery;
                else cooldownRecovery = cooldownRecovery - 99;
                CooldownRecovery = cooldownRecovery;
                return cooldownRecovery;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_CASTING_SPEED", CustomItemPropertyType.CastingSpeed, value);
        }

        public virtual int CraftBonusArmorsmith
        {
            get
            {
                int craftBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.CraftBonusArmorsmith);
                if (craftBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_ARMORSMITH");
                CraftBonusArmorsmith = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_ARMORSMITH", CustomItemPropertyType.CraftBonusArmorsmith, value);
        }
        public virtual int CraftBonusWeaponsmith
        {
            get
            {
                int craftBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.CraftBonusWeaponsmith);
                if (craftBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_WEAPONSMITH");
                CraftBonusWeaponsmith = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_WEAPONSMITH", CustomItemPropertyType.CraftBonusWeaponsmith, value);
        }
        public virtual int CraftBonusCooking
        {
            get
            {
                int craftBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.CraftBonusCooking);
                if (craftBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_COOKING");
                CraftBonusCooking = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_COOKING", CustomItemPropertyType.CraftBonusCooking, value);
        }
        public virtual int CraftBonusEngineering
        {
            get
            {
                int craftBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.CraftBonusEngineering);
                if (craftBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_ENGINEERING");
                CraftBonusEngineering = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_ENGINEERING", CustomItemPropertyType.CraftBonusEngineering, value);
        }
        public virtual int CraftBonusFabrication
        {
            get
            {
                int craftBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.CraftBonusFabrication);
                if (craftBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_FABRICATION");
                CraftBonusFabrication = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_FABRICATION", CustomItemPropertyType.CraftBonusFabrication, value);
        }
        public virtual SkillType AssociatedSkillType
        {
            get
            {
                int skillType = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.AssociatedSkill);
                if (skillType <= -1) return (SkillType)_.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_ASSOCIATED_SKILL_ID");
                AssociatedSkillType = (SkillType)skillType;
                return (SkillType)skillType;
            }
            set 
            {
                GetItemPropertyValueAndRemove((int)CustomItemPropertyType.AssociatedSkill);
                _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_ASSOCIATED_SKILL_ID", (int)value);
            }
        }
        public virtual int CraftTierLevel
        {
            get
            {
                int craftTier = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.CraftTierLevel);
                if (craftTier <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_TIER_LEVEL");
                CraftTierLevel = craftTier;
                return craftTier;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_CRAFT_TIER_LEVEL", CustomItemPropertyType.CraftTierLevel, value);
        }
        public virtual int HPBonus
        {
            get
            {
                int hpBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.HPBonus);
                if (hpBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_HP_BONUS");
                HPBonus = hpBonus;
                return hpBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_HP_BONUS", CustomItemPropertyType.HPBonus, value);
        }
        public virtual int FPBonus
        {
            get
            {
                int fpBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.FPBonus);
                if (fpBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_FP_BONUS");
                FPBonus = fpBonus;
                return fpBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_FP_BONUS", CustomItemPropertyType.FPBonus, value);
        }

        public virtual int EnmityRate
        {
            get
            {
                int enmityRate = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.EnmityRate);
                if (enmityRate <= 0) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_ENMITY_RATE");

                if (enmityRate <= 50) enmityRate = -enmityRate;
                else enmityRate = enmityRate - 50;
                EnmityRate = enmityRate;
                return enmityRate;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_ENMITY_RATE", CustomItemPropertyType.EnmityRate, value);
        }

        
        public virtual int LuckBonus
        {
            get
            {
                int luckBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.LuckBonus);
                if (luckBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_LUCK_BONUS");
                LuckBonus = luckBonus;
                return luckBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_LUCK_BONUS", CustomItemPropertyType.LuckBonus, value);
        }
        public virtual int MeditateBonus
        {
            get
            {
                int meditateBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.MeditateBonus);
                if (meditateBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_MEDITATE_BONUS");
                MeditateBonus = meditateBonus;
                return meditateBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_MEDITATE_BONUS", CustomItemPropertyType.MeditateBonus, value);
        }
        public virtual int RestBonus
        {
            get
            {
                int restBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.RestBonus);
                if (restBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_REST_BONUS");
                RestBonus = restBonus;
                return restBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_REST_BONUS", CustomItemPropertyType.RestBonus, value);
        }
        public virtual int MedicineBonus
        {
            get
            {
                int medicineBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.MedicineBonus);
                if (medicineBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_MEDICINE_BONUS");
                MedicineBonus = medicineBonus;
                return medicineBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_MEDICINE_BONUS", CustomItemPropertyType.MedicineBonus, value);
        }
        public virtual int HPRegenBonus
        {
            get
            {
                int hpRegenBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.HPRegenBonus);
                if (hpRegenBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_HP_REGEN_BONUS");
                HPRegenBonus = hpRegenBonus;
                return hpRegenBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_HP_REGEN_BONUS", CustomItemPropertyType.HPRegenBonus, value);
        }
        public virtual int FPRegenBonus
        {
            get
            {
                int fpRegenBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.FPRegenBonus);
                if (fpRegenBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_FP_REGEN_BONUS");
                FPRegenBonus = fpRegenBonus;
                return fpRegenBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_FP_REGEN_BONUS", CustomItemPropertyType.FPRegenBonus, value);
        }
        public virtual int BaseAttackBonus
        {
            get
            {
                int baseAttackBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.BaseAttackBonus);
                if (baseAttackBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_BASE_ATTACK_BONUS");
                BaseAttackBonus = baseAttackBonus;
                return baseAttackBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_BASE_ATTACK_BONUS", CustomItemPropertyType.BaseAttackBonus, value);
        }
        public virtual int StructureBonus
        {
            get
            {
                int structureBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.StructureBonus);
                if (structureBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_STRUCTURE_BONUS");
                StructureBonus = structureBonus;
                return structureBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_STRUCTURE_BONUS", CustomItemPropertyType.StructureBonus, value);
        }

        public virtual int SneakAttackBonus
        {
            get => _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_SNEAK_ATTACK_BONUS");
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_SNEAK_ATTACK_BONUS", value);
        }

        public virtual int DamageBonus
        {
            get => _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_DAMAGE_BONUS");
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_DAMAGE_BONUS", value);
        }

        public virtual int StrengthBonus
        {
            get => _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_STRENGTH_BONUS");
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_STRENGTH_BONUS", value);
        }
        public virtual int DexterityBonus
        {
            get => _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_DEXTERITY_BONUS");
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_DEXTERITY_BONUS", value);
        }
        public virtual int ConstitutionBonus
        {
            get => _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CONSTITUTION_BONUS");
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CONSTITUTION_BONUS", value);
        }
        public virtual int WisdomBonus
        {
            get => _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_WISDOM_BONUS");
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_WISDOM_BONUS", value);
        }
        public virtual int IntelligenceBonus
        {
            get => _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_INTELLIGENCE_BONUS");
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_INTELLIGENCE_BONUS", value);
        }
        public virtual int CharismaBonus
        {
            get => _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CHARISMA_BONUS");
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CHARISMA_BONUS", value);
        }
        public virtual int DurationBonus
        {
            get => _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_DURATION_BONUS");
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_DURATION_BONUS", value);
        }

        public virtual void ReduceItemStack()
        {
            int stackSize = _.GetItemStackSize(Object);
            if (stackSize > 1)
            {
                _.SetItemStackSize(Object, stackSize - 1);
            }
            else
            {
                _.DestroyObject(Object);
            }
        }

        public virtual bool IsRanged => _.GetWeaponRanged(Object) == 1;


        public int GetItemPropertyValueAndRemove(int itemPropertyID)
        {
            ItemProperty ip = _.GetFirstItemProperty(Object);
            while (_.GetIsItemPropertyValid(ip) == TRUE)
            {
                if (_.GetItemPropertyType(ip) == itemPropertyID)
                {
                    var result = _.GetItemPropertyCostTableValue(ip);
                    _.RemoveItemProperty(Object, ip);
                    return result;
                }

                ip = _.GetNextItemProperty(Object);
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

        public static implicit operator NWGameObject(NWItem o)
        {
            return o.Object;
        }
        public static implicit operator NWItem(NWGameObject o)
        {
            return new NWItem(o);
        }
    }
}
