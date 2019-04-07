using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Perk;
using SWLOR.Game.Server.Service;
using System;
using SWLOR.Game.Server.ValueObject;
using PerkExecutionType = SWLOR.Game.Server.Enumeration.PerkExecutionType;

namespace SWLOR.Game.Server.Event.Delayed
{
    public class FinishAbilityUse : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            using (new Profiler(nameof(FinishAbilityUse)))
            {
                NWPlayer pc = (NWPlayer) args[0];
                string spellUUID = Convert.ToString(args[1]);
                int perkID = (int) args[2];
                NWObject target = (NWObject) args[3];
                int pcPerkLevel = (int) args[4];
                int featID = (int) args[5];
                float armorPenalty = (float) args[6];

                Data.Entity.Perk dbPerk = DataService.Single<Data.Entity.Perk>(x => x.ID == perkID);
                PerkExecutionType executionType = dbPerk.ExecutionTypeID;
                IPerkHandler perk = PerkService.GetPerkHandler(perkID);

                int? cooldownID = perk.CooldownCategoryID(pc, dbPerk.CooldownCategoryID, featID);
                CooldownCategory cooldown = cooldownID == null ? null : DataService.SingleOrDefault<CooldownCategory>(x => x.ID == cooldownID);

                if (pc.GetLocalInt(spellUUID) == (int) SpellStatusType.Interrupted || // Moved during casting
                    pc.CurrentHP < 0 || pc.IsDead) // Or is dead/dying
                {
                    pc.DeleteLocalInt(spellUUID);
                    return false;
                }

                pc.DeleteLocalInt(spellUUID);

                if (executionType == PerkExecutionType.ForceAbility ||
                    executionType == PerkExecutionType.CombatAbility ||
                    executionType == PerkExecutionType.Stance)
                {
                    perk.OnImpact(pc, target, pcPerkLevel, featID);

                    if (dbPerk.CastAnimationID != null && dbPerk.CastAnimationID > 0)
                    {
                        pc.AssignCommand(() => { _.ActionPlayAnimation((int) dbPerk.CastAnimationID, 1f, 1f); });
                    }

                    if (target.IsNPC)
                    {
                        AbilityService.ApplyEnmity(pc, (target.Object), dbPerk);
                    }
                }
                else if (executionType == PerkExecutionType.QueuedWeaponSkill)
                {
                    AbilityService.HandleQueueWeaponSkill(pc, dbPerk, perk, featID);
                }


                // Adjust FP only if spell cost > 0
                Data.Entity.Player pcEntity = DataService.Single<Data.Entity.Player>(x => x.ID == pc.GlobalID);
                PerkLevel perkLevel = DataService.Single<PerkLevel>(x => x.PerkID == perkID && x.Level == pcPerkLevel);
                int fpCost = perk.FPCost(pc, perkLevel.BaseFPCost, featID);

                if (fpCost > 0)
                {
                    pcEntity.CurrentFP = pcEntity.CurrentFP - fpCost;
                    DataService.SubmitDataChange(pcEntity, DatabaseActionType.Update);
                    pc.SendMessage(ColorTokenService.Custom("FP: " + pcEntity.CurrentFP + " / " + pcEntity.MaxFP, 32, 223, 219));

                }
                
                if (cooldown != null)
                {
                    // Mark cooldown on category
                    AbilityService.ApplyCooldown(pc, cooldown, perk, featID, armorPenalty);
                }

                pc.IsBusy = false;
                pc.SetLocalInt(spellUUID, (int) SpellStatusType.Completed);

                return true;
            }
        }
    }
}
