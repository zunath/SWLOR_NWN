using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject.Contracts;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.GameObject
{
    public class NWItem : NWObject, INWItem
    {
        private readonly IDurabilityService _durability;
        private readonly IItemService _item;

        public NWItem(INWScript script,
            IDurabilityService durability,
            IItemService item,
            AppState state)
            : base(script, state)
        {
            _durability = durability;
            _item = item;
        }

        public new static NWItem Wrap(Object @object)
        {
            var obj = (NWItem)App.Resolve<INWItem>();
            obj.Object = @object;
            
            return obj;
        }

        public virtual NWCreature Possessor => NWCreature.Wrap(_.GetItemPossessor(Object));

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

        public virtual float MaxDurability
        {
            get
            {
                int maxDurability = GetItemPropertyValueAndRemove((int) CustomItemPropertyType.MaxDurability);
                if (maxDurability <= -1) return _durability.GetMaxDurability(this);
                MaxDurability = maxDurability;
                return maxDurability;
            }
            set
            {
                _.SetLocalInt(Object, "DURABILITY_OVERRIDE", TRUE);
                _durability.SetMaxDurability(this, value);
            }
        }

        public virtual float Durability
        {
            get
            {
                int durability = GetItemPropertyValueAndRemove((int) CustomItemPropertyType.Durability);
                if(durability <= -1) return _durability.GetDurability(this);
                Durability = durability;
                return durability;
            }
            set
            {
                _.SetLocalInt(Object, "DURABILITY_OVERRIDE", TRUE); 
                _durability.SetDurability(this, value);
            }
        }
        
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
                CustomItemType type = _item.GetCustomItemType(this);
                CustomItemType = type;
                
                return type;
            }
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_TYPE", (int)value);
        }

        public virtual int RecommendedLevel
        {
            get
            {
                int recommendedLevel = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.RecommendedLevel);
                if(recommendedLevel <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_TYPE_RECOMMENDED_LEVEL");
                RecommendedLevel = recommendedLevel;
                return recommendedLevel;
            }
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_TYPE_RECOMMENDED_LEVEL", value);
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
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_TYPE_LEVEL_INCREASE", value);
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
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_HARVESTING_BONUS", value);
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
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_SCANNING_BONUS", value);
        }

        public virtual int CastingSpeed
        {
            get
            {
                int castingSpeed = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.CastingSpeed);
                if (castingSpeed <= 0) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CASTING_SPEED");
                
                if (castingSpeed <= 99) castingSpeed = -castingSpeed;
                else castingSpeed = castingSpeed - 99;
                CastingSpeed = castingSpeed;
                return castingSpeed;
            }
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CASTING_SPEED", value);
        }
        
        public virtual int CraftBonusArmorsmith
        {
            get
            {
                int craftBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.CraftBonusArmorsmith);
                if(craftBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_ARMORSMITH");
                CraftBonusArmorsmith = craftBonus;
                return craftBonus;
            }
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_ARMORSMITH", value);
        }
        public virtual int CraftBonusWeaponsmith
        {
            get
            {
                int craftBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.CraftBonusWeaponsmith);
                if(craftBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_WEAPONSMITH");
                CraftBonusWeaponsmith = craftBonus;
                return craftBonus;
            }
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_WEAPONSMITH", value);
        }
        public virtual int CraftBonusCooking
        {
            get
            {
                int craftBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.CraftBonusCooking);
                if(craftBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_COOKING");
                CraftBonusCooking = craftBonus;
                return craftBonus;
            }
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_COOKING", value);
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
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_ENGINEERING", value);
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
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_BONUS_FABRICATION", value);
        }
        public virtual SkillType AssociatedSkillType
        {
            get
            {
                int skillType = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.AssociatedSkill);
                if(skillType <= -1) return (SkillType)_.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_ASSOCIATED_SKILL_ID");
                AssociatedSkillType = (SkillType)skillType;
                return (SkillType)skillType;
            }
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_ASSOCIATED_SKILL_ID", (int)value);
        }
        public virtual int CraftTierLevel
        {
            get
            {
                int craftTier = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.CraftTierLevel);
                if(craftTier <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_TIER_LEVEL");
                CraftTierLevel = craftTier;
                return craftTier;
            }
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_CRAFT_TIER_LEVEL", value);
        }
        public virtual int HPBonus
        {
            get
            {
                int hpBonus = GetItemPropertyValueAndRemove((int) CustomItemPropertyType.HPBonus);
                if(hpBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_HP_BONUS");
                HPBonus = hpBonus;
                return hpBonus;
            }
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_HP_BONUS", value);
        }
        public virtual int FPBonus
        {
            get
            {
                int fpBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.FPBonus);
                if(fpBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_FP_BONUS");
                FPBonus = fpBonus;
                return fpBonus;
            }
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_FP_BONUS", value);
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
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_ENMITY_RATE", value);
        }

        public virtual int DarkAbilityBonus
        {
            get
            {
                int darkAbilityBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.DarkAbilityBonus);
                if(darkAbilityBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_DARK_ABILITY_BONUS");
                DarkAbilityBonus = darkAbilityBonus;
                return darkAbilityBonus;
            }
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_DARK_ABILITY_BONUS", value);
        }
        public virtual int LightAbilityBonus
        {
            get
            {
                int lightAbilityBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.LightAbilityBonus);
                if(lightAbilityBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_LIGHT_ABILITY_BONUS");
                LightAbilityBonus = lightAbilityBonus;
                return lightAbilityBonus;
            }
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_LIGHT_ABILITY_BONUS", value);
        }
        public virtual int SummoningBonus
        {
            get
            {
                int summoningBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.SummoningBonus);
                if(summoningBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_SUMMONING_BONUS");
                SummoningBonus = summoningBonus;
                return summoningBonus;
            }
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_SUMMONING_BONUS", value);
        }
        public virtual int LuckBonus
        {
            get
            {
                int luckBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.LuckBonus);
                if(luckBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_LUCK_BONUS");
                LuckBonus = luckBonus;
                return luckBonus;
            }
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_LUCK_BONUS", value);
        }
        public virtual int MeditateBonus
        {
            get
            {
                int meditateBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.MeditateBonus);
                if(meditateBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_MEDITATE_BONUS");
                MeditateBonus = meditateBonus;
                return meditateBonus;
            }
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_MEDITATE_BONUS", value);
        }
        public virtual int FirstAidBonus
        {
            get
            {
                int firstAidBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.FirstAidBonus);
                if(firstAidBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_FIRST_AID_BONUS");
                FirstAidBonus = firstAidBonus;
                return firstAidBonus;
            }
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_FIRST_AID_BONUS", value);
        }
        public virtual int HPRegenBonus
        {
            get
            {
                int hpRegenBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.HPRegenBonus);
                if(hpRegenBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_HP_REGEN_BONUS");
                HPRegenBonus = hpRegenBonus;
                return hpRegenBonus;
            }
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_HP_REGEN_BONUS", value);
        }
        public virtual int FPRegenBonus
        {
            get
            {
                int fpRegenBonus = GetItemPropertyValueAndRemove((int)CustomItemPropertyType.FPRegenBonus);
                if(fpRegenBonus <= -1) return _.GetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_FP_REGEN_BONUS");
                FPRegenBonus = fpRegenBonus;
                return fpRegenBonus;
            }
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_FP_REGEN_BONUS", value);
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
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_BASE_ATTACK_BONUS", value);
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
            set => _.SetLocalInt(Object, "CUSTOM_ITEM_PROPERTY_STRUCTURE_BONUS", value);
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


        private int GetItemPropertyValueAndRemove(int itemPropertyID)
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


        public static implicit operator Object(NWItem o)
        {
            return o.Object;
        }
        public static implicit operator NWItem(Object o)
        {
            INWScript _ = App.Resolve<INWScript>();

            return (_.GetObjectType(o) == OBJECT_TYPE_ITEM) ?
                Wrap(o) :
                throw new InvalidCastException();
        }
    }
}
