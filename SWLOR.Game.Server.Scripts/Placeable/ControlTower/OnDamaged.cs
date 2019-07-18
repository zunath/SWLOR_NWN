using System;
using System.Globalization;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Placeable.ControlTower
{
    public class OnDamaged: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWCreature attacker = (_.GetLastDamager(NWGameObject.OBJECT_SELF));
            NWPlaceable tower = (NWGameObject.OBJECT_SELF);
            NWItem weapon = (_.GetLastWeaponUsed(attacker.Object));
            int damage = _.GetTotalDamageDealt();
            var structureID = tower.GetLocalString("PC_BASE_STRUCTURE_ID");
            PCBaseStructure structure = DataService.PCBaseStructure.GetByID(new Guid(structureID));
            int maxShieldHP = BaseService.CalculateMaxShieldHP(structure);
            PCBase pcBase = DataService.PCBase.GetByID(structure.PCBaseID);
            var playerIDs = DataService.PCBasePermission.GetAllByHasPrivatePermissionToBase(structure.PCBaseID)
                                 .Select(s => s.PlayerID);
            var toNotify = NWModule.Get().Players.Where(x => playerIDs.Contains(x.GlobalID));
            DateTime timer = DateTime.UtcNow.AddSeconds(30);
            string clock = timer.ToString(CultureInfo.InvariantCulture);
            string sector = BaseService.GetSectorOfLocation(attacker.Location);
            if (DateTime.UtcNow <= DateTime.Parse(clock))
            {
                foreach (NWPlayer player in toNotify)
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
                hpPercentage = (float)pcBase.ShieldHP / (float)maxShieldHP * 100.0f;
            }

            // Notify the attacker.
            attacker.SendMessage("Tower Shields: " + hpPercentage.ToString("0.00") + "%");
            if (pcBase.IsInReinforcedMode)
            {
                attacker.SendMessage("Control tower is in reinforced mode and cannot be damaged. Reinforced mode will be disabled when the tower runs out of fuel.");
            }

            // HP is tracked in the database. Heal the placeable so it doesn't get destroyed.
            _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectHeal(9999), tower.Object);

            if(attacker.IsPlayer)
            {
                DurabilityService.RunItemDecay(attacker.Object, weapon, RandomService.RandomFloat(0.01f, 0.03f));
            }

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
                    MessageHub.Instance.Publish(new OnBaseDestroyed(pcBase, attacker));
                    return;
                }
            }

            DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);
            DataService.SubmitDataChange(structure, DatabaseActionType.Update);
        }
    }
}
