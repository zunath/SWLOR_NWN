using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Legacy.Enumeration;
using SkillType = SWLOR.Game.Server.Legacy.Enumeration.SkillType;

namespace SWLOR.Game.Server.Legacy.GameObject
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
                for (var ip = NWScript.GetFirstItemProperty(Object); NWScript.GetIsItemPropertyValid(ip) == true; ip = NWScript.GetNextItemProperty(Object))
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
                var armorClass = GetItemPropertyValueAndRemove(ItemPropertyType.ArmorClass);
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
                var itemType = (int)ItemPropertyType.Invalid; // GetItemPropertyValueAndRemove(ItemPropertyType.ItemType);
                var storedItemType = (CustomItemType)NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_TYPE");

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
                var type = GetCustomItemType(this);
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
                BaseItem.Rifle,
                BaseItem.Cannon
            };

            BaseItem[] blasterPistol =
            {
                BaseItem.Pistol,
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
                var recommendedLevel = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (recommendedLevel <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_TYPE_RECOMMENDED_LEVEL");
                RecommendedLevel = recommendedLevel;
                return recommendedLevel;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_TYPE_RECOMMENDED_LEVEL", ItemPropertyType.Invalid, value);
        }

        public virtual int LevelIncrease
        {
            get
            {
                var levelIncrease = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (levelIncrease <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_TYPE_LEVEL_INCREASE");
                LevelIncrease = levelIncrease;
                return levelIncrease;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_TYPE_LEVEL_INCREASE", ItemPropertyType.Invalid, value);
        }

        public virtual int HarvestingBonus
        {
            get
            {
                var craftBonus = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (craftBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_HARVESTING_BONUS");
                HarvestingBonus = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_HARVESTING_BONUS", ItemPropertyType.Invalid, value);
        }

        public virtual int PilotingBonus
        {
            get
            {
                var craftBonus = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (craftBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_PILOTING_BONUS");
                PilotingBonus = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_PILOTING_BONUS", ItemPropertyType.Invalid, value);
        }

        public virtual int ScanningBonus
        {
            get
            {
                var craftBonus = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (craftBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_SCANNING_BONUS");
                ScanningBonus = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_SCANNING_BONUS", ItemPropertyType.Invalid, value);
        }

        public virtual int ScavengingBonus
        {
            get
            {
                var craftBonus = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (craftBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_SCAVENGING_BONUS");
                ScavengingBonus = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_SCAVENGING_BONUS", ItemPropertyType.Invalid, value);
        }

        public virtual int CooldownRecovery
        {
            get
            {
                var cooldownRecovery = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                // Variable name is kept as-is for backwards compatibility.
                if (cooldownRecovery <= 0) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CASTING_SPEED");

                if (cooldownRecovery <= 99) cooldownRecovery = -cooldownRecovery;
                else cooldownRecovery = cooldownRecovery - 99;
                CooldownRecovery = cooldownRecovery;
                return cooldownRecovery;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_CASTING_SPEED", ItemPropertyType.Invalid, value);
        }

        public virtual int CraftBonusArmorsmith
        {
            get
            {
                var craftBonus = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (craftBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_ARMORSMITH");
                CraftBonusArmorsmith = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_ARMORSMITH", ItemPropertyType.Invalid, value);
        }
        public virtual int CraftBonusWeaponsmith
        {
            get
            {
                var craftBonus = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (craftBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_WEAPONSMITH");
                CraftBonusWeaponsmith = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_WEAPONSMITH", ItemPropertyType.Invalid, value);
        }
        public virtual int CraftBonusCooking
        {
            get
            {
                var craftBonus = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (craftBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_COOKING");
                CraftBonusCooking = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_COOKING", ItemPropertyType.Invalid, value);
        }
        public virtual int CraftBonusEngineering
        {
            get
            {
                var craftBonus = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (craftBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_ENGINEERING");
                CraftBonusEngineering = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_ENGINEERING", ItemPropertyType.Invalid, value);
        }
        public virtual int CraftBonusFabrication
        {
            get
            {
                var craftBonus = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (craftBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_FABRICATION");
                CraftBonusFabrication = craftBonus;
                return craftBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_FABRICATION", ItemPropertyType.Invalid, value);
        }
        public virtual SkillType AssociatedSkillType
        {
            get
            {
                var skillType = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (skillType <= -1) return (SkillType)NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_ASSOCIATED_SKILL_ID");
                AssociatedSkillType = (SkillType)skillType;
                return (SkillType)skillType;
            }
            set 
            {
                GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                NWScript.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_ASSOCIATED_SKILL_ID", (int)value);
            }
        }
        public virtual int CraftTierLevel
        {
            get
            {
                var craftTier = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (craftTier <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_TIER_LEVEL");
                CraftTierLevel = craftTier;
                return craftTier;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_CRAFT_TIER_LEVEL", ItemPropertyType.Invalid, value);
        }
        public virtual int HPBonus
        {
            get
            {
                var hpBonus = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (hpBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_HP_BONUS");
                HPBonus = hpBonus;
                return hpBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_HP_BONUS", ItemPropertyType.Invalid, value);
        }
        public virtual int FPBonus
        {
            get
            {
                var fpBonus = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (fpBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_FP_BONUS");
                FPBonus = fpBonus;
                return fpBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_FP_BONUS", ItemPropertyType.Invalid, value);
        }

        public virtual int EnmityRate
        {
            get
            {
                var enmityRate = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (enmityRate <= 0) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_ENMITY_RATE");

                if (enmityRate <= 50) enmityRate = -enmityRate;
                else enmityRate = enmityRate - 50;
                EnmityRate = enmityRate;
                return enmityRate;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_ENMITY_RATE", ItemPropertyType.Invalid, value);
        }

        
        public virtual int LuckBonus
        {
            get
            {
                var luckBonus = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (luckBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_LUCK_BONUS");
                LuckBonus = luckBonus;
                return luckBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_LUCK_BONUS", ItemPropertyType.Invalid, value);
        }
        public virtual int MeditateBonus
        {
            get
            {
                var meditateBonus = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (meditateBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_MEDITATE_BONUS");
                MeditateBonus = meditateBonus;
                return meditateBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_MEDITATE_BONUS", ItemPropertyType.Invalid, value);
        }
        public virtual int RestBonus
        {
            get
            {
                var restBonus = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (restBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_REST_BONUS");
                RestBonus = restBonus;
                return restBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_REST_BONUS", ItemPropertyType.Invalid, value);
        }
        public virtual int MedicineBonus
        {
            get
            {
                var medicineBonus = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (medicineBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_MEDICINE_BONUS");
                MedicineBonus = medicineBonus;
                return medicineBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_MEDICINE_BONUS", ItemPropertyType.Invalid, value);
        }
        public virtual int HPRegenBonus
        {
            get
            {
                var hpRegenBonus = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (hpRegenBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_HP_REGEN_BONUS");
                HPRegenBonus = hpRegenBonus;
                return hpRegenBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_HP_REGEN_BONUS", ItemPropertyType.Invalid, value);
        }
        public virtual int FPRegenBonus
        {
            get
            {
                var fpRegenBonus = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (fpRegenBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_FP_REGEN_BONUS");
                FPRegenBonus = fpRegenBonus;
                return fpRegenBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_FP_REGEN_BONUS", ItemPropertyType.Invalid, value);
        }
        public virtual int BaseAttackBonus
        {
            get
            {
                var baseAttackBonus = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (baseAttackBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_BASE_ATTACK_BONUS");
                BaseAttackBonus = baseAttackBonus;
                return baseAttackBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_BASE_ATTACK_BONUS", ItemPropertyType.Invalid, value);
        }
        public virtual int StructureBonus
        {
            get
            {
                var structureBonus = GetItemPropertyValueAndRemove(ItemPropertyType.Invalid);
                if (structureBonus <= -1) return NWScript.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_STRUCTURE_BONUS");
                StructureBonus = structureBonus;
                return structureBonus;
            }
            set => SetCustomProperty("CUSTOM_ITEM_PROPERTY_STRUCTURE_BONUS", ItemPropertyType.Invalid, value);
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
            var stackSize = NWScript.GetItemStackSize(Object);
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
            var ip = NWScript.GetFirstItemProperty(Object);
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
            var lhsNull = lhs is null;
            var rhsNull = rhs is null;
            return (lhsNull && rhsNull) || (!lhsNull && !rhsNull && lhs.Object == rhs.Object);
        }

        public static bool operator !=(NWItem lhs, NWItem rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object o)
        {
            var other = o as NWItem;
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
