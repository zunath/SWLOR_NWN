using System.Linq;
using NWN;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject;
using static SWLOR.Game.Server.NWScript._;
using _ = SWLOR.Game.Server.NWScript._;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

namespace SWLOR.Game.Server.Item
{
    public class ResourceScanner: IActionItem
    {
        public string CustomKey => null;

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            _.ApplyEffectAtLocation(DurationType.Temporary, _.EffectVisualEffect(Vfx.Vfx_Dur_Paralyze_Hold), target.Location, Seconds(user, item, target, targetLocation, null));
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            Location effectLocation;
            NWPlayer player = (user.Object);
            if(!string.IsNullOrWhiteSpace(target.GetLocalString("RESOURCE_RESREF")))
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

            _.ApplyEffectAtLocation(DurationType.Instant, _.EffectVisualEffect(Vfx.Vfx_Fnf_Summon_Monster_3), effectLocation);

            if (user.IsPlayer && user.GetLocalBoolean(target.GlobalID.ToString()) == false)
            {
                int scanningBonus = item.ScanningBonus;
                SkillService.GiveSkillXP(player, Skill.Harvesting, 150);
                user.SetLocalInt(target.GlobalID.ToString(), 1 + scanningBonus); 
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
                scanningTime = BaseScanningTime - BaseScanningTime * (PerkService.GetCreaturePerkLevel(player, PerkType.SpeedyResourceScanner) * 0.1f);

            }
            return scanningTime;
        }

        public bool FaceTarget()
        {
            return true;
        }


        public Animation AnimationType()
        {
            return Animation.Get_Mid;
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
