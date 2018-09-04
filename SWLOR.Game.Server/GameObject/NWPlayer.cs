using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject.Contracts;

using NWN;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.GameObject
{
    public class NWPlayer : NWCreature, INWPlayer
    {
        private readonly ICustomEffectService _customEffect;
        private readonly ISkillService _skill;
        private readonly IItemService _item;

        public NWPlayer(INWScript script, 
            INWNXCreature nwnxCreature,
            AppState state,
            ICustomEffectService customEffect,
            ISkillService skill,
            IItemService item)
            : base(script, nwnxCreature, state)
        {
            _customEffect = customEffect;
            _skill = skill;
            _item = item;
        }

        public new static NWPlayer Wrap(Object @object)
        {
            var obj = (NWPlayer)App.Resolve<INWPlayer>();
            obj.Object = @object;

            return obj;
        }
        
        public virtual bool IsBusy
        {
            get => GetLocalInt("IS_BUSY") == 1;
            set => SetLocalInt("IS_BUSY", value ? 1 : 0);
        }


        public virtual List<NWPlayer> GetPartyMembers()
        {
            List<NWPlayer> partyMembers = new List<NWPlayer>();
            Object member = _.GetFirstFactionMember(Object);
            while (_.GetIsObjectValid(member) == NWScript.TRUE)
            {
                partyMembers.Add(Wrap(member));
                member = _.GetNextFactionMember(Object);
            }

            return partyMembers;
        }

        public virtual PlayerCharacter ToEntity()
        {
            PlayerCharacter entity = new PlayerCharacter
            {
                PlayerID = GlobalID,
                CharacterName = Name,
                HitPoints = CurrentHP,
                LocationAreaTag = _.GetTag(_.GetAreaFromLocation(Location)),
                LocationX = Position.m_X,
                LocationY = Position.m_Y,
                LocationZ = Position.m_Z,
                LocationOrientation = Facing,
                CreateTimestamp = DateTime.UtcNow,
                MaxHunger = 150,
                CurrentHunger = 150,
                CurrentHungerTick = 300,
                UnallocatedSP = 5,
                NextSPResetDate = null,
                ResetTokens = 3,
                NextResetTokenReceiveDate = null,
                HPRegenerationAmount = 1,
                RegenerationTick = 20,
                RegenerationRate = 0,
                VersionNumber = 1,
                MaxFP = 0,
                CurrentFP = 0,
                CurrentFPTick = 20,
                RevivalStoneCount = 3,
                RespawnAreaTag = string.Empty,
                RespawnLocationX = 0.0f,
                RespawnLocationY = 0.0f,
                RespawnLocationZ = 0.0f,
                RespawnLocationOrientation = 0.0f,
                DateLastForcedSPReset = null,
                DateSanctuaryEnds = DateTime.UtcNow + TimeSpan.FromDays(3),
                IsSanctuaryOverrideEnabled = false,
                STRBase = RawStrength, 
                DEXBase = RawDexterity, 
                CONBase = RawConstitution, 
                INTBase = RawIntelligence, 
                WISBase = RawWisdom, 
                CHABase = RawCharisma, 
                TotalSPAcquired = 0,
                DisplayHelmet = true
            };
            
            return entity;
        }

        // Effective stats take into account player skill level versus the level of the item.
        // Penalties are applied if the difference is too wide.

        private int CalculateAdjustedValue(int baseValue, int recommendedLevel, int skillRank, int minimumValue = 1)
        {
            int adjustedValue = (int) CalculateAdjustedValue((float)baseValue, recommendedLevel, skillRank, minimumValue);
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

        public virtual int CalculateEffectiveArmorClass(NWItem ignoreItem)
        {
            int heavyRank = _skill.GetPCSkill(this, SkillType.HeavyArmor).Rank;
            int lightRank = _skill.GetPCSkill(this, SkillType.LightArmor).Rank;
            int forceRank = _skill.GetPCSkill(this, SkillType.ForceArmor).Rank;

            int ac = 0;
            for (int slot = 0; slot < NWScript.NUM_INVENTORY_SLOTS; slot++)
            {
                NWItem oItem = NWItem.Wrap(_.GetItemInSlot(slot, Object));
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
                else if(oItem.CustomItemType == CustomItemType.LightArmor &&
                    oItem.RecommendedLevel > lightRank)
                {
                    skillRankToUse = lightRank;
                }
                else if (oItem.CustomItemType == CustomItemType.ForceArmor &&
                         oItem.RecommendedLevel > forceRank)
                {
                    skillRankToUse = forceRank;
                }
                
                int itemAC = oItem.AC + oItem.CustomAC;
                itemAC = CalculateAdjustedValue(itemAC, oItem.RecommendedLevel, skillRankToUse);
                ac += itemAC;
            }
            return ac + _customEffect.CalculateEffectAC(this);
        }

        public virtual int EffectiveCastingSpeed
        {
            get
            {
                int castingSpeed = 0;

                for (int itemSlot = 0; itemSlot < NWScript.NUM_INVENTORY_SLOTS; itemSlot++)
                {
                    NWItem item = NWItem.Wrap(_.GetItemInSlot(itemSlot, Object));
                    if (!item.IsValid) continue;
                    int itemCastingSpeed = item.CastingSpeed;

                    // Penalties don't scale.
                    if (itemCastingSpeed > 0)
                    {
                        SkillType skill = _skill.GetSkillTypeForItem(item);
                        int rank = _skill.GetPCSkill(this, skill).Rank;
                        itemCastingSpeed = CalculateAdjustedValue(itemCastingSpeed, item.RecommendedLevel, rank);
                    }
                    
                    castingSpeed = castingSpeed + itemCastingSpeed;
                }

                if (castingSpeed < -99)
                    castingSpeed = -99;
                else if (castingSpeed > 99)
                    castingSpeed = 99;

                return castingSpeed;
            }
        }


        public virtual float EffectiveEnmityRate
        {
            get
            {
                float rate = 1.0f;
                for (int itemSlot = 0; itemSlot < NWScript.NUM_INVENTORY_SLOTS; itemSlot++)
                {
                    NWItem item = NWItem.Wrap(_.GetItemInSlot(itemSlot, Object));
                    if (!item.IsValid) continue;
                    SkillType skill = _skill.GetSkillTypeForItem(item);
                    int rank = _skill.GetPCSkill(this, skill).Rank;
                    float itemRate = 0.01f * item.EnmityRate;
                    itemRate = CalculateAdjustedValue(itemRate, item.RecommendedLevel, rank, 0.00f);

                    rate += itemRate;
                }

                if (rate < 0.5f) rate = 0.5f;
                else if (rate > 1.5f) rate = 1.5f;

                return rate;
            }
        }

        public virtual int EffectiveDarkAbilityBonus
        {
            get
            {
                int darkBonus = 0;
                for (int itemSlot = 0; itemSlot < NWScript.NUM_INVENTORY_SLOTS; itemSlot++)
                {
                    NWItem item = NWItem.Wrap(_.GetItemInSlot(itemSlot, Object));
                    if (!item.IsValid) continue;
                    SkillType skill = _skill.GetSkillTypeForItem(item);
                    int rank = _skill.GetPCSkill(this, skill).Rank;
                    int itemDarkBonus = CalculateAdjustedValue(item.DarkAbilityBonus, item.RecommendedLevel, rank, 0);
                    
                    darkBonus += itemDarkBonus;
                }

                return darkBonus;
            }
        }

        public virtual int EffectiveLightAbilityBonus
        {
            get
            {
                int lightBonus = 0;
                for (int itemSlot = 0; itemSlot < NWScript.NUM_INVENTORY_SLOTS; itemSlot++)
                {
                    NWItem item = NWItem.Wrap(_.GetItemInSlot(itemSlot, Object));
                    if (!item.IsValid) continue;
                    SkillType skill = _skill.GetSkillTypeForItem(item);
                    int rank = _skill.GetPCSkill(this, skill).Rank;
                    int itemLightBonus = CalculateAdjustedValue(item.LightAbilityBonus, item.RecommendedLevel, rank, 0);

                    lightBonus += itemLightBonus;
                }

                return lightBonus;
            }
        }

        public virtual int EffectiveSummoningBonus
        {
            get
            {
                int summoningBonus = 0;
                for (int itemSlot = 0; itemSlot < NWScript.NUM_INVENTORY_SLOTS; itemSlot++)
                {
                    NWItem item = NWItem.Wrap(_.GetItemInSlot(itemSlot, Object));
                    if (!item.IsValid) continue;
                    SkillType skill = _skill.GetSkillTypeForItem(item);
                    int rank = _skill.GetPCSkill(this, skill).Rank;
                    int itemSummoningBonus = CalculateAdjustedValue(item.SummoningBonus, item.RecommendedLevel, rank, 0);

                    summoningBonus += itemSummoningBonus;
                }

                return summoningBonus;
            }
        }

        public virtual int EffectiveLuckBonus
        {
            get
            {
                int luckBonus = 0;
                for (int itemSlot = 0; itemSlot < NWScript.NUM_INVENTORY_SLOTS; itemSlot++)
                {
                    NWItem item = NWItem.Wrap(_.GetItemInSlot(itemSlot, Object));
                    if (!item.IsValid) continue;
                    SkillType skill = _skill.GetSkillTypeForItem(item);
                    int rank = _skill.GetPCSkill(this, skill).Rank;
                    int itemLuckBonus = CalculateAdjustedValue(item.LuckBonus, item.RecommendedLevel, rank, 0);

                    luckBonus += itemLuckBonus;
                }

                return luckBonus;
            }
        }
        public virtual int EffectiveMeditateBonus
        {
            get
            {
                int meditateBonus = 0;
                for (int itemSlot = 0; itemSlot < NWScript.NUM_INVENTORY_SLOTS; itemSlot++)
                {
                    NWItem item = NWItem.Wrap(_.GetItemInSlot(itemSlot, Object));
                    if (!item.IsValid) continue;
                    SkillType skill = _skill.GetSkillTypeForItem(item);
                    int rank = _skill.GetPCSkill(this, skill).Rank;
                    int itemMeditateBonus = CalculateAdjustedValue(item.MeditateBonus, item.RecommendedLevel, rank, 0);

                    meditateBonus += itemMeditateBonus;
                }

                return meditateBonus;
            }
        }

        public virtual int EffectiveFirstAidBonus
        {
            get
            {
                int firstAidBonus = 0;
                for (int itemSlot = 0; itemSlot < NWScript.NUM_INVENTORY_SLOTS; itemSlot++)
                {
                    NWItem item = NWItem.Wrap(_.GetItemInSlot(itemSlot, Object));
                    if (!item.IsValid) continue;
                    SkillType skill = _skill.GetSkillTypeForItem(item);
                    int rank = _skill.GetPCSkill(this, skill).Rank;
                    int itemFirstAidBonus = CalculateAdjustedValue(item.FirstAidBonus, item.RecommendedLevel, rank, 0);

                    firstAidBonus += itemFirstAidBonus;
                }

                return firstAidBonus;
            }
        }

        public virtual int EffectiveHPRegenBonus
        {
            get
            {
                int hpRegenBonus = 0;
                for (int itemSlot = 0; itemSlot < NWScript.NUM_INVENTORY_SLOTS; itemSlot++)
                {
                    NWItem item = NWItem.Wrap(_.GetItemInSlot(itemSlot, Object));
                    if (!item.IsValid) continue;
                    SkillType skill = _skill.GetSkillTypeForItem(item);
                    int rank = _skill.GetPCSkill(this, skill).Rank;
                    int itemHPRegenBonus = CalculateAdjustedValue(item.HPRegenBonus, item.RecommendedLevel, rank, 0);

                    hpRegenBonus += itemHPRegenBonus;
                }

                return hpRegenBonus;
            }
        }

        public virtual int EffectiveFPRegenBonus
        {
            get
            {
                int fpRegenBonus = 0;
                for (int itemSlot = 0; itemSlot < NWScript.NUM_INVENTORY_SLOTS; itemSlot++)
                {
                    NWItem item = NWItem.Wrap(_.GetItemInSlot(itemSlot, Object));
                    if (!item.IsValid) continue;
                    SkillType skill = _skill.GetSkillTypeForItem(item);
                    int rank = _skill.GetPCSkill(this, skill).Rank;
                    int itemFPRegenBonus = CalculateAdjustedValue(item.FPRegenBonus, item.RecommendedLevel, rank, 0);

                    fpRegenBonus += itemFPRegenBonus;
                }

                return fpRegenBonus;
            }
        }

        public virtual int EffectiveWeaponsmithBonus
        {
            get
            {
                int weaponsmithBonus = 0;
                for (int itemSlot = 0; itemSlot < NWScript.NUM_INVENTORY_SLOTS; itemSlot++)
                {
                    NWItem item = NWItem.Wrap(_.GetItemInSlot(itemSlot, Object));
                    if (!item.IsValid) continue;
                    SkillType skill = _skill.GetSkillTypeForItem(item);
                    int rank = _skill.GetPCSkill(this, skill).Rank;
                    int itemWeaponsmithBonus = CalculateAdjustedValue(item.CraftBonusWeaponsmith, item.RecommendedLevel, rank, 0);

                    weaponsmithBonus += itemWeaponsmithBonus;
                }

                return weaponsmithBonus;
            }
        }
        public virtual int EffectiveCookingBonus
        {
            get
            {
                int cookingBonus = 0;
                for (int itemSlot = 0; itemSlot < NWScript.NUM_INVENTORY_SLOTS; itemSlot++)
                {
                    NWItem item = NWItem.Wrap(_.GetItemInSlot(itemSlot, Object));
                    if (!item.IsValid) continue;
                    SkillType skill = _skill.GetSkillTypeForItem(item);
                    int rank = _skill.GetPCSkill(this, skill).Rank;
                    int itemCookingBonus = CalculateAdjustedValue(item.CraftBonusCooking, item.RecommendedLevel, rank, 0);

                    cookingBonus += itemCookingBonus;
                }

                return cookingBonus;
            }
        }
        public virtual int EffectiveEngineeringBonus
        {
            get
            {
                int engineeringBonus = 0;
                for (int itemSlot = 0; itemSlot < NWScript.NUM_INVENTORY_SLOTS; itemSlot++)
                {
                    NWItem item = NWItem.Wrap(_.GetItemInSlot(itemSlot, Object));
                    SkillType skill = _skill.GetSkillTypeForItem(item);
                    int rank = _skill.GetPCSkill(this, skill).Rank;
                    int itemEngineeringBonus = CalculateAdjustedValue(item.CraftBonusEngineering, item.RecommendedLevel, rank, 0);

                    engineeringBonus += itemEngineeringBonus;
                }

                return engineeringBonus;
            }
        }
        public virtual int EffectiveFabricationBonus
        {
            get
            {
                int fabricationBonus = 0;
                for (int itemSlot = 0; itemSlot < NWScript.NUM_INVENTORY_SLOTS; itemSlot++)
                {
                    NWItem item = NWItem.Wrap(_.GetItemInSlot(itemSlot, Object));
                    SkillType skill = _skill.GetSkillTypeForItem(item);
                    int rank = _skill.GetPCSkill(this, skill).Rank;
                    int itemFabricationBonus = CalculateAdjustedValue(item.CraftBonusFabrication, item.RecommendedLevel, rank, 0);

                    fabricationBonus += itemFabricationBonus;
                }

                return fabricationBonus;
            }
        }
        public virtual int EffectiveArmorsmithBonus
        {
            get
            {
                int armorsmithBonus = 0;
                for (int itemSlot = 0; itemSlot < NWScript.NUM_INVENTORY_SLOTS; itemSlot++)
                {
                    NWItem item = NWItem.Wrap(_.GetItemInSlot(itemSlot, Object));
                    if (!item.IsValid) continue;
                    SkillType skill = _skill.GetSkillTypeForItem(item);
                    int rank = _skill.GetPCSkill(this, skill).Rank;
                    int itemArmorsmithBonus = CalculateAdjustedValue(item.CraftBonusArmorsmith, item.RecommendedLevel, rank, 0);

                    armorsmithBonus += itemArmorsmithBonus;
                }

                return armorsmithBonus;
            }
        }

        public virtual int EffectiveSneakAttackBonus
        {
            get
            {
                int sneakAttackBonus = 0;
                for (int itemSlot = 0; itemSlot < NWScript.NUM_INVENTORY_SLOTS; itemSlot++)
                {
                    NWItem item = NWItem.Wrap(_.GetItemInSlot(itemSlot, Object));
                    if (!item.IsValid) continue;
                    SkillType skill = _skill.GetSkillTypeForItem(item);
                    int rank = _skill.GetPCSkill(this, skill).Rank;
                    int itemSneakAttackBonus = CalculateAdjustedValue(item.SneakAttackBonus, item.RecommendedLevel, rank, 0);

                    sneakAttackBonus += itemSneakAttackBonus;
                }

                return sneakAttackBonus;
            }
        }



    }
}
