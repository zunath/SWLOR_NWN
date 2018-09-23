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
    public class ResourceScanner: IActionItem
    {
        private readonly INWScript _;
        private readonly ISpawnService _spawn;
        private readonly IRandomService _random;
        private readonly IPerkService _perk;
        private readonly IResourceService _resource;
        private readonly ISkillService _skill;
        private readonly IDurabilityService _durability;
        private readonly IBiowarePosition _biowarePosition;

        public ResourceScanner(
            INWScript script,
            ISpawnService spawn,
            IRandomService random,
            IPerkService perk,
            IResourceService resource,
            ISkillService skill,
            IDurabilityService durability,
            IBiowarePosition biowarePosition)
        {
            _ = script;
            _spawn = spawn;
            _random = random;
            _perk = perk;
            _resource = resource;
            _skill = skill;
            _durability = durability;
            _biowarePosition = biowarePosition;
        }

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            Location effectLocation;
            NWPlayer player = (user.Object);
            // Targeted a location or self. Locate nearest resource.
            if (!target.IsValid || Equals(user, target))
            {
                ScanArea(user, targetLocation);
                _durability.RunItemDecay(player, item, _random.RandomFloat(0.02f, 0.08f));
                effectLocation = targetLocation;

            }
            else if(!string.IsNullOrWhiteSpace(target.GetLocalString("RESOURCE_RESREF")))
            {
                ScanResource(user, target);
                _durability.RunItemDecay(player, item, _random.RandomFloat(0.05f, 0.1f));
                effectLocation = target.Location;
            }
            else
            {
                user.FloatingText("You cannot scan that object with this type of scanner.");
                return;
            }

            _.ApplyEffectAtLocation(DURATION_TYPE_INSTANT, _.EffectVisualEffect(VFX_FNF_SUMMON_MONSTER_3), effectLocation);

            if (user.IsPlayer && user.GetLocalInt(target.GlobalID) == FALSE)
            {
                int scanningBonus = item.ScanningBonus;
                _skill.GiveSkillXP(player, SkillType.Harvesting, 150);
                user.SetLocalInt(target.GlobalID, 1 + scanningBonus); 
            }
        }

        private void ScanArea(NWCreature user, Location targetLocation)
        {
            NWArea area = (_.GetAreaFromLocation(targetLocation));
            var spawns = _spawn.GetAreaPlaceableSpawns(area.Resref);
            var spawn = spawns
                .Where(x => !string.IsNullOrWhiteSpace(x.SpawnPlaceable.GetLocalString("RESOURCE_RESREF")) &&
                            x.SpawnPlaceable.IsValid)
                .OrderBy(o => _.GetDistanceBetweenLocations(targetLocation, o.Spawn.Location))
                .FirstOrDefault();
            const float BaseScanningRange = 20.0f;
            if (spawn == null || _.GetDistanceBetweenLocations(targetLocation, spawn.SpawnLocation) > BaseScanningRange)
            {
                user.FloatingText("Couldn't locate any nearby resources...");
            }
            else
            {
                var position = _.GetPositionFromLocation(spawn.SpawnLocation);
                int cellX = (int)(position.m_X / 10);
                int cellY = (int)(position.m_Y / 10);

                _biowarePosition.TurnToFaceLocation(spawn.SpawnLocation, user);

                user.FloatingText("Nearest resource is located at coordinates (" + cellX + ", " + cellY + ")");
            }
        }

        private void ScanResource(NWCreature user, NWObject target)
        {
            NWPlaceable resource = (target.Object);
            user.SendMessage("[Resource Details]: " + _resource.GetResourceDescription(resource));
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            const float BaseScanningTime = 16.0f;
            float scanningTime = BaseScanningTime;

            if (user.IsPlayer)
            {
                var player = (user.Object);
                scanningTime = BaseScanningTime - (BaseScanningTime * _perk.GetPCPerkLevel(player, PerkType.SpeedyScanner));

            }
            return scanningTime;
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
            if ((!target.IsValid && !Equals(user, target)) && string.IsNullOrWhiteSpace(target.GetLocalString("RESOURCE_RESREF"))) 
                return "You cannot scan that target with this type of scanner.";
            return null;
        }

        public bool AllowLocationTarget()
        {
            return true;
        }
    }
}
