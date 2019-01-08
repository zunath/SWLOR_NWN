using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Perk;
using SWLOR.Game.Server.Service.Contracts;
using System;
using SWLOR.Game.Server.Data.Entity;
using PerkExecutionType = SWLOR.Game.Server.Enumeration.PerkExecutionType;

namespace SWLOR.Game.Server.Event.Delayed
{
    public class FinishAbilityUse : IRegisteredEvent
    {
        private readonly IDataService _data;
        private readonly INWScript _;
        private readonly IAbilityService _ability;
        private readonly IColorTokenService _color;
        private readonly ICustomEffectService _customEffect;

        public FinishAbilityUse(
            IDataService data,
            INWScript script,
            IAbilityService ability,
            IColorTokenService color,
            ICustomEffectService customEffect)
        {
            _data = data;
            _ = script;
            _ability = ability;
            _color = color;
            _customEffect = customEffect;
        }

        public bool Run(params object[] args)
        {
            NWPlayer pc = (NWPlayer)args[0];
            string spellUUID = Convert.ToString(args[1]);
            int perkID = (int)args[2];
            NWObject target = (NWObject)args[3];
            int pcPerkLevel = (int) args[4];
            int featID = (int) args[5];

            Data.Entity.Perk entity = _data.Single<Data.Entity.Perk>(x => x.ID == perkID);
            PerkExecutionType executionType = (PerkExecutionType) entity.ExecutionTypeID;

            return App.ResolveByInterface<IPerk, bool>("Perk." + entity.ScriptName, perk =>
            {
                int? cooldownID = perk.CooldownCategoryID(pc, entity.CooldownCategoryID, featID);
                CooldownCategory cooldown = cooldownID == null ? 
                    null : 
                    _data.SingleOrDefault<CooldownCategory>(x => x.ID == cooldownID);

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
                        _ability.ApplyEnmity(pc, (target.Object), entity);
                    }
                }
                else if(executionType == PerkExecutionType.QueuedWeaponSkill)
                {
                    _ability.HandleQueueWeaponSkill(pc, entity, perk, featID);
                }


                // Adjust FP only if spell cost > 0
                Data.Entity.Player pcEntity = _data.Single<Data.Entity.Player>(x => x.ID == pc.GlobalID);
                int fpCost = perk.FPCost(pc, entity.BaseFPCost, featID);

                if (fpCost > 0)
                {
                    pcEntity.CurrentFP = pcEntity.CurrentFP - fpCost;
                    _data.SubmitDataChange(pcEntity, DatabaseActionType.Update);
                    pc.SendMessage(_color.Custom("FP: " + pcEntity.CurrentFP + " / " + pcEntity.MaxFP, 32, 223, 219));

                }

                bool hasChainspell = _customEffect.DoesPCHaveCustomEffect(pc, CustomEffectType.Chainspell) &&
                    executionType == PerkExecutionType.ForceAbility;

                if(!hasChainspell && cooldown != null)
                {
                    // Mark cooldown on category
                    _ability.ApplyCooldown(pc, cooldown, perk, featID);
                }
                pc.IsBusy = false;
                pc.SetLocalInt(spellUUID, (int)SpellStatusType.Completed);

                return true;
            });
        }


    }
}
