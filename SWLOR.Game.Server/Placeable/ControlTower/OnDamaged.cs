﻿using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using static NWN._;
using Object = NWN.Object;
using System.Globalization;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Placeable.ControlTower
{
    public class OnDamaged: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWCreature attacker = (_.GetLastDamager(Object.OBJECT_SELF));
            NWPlaceable tower = (Object.OBJECT_SELF);
            NWItem weapon = (_.GetLastWeaponUsed(attacker.Object));
            int damage = _.GetTotalDamageDealt();
            var structureID = tower.GetLocalString("PC_BASE_STRUCTURE_ID");
            PCBaseStructure structure = DataService.Single<PCBaseStructure>(x => x.ID == new Guid(structureID));
            int maxShieldHP = BaseService.CalculateMaxShieldHP(structure);
            PCBase pcBase = DataService.Get<PCBase>(structure.PCBaseID);
            var playerIDs = DataService.Where<PCBasePermission>(x => x.PCBaseID == structure.PCBaseID && 
                                                               !x.IsPublicPermission)
                                 .Select(s => s.PlayerID);
            var toNotify = NWModule.Get().Players.Where(x => playerIDs.Contains(x.GlobalID));
            DateTime timer = DateTime.UtcNow.AddSeconds(30);
            string clock = timer.ToString(CultureInfo.InvariantCulture);
            string sector = BaseService.GetSectorOfLocation(attacker.Location);
            if (DateTime.UtcNow <= DateTime.Parse(clock))
            {
                foreach(NWPlayer player in toNotify)
                {
                    player.SendMessage("Your base in " + attacker.Area.Name + " " + sector + " is under attack!");
                }
            }

            // Apply damage to the shields. Never fall below 0.
            pcBase.ShieldHP -= damage;
            if (pcBase.ShieldHP <= 0) pcBase.ShieldHP = 0;

            // Calculate the amount of shield percentage remaining. If the tower is in reinforced mode, HP 
            // will always be set back to 25% of shields.
            float hpPercentage = (float)pcBase.ShieldHP / (float)maxShieldHP * 100.0f;
            if (hpPercentage <= 25.0f && pcBase.ReinforcedFuel > 0)
            {
                pcBase.IsInReinforcedMode = true;
                pcBase.ShieldHP = (int)(maxShieldHP * 0.25f);
                hpPercentage = (float) pcBase.ShieldHP / (float) maxShieldHP * 100.0f;
            }

            // Notify the attacker.
            attacker.SendMessage("Tower Shields: " + hpPercentage.ToString("0.00") + "%");
            if (pcBase.IsInReinforcedMode)
            {
                attacker.SendMessage("Control tower is in reinforced mode and cannot be damaged. Reinforced mode will be disabled when the tower runs out of fuel.");
            }

            // HP is tracked in the database. Heal the placeable so it doesn't get destroyed.
            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectHeal(9999), tower.Object);

            var durability = DurabilityService.GetDurability(weapon) - RandomService.RandomFloat(0.01f, 0.03f);
            DurabilityService.SetDurability(weapon, durability);

            // If the shields have fallen to zero, the tower will begin to take structure damage.
            if (pcBase.ShieldHP <= 0)
            {
                pcBase.ShieldHP = 0;
                
                structure.Durability -= RandomService.RandomFloat(0.5f, 2.0f);
                if (structure.Durability < 0.0f) structure.Durability = 0.0f;
                attacker.SendMessage("Structure Durability: " + structure.Durability.ToString("0.00"));

                // If the structure durability drops to zero, destroy everything in the base.
                if (structure.Durability <= 0.0f)
                {
                    structure.Durability = 0.0f;
                    BaseService.ClearPCBaseByID(pcBase.ID, true, false);
                    return true;
                }
            }

            DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);
            DataService.SubmitDataChange(structure, DatabaseActionType.Update);
            return true;
        }
    }
}
