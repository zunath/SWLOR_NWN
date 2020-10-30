using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Item
{
    public class RepairKit : IActionItem
    {
        public string CustomKey => null;

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            SkillType skillType = GetSkillType(item);
            NWItem targetitem = (target.Object);
            int tech = item.GetLocalInt("TECH_LEVEL");
            float maxDurabilityReductionPenalty = item.GetLocalFloat("MAX_DURABILITY_REDUCTION_PENALTY");
            int repairAmount = tech * 2;
            int skillRank;
            int level = targetitem.RecommendedLevel;
            int delta = 0;
            int baseXP = 0;
            if (skillType == SkillType.Armorsmith)
            {
                skillRank = (SkillService.GetPCSkillRank(user.Object, skillType));
                repairAmount += item.CraftBonusArmorsmith + (PerkService.GetCreaturePerkLevel(user.Object, PerkType.ArmorRepair) * 2);
                delta = level - skillRank;
            }
            else if (skillType == SkillType.Weaponsmith)
            {
                skillRank = (SkillService.GetPCSkillRank(user.Object, skillType));
                repairAmount += item.CraftBonusWeaponsmith + (PerkService.GetCreaturePerkLevel(user.Object, PerkType.WeaponRepair) * 2);
                delta = level - skillRank;
            }
            else if (skillType == SkillType.Engineering)
            {
                skillRank = (SkillService.GetPCSkillRank(user.Object, skillType));
                repairAmount += item.CraftBonusEngineering + (PerkService.GetCreaturePerkLevel(user.Object, PerkType.ElectronicRepair) * 2);
                delta = level - skillRank;
            }
            float minReduction = 0.05f * tech;
            float maxReduction = 0.15f * tech;
            float reductionAmount = RandomService.RandomFloat(minReduction, maxReduction);
            if (delta >= 6) baseXP = 400;
            else if (delta == 5) baseXP = 350;
            else if (delta == 4) baseXP = 325;
            else if (delta == 3) baseXP = 300;
            else if (delta == 2) baseXP = 250;
            else if (delta == 1) baseXP = 225;
            else if (delta == 0) baseXP = 200;
            else if (delta == -1) baseXP = 150;
            else if (delta == -2) baseXP = 100;
            else if (delta == -3) baseXP = 50;
            else if (delta == -4) baseXP = 25;
            SkillService.GiveSkillXP(user.Object, skillType, baseXP);
            DurabilityService.RunItemRepair(user.Object, target.Object, repairAmount, reductionAmount + maxDurabilityReductionPenalty);
        }

        private static SkillType GetSkillType(NWItem item)
        {
            if (GetLocalBool(item, "LIGHTSABER") == true)
            {
                return SkillType.Engineering;
            }

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
                case CustomItemType.Polearm:
                case CustomItemType.TwinBlade:
                case CustomItemType.MartialArtWeapon:
                case CustomItemType.Throwing:
                    return SkillType.Weaponsmith;

                case CustomItemType.Lightsaber:
                case CustomItemType.BlasterPistol:
                case CustomItemType.BlasterRifle:
                case CustomItemType.Saberstaff:
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

        public Animation AnimationID()
        {
            return Animation.LoopingGetMid;
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
            float maxDurability = DurabilityService.GetMaxDurability(targetItem);
            float durability = DurabilityService.GetDurability(targetItem);

            if (target.ObjectType != ObjectType.Item)
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
            return null;
        }

        public bool AllowLocationTarget()
        {
            return false;
        }
    }
}
