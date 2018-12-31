using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using System;

namespace SWLOR.Game.Server.Service
{
    public class ResourceService : IResourceService
    {
        private readonly INWScript _;
        private readonly IItemService _item;
        private readonly IRandomService _random;
        private readonly ISkillService _skill;
        private readonly IPlayerStatService _playerStat;

        public ResourceService(
            INWScript script,
            IItemService item,
            IRandomService random,
            ISkillService skill,
            IPlayerStatService playerStat)
        {
            _ = script;
            _item = item;
            _random = random;
            _skill = skill;
            _playerStat = playerStat;
        }
        
        public string GetResourceDescription(NWPlaceable resource)
        {
            NWPlaceable tempStorage = (_.GetObjectByTag("TEMP_ITEM_STORAGE"));
            string resref = resource.GetLocalString("RESOURCE_RESREF");
            string qualityName = resource.GetLocalString("RESOURCE_QUALITY_NAME");

            NWItem tempItem = (_.CreateItemOnObject(resref, tempStorage.Object));
            string resourceName = tempItem.Name;
            
            int typeID = 0;

            foreach (var ip in tempItem.ItemProperties)
            {
                if (_.GetItemPropertyType(ip) == (int)CustomItemPropertyType.ComponentType)
                {
                    typeID = _.GetItemPropertyCostTableValue(ip);
                    break;
                }
            }

            if (typeID <= 0)
            {
                return "Invalid component type";
            }

            int tlkID = Convert.ToInt32(_.Get2DAString("iprp_comptype", "Name", typeID));
            string componentName = _.GetStringByStrRef(tlkID);

            string description = qualityName + " " +
                                 resourceName + " (" +
                                 componentName + ")";

            tempItem.Destroy();

            return description;
        }

        public int GetDifficultyAdjustment(ResourceQuality quality)
        {
            switch (quality)
            {
                case ResourceQuality.Low: return -1;
                case ResourceQuality.Normal: return 0;
                case ResourceQuality.High: return 1;
                case ResourceQuality.VeryHigh: return 2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(quality), quality, null);
            }
        }

        public int CalculateChanceForComponentBonus(NWPlayer player, int tier, ResourceQuality quality, bool scavenging = false)
        {
            int rank = (scavenging ? _skill.GetPCSkillRank(player, SkillType.Scavenging) : _skill.GetPCSkillRank(player, SkillType.Harvesting));
            int difficulty = (tier - 1) * 10 + GetDifficultyAdjustment(quality);
            int delta = difficulty - rank;

            if (delta >= 7) return 0;
            if (delta <= -7) return 45;

            int chance = 0;
            switch (delta)
            {
                case 6: chance = 1; break;
                case 5: chance = 2; break;
                case 4: chance = 3; break;
                case 3: chance = 6; break;
                case 2: chance = 9; break;
                case 1: chance = 12; break;
                case 0: chance = 15; break;
                case -1: chance = 18; break;
                case -2: chance = 20; break;
                case -3: chance = 21; break;
                case -4: chance = 23; break;
                case -5: chance = 25; break;
                case -6: chance = 27; break;
            }

            var effectiveStats = _playerStat.GetPlayerItemEffectiveStats(player);
            int itemBonus = (scavenging ? effectiveStats.Scavenging : effectiveStats.Harvesting) / 2;
            if (itemBonus > 30) itemBonus = 30;
            chance += itemBonus;

            return chance;
        }



        public Tuple<ItemProperty, int> GetRandomComponentBonusIP(ResourceQuality quality)
        {
            string[] commonIP =
            {
                "compbon_ac1",
                "compbon_arm1",
                "compbon_cspd1",
                "compbon_charges1",
                "compbon_charges2",
                "compbon_cooking1",
                "compbon_dmg1",
                "compbon_dkdown1",
                "compbon_dkup1",
                "compbon_dur1",
                "compbon_dur2",
                "compbon_eng1",
                "compbon_enmdown1",
                "compbon_harv1",
                "compbon_wpn1",
                "compbon_hp1",
                "compbon_fp1",
                "compbon_enmup1",
                "compbon_med1",
                "compbon_faid1",
                "compbon_fab1",
                "compbon_scanup1",
                "compbon_rest1",
                "compbon_ab1"
            };

            string[] uncommonIP =
            {
                "compbon_ac2",
                "compbon_arm2",
                "compbon_cspd2",
                "compbon_charges3",
                "compbon_cooking2",
                "compbon_dmg2",
                "compbon_dkdown2",
                "compbon_dkup2",
                "compbon_dur3",
                "compbon_eng2",
                "compbon_enmdown2",
                "compbon_harv2",
                "compbon_wpn2",
                "compbon_hp2",
                "compbon_fp2",
                "compbon_enmup2",
                "compbon_luck1",
                "compbon_med2",
                "compbon_faid2",
                "compbon_hpregen1",
                "compbon_fpregen1",
                "compbon_snkatk1",
                "compbon_fab2",
                "compbon_scanup2",
                "compbon_rest2",
                "compbon_str1",
                "compbon_dex1",
                "compbon_con1",
                "compbon_wis1",
                "compbon_int1",
                "compbon_cha1",
                "compbon_ab2"

            };

            string[] rareIP =
            {
                "compbon_ac3",
                "compbon_arm3",
                "compbon_cspd3",
                "compbon_cooking3",
                "compbon_dmg3",
                "compbon_dkdown3",
                "compbon_dkup3",
                "compbon_eng3",
                "compbon_enmdown3",
                "compbon_harv3",
                "compbon_wpn3",
                "compbon_hp3",
                "compbon_fp3",
                "compbon_enmup3",
                "compbon_luck2",
                "compbon_med3",
                "compbon_faid3",
                "compbon_hpregen2",
                "compbon_fpregen2",
                "compbon_snkatk2",
                "compbon_fab3",
                "compbon_scanup3",
                "compbon_rest3",
                "compbon_str2",
                "compbon_dex2",
                "compbon_con2",
                "compbon_wis2",
                "compbon_int2",
                "compbon_cha2",
                "compbon_ab3"
            };

            string[] ultraRareIP =
            {
                "compbon_luck3",
                "compbon_hpregen3",
                "compbon_fpregen3",
                "compbon_snkatk3",
                "compbon_str3",
                "compbon_dex3",
                "compbon_con3",
                "compbon_wis3",
                "compbon_int3",
                "compbon_cha3",

            };

            // Order: Common, Uncommon, Rare, Ultra Rare
            int[] chance;

            switch (quality)
            {
                case ResourceQuality.Low:
                    chance = new[] { 100, 0, 0, 0 };
                    break;
                case ResourceQuality.Normal:
                    chance = new[] { 80, 20, 0, 0 };
                    break;
                case ResourceQuality.High:
                    chance = new[] { 20, 40, 40, 0 };
                    break;
                case ResourceQuality.VeryHigh:
                    chance = new[] { 0, 0, 70, 30 };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(quality));
            }

            string[] setToUse = null;

            int index = _random.GetRandomWeightedIndex(chance);

            switch (index)
            {
                case 0: setToUse = commonIP; break;
                case 1: setToUse = uncommonIP; break;
                case 2: setToUse = rareIP; break;
                case 3: setToUse = ultraRareIP; break;
            }

            if (setToUse == null) throw new NullReferenceException(nameof(setToUse));

            index = _random.Random(0, setToUse.Length - 1);

            string itemTag = setToUse[index];

            return new Tuple<ItemProperty, int>(_item.GetCustomItemPropertyByItemTag(itemTag), index); 
        }
    }
}
