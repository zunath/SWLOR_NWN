using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
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
    public class SSRepairKit : IActionItem
    {
        public string CustomKey => null;

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            var area = user.Area;
            var player = new NWPlayer(user);
            var structureID = GetLocalString(area, "PC_BASE_STRUCTURE_ID");
            var structureGuid = new Guid(structureID);

            var pcbs = DataService.PCBaseStructure.GetByID(structureGuid);
            var structure = DataService.BaseStructure.GetByID(pcbs.BaseStructureID);

            var repair = SkillService.GetPCSkillRank(player, SkillType.Piloting);
            var maxRepair = (int)structure.Durability - (int)pcbs.Durability;

            if (maxRepair < repair) repair = maxRepair;

            // TODO - add perks to make repairing faster/better/shinier/etc.
            // Maybe a perk to allow repairing in space, with ground repairs only otherwise?

            NWCreature ship = GetLocalObject(area, "CREATURE");

            if (ship.IsValid)
            {
                ship.SetLocalInt("HP", ship.GetLocalInt("HP") + repair);
                ship.FloatingText("Hull repaired: " + ship.GetLocalInt("HP") + "/" + ship.MaxHP);
            }

            pcbs.Durability += repair;
            DataService.SubmitDataChange(pcbs, DatabaseActionType.Update);

            player.SendMessage("Ship repaired for " + repair + " points. (Hull points: " + pcbs.Durability + "/" + structure.Durability + ")");
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            if (PerkService.GetCreaturePerkLevel(new NWPlayer(user), PerkType.CombatRepair) >= 2)
            {
                return 6.0f;
            }

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
            var area = user.Area;

            if (GetLocalInt(area, "BUILDING_TYPE") != (int)BuildingType.Starship)
            {
                return "This repair kit may only be used inside a starship";
            }

            var structureID = GetLocalString(area, "PC_BASE_STRUCTURE_ID");
            var structureGuid = new Guid(structureID);

            var pcbs = DataService.PCBaseStructure.GetByID(structureGuid);
            var structure = DataService.BaseStructure.GetByID(pcbs.BaseStructureID);

            if (structure.Durability == pcbs.Durability)
            {
                return "This starship is already fully repaired.";
            }

            var canRepair = (PerkService.GetCreaturePerkLevel(new NWPlayer(user), PerkType.CombatRepair) >= 1);
            var pcBase = DataService.PCBase.GetByID(pcbs.PCBaseID);

            if (!canRepair && SpaceService.IsLocationSpace(pcBase.ShipLocation))
            {
                return "You need the Combat Repair perk to repair ships in space.";
            }

            return "";
        }

        public bool AllowLocationTarget()
        {
            return true;
        }
    }
}
