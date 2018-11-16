using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Item
{
    public class RepairKit: IActionItem
    {
        private readonly IDurabilityService _durability;
        private readonly IPerkService _perk;
        private readonly IRandomService _random;

        public RepairKit(
            IDurabilityService durability,
            IPerkService perk,
            IRandomService random)
        {
            _durability = durability;
            _perk = perk;
            _random = random;
        }

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            SkillType skillType = GetSkillType(item);
            int tech = item.GetLocalInt("TECH_LEVEL");
            float maxDurabilityReductionPenalty = item.GetLocalFloat("MAX_DURABILITY_REDUCTION_PENALTY");
            int repairAmount = tech * 2;

            if (skillType == SkillType.Armorsmith)
                repairAmount += item.CraftBonusArmorsmith;
            else if (skillType == SkillType.Weaponsmith)
                repairAmount += item.CraftBonusWeaponsmith;

            float minReduction = 0.05f * tech;
            float maxReduction = 0.15f * tech;
            float reductionAmount = _random.RandomFloat(minReduction, maxReduction);

            _durability.RunItemRepair(user.Object, target.Object, repairAmount, reductionAmount + maxDurabilityReductionPenalty);
        }

        private SkillType GetSkillType(NWItem item)
        {
            CustomItemType repairItemType = (CustomItemType)item.GetLocalInt("REPAIR_CUSTOM_ITEM_TYPE_ID");
            switch (repairItemType)
            {
                case CustomItemType.LightArmor:
                case CustomItemType.HeavyArmor:
                case CustomItemType.ForceArmor:
                case CustomItemType.Shield:
                    return SkillType.Armorsmith;

                case CustomItemType.Vibroblade:
                case CustomItemType.FinesseVibroblade:
                case CustomItemType.Baton:
                case CustomItemType.HeavyVibroblade:
                case CustomItemType.Saberstaff:
                case CustomItemType.Polearm:
                case CustomItemType.TwinBlade:
                case CustomItemType.MartialArtWeapon:
                    return SkillType.Weaponsmith;
                    
                case CustomItemType.Lightsaber:
                case CustomItemType.BlasterPistol:
                case CustomItemType.BlasterRifle:
                    return SkillType.Engineering;
            }

            return SkillType.Unknown;
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return 12.0f;
        }

        public bool FaceTarget()
        {
            return false;
        }

        public int AnimationID()
        {
            return NWScript.ANIMATION_LOOPING_GET_MID;
        }

        public float MaxDistance(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return 0;
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return true;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            NWItem targetItem = target.Object;
            float maxDurability = _durability.GetMaxDurability(targetItem);
            float durability = _durability.GetDurability(targetItem);

            if (target.ObjectType != NWScript.OBJECT_TYPE_ITEM)
            {
                return "Only items may be targeted by repair kits.";
            }

            if (targetItem.CustomItemType != (CustomItemType)item.GetLocalInt("REPAIR_CUSTOM_ITEM_TYPE_ID"))
            {
                return "You cannot repair that item with this repair kit.";
            }

            if (maxDurability <= 0.0f ||
                durability >= maxDurability)
            {
                return "That item does not need to be repaired.";
            }

            if (durability <= 0.0f)
            {
                return "That item is broken and cannot be repaired.";
            }

            if (maxDurability <= 0.1f)
            {
                return "You cannot repair that item any more.";
            }

            SkillType skillType = GetSkillType(item);
            int techLevel = item.GetLocalInt("TECH_LEVEL");

            if (skillType == SkillType.Armorsmith)
            {
                if (_perk.GetPCPerkLevel(user.Object, PerkType.ArmorRepair) < techLevel)
                {
                    return "Your level in the 'Armor Repair' perk is too low to use this repair kit.";
                }
            }
            else if (skillType == SkillType.Weaponsmith)
            {
                if (_perk.GetPCPerkLevel(user.Object, PerkType.WeaponRepair) < techLevel)
                {
                    return "Your level in the 'Weapon Repair' perk is too low to use this repair kit.";
                }
            }
            else if (skillType == SkillType.Engineering)
            {
                if(_perk.GetPCPerkLevel(user.Object, PerkType.ElectronicRepair) < techLevel)
                {
                    return "Your level in the 'Electronic Repair' perk is too low to use this repair kit.";
                }
            }

            return null;
        }

        public bool AllowLocationTarget()
        {
            return false;
        }
    }
}
