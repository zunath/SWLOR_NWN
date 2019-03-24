using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Perk;

using System;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service;
using PerkExecutionType = SWLOR.Game.Server.Enumeration.PerkExecutionType;

namespace SWLOR.Game.Server.Event.Delayed
{
    public class FinishAbilityUse : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlayer pc = (NWPlayer)args[0];
            string spellUUID = Convert.ToString(args[1]);
            int perkID = (int)args[2];
            NWObject target = (NWObject)args[3];
            int pcPerkLevel = (int) args[4];
            int featID = (int) args[5];

            Data.Entity.Perk entity = DataService.Single<Data.Entity.Perk>(x => x.ID == perkID);
            PerkExecutionType executionType = (PerkExecutionType) entity.ExecutionTypeID;

            return App.ResolveByInterface<IPerkBehaviour, bool>("Perk." + entity.ScriptName, perk =>
            {
                int? cooldownID = perk.CooldownCategoryID(pc, entity.CooldownCategoryID, featID);
                CooldownCategory cooldown = cooldownID == null ? 
                    null : 
                    DataService.SingleOrDefault<CooldownCategory>(x => x.ID == cooldownID);

                if (pc.GetLocalInt(spellUUID) == (int)SpellStatusType.Interrupted || // Moved during casting
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
                    
                    if (entity.CastAnimationID != null && entity.CastAnimationID > 0)
                    {
                        pc.AssignCommand(() =>
                        {
                            _.ActionPlayAnimation((int)entity.CastAnimationID, 1f, 1f);
                        });
                    }

                    if (target.IsNPC)
                    {
                        AbilityService.ApplyEnmity(pc, (target.Object), entity);
                    }
                }
                else if(executionType == PerkExecutionType.QueuedWeaponSkill)
                {
                    AbilityService.HandleQueueWeaponSkill(pc, entity, perk, featID);
                }


                // Adjust FP only if spell cost > 0
                Data.Entity.Player pcEntity = DataService.Single<Data.Entity.Player>(x => x.ID == pc.GlobalID);
                int fpCost = perk.FPCost(pc, entity.BaseFPCost, featID);

                if (fpCost > 0)
                {
                    pcEntity.CurrentFP = pcEntity.CurrentFP - fpCost;
                    DataService.SubmitDataChange(pcEntity, DatabaseActionType.Update);
                    pc.SendMessage(ColorTokenService.Custom("FP: " + pcEntity.CurrentFP + " / " + pcEntity.MaxFP, 32, 223, 219));

                }

                bool hasChainspell = CustomEffectService.DoesPCHaveCustomEffect(pc, CustomEffectType.Chainspell) &&
                    executionType == PerkExecutionType.ForceAbility;

                if(!hasChainspell && cooldown != null)
                {
                    // Mark cooldown on category
                    AbilityService.ApplyCooldown(pc, cooldown, perk, featID);
                }
                pc.IsBusy = false;
                pc.SetLocalInt(spellUUID, (int)SpellStatusType.Completed);

                return true;
            });
        }


    }
}
