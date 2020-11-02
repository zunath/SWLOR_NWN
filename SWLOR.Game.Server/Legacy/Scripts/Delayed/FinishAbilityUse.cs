using System;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Event.SWLOR;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.ValueObject;
using SWLOR.Game.Server.Service;
using PerkExecutionType = SWLOR.Game.Server.Legacy.Enumeration.PerkExecutionType;
using Skill = SWLOR.Game.Server.Core.NWScript.Enum.Skill;

namespace SWLOR.Game.Server.Legacy.Scripts.Delayed
{
    public class FinishAbilityUse : IScript
    {
        private Guid _eventID;

        public void SubscribeEvents()
        {
            _eventID = MessageHub.Instance.Subscribe<OnFinishAbilityUse>(OnFinishAbilityUse);
        }

        public void UnsubscribeEvents()
        {
            MessageHub.Instance.Unsubscribe(_eventID);
        }

        private void OnFinishAbilityUse(OnFinishAbilityUse data)
        {

            using (new Profiler(nameof(FinishAbilityUse)))
            {
                // These arguments are sent from the AbilityService's ActivateAbility method.
                var activator = data.Activator;
                var spellUUID = data.SpellUUID;
                var perkID = data.PerkID;
                var target = data.Target;
                var pcPerkLevel = data.PCPerkLevel;
                var spellTier = data.SpellTier;
                var armorPenalty = data.ArmorPenalty;

                // Get the relevant perk information from the database.
                var dbPerk = DataService.Perk.GetByID(perkID);

                // The execution type determines how the perk behaves and the rules surrounding it.
                var executionType = dbPerk.ExecutionTypeID;

                // Get the class which handles this perk's behaviour.
                var perk = PerkService.GetPerkHandler(perkID);

                // Pull back cooldown information.
                var cooldownID = perk.CooldownCategoryID(activator, dbPerk.CooldownCategoryID, spellTier);
                var cooldown = cooldownID == null ? null : DataService.CooldownCategory.GetByIDOrDefault((int)cooldownID);

                // If the activator interrupted the spell or died, we can bail out early.
                if (activator.GetLocalInt(spellUUID) == (int)SpellStatusType.Interrupted || // Moved during casting
                    activator.CurrentHP < 0 || activator.IsDead) // Or is dead/dying
                {
                    activator.DeleteLocalInt(spellUUID);
                    return;
                }

                // Remove the temporary UUID which is tracking this spell cast.
                activator.DeleteLocalInt(spellUUID);

                // Force Abilities, Combat Abilities, Stances, and Concentration Abilities
                if (executionType == PerkExecutionType.ForceAbility ||
                    executionType == PerkExecutionType.CombatAbility ||
                    executionType == PerkExecutionType.Stance ||
                    executionType == PerkExecutionType.ConcentrationAbility)
                {
                    // Run the impact script.
                    perk.OnImpact(activator, target, pcPerkLevel, spellTier);

                    // If an animation is specified for this perk, play it now.
                    if (dbPerk.CastAnimationID != null && dbPerk.CastAnimationID > 0)
                    {
                        var animation = (Animation) dbPerk.CastAnimationID;
                        activator.AssignCommand(() => { NWScript.ActionPlayAnimation(animation, 1f, 1f); });
                    }

                    // If the target is an NPC, assign enmity towards this creature for that NPC.
                    if (target.IsNPC)
                    {
                        AbilityService.ApplyEnmity(activator, target.Object, dbPerk);
                    }
                }

                // Adjust creature's current FP, if necessary.
                // Adjust FP only if spell cost > 0
                var perkFeat = DataService.PerkFeat.GetByPerkIDAndLevelUnlocked(perkID, spellTier);
                var fpCost = perk.FPCost(activator, perkFeat.BaseFPCost, spellTier);

                if (fpCost > 0)
                {
                    var currentFP = AbilityService.GetCurrentFP(activator);
                    var maxFP = AbilityService.GetMaxFP(activator);
                    currentFP -= fpCost;
                    AbilityService.SetCurrentFP(activator, currentFP);
                    activator.SendMessage(ColorToken.Custom("FP: " + currentFP + " / " + maxFP, 32, 223, 219));
                }

                // Notify activator of concentration ability change and also update it in the DB.
                if (executionType == PerkExecutionType.ConcentrationAbility)
                {
                    AbilityService.StartConcentrationEffect(activator, perkID, spellTier);
                    activator.SendMessage("Concentration ability activated: " + dbPerk.Name);

                    // The Skill Increase effect icon and name has been overwritten. Apply the effect to the player now.
                    // This doesn't do anything - it simply gives a visual cue that the player has an active concentration effect.
                    NWScript.ApplyEffectToObject(DurationType.Permanent, NWScript.EffectSkillIncrease(Skill.UseMagicDevice, 1), activator);
                }

                // Handle applying cooldowns, if necessary.
                if (cooldown != null)
                {
                    AbilityService.ApplyCooldown(activator, cooldown, perk, spellTier, armorPenalty);
                }

                // Mark the creature as no longer busy.
                activator.IsBusy = false;

                // Mark the spell cast as complete.
                activator.SetLocalInt(spellUUID, (int)SpellStatusType.Completed);

            }
        }

        public void Main()
        {
        }
    }
}
