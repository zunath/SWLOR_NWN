﻿using System.Linq;
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
    public class ResourceScanner: IActionItem
    {
        public string CustomKey => null;

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            _.ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, _.EffectVisualEffect(VFX_DUR_PARALYZE_HOLD), target.Location, Seconds(user, item, target, targetLocation, null));
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
                DurabilityService.RunItemDecay(player, item, RandomService.RandomFloat(0.02f, 0.08f));
                effectLocation = targetLocation;

            }
            else if(!string.IsNullOrWhiteSpace(target.GetLocalString("RESOURCE_RESREF")))
            {
                ScanResource(user, target);
                DurabilityService.RunItemDecay(player, item, RandomService.RandomFloat(0.05f, 0.1f));
                effectLocation = target.Location;
            }
            else
            {
                user.FloatingText("You cannot scan that object with this type of scanner.");
                return;
            }

            _.ApplyEffectAtLocation(DURATION_TYPE_INSTANT, _.EffectVisualEffect(VFX_FNF_SUMMON_MONSTER_3), effectLocation);

            if (user.IsPlayer && user.GetLocalInt(target.GlobalID.ToString()) == FALSE)
            {
                int scanningBonus = item.ScanningBonus;
                SkillService.GiveSkillXP(player, SkillType.Harvesting, 150);
                user.SetLocalInt(target.GlobalID.ToString(), 1 + scanningBonus); 
            }
        }

        private void ScanArea(NWCreature user, Location targetLocation)
        {
            NWArea area = (_.GetAreaFromLocation(targetLocation));
            var spawns = SpawnService.GetAreaPlaceableSpawns(area);
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

                BiowarePosition.TurnToFaceLocation(spawn.SpawnLocation, user);

                user.FloatingText("Nearest resource is located at coordinates (" + cellX + ", " + cellY + ")");
            }
        }

        private void ScanResource(NWCreature user, NWObject target)
        {
            NWPlaceable resource = (target.Object);
            user.SendMessage("[Resource Details]: " + ResourceService.GetResourceDescription(resource));
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            const float BaseScanningTime = 16.0f;
            float scanningTime = BaseScanningTime;

            if (user.IsPlayer)
            {
                var player = (user.Object);
                scanningTime = BaseScanningTime - BaseScanningTime * (PerkService.GetPCPerkLevel(player, PerkType.SpeedyResourceScanner) * 0.1f);

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
