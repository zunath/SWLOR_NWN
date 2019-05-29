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
                bool submitDataChangeUpdate = false;

                // These arguments are sent from the AbilityService's ActivateAbility method.
                NWPlayer pc = (NWPlayer) args[0];
                string spellUUID = Convert.ToString(args[1]);
                int perkID = (int) args[2];
                NWObject target = (NWObject) args[3];
                int pcPerkLevel = (int) args[4];
                int spellTier = (int) args[5];
                float armorPenalty = (float) args[6];

                // Get the relevant perk information from the database.
                Data.Entity.Perk dbPerk = DataService.Single<Data.Entity.Perk>(x => x.ID == perkID);

                // The execution type determines how the perk behaves and the rules surrounding it.
                PerkExecutionType executionType = dbPerk.ExecutionTypeID;

                // Get the class which handles this perk's behaviour.
                IPerkHandler perk = PerkService.GetPerkHandler(perkID);

                // Pull back cooldown information.
                int? cooldownID = perk.CooldownCategoryID(pc, dbPerk.CooldownCategoryID, spellTier);
                CooldownCategory cooldown = cooldownID == null ? null : DataService.SingleOrDefault<CooldownCategory>(x => x.ID == cooldownID);

                // If the player interrupted the spell or died, we can bail out early.
                if (pc.GetLocalInt(spellUUID) == (int) SpellStatusType.Interrupted || // Moved during casting
                    pc.CurrentHP < 0 || pc.IsDead) // Or is dead/dying
                {
                    pc.DeleteLocalInt(spellUUID);
                    return false;
                }

                // Remove the temporary UUID which is tracking this spell cast.
                pc.DeleteLocalInt(spellUUID);

                // Force Abilities, Combat Abilities, Stances, and Concentration Abilities
                if (executionType == PerkExecutionType.ForceAbility ||
                    executionType == PerkExecutionType.CombatAbility ||
                    executionType == PerkExecutionType.Stance ||
                    executionType == PerkExecutionType.ConcentrationAbility)
                {
                    // Run the impact script.
                    perk.OnImpact(pc, target, pcPerkLevel, spellTier);

                    // If an animation is specified for this perk, play it now.
                    if (dbPerk.CastAnimationID != null && dbPerk.CastAnimationID > 0)
                    {
                        pc.AssignCommand(() => { _.ActionPlayAnimation((int) dbPerk.CastAnimationID, 1f, 1f); });
                    }

                    // If the target is an NPC, assign enmity towards this player for that NPC.
                    if (target.IsNPC)
                    {
                        AbilityService.ApplyEnmity(pc, (target.Object), dbPerk);
                    }

                }
                Data.Entity.Player pcEntity = DataService.Single<Data.Entity.Player>(x => x.ID == pc.GlobalID);


                // Adjust player's current FP, if necessary.
                // Concentration abilities do not require FP to activate.
                if (executionType != PerkExecutionType.ConcentrationAbility)
                {
                    // Adjust FP only if spell cost > 0
                    PerkLevel perkLevel = DataService.Single<PerkLevel>(x => x.PerkID == perkID && x.Level == pcPerkLevel);
                    int fpCost = perk.FPCost(pc, perkLevel.BaseFPCost, spellTier);

                    if (fpCost > 0)
                    {
                        pcEntity.CurrentFP = pcEntity.CurrentFP - fpCost;
                        submitDataChangeUpdate = true;
                        pc.SendMessage(ColorTokenService.Custom("FP: " + pcEntity.CurrentFP + " / " + pcEntity.MaxFP, 32, 223, 219));
                    }
                }
                // Notify player of concentration ability change and also update it in the DB.
                else
                {
                    pcEntity.ActiveConcentrationPerkID = perkID;
                    pcEntity.ActiveConcentrationTier = spellTier;
                    submitDataChangeUpdate = true;
                    pc.SendMessage("Concentration ability activated: " + dbPerk.Name);

                    // The Skill Increase effect icon and name has been overwritten. Apply the effect to the player now.
                    // This doesn't do anything - it simply gives a visual cue that the player has an active concentration effect.
                    _.ApplyEffectToObject(_.DURATION_TYPE_PERMANENT, _.EffectSkillIncrease(_.SKILL_USE_MAGIC_DEVICE, 1), pc);
                }
                
                // Handle applying cooldowns, if necessary.
                if (cooldown != null)
                {
                    AbilityService.ApplyCooldown(pc, cooldown, perk, spellTier, armorPenalty);
                }
                
                // Mark the player as no longer busy.
                pc.IsBusy = false;

                // Mark the spell cast as complete.
                pc.SetLocalInt(spellUUID, (int) SpellStatusType.Completed);

                // Submit a change to the player DB entry only if something changed (FP or active concentration ability).
                if(submitDataChangeUpdate)
                    DataService.SubmitDataChange(pcEntity, DatabaseActionType.Update);

                return true;
            }
        }
    }
}
