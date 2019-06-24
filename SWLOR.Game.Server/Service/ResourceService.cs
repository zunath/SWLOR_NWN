using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service
{
    public static class ResourceService
    {
        
        public static string GetResourceDescription(NWPlaceable resource)
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

        public static int GetDifficultyAdjustment(ResourceQuality quality)
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

        public static int CalculateChanceForComponentBonus(NWPlayer player, int tier, ResourceQuality quality, bool scavenging = false)
        {
            int rank = (scavenging ? SkillService.GetPCSkillRank(player, SkillType.Scavenging) : SkillService.GetPCSkillRank(player, SkillType.Harvesting));
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

            var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(player);
            int itemBonus = (scavenging ? effectiveStats.Scavenging : effectiveStats.Harvesting) / 2;
            if (itemBonus > 30) itemBonus = 30;
            chance += itemBonus;

            return chance;
        }

        private static int Colorize(string tag)
        {
            // Returns an index representing the colour to use for a particular property.
            // AC = Green = 0
            // Skill Up = Blue = 1
            // Activation Speed = purple = 2
            // Charges = orange = 3
            // Ability Up = light purple = 4
            // Misc (sneak attack, emnity up/down) = Yellow = 5
            // Attack, damage = red = 6
            // HP, FP, HP regen, FP regen, rest, meditate = Cyan = 7
            HashSet<string> greenIP = new HashSet<string>
            {
                 "compbon_ac1",
                 "compbon_ac2",
                 "compbon_ac3"
            };

            HashSet<string> blueIP = new HashSet<string>
            {
                "compbon_arm1",
                "compbon_cooking1",
                "compbon_eng1",
                "compbon_harv1",
                "compbon_wpn1",
                "compbon_faid1",
                "compbon_fab1",
                "compbon_scanup1",
                "compbon_arm2",
                "compbon_cooking2",
                "compbon_eng2",
                "compbon_harv2",
                "compbon_wpn2",
                "compbon_faid2",
                "compbon_fab2",
                "compbon_scanup2",
                "compbon_arm3",
                "compbon_cooking3",
                "compbon_eng3",
                "compbon_harv3",
                "compbon_wpn3",
                "compbon_faid3",
                "compbon_fab3",
                "compbon_scanup3",
            };

            HashSet<string> purpleIP = new HashSet<string>
            {
                "compbon_cspd1",
                "compbon_cspd2",
                "compbon_cspd3"
            };

            HashSet<string> orangeIP = new HashSet<string>
            {
                "compbon_charges1",
                "compbon_charges2",
                "compbon_charges3"
            };

            HashSet<string> lightPurpleIP = new HashSet<string>
            {
                "compbon_str1",
                "compbon_dex1",
                "compbon_con1",
                "compbon_wis1",
                "compbon_int1",
                "compbon_cha1",
                "compbon_luck1",
                "compbon_str2",
                "compbon_dex2",
                "compbon_con2",
                "compbon_wis2",
                "compbon_int2",
                "compbon_cha2",
                "compbon_luck2",
                "compbon_luck3",
                "compbon_str3",
                "compbon_dex3",
                "compbon_con3",
                "compbon_wis3",
                "compbon_int3",
                "compbon_cha3"
            };

            HashSet<string> yellowIP = new HashSet<string>
            {
                "compbon_enmdown1",
                "compbon_enmup1",
                "compbon_snkatk1",
                "compbon_enmdown2",
                "compbon_enmup2",
                "compbon_snkatk2",
                "compbon_enmdown3",
                "compbon_enmup3",
                "compbon_snkatk3"
            };

            HashSet<string> redIP = new HashSet<string>
            {
                "compbon_dmg1",
                "compbon_ab1",
                "compbon_dmg2",
                "compbon_ab2",
                "compbon_dmg3",
                "compbon_ab3"
            };

            HashSet<string> cyanIP = new HashSet<string>
            {
                "compbon_hpregen1",
                "compbon_fpregen1",
                "compbon_rest1",
                "compbon_med1",
                "compbon_hp2",
                "compbon_fp2",
                "compbon_hpregen2",
                "compbon_fpregen2",
                "compbon_rest2",
                "compbon_med2",
                "compbon_hpregen3",
                "compbon_fpregen3",
                "compbon_rest3",
                "compbon_med3",
                "compbon_hp4",
                "compbon_fp4",
                "compbon_hp6",
                "compbon_fp6"
            };

            if (greenIP.Contains(tag)) return 0;
            if (blueIP.Contains(tag)) return 1;
            if (purpleIP.Contains(tag)) return 2;
            if (orangeIP.Contains(tag)) return 3;
            if (lightPurpleIP.Contains(tag)) return 4;
            if (yellowIP.Contains(tag)) return 5;
            if (redIP.Contains(tag)) return 6;
            if (cyanIP.Contains(tag)) return 7;

            return 8;
        }

        public static Tuple<ItemProperty, int> GetRandomComponentBonusIP(ResourceQuality quality)
        {
            string[] normalIP =
            {
                "compbon_arm1",         // Armorsmith 1
                "compbon_cspd1",        // Cooldown Recovery 1
                "compbon_charges1",     // Charges 1
                //"compbon_cooking1",     // Cooking 1
                "compbon_dmg1",         // Damage 1
                "compbon_eng1",         // Engineering 1
                "compbon_enmdown1",     // Enmity Down 1
                "compbon_harv1",        // Harvesting 1
                "compbon_wpn1",         // Weaponsmith 1
                "compbon_hp2",          // Hit Points 2
                "compbon_fp2",          // Force Points 2
                "compbon_enmup1",       // Enmity Up 1
                "compbon_med1",         // Meditate 1
                "compbon_faid1",        // Medicine 1
                "compbon_fab1",         // Fabrication 1
                "compbon_scanup1",      // Scanning 1
                "compbon_rest1",        // Rest 1
                "compbon_ab1"           // Attack Bonus 1
            };

            string[] highIP =
            {
                "compbon_arm2",         // Armorsmith 2
                "compbon_cspd2",        // Cooldown Recovery 2
                "compbon_charges2",     // Charges 2
                //"compbon_cooking2",     // Cooking 2
                "compbon_dmg2",         // Damage 2
                "compbon_eng2",         // Engineering 2
                "compbon_enmdown2",     // Enmity Down 2
                "compbon_harv2",        // Harvesting 2
                "compbon_wpn2",         // Weaponsmith 2
                "compbon_hp4",          // Hit Points 4
                "compbon_fp4",          // Force Points 4
                "compbon_enmup2",       // Enmity Up 2
                "compbon_luck1",        // Luck 1
                "compbon_med2",         // Meditate 2
                "compbon_faid2",        // Medicine 2
                "compbon_hpregen1",     // HP Regen 1
                "compbon_fpregen1",     // FP Regen 1
                "compbon_snkatk1",      // Sneak Attack 1
                "compbon_fab2",         // Fabrication 2
                "compbon_scanup2",      // Scanning 2
                "compbon_rest2",        // Rest 2
                "compbon_str1",         // Strength 1
                "compbon_dex1",         // Dexterity 1
                "compbon_con1",         // Constitution 1
                "compbon_wis1",         // Wisdom 1
                "compbon_int1",         // Intelligence 1
                "compbon_cha1",         // Charisma 1
                "compbon_ab2",          // Attack Bonus 2
                "compbon_scavup1"       // Scavenging 1

            };

            string[] veryHighIP =
            {
                "compbon_arm3",         // Armorsmith 3
                "compbon_cspd3",        // Cooldown Recovery 3
                //"compbon_cooking3",     // Cooking 3
                "compbon_charges3",     // Charges 3
                "compbon_dmg3",         // Damage 3
                "compbon_eng3",         // Engineering 3
                "compbon_enmdown3",     // Enmity Down 3
                "compbon_harv3",        // Harvesting 3
                "compbon_wpn3",         // Weaponsmith 3
                "compbon_hp6",          // Hit Points 6
                "compbon_fp6",          // Force Points 6
                "compbon_enmup3",       // Enmity Up 3
                "compbon_luck2",        // Luck 2
                "compbon_med3",         // Meditate 3
                "compbon_faid3",        // Medicine 3
                "compbon_hpregen2",     // HP Regen 2
                "compbon_fpregen2",     // FP Regen 2
                "compbon_snkatk2",      // Sneak Attack 2
                "compbon_fab3",         // Fabrication 3
                "compbon_scanup3",      // Scanning 3
                "compbon_rest3",        // Rest 3
                "compbon_str2",         // Strength 2
                "compbon_dex2",         // Dexterity 2
                "compbon_con2",         // Constitution 2
                "compbon_wis2",         // Wisdom 2
                "compbon_int2",         // Intelligence 2
                "compbon_cha2",         // Charisma 2
                "compbon_ab3",          // Attack Bonus 3
                "compbon_scavup2"       // Scavenging 2
            };
            
            string[] setToUse;

            switch (quality)
            {
                case ResourceQuality.Low:
                case ResourceQuality.Normal:
                    setToUse = normalIP;
                    break;
                case ResourceQuality.High:
                    setToUse = highIP;
                    break;
                case ResourceQuality.VeryHigh:
                    setToUse = veryHighIP;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(quality), quality, null);
            }
            
            int index = RandomService.Random(0, setToUse.Length - 1);
            string itemTag = setToUse[index];

            return new Tuple<ItemProperty, int>(ItemService.GetCustomItemPropertyByItemTag(itemTag), Colorize(itemTag)); 
        }
    }
}
