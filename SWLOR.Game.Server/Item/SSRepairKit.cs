using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Item
{
    public class SSRepairKit : IActionItem
    {
        private readonly INWScript _;
        private readonly IBaseService _base;
        private readonly IDataService _data;
        private readonly IPerkService _perk;
        private readonly ISkillService _skill;
        private readonly ISpaceService _space;
        public SSRepairKit(
            INWScript script,
            IBaseService baseService,
            IDataService data,
            IPerkService perk,
            ISkillService skill,
            ISpaceService space)
        {
            _ = script;
            _base = baseService;
            _data = data;
            _perk = perk;
            _skill = skill;
            _space = space;
        }

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWArea area = user.Area;
            NWPlayer player = new NWPlayer(user);
            string structureID = area.GetLocalString("PC_BASE_STRUCTURE_ID");

            PCBaseStructure pcbs = _data.Single<PCBaseStructure>(x => x.ID.ToString() == structureID);
            BaseStructure structure = _data.Get<BaseStructure>(pcbs.BaseStructureID);

            int repair = _skill.GetPCSkillRank(player, SkillType.Piloting);
            int maxRepair = (int)structure.Durability - (int)pcbs.Durability;

            if (maxRepair < repair) repair = maxRepair;

            // TODO - add perks to make repairing faster/better/shinier/etc.
            // Maybe a perk to allow repairing in space, with ground repairs only otherwise?

            NWCreature ship = area.GetLocalObject("CREATURE");

            if (ship.IsValid)
            {
                ship.SetLocalInt("HP", ship.GetLocalInt("HP") + repair);
                ship.FloatingText("Hull repaired: " + ship.GetLocalInt("HP") + "/" + ship.MaxHP);
            }

            pcbs.Durability += repair;
            _data.SubmitDataChange(pcbs, DatabaseActionType.Update);

            player.SendMessage("Ship repaired for " + repair + " points. (Hull points: " + pcbs.Durability + "/" + structure.Durability + ")");
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            if (_perk.GetPCPerkLevel(new NWPlayer(user), PerkType.CombatRepair) >= 2)
            {
                return 6.0f;
            }

            return 12.0f;
        }

        public bool FaceTarget()
        {
            return false;
        }

        public int AnimationID()
        {
            return ANIMATION_LOOPING_GET_MID;
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
            NWArea area = user.Area;

            if (area.GetLocalInt("BUILDING_TYPE") != (int)Enumeration.BuildingType.Starship)
            {
                return "This repair kit may only be used inside a starship";
            }

            string structureID = area.GetLocalString("PC_BASE_STRUCTURE_ID");

            PCBaseStructure pcbs = _data.Single<PCBaseStructure>(x => x.ID.ToString() == structureID);
            BaseStructure structure = _data.Get<BaseStructure>(pcbs.BaseStructureID);

            if (structure.Durability == pcbs.Durability)
            {
                return "This starship is already fully repaired.";
            }

            bool canRepair = (_perk.GetPCPerkLevel(new NWPlayer(user), PerkType.CombatRepair) >= 1);
            PCBase pcBase = _data.Get<PCBase>(pcbs.PCBaseID);

            if (!canRepair && _space.IsLocationSpace(pcBase.ShipLocation))
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
