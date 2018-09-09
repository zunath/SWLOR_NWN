using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.ControlTower
{
    public class OnHeartbeat: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataContext _db;
        private readonly IBaseService _base;

        public OnHeartbeat(
            INWScript script,
            IDataContext db,
            IBaseService @base)
        {
            _ = script;
            _db = db;
            _base = @base;
        }
        public bool Run(params object[] args)
        {
            NWPlaceable tower = NWPlaceable.Wrap(Object.OBJECT_SELF);
            int structureID = tower.GetLocalInt("PC_BASE_STRUCTURE_ID");
            PCBaseStructure structure = _db.PCBaseStructures.Single(x => x.PCBaseStructureID == structureID);
            int maxShieldHP = _base.CalculateMaxShieldHP(structure);

            if (structure.IsInReinforcedMode)
            {
                structure.ReinforcedFuel--;

                if (structure.ReinforcedFuel <= 0)
                {
                    structure.ReinforcedFuel = 0;
                    structure.IsInReinforcedMode = false;
                }
            }
            else
            {
                structure.ShieldHP += 12 + (4 * structure.StructureBonus);
            }

            if (structure.ShieldHP > maxShieldHP)
                structure.ShieldHP = maxShieldHP;

            _db.SaveChanges();
            return true;
        }
    }
}
