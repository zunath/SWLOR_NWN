using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;
using Object = NWN.Object;
using System.Globalization;

namespace SWLOR.Game.Server.Placeable.ControlTower
{
    public class OnDamaged: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataService _data;
        private readonly IRandomService _random;
        private readonly IBaseService _base;
        private readonly IDurabilityService _durability;

        public OnDamaged(
            INWScript script,
            IDataService data,
            IRandomService random,
            IBaseService @base,
            IDurabilityService durability)
        {
            _ = script;
            _data = data;
            _random = random;
            _base = @base;
            _durability = durability;
        }

        public bool Run(params object[] args)
        {
            NWCreature attacker = (_.GetLastDamager(Object.OBJECT_SELF));
            NWPlaceable tower = (Object.OBJECT_SELF);
            NWItem weapon = (_.GetLastWeaponUsed(attacker.Object));
            int damage = _.GetTotalDamageDealt();
            var structureID = tower.GetLocalString("PC_BASE_STRUCTURE_ID");
            PCBaseStructure structure = _data.Single<PCBaseStructure>(x => x.ID == new Guid(structureID));
            int maxShieldHP = _base.CalculateMaxShieldHP(structure);
            PCBase pcBase = _data.Get<PCBase>(structure.PCBaseID);
            var playerIDs = _data.Where<PCBasePermission>(x => x.PCBaseID == structure.PCBaseID && 
                                                               !x.IsPublicPermission)
                                 .Select(s => s.PlayerID);
            var toNotify = NWModule.Get().Players.Where(x => playerIDs.Contains(x.GlobalID));
            DateTime timer = DateTime.UtcNow.AddSeconds(30);
            string clock = timer.ToString(CultureInfo.InvariantCulture);
            string sector = _base.GetSectorOfLocation(attacker.Location);
            if (DateTime.UtcNow <= DateTime.Parse(clock))
            {
                foreach(NWPlayer player in toNotify)
                {
                    player.SendMessage("Your base in " + attacker.Area.Name + " " + sector + " is under attack!");
                }
            }
            pcBase.ShieldHP -= damage;
            if (pcBase.ShieldHP <= 0) pcBase.ShieldHP = 0;
            float hpPercentage = (float)pcBase.ShieldHP / (float)maxShieldHP * 100.0f;
            if (hpPercentage <= 25.0f && pcBase.ReinforcedFuel > 0)
            {
                pcBase.IsInReinforcedMode = true;
                pcBase.ShieldHP = (int)(maxShieldHP * 0.25f);
            }

            attacker.SendMessage("Tower Shields: " + hpPercentage.ToString("0.00") + "%");

            if (pcBase.IsInReinforcedMode)
            {
                attacker.SendMessage("Control tower is in reinforced mode and cannot be damaged. Reinforced mode will be disabled when the tower runs out of fuel.");
            }

            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectHeal(9999), tower.Object);

            var durability = _durability.GetDurability(weapon) - _random.RandomFloat(0.01f, 0.03f);
            _durability.SetDurability(weapon, durability);

            if (pcBase.ShieldHP <= 0)
            {
                pcBase.ShieldHP = 0;
                
                structure.Durability -= _random.RandomFloat(0.5f, 2.0f);
                if (structure.Durability < 0.0f) structure.Durability = 0.0f;
                attacker.SendMessage("Structure Durability: " + structure.Durability.ToString("0.00"));

                if (structure.Durability <= 0.0f)
                {
                    structure.Durability = 0.0f;
                    _base.ClearPCBaseByID(pcBase.ID, true, false);
                    return true;
                }
            }

            _data.SubmitDataChange(pcBase, DatabaseActionType.Update);
            _data.SubmitDataChange(structure, DatabaseActionType.Update);
            return true;
        }
    }
}
