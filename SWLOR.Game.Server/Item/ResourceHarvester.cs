using System.Linq;
using NWN;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Item
{
    public class ResourceHarvester : IActionItem
    {
        private readonly INWScript _;
        private readonly IRandomService _random;
        private readonly IPerkService _perk;
        private readonly IResourceService _resource;
        private readonly ISkillService _skill;
        private readonly IBiowareXP2 _biowareXP2;
        private readonly IDurabilityService _durability;

        public ResourceHarvester(
            INWScript script,
            IRandomService random,
            IPerkService perk,
            IResourceService resource,
            ISkillService skill,
            IBiowareXP2 biowareXP2,
            IDurabilityService durability)
        {
            _ = script;
            _random = random;
            _perk = perk;
            _resource = resource;
            _skill = skill;
            _biowareXP2 = biowareXP2;
            _durability = durability;
        }

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWPlayer player = (user.Object);
            ResourceQuality quality = (ResourceQuality)target.GetLocalInt("RESOURCE_QUALITY");
            int tier = target.GetLocalInt("RESOURCE_TIER");
            int remaining = target.GetLocalInt("RESOURCE_COUNT") - 1;
            string itemResref = target.GetLocalString("RESOURCE_RESREF");
            int ipBonusChance = _resource.CalculateChanceForComponentBonus(player, tier, quality);
            int roll = _random.Random(1, 100);
            int rank = _skill.GetPCSkill(player, SkillType.Harvesting).Rank;
            int difficulty = (tier-1) * 10 + _resource.GetDifficultyAdjustment(quality);
            int delta = difficulty - rank;
            int itemHarvestBonus = item.HarvestingBonus;
            int scanningBonus = user.GetLocalInt(target.GlobalID);

            ipBonusChance += (itemHarvestBonus * 2) + (scanningBonus * 2);

            NWItem resource = (_.CreateItemOnObject(itemResref, player.Object));

            if (roll <= ipBonusChance)
            {
                ItemProperty ip = _resource.GetRandomComponentBonusIP(quality);
                _biowareXP2.IPSafeAddItemProperty(resource, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, true, true);
            }

            user.SendMessage("You harvest " + resource.Name + ".");
            _durability.RunItemDecay(player, item, _random.RandomFloat(0.03f, 0.07f));
            int xp = 350 + (delta * 50);
            _skill.GiveSkillXP(player, SkillType.Harvesting, xp);

            if (remaining <= 0)
            {
                target.Destroy();
                user.DeleteLocalInt(target.GlobalID);
            }
            else
            {
                target.SetLocalInt("RESOURCE_COUNT", remaining);
            }

            _.ApplyEffectAtLocation(DURATION_TYPE_INSTANT, _.EffectVisualEffect(VFX_FNF_SUMMON_MONSTER_3), target.Location);
        }
        

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            const float BaseHarvestingTime = 16.0f;
            float harvestingTime = BaseHarvestingTime;

            if (user.IsPlayer)
            {
                var player = (user.Object);
                harvestingTime = BaseHarvestingTime - (BaseHarvestingTime * _perk.GetPCPerkLevel(player, PerkType.SpeedyHarvester));

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

        public float MaxDistance()
        {
            return 5.0f;
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return false;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            NWPlayer player = (user.Object);
            ResourceQuality quality = (ResourceQuality)target.GetLocalInt("RESOURCE_QUALITY");
            int tier = target.GetLocalInt("RESOURCE_TIER");
            int rank = _skill.GetPCSkill(player, SkillType.Harvesting).Rank;
            int difficulty = (tier - 1) * 10 + _resource.GetDifficultyAdjustment(quality);
            int delta = difficulty - rank;

            if (delta >= 7)
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
