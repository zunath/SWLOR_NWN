using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Legacy.Bioware;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Item.Contracts;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.ValueObject;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using PerkType = SWLOR.Game.Server.Legacy.Enumeration.PerkType;
using SkillType = SWLOR.Game.Server.Legacy.Enumeration.SkillType;

namespace SWLOR.Game.Server.Legacy.Item
{
    public class ResourceHarvester : IActionItem
    {
        public string CustomKey => null;

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            ApplyEffectAtLocation(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Paralyze_Hold), target.Location, Seconds(user, item, target, targetLocation, null));
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWPlayer player = user.Object;
            var quality = (ResourceQuality)target.GetLocalInt("RESOURCE_QUALITY");
            var tier = target.GetLocalInt("RESOURCE_TIER");
            var remaining = target.GetLocalInt("RESOURCE_COUNT") - 1;
            var itemResref = target.GetLocalString("RESOURCE_RESREF");
            var gemChance = ResourceService.CalculateChanceForComponentBonus(player, tier, quality);
            var roll = RandomService.Random(1, 100);
            var rank = SkillService.GetPCSkillRank(player, SkillType.Harvesting);
            if (item.RecommendedLevel < rank)
                rank = item.RecommendedLevel;

            var difficulty = (tier-1) * 10 + ResourceService.GetDifficultyAdjustment(quality);
            var delta = difficulty - rank;

            var baseXP = 0;
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

            var itemHarvestBonus = item.HarvestingBonus;
            var scanningBonus = user.GetLocalInt(target.GlobalID.ToString());
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

            var decayMinimum = 0.03f;
            var decayMaximum = 0.07f;

            if(delta > 0)
            {
                decayMinimum += delta * 0.1f;
                decayMaximum += delta * 0.1f;
            }

            DurabilityService.RunItemDecay(player, item, RandomService.RandomFloat(decayMinimum, decayMaximum));
            var xp = baseXP;
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

            ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Summon_Monster_3), target.Location);
        }
        

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            const float BaseHarvestingTime = 16.0f;
            var harvestingTime = BaseHarvestingTime;

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

        public Animation AnimationID()
        {
            return Animation.LoopingGetMid;
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

            var qualityID = target.GetLocalInt("RESOURCE_QUALITY");

            if(qualityID <= 0)
            {
                return "You cannot harvest that object.";
            }

            NWPlayer player = (user.Object);
            var quality = (ResourceQuality)qualityID;
            var tier = target.GetLocalInt("RESOURCE_TIER");
            var rank = SkillService.GetPCSkillRank(player, SkillType.Harvesting);
            var difficulty = (tier - 1) * 10 + ResourceService.GetDifficultyAdjustment(quality);
            var delta = difficulty - rank;

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
