using NWN;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;
using static NWN._;

namespace SWLOR.Game.Server.Item
{
    public class ResourceHarvester : IActionItem
    {
        public string CustomKey => null;

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_PARALYZE_HOLD), target.Location, Seconds(user, item, target, targetLocation, null));
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWPlayer player = user.Object;
            ResourceQuality quality = (ResourceQuality)target.GetLocalInt("RESOURCE_QUALITY");
            int tier = target.GetLocalInt("RESOURCE_TIER");
            int remaining = target.GetLocalInt("RESOURCE_COUNT") - 1;
            string itemResref = target.GetLocalString("RESOURCE_RESREF");
            int gemChance = ResourceService.CalculateChanceForComponentBonus(player, tier, quality);
            int roll = RandomService.Random(1, 100);
            int rank = SkillService.GetPCSkillRank(player, SkillType.Harvesting);
            if (item.RecommendedLevel < rank)
                rank = item.RecommendedLevel;

            int difficulty = (tier-1) * 10 + ResourceService.GetDifficultyAdjustment(quality);
            int delta = difficulty - rank;

            int baseXP = 0;
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

            int itemHarvestBonus = item.HarvestingBonus;
            int scanningBonus = user.GetLocalInt(target.GlobalID.ToString());
            gemChance += itemHarvestBonus * 2 + scanningBonus * 2;

            baseXP = baseXP + scanningBonus * 5;

            // Spawn the normal resource.
            NWItem resource = CreateItemOnObject(itemResref, player);
            user.SendMessage("You harvest " + resource.Name + ".");

            // If player meets the chance to acquire a gem, create one and modify its properties.
            if (quality > ResourceQuality.Low && roll <= gemChance)
            {
                // Gemstone quality is determined by the quality of the vein.
                switch (quality)
                {
                    case ResourceQuality.Normal:
                        resource = CreateItemOnObject("flawed_gemstone", player);
                        break;
                    case ResourceQuality.High:
                        resource = CreateItemOnObject("gemstone", player);
                        break;
                    case ResourceQuality.VeryHigh:
                        resource = CreateItemOnObject("perfect_gemstone", player);
                        break;
                }

                var ip = ResourceService.GetRandomComponentBonusIP(quality);
                BiowareXP2.IPSafeAddItemProperty(resource, ip.Item1, 0.0f, AddItemPropertyPolicy.IgnoreExisting, true, true);

                switch (ip.Item2)
                {
                    case 0:
                        resource.Name = ColorTokenService.Green(resource.Name);
                        break;
                    case 1:
                        resource.Name = ColorTokenService.Blue(resource.Name);
                        break;
                    case 2:
                        resource.Name = ColorTokenService.Purple(resource.Name);
                        break;
                    case 3:
                        resource.Name = ColorTokenService.Orange(resource.Name);
                        break;
                    case 4:
                        resource.Name = ColorTokenService.LightPurple(resource.Name);
                        break;
                    case 5:
                        resource.Name = ColorTokenService.Yellow(resource.Name);
                        break;
                    case 6:
                        resource.Name = ColorTokenService.Red(resource.Name);
                        break;
                    case 7:
                        resource.Name = ColorTokenService.Cyan(resource.Name);
                        break;
                }

                user.SendMessage("You harvest " + resource.Name + ".");
            }

            float decayMinimum = 0.03f;
            float decayMaximum = 0.07f;

            if(delta > 0)
            {
                decayMinimum += delta * 0.1f;
                decayMaximum += delta * 0.1f;
            }

            DurabilityService.RunItemDecay(player, item, RandomService.RandomFloat(decayMinimum, decayMaximum));
            int xp = baseXP;
            SkillService.GiveSkillXP(player, SkillType.Harvesting, xp);

            if (remaining <= 0)
            {
                NWPlaceable prop = target.GetLocalObject("RESOURCE_PROP_OBJ");

                if(prop.IsValid)
                {
                    prop.Destroy();
                }

                target.Destroy();
                user.DeleteLocalInt(target.GlobalID.ToString());
            }
            else
            {
                target.SetLocalInt("RESOURCE_COUNT", remaining);
            }

            ApplyEffectAtLocation(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_FNF_SUMMON_MONSTER_3), target.Location);
        }
        

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            const float BaseHarvestingTime = 16.0f;
            float harvestingTime = BaseHarvestingTime;

            if (user.IsPlayer)
            {
                var player = (user.Object);
                harvestingTime = BaseHarvestingTime - BaseHarvestingTime * (PerkService.GetCreaturePerkLevel(player, PerkType.SpeedyHarvester) * 0.1f);

            }
            return harvestingTime;
        }

        public bool FaceTarget()
        {
            return true;
        }

        public int AnimationID()
        {
            return ANIMATION_LOOPING_GET_MID;
        }

        public float MaxDistance(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return 5.0f;
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return false;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            if (!target.IsValid)
            {
                return "Please select a target to harvest.";
            }

            int qualityID = target.GetLocalInt("RESOURCE_QUALITY");

            if(qualityID <= 0)
            {
                return "You cannot harvest that object.";
            }

            NWPlayer player = (user.Object);
            ResourceQuality quality = (ResourceQuality)qualityID;
            int tier = target.GetLocalInt("RESOURCE_TIER");
            int rank = SkillService.GetPCSkillRank(player, SkillType.Harvesting);
            int difficulty = (tier - 1) * 10 + ResourceService.GetDifficultyAdjustment(quality);
            int delta = difficulty - rank;

            if (delta >= 5)
            {
                return "Your Harvesting skill rank is too low to harvest this resource.";
            }


            return null;
        }

        public bool AllowLocationTarget()
        {
            return true;
        }
    }
}
