using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using System;

namespace SWLOR.Game.Server.Service
{
    public class ResourceService : IResourceService
    {
        private readonly IItemService _item;
        private readonly IRandomService _random;
        private readonly ISkillService _skill;

        public ResourceService(
            IItemService item,
            IRandomService random,
            ISkillService skill)
        {
            _item = item;
            _random = random;
            _skill = skill;
        }

        public string GetOreQualityName(ResourceQuality quality)
        {
            switch (quality)
            {
                case ResourceQuality.Low:
                    return "Low Quality";
                case ResourceQuality.Normal:
                    return "Normal Quality";
                case ResourceQuality.High:
                    return "High Quality";
                case ResourceQuality.VeryHigh:
                    return "Very High Quality";
                default:
                    return "Unknown Quality";
            }
        }

        public string GetResourceTypeName(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Ore:
                    return "Ore";
                case ResourceType.Organic:
                    return "Organic Material";
                default:
                    return "Unknown Resource";
            }
        }

        public string GetResourcePlaceableResref(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Ore: return "ore_vein";
                case ResourceType.Organic: return "tree";
                default:
                    throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, null);
            }
        }

        public string GetResourceItemResref(ResourceType resourceType, int tier)
        {
            // Raw Ore
            if (resourceType == ResourceType.Ore)
            {
                switch (tier)
                {
                    case 1: return "raw_veldite";
                    case 2: return "raw_scordspar";
                    case 3: return "raw_plagionite";
                    case 4: return "raw_keromber";
                    case 5: return "raw_jasioclase";
                    case 6: return "raw_hemorgite";
                    case 7: return "raw_ochne";
                    case 8: return "raw_croknor";
                    case 9: return "raw_arkoxit";
                    case 10: return "raw_bisteiss";
                    default: return string.Empty;
                }
            }
            // Organic Material
            else if (resourceType == ResourceType.Organic)
            {
                switch (tier)
                {
                    case 1: return "elm_wood";
                    case 2: return "ash_wood";
                    case 3: return "walnut_wood";
                    case 4: return "arrowwood_wood";
                    case 5: return "rosewood_wood";
                    case 6: return "mahogany_wood";
                    case 7: return "maple_wood";
                    case 8: return "willow_wood";
                    case 9: return "lauan_wood";
                    case 10: return "ebony_wood";
                    default: return string.Empty;
                }
            }

            return string.Empty;
        }

        public string GetResourceName(ResourceType resourceType, int tier)
        {
            string resref = GetResourceItemResref(resourceType, tier);
            return _item.GetNameByResref(resref);
        }

        public string GetResourceDescription(ResourceType resourceType, ResourceQuality quality, int tier)
        {
            string description = GetOreQualityName(quality) + " " +
                                 GetResourceName(resourceType, tier) + " (" +
                                 GetResourceTypeName(resourceType) + ")";

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

        public int CalculateChanceForComponentBonus(NWPlayer player, int tier, ResourceQuality quality)
        {
            int rank = _skill.GetPCSkill(player, SkillType.Harvesting).Rank;
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

            return chance;
        }



        public ItemProperty GetRandomComponentBonusIP(ResourceQuality quality)
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
            };

            string[] uncommonIP =
            {
                "compbon_ac2",
                "compbon_arm2",
                "compbon_bab1",
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

            };

            string[] rareIP =
            {
                "compbon_ac3",
                "compbon_arm3",
                "compbon_bab2",
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

            };

            string[] ultraRareIP =
            {
                "compbon_bab3",
                "compbon_luck3",
                "compbon_hpregen3",
                "compbon_fpregen3",
                "compbon_snkatk3",

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

            return _item.GetCustomItemPropertyByItemTag(itemTag);
        }
    }
}
