using NWN;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Perk;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Event.Creature;
using SWLOR.Game.Server.Event.Feat;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.ValueObject;
using static NWN._;
using PerkExecutionType = SWLOR.Game.Server.Enumeration.PerkExecutionType;

namespace SWLOR.Game.Server.Service
{
    public static class AbilityService
    {

        // These variables are used throughout the engine to flag the type of damage being done to 
        // a creature.  The damage code reads this to determine what bonus effects to apply.
        // The LAST_ATTACK variable name should be appended with the GlobalID of the attacking
        // (N)PC so that attacks from different creatures are treated correctly. 
        public const string LAST_ATTACK = "LAST_ATTACK_";

        public static int ATTACK_PHYSICAL = 1;  // Weapon attacks and weapon skills
        public static int ATTACK_FORCE = 2;  // Force effects
        public static int ATTACK_COMBATABILITY = 3; // Combat tricks like Provoke
        public static int ATTACK_DOT = 4; // Subsequent damage effects

        private static readonly List<NWCreature> ConcentratingCreatures = new List<NWCreature>();

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleEnter>(message => OnModuleEnter());
            MessageHub.Instance.Subscribe<OnHitCastSpell>(message => OnHitCastSpell());
            MessageHub.Instance.Subscribe<OnModuleUseFeat>(message => OnModuleUseFeat());
            MessageHub.Instance.Subscribe<OnObjectProcessorRan>(message => ProcessConcentrationEffects());
            MessageHub.Instance.Subscribe<OnModuleDeath>(message => OnModuleDeath());
            MessageHub.Instance.Subscribe<OnCreatureSpawn>(message => RegisterCreaturePerks(message.Self));
        }

        /// <summary>
        /// Reapplies the concentration effect icon if player logs in with an active effect.
        /// </summary>
        private static void OnModuleEnter()
        {
            NWPlayer pc = _.GetEnteringObject();
            if (!pc.IsPlayer) return;

            // Reapply the visual effect icon to player if they logged in with an active concentration ability.
            Player dbPlayer = DataService.Player.GetByID(pc.GlobalID);
            if (dbPlayer.ActiveConcentrationPerkID != null)
            {
                _.ApplyEffectToObject(_.DURATION_TYPE_PERMANENT, _.EffectSkillIncrease(_.SKILL_USE_MAGIC_DEVICE, 1), pc);
                ConcentratingCreatures.Add(pc);
            }
        }

        /// <summary>
        /// Runs validation checks to ensure activator can use a perk feat.
        /// Activation will fail if any of the following are true:
        ///     - Target is invalid
        ///     - Activator is a ship
        ///     - Feat is not a perk feat
        ///     - Cooldown has not passed
        /// </summary>
        /// <param name="activator">The creature activating a perk feat.</param>
        /// <param name="target">The target of the perk feat.</param>
        /// <param name="featID">The ID number of the feat being used.</param>
        /// <returns>true if able to use perk feat on target, false otherwise.</returns>
        public static bool CanUsePerkFeat(NWCreature activator, NWObject target, int featID)
        {
            var perkFeat = DataService.PerkFeat.GetByFeatIDOrDefault(featID);

            // There's no matching feat in the DB for this ability. Exit early.
            if (perkFeat == null) return false;

            // Retrieve the perk information.
            Data.Entity.Perk perk = DataService.Perk.GetByIDOrDefault(perkFeat.PerkID);

            // No perk could be found. Exit early.
            if (perk == null) return false;

            // Check to see if we are a spaceship.  Spaceships can't use abilities...
            if (activator.GetLocalInt("IS_SHIP") > 0 || activator.GetLocalInt("IS_GUNNER") > 0)
            {
                activator.SendMessage("You cannot use that ability while piloting a ship.");
                return false;
            }

            // Retrieve the perk-specific handler logic.
            var handler = PerkService.GetPerkHandler(perkFeat.PerkID);

            // Get the creature's perk level.
            int creaturePerkLevel = PerkService.GetCreaturePerkLevel(activator, perk.ID);

            // If player is disabling an existing stance, remove that effect.
            if (perk.ExecutionTypeID == PerkExecutionType.Stance)
            {
                // Can't process NPC stances at the moment. Need to do some more refactoring before this is possible.
                // todo: handle NPC stances.
                if (!activator.IsPlayer) return false;

                PCCustomEffect stanceEffect = DataService.PCCustomEffect.GetByStancePerkOrDefault(activator.GlobalID, perk.ID);

                if (stanceEffect != null)
                {
                    if (CustomEffectService.RemoveStance(activator))
                    {
                        return false;
                    }
                }
            }

            // Check for a valid perk level.
            if (creaturePerkLevel <= 0)
            {
                activator.SendMessage("You do not meet the prerequisites to use this ability.");
                return false;
            }

            // Verify that this hostile action meets PVP sanctuary restriction rules. 
            if (handler.IsHostile() && target.IsPlayer)
            {
                if (!PVPSanctuaryService.IsPVPAttackAllowed(activator.Object, target.Object)) return false;
            }

            // Activator and target must be in the same area and within line of sight.
            if (activator.Area.Resref != target.Area.Resref ||
                    _.LineOfSightObject(activator.Object, target.Object) == FALSE)
            {
                activator.SendMessage("You cannot see your target.");
                return false;
            }

            // Run this perk's specific checks on whether the activator may use this perk on the target.
            string canCast = handler.CanCastSpell(activator, target, perkFeat.PerkLevelUnlocked);
            if (!string.IsNullOrWhiteSpace(canCast))
            {
                activator.SendMessage(canCast);
                return false;
            }

            // Calculate the FP cost to use this ability. Verify activator has sufficient FP.
            int fpCost = handler.FPCost(activator, handler.FPCost(activator, perkFeat.BaseFPCost, perkFeat.PerkLevelUnlocked), perkFeat.PerkLevelUnlocked);
            int currentFP = GetCurrentFP(activator);
            if (currentFP < fpCost)
            {
                activator.SendMessage("You do not have enough FP. (Required: " + fpCost + ". You have: " + currentFP + ")");
                return false;
            }

            // Verify activator isn't busy or dead.
            if (activator.IsBusy || activator.CurrentHP <= 0)
            {
                activator.SendMessage("You are too busy to activate that ability.");
                return false;
            }

            // verify activator is commandable. https://github.com/zunath/SWLOR_NWN/issues/940#issue-467175951
            if (!activator.IsCommandable)
            {
                activator.SendMessage("You cannot take actions currently.");
                return false;
            }

            // If we're executing a concentration ability, check and see if the activator currently has this ability
            // active. If it's active, then we immediately remove its effect and bail out.
            // Any other ability (including other concentration abilities) execute as normal.
            if (perk.ExecutionTypeID == PerkExecutionType.ConcentrationAbility)
            {
                // Retrieve the concentration effect for this creature.
                var concentrationEffect = GetActiveConcentrationEffect(activator);
                if ((int)concentrationEffect.Type == perk.ID)
                {
                    // It's active. Time to disable it.
                    EndConcentrationEffect(activator);
                    activator.SendMessage("Concentration ability '" + perk.Name + "' deactivated.");
                    SendAOEMessage(activator, activator.Name + " deactivates concentration ability '" + perk.Name + "'.");
                    return false;
                }
            }

            // Retrieve the cooldown information and determine the unlock time.
            int? cooldownCategoryID = handler.CooldownCategoryID(activator, perk.CooldownCategoryID, perkFeat.PerkLevelUnlocked);
            DateTime now = DateTime.UtcNow;
            DateTime unlockDateTime = cooldownCategoryID == null ? now : GetAbilityCooldownUnlocked(activator, (int)cooldownCategoryID);

            // Check if we've passed the unlock date. Exit early if we have not.
            if (unlockDateTime > now)
            {
                string timeToWait = TimeService.GetTimeToWaitLongIntervals(now, unlockDateTime, false);
                activator.SendMessage("That ability can be used in " + timeToWait + ".");
                return false;
            }

            // Passed all checks. Return true.
            return true;
        }


        /// <summary>
        /// Processes all feats which are linked to perks.
        /// </summary>
        private static void OnModuleUseFeat()
        {
            // Activator is the creature who used the feat.
            // Target is who the activator selected to use this feat on.
            NWCreature activator = NWGameObject.OBJECT_SELF;
            NWCreature target = NWNXEvents.OnFeatUsed_GetTarget().Object;
            int featID = NWNXEvents.OnFeatUsed_GetFeatID();

            // Ensure this perk feat can be activated.
            if (!CanUsePerkFeat(activator, target, featID)) return;

            // Retrieve information necessary for activation of perk feat.
            var perkFeat = DataService.PerkFeat.GetByFeatIDOrDefault(featID);
            Data.Entity.Perk perk = DataService.Perk.GetByIDOrDefault(perkFeat.PerkID);
            int creaturePerkLevel = PerkService.GetCreaturePerkLevel(activator, perk.ID);
            var handler = PerkService.GetPerkHandler(perkFeat.PerkID);

            SendAOEMessage(activator, activator.Name + " readies " + perk.Name + ".");

            // Force Abilities (aka Spells)
            if (perk.ExecutionTypeID == PerkExecutionType.ForceAbility)
            {
                target.SetLocalInt(LAST_ATTACK + activator.GlobalID, ATTACK_FORCE);
                ActivateAbility(activator, target, perk, handler, creaturePerkLevel, PerkExecutionType.ForceAbility, perkFeat.PerkLevelUnlocked);
            }
            // Combat Abilities
            else if (perk.ExecutionTypeID == PerkExecutionType.CombatAbility)
            {
                target.SetLocalInt(LAST_ATTACK + activator.GlobalID, ATTACK_PHYSICAL);
                ActivateAbility(activator, target, perk, handler, creaturePerkLevel, PerkExecutionType.CombatAbility, perkFeat.PerkLevelUnlocked);
            }
            // Queued Weapon Skills
            else if (perk.ExecutionTypeID == PerkExecutionType.QueuedWeaponSkill)
            {
                target.SetLocalInt(LAST_ATTACK + activator.GlobalID, ATTACK_PHYSICAL);
                HandleQueueWeaponSkill(activator, perk, handler, featID);
            }
            // Stances
            else if (perk.ExecutionTypeID == PerkExecutionType.Stance)
            {
                target.SetLocalInt(LAST_ATTACK + activator.GlobalID, ATTACK_COMBATABILITY);
                ActivateAbility(activator, target, perk, handler, creaturePerkLevel, PerkExecutionType.Stance, perkFeat.PerkLevelUnlocked);
            }
            // Concentration Abilities
            else if (perk.ExecutionTypeID == PerkExecutionType.ConcentrationAbility)
            {
                target.SetLocalInt(LAST_ATTACK + activator.GlobalID, ATTACK_FORCE);
                ActivateAbility(activator, target, perk, handler, creaturePerkLevel, PerkExecutionType.ConcentrationAbility, perkFeat.PerkLevelUnlocked);
            }
        }

        /// <summary>
        /// Retrieves the DateTime in which the specified cooldownCategoryID will be available for usel
        /// </summary>
        /// <param name="activator">The creature whose cooldown we're checking.</param>
        /// <param name="cooldownCategoryID">The cooldown category we're checking for.</param>
        /// <returns></returns>
        private static DateTime GetAbilityCooldownUnlocked(NWCreature activator, int cooldownCategoryID)
        {
            // Players: Retrieve info from cache/DB, if it doesn't exist create a new record and insert it. Return unlock date.
            if (activator.IsPlayer)
            {
                PCCooldown pcCooldown = DataService.PCCooldown.GetByPlayerAndCooldownCategoryIDOrDefault(activator.GlobalID, cooldownCategoryID);
                if (pcCooldown == null)
                {
                    pcCooldown = new PCCooldown
                    {
                        CooldownCategoryID = Convert.ToInt32(cooldownCategoryID),
                        DateUnlocked = DateTime.UtcNow.AddSeconds(-1),
                        PlayerID = activator.GlobalID
                    };

                    DataService.SubmitDataChange(pcCooldown, DatabaseActionType.Insert);
                }

                return pcCooldown.DateUnlocked;
            }
            // Creatures: Retrieve info from local variable, convert to DateTime if possible. Return parsed unlock date.
            else
            {
                string unlockDate = activator.GetLocalString("ABILITY_COOLDOWN_ID_" + cooldownCategoryID);
                if (string.IsNullOrWhiteSpace(unlockDate))
                {
                    return DateTime.UtcNow.AddSeconds(-1);
                }
                else
                {
                    return DateTime.ParseExact(unlockDate, "yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture);
                }
            }
        }
        
        /// <summary>
        /// Returns the currently active concentration perk for a given creature.
        /// If no concentration perk is active, PerkType.Unknown will be returned.
        /// </summary>
        /// <param name="creature"></param>
        /// <returns>A ConcentrationEffect containing data about the active concentration effect.</returns>
        public static ConcentrationEffect GetActiveConcentrationEffect(NWCreature creature)
        {
            if (creature.IsPlayer)
            {
                Player dbPlayer = DataService.Player.GetByID(creature.GlobalID);
                if (dbPlayer.ActiveConcentrationPerkID == null) return new ConcentrationEffect(PerkType.Unknown, 0);

                return new ConcentrationEffect((PerkType)dbPlayer.ActiveConcentrationPerkID, dbPlayer.ActiveConcentrationTier);
            }
            else
            {
                // Creatures are assumed to always use the highest perk level available.
                int perkID = creature.GetLocalInt("ACTIVE_CONCENTRATION_PERK_ID");
                int tier = creature.GetLocalInt("PERK_LEVEL_" + perkID);
                PerkType type = perkID <= 0 ? PerkType.Unknown : (PerkType) perkID;
                return new ConcentrationEffect(type, tier);
            }

        }

        public static void StartConcentrationEffect(NWCreature creature, int perkID, int spellTier)
        {
            if (creature.IsPlayer)
            {
                var player = DataService.Player.GetByID(creature.GlobalID);
                player.ActiveConcentrationPerkID = perkID;
                player.ActiveConcentrationTier = spellTier;
                DataService.SubmitDataChange(player, DatabaseActionType.Update);
            }
            else
            {
                creature.SetLocalInt("ACTIVE_CONCENTRATION_PERK_ID", perkID);
            }
            ConcentratingCreatures.Add(creature);
        }

        /// <summary>
        /// Ends a creature's concentration effect immediately.
        /// No message is sent by this method, so be sure to send one if you need to inform a player.
        /// </summary>
        /// <param name="creature">The creatures whose concentration we're ending.</param>
        public static void EndConcentrationEffect(NWCreature creature)
        {
            if (creature.IsPlayer)
            {
                Player player = DataService.Player.GetByID(creature.GlobalID);
                if (player.ActiveConcentrationPerkID == null) return;

                player.ActiveConcentrationPerkID = null;
                player.ActiveConcentrationTier = 0;
                DataService.SubmitDataChange(player, DatabaseActionType.Update);
            }
            else
            {
                creature.DeleteLocalInt("ACTIVE_CONCENTRATION_PERK_ID");
            }

            creature.DeleteLocalInt("ACTIVE_CONCENTRATION_ABILITY_TICK");
            creature.DeleteLocalObject("CONCENTRATION_TARGET");
            creature.RemoveEffect(_.EFFECT_TYPE_SKILL_INCREASE); // Remove the effect icon.

            ConcentratingCreatures.Remove(creature);
        }

        private static void ProcessConcentrationEffects()
        {
            // Loop through each player. If they have a concentration ability active,
            // process it using that perk's OnConcentrationTick() method.
            for(int index = ConcentratingCreatures.Count-1; index >= 0; index--)
            {
                var creature = ConcentratingCreatures[index];
                var activeAbility = GetActiveConcentrationEffect(creature);
                int perkID = (int)activeAbility.Type;
                int tier = activeAbility.Tier;
                bool ended = false;

                // If we have an invalid creature for any reason, remove it and move to the next one.
                if (!creature.IsValid || creature.CurrentHP <= 0 || activeAbility.Type == PerkType.Unknown)
                {
                    ConcentratingCreatures.RemoveAt(index);
                    continue;
                }
                
                // Track the current tick.
                int tick = creature.GetLocalInt("ACTIVE_CONCENTRATION_ABILITY_TICK") + 1;
                creature.SetLocalInt("ACTIVE_CONCENTRATION_ABILITY_TICK", tick);
                
                PerkFeat perkFeat = DataService.PerkFeat.GetByPerkIDAndLevelUnlocked(perkID, tier);                
                
                // Are we ready to continue processing this concentration effect?
                if (tick % perkFeat.ConcentrationTickInterval != 0) return;

                // Get the perk handler, FP cost, and the target.
                var handler = PerkService.GetPerkHandler(perkID);
                int fpCost = handler.FPCost(creature, perkFeat.ConcentrationFPCost, tier);
                NWObject target = creature.GetLocalObject("CONCENTRATION_TARGET");
                int currentFP = GetCurrentFP(creature);
                int maxFP = GetMaxFP(creature);

                // Is the target still valid?
                if (!target.IsValid || target.CurrentHP <= 0)
                {
                    creature.SendMessage("Concentration effect has ended because your target is no longer valid.");
                    EndConcentrationEffect(creature);
                    ended = true;
                }
                // Does player have enough FP to maintain this concentration?
                else if (currentFP < fpCost)
                {
                    creature.SendMessage("Concentration effect has ended because you ran out of FP.");
                    EndConcentrationEffect(creature);
                    ended = true;
                }
                // Is the target still within range and in the same area?
                else if (creature.Area.Object != target.Area.Object ||
                         _.GetDistanceBetween(creature, target) > 50.0f)
                {
                    creature.SendMessage("Concentration effect has ended because your target has gone out of range.");
                    EndConcentrationEffect(creature);
                    ended = true;
                }
                // Otherwise deduct the required FP.
                else
                {
                    currentFP -= fpCost;
                }

                SetCurrentFP(creature, currentFP);

                // Send a FP status message if the effect ended or it's been six seconds since the last one.
                if (ended || tick % 6 == 0)
                {
                    creature.SendMessage(ColorTokenService.Custom("FP: " + currentFP + " / " + maxFP, 32, 223, 219));
                }
                
                // Run this individual perk's concentration tick method if it didn't end this tick.
                if (!ended && target.IsValid)
                {
                    handler.OnConcentrationTick(creature, target, tier, tick);
                }
            }
        }

        private static void OnModuleDeath()
        {
            NWPlayer player = _.GetLastPlayerDied();
            if (!player.IsPlayer) return;

            EndConcentrationEffect(player);
        }
        
        public static void ApplyEnmity(NWCreature attacker, NWCreature target, Data.Entity.Perk perk)
        {
            switch ((EnmityAdjustmentRuleType)perk.EnmityAdjustmentRuleID)
            {
                case EnmityAdjustmentRuleType.AllTaggedTargets:
                    EnmityService.AdjustEnmityOnAllTaggedCreatures(attacker, perk.Enmity);
                    break;
                case EnmityAdjustmentRuleType.TargetOnly:
                    if (target.IsValid)
                    {
                        EnmityService.AdjustEnmity(target, attacker, perk.Enmity);
                    }
                    break;
                case EnmityAdjustmentRuleType.Custom:
                    var handler = PerkService.GetPerkHandler(perk.ID);
                    handler.OnCustomEnmityRule(attacker, perk.Enmity);
                    break;
            }
        }

        private static void ActivateAbility(
            NWCreature activator,
            NWObject target,
            Data.Entity.Perk entity,
            IPerkHandler perkHandler,
            int pcPerkLevel,
            PerkExecutionType executionType,
            int spellTier)
        {
            string uuid = Guid.NewGuid().ToString();
            float baseActivationTime = perkHandler.CastingTime(activator, (float)entity.BaseCastingTime, spellTier);
            float activationTime = baseActivationTime;
            int vfxID = -1;
            int animationID = -1;
            
            if (baseActivationTime > 0f && activationTime < 1.0f)
                activationTime = 1.0f;

            // Force ability armor penalties
            float armorPenalty = 0.0f;
            if (executionType == PerkExecutionType.ForceAbility || 
                executionType == PerkExecutionType.ConcentrationAbility)
            {
                string penaltyMessage = string.Empty;
                foreach (var item in activator.EquippedItems)
                {
                    if (item.CustomItemType == CustomItemType.HeavyArmor)
                    {
                        armorPenalty = 2;
                        penaltyMessage = "Heavy armor slows your force cooldown by 100%.";
                        break;
                    }
                    else if (item.CustomItemType == CustomItemType.LightArmor)
                    {
                        armorPenalty = 1.25f;
                        penaltyMessage = "Light armor slows your force cooldown by 25%.";
                    }
                }

                // If there's an armor penalty, send a message to the player.
                if (armorPenalty > 0.0f)
                {
                    activator.SendMessage(penaltyMessage);
                }

            }

            // If player is in stealth mode, force them out of stealth mode.
            if (_.GetActionMode(activator.Object, ACTION_MODE_STEALTH) == 1)
                _.SetActionMode(activator.Object, ACTION_MODE_STEALTH, 0);

            // Make the player face their target.
            _.ClearAllActions();
            BiowarePosition.TurnToFaceObject(target, activator);

            // Force and Concentration Abilities will display a visual effect during the casting process.
            if (executionType == PerkExecutionType.ForceAbility || 
                executionType == PerkExecutionType.ConcentrationAbility)
            {
                vfxID = VFX_DUR_IOUNSTONE_YELLOW;
                animationID = ANIMATION_LOOPING_CONJURE1;
            }

            if (executionType == PerkExecutionType.ConcentrationAbility)
            {
                activator.SetLocalObject("CONCENTRATION_TARGET", target);
            }

            // If a VFX ID has been specified, play that effect instead of the default one.
            if (vfxID > -1)
            {
                var vfx = _.EffectVisualEffect(vfxID);
                vfx = _.TagEffect(vfx, "ACTIVATION_VFX");
                _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, vfx, activator.Object, activationTime + 0.2f);
            }

            // If an animation has been specified, make the player play that animation now.
            // bypassing if perk is throw saber due to couldn't get the animation to work via db table edit
            if (animationID > -1 && entity.ID != (int) PerkType.ThrowSaber)                
            {
                activator.AssignCommand(() => _.ActionPlayAnimation(animationID, 1.0f, activationTime - 0.1f));
            }

            // Mark player as busy. Busy players can't take other actions (crafting, harvesting, etc.)
            activator.IsBusy = true;
            
            // Non-players can't be interrupted via movement.
            if(!activator.IsPlayer)
            {
                // Begin the check for spell interruption. If the activator moves, the spell will be canceled.
                CheckForSpellInterruption(activator, uuid, activator.Position);
            }

            activator.SetLocalInt(uuid, (int)SpellStatusType.Started);

            // If there's a casting delay, display a timing bar on-screen.
            if (activationTime > 0)
            {
                NWNXPlayer.StartGuiTimingBar(activator, (int)activationTime, string.Empty);
            }

            // Run the FinishAbilityUse event at the end of the activation time.
            int perkID = entity.ID;

            var @event = new OnFinishAbilityUse(activator, uuid, perkID, target, pcPerkLevel, spellTier, armorPenalty);
            activator.DelayEvent(activationTime + 0.2f, @event);
        }

        public static void ApplyCooldown(NWCreature creature, CooldownCategory cooldown, IPerkHandler handler, int spellTier, float armorPenalty)
        {
            if (armorPenalty <= 0.0f) armorPenalty = 1.0f;
            
            // If player has a a cooldown recovery bonus on their equipment, apply that change now.

            if (creature.IsPlayer)
            {
                var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(creature.Object);
                if (effectiveStats.CooldownRecovery > 0)
                {
                    armorPenalty -= (effectiveStats.CooldownRecovery * 0.01f);
                }
            }

            // There's a cap of 50% cooldown reduction from equipment.
            if (armorPenalty < 0.5f)
                armorPenalty = 0.5f;
            
            float finalCooldown = handler.CooldownTime(creature, (float)cooldown.BaseCooldownTime, spellTier) * armorPenalty;
            int cooldownSeconds = (int)finalCooldown;
            int cooldownMillis = (int)((finalCooldown - cooldownSeconds) * 100);
            DateTime unlockDate = DateTime.UtcNow.AddSeconds(cooldownSeconds).AddMilliseconds(cooldownMillis);

            if (creature.IsPlayer)
            {
                PCCooldown pcCooldown = DataService.PCCooldown.GetByPlayerAndCooldownCategoryID(creature.GlobalID, cooldown.ID);
                pcCooldown.DateUnlocked = unlockDate;
                DataService.SubmitDataChange(pcCooldown, DatabaseActionType.Update);
            }
            else
            {
                string unlockDateString = unlockDate.ToString("yyyy-MM-dd hh:mm:ss");
                creature.SetLocalString("ABILITY_COOLDOWN_ID_" + (int)handler.PerkType, unlockDateString);
            }
        }

        private static void CheckForSpellInterruption(NWCreature activator, string spellUUID, Vector position)
        {
            if (activator.GetLocalInt(spellUUID) == (int)SpellStatusType.Completed) return;

            Vector currentPosition = activator.Position;

            if (currentPosition.m_X != position.m_X ||
                currentPosition.m_Y != position.m_Y ||
                currentPosition.m_Z != position.m_Z)
            {
                var effect = activator.Effects.SingleOrDefault(x => _.GetEffectTag(x) == "ACTIVATION_VFX");
                if (effect != null)
                {
                    _.RemoveEffect(activator, effect);
                }

                NWNXPlayer.StopGuiTimingBar(activator, "", -1);
                activator.IsBusy = false;
                activator.SetLocalInt(spellUUID, (int)SpellStatusType.Interrupted);
                activator.SendMessage("Your ability has been interrupted.");
                return;
            }

            _.DelayCommand(0.5f, () => { CheckForSpellInterruption(activator, spellUUID, position); });
        }

        private static void HandleQueueWeaponSkill(NWCreature activator, Data.Entity.Perk entity, IPerkHandler ability, int spellFeatID)
        {
            var perkFeat = DataService.PerkFeat.GetByFeatID(spellFeatID);
            int? cooldownCategoryID = ability.CooldownCategoryID(activator, entity.CooldownCategoryID, perkFeat.PerkLevelUnlocked);
            var cooldownCategory = DataService.CooldownCategory.GetByID(Convert.ToInt32(cooldownCategoryID));
            string queueUUID = Guid.NewGuid().ToString();
            activator.SetLocalInt("ACTIVE_WEAPON_SKILL", entity.ID);
            activator.SetLocalString("ACTIVE_WEAPON_SKILL_UUID", queueUUID);
            activator.SetLocalInt("ACTIVE_WEAPON_SKILL_FEAT_ID", spellFeatID);
            activator.SendMessage("Weapon skill '" + entity.Name + "' queued for next attack.");
            SendAOEMessage(activator, activator.Name + " readies weapon skill '" + entity.Name + "'.");

            ApplyCooldown(activator, cooldownCategory, ability, perkFeat.PerkLevelUnlocked, 0.0f);

            // Player must attack within 30 seconds after queueing or else it wears off.
            _.DelayCommand(30f, () =>
            {
                if (activator.GetLocalString("ACTIVE_WEAPON_SKILL_UUID") == queueUUID)
                {
                    activator.DeleteLocalInt("ACTIVE_WEAPON_SKILL");
                    activator.DeleteLocalString("ACTIVE_WEAPON_SKILL_UUID");
                    activator.DeleteLocalInt("ACTIVE_WEAPON_SKILL_FEAT_ID");
                    activator.SendMessage("Your weapon skill '" + entity.Name + "' is no longer queued.");
                    SendAOEMessage(activator, activator.Name + " no longer has weapon skill '" + entity.Name + "' readied.");
                }
            });
        }

        /// <summary>
        /// Returns the current FP amount of a creature.
        /// </summary>
        /// <param name="creature">The creature whose FP we're getting.</param>
        /// <returns>The amount of FP the creature currently has.</returns>
        public static int GetCurrentFP(NWCreature creature)
        {
            if (creature.IsPlayer)
            {
                var player = DataService.Player.GetByID(creature.GlobalID);
                return player.CurrentFP;
            }
            else
            {
                return creature.GetLocalInt("CURRENT_FP");
            }
        }

        /// <summary>
        /// Sets the current FP amount of a creature to a specific value.
        /// This value must be between 0 and the creature's maximum FP.
        /// </summary>
        /// <param name="creature">The creature whose FP we're setting.</param>
        /// <param name="amount">The amount of FP to set it to.</param>
        public static void SetCurrentFP(NWCreature creature, int amount)
        {
            if (amount < 0) amount = 0;

            if (creature.IsPlayer)
            {
                var player = DataService.Player.GetByID(creature.GlobalID);
                if (amount > player.MaxFP) amount = player.MaxFP;

                player.CurrentFP = amount;
                DataService.SubmitDataChange(player, DatabaseActionType.Update);
            }
            else
            {
                int maxFP = creature.GetLocalInt("MAX_FP");
                if (amount > maxFP) amount = maxFP;
                creature.SetLocalInt("CURRENT_FP", amount);
            }
        }

        /// <summary>
        /// Retrieves the maximum FP a creature has.
        /// </summary>
        /// <param name="creature">The creature whose max FP we're getting.</param>
        /// <returns>The max FP a creature has.</returns>
        public static int GetMaxFP(NWCreature creature)
        {
            if (creature.IsPlayer)
            {
                var player = DataService.Player.GetByID(creature.GlobalID);
                return player.MaxFP;
            }
            else
            {
                return creature.GetLocalInt("MAX_FP");
            }
        }

        /// <summary>
        /// Sets the max FP for a creature to a specific amount.
        /// </summary>
        /// <param name="creature">The creature whose max FP we're setting.</param>
        /// <param name="amount">The amount of max FP to assign to the creature.</param>
        public static void SetMaxFP(NWCreature creature, int amount)
        {
            if (amount < 0) amount = 0;

            if (creature.IsPlayer)
            {
                var player = DataService.Player.GetByID(creature.GlobalID);
                player.MaxFP = amount;
                if (player.CurrentFP > player.MaxFP)
                    player.CurrentFP = player.MaxFP;
                DataService.SubmitDataChange(player, DatabaseActionType.Update);
            }
            else
            {
                if(creature.GetLocalInt("CURRENT_FP") > amount)
                    creature.SetLocalInt("CURRENT_FP", amount);
                creature.SetLocalInt("MAX_FP", amount);
            }
        }

        public static Player RestorePlayerFP(NWPlayer oPC, int amount, Player entity)
        {
            entity.CurrentFP = entity.CurrentFP + amount;
            if (entity.CurrentFP > entity.MaxFP)
                entity.CurrentFP = entity.MaxFP;

            oPC.SendMessage(ColorTokenService.Custom("FP: " + entity.CurrentFP + " / " + entity.MaxFP, 32, 223, 219));

            return entity;
        }

        public static void RestorePlayerFP(NWPlayer oPC, int amount)
        {
            Player entity = DataService.Player.GetByID(oPC.GlobalID);
            RestorePlayerFP(oPC, amount, entity);
            DataService.SubmitDataChange(entity, DatabaseActionType.Update);
        }

        private static void OnHitCastSpell()
        {
            NWPlayer oPC = NWGameObject.OBJECT_SELF;
            if (!oPC.IsValid) return;

            NWObject oTarget = _.GetSpellTargetObject();
            NWItem oItem = _.GetSpellCastItem();

            // If this method was triggered by our own armor (from getting hit), return. 
            if (oItem.BaseItemType == BASE_ITEM_ARMOR) return;

            // Flag this attack as physical so that the damage scripts treat it properly.
            LoggingService.Trace(TraceComponent.LastAttack, "Setting attack type from " + oPC.GlobalID + " against " + _.GetName(oTarget) + " to physical (" + ATTACK_PHYSICAL.ToString() + ")");
            oTarget.SetLocalInt(LAST_ATTACK + oPC.GlobalID, ATTACK_PHYSICAL);

            HandleGrenadeProficiency(oPC, oTarget);
            HandlePlasmaCellPerk(oPC, oTarget);
            int activeWeaponSkillID = oPC.GetLocalInt("ACTIVE_WEAPON_SKILL");
            if (activeWeaponSkillID <= 0) return;
            int activeWeaponSkillFeatID = oPC.GetLocalInt("ACTIVE_WEAPON_SKILL_FEAT_ID");
            if (activeWeaponSkillFeatID < 0) activeWeaponSkillFeatID = -1;

            PCPerk entity = DataService.PCPerk.GetByPlayerAndPerkID(oPC.GlobalID, activeWeaponSkillID);
            var perk = DataService.Perk.GetByID(entity.PerkID);
            var perkFeat = DataService.PerkFeat.GetByFeatID(activeWeaponSkillFeatID);
            var handler = PerkService.GetPerkHandler(activeWeaponSkillID);

            string canCast = handler.CanCastSpell(oPC, oTarget, perkFeat.PerkLevelUnlocked);
            if (string.IsNullOrWhiteSpace(canCast))
            {
                handler.OnImpact(oPC, oTarget, entity.PerkLevel, perkFeat.PerkLevelUnlocked);

                if (oTarget.IsNPC)
                {
                    ApplyEnmity(oPC, oTarget.Object, perk);
                }
            }
            else oPC.SendMessage(canCast);

            oPC.DeleteLocalString("ACTIVE_WEAPON_SKILL_UUID");
            oPC.DeleteLocalInt("ACTIVE_WEAPON_SKILL");
            oPC.DeleteLocalInt("ACTIVE_WEAPON_SKILL_FEAT_ID");

        }

        public static void HandlePlasmaCellPerk(NWPlayer player, NWObject target)
        {
            if (!player.IsPlayer) return;
            if (_.GetHasFeat((int)CustomFeatType.PlasmaCell, player) == FALSE) return;  // Check if player has the perk
            if (player.RightHand.CustomItemType != CustomItemType.BlasterPistol &&
                player.RightHand.CustomItemType != CustomItemType.BlasterRifle) return; // Check if player has the right weapons
            if (target.GetLocalInt("TRANQUILIZER_EFFECT_FIRST_RUN") == _.TRUE) return;   // Check if Tranquilizer is on to avoid conflict
            if (player.GetLocalInt("PLASMA_CELL_TOGGLE_OFF") == _.TRUE) return;  // Check if Plasma Cell toggle is on or off
            if (target.GetLocalInt("TRANQUILIZER_EFFECT_FIRST_RUN") == _.TRUE) return;

            int perkLevel = PerkService.GetCreaturePerkLevel(player, PerkType.PlasmaCell);
            int chance;
            CustomEffectType[] damageTypes;
            switch (perkLevel)
            {
                case 1:
                    chance = 10;
                    damageTypes = new[] { CustomEffectType.FireCell };
                    break;
                case 2:
                    chance = 10;
                    damageTypes = new[] { CustomEffectType.FireCell, CustomEffectType.ElectricCell };
                    break;
                case 3:
                    chance = 20;
                    damageTypes = new[] { CustomEffectType.FireCell, CustomEffectType.ElectricCell };
                    break;
                case 4:
                    chance = 20;
                    damageTypes = new[] { CustomEffectType.FireCell, CustomEffectType.ElectricCell, CustomEffectType.SonicCell };
                    break;
                case 5:
                    chance = 30;
                    damageTypes = new[] { CustomEffectType.FireCell, CustomEffectType.ElectricCell, CustomEffectType.SonicCell };
                    break;
                case 6:
                    chance = 30;
                    damageTypes = new[] { CustomEffectType.FireCell, CustomEffectType.ElectricCell, CustomEffectType.SonicCell, CustomEffectType.AcidCell };
                    break;
                case 7:
                    chance = 40;
                    damageTypes = new[] { CustomEffectType.FireCell, CustomEffectType.ElectricCell, CustomEffectType.SonicCell, CustomEffectType.AcidCell };
                    break;
                case 8:
                    chance = 40;
                    damageTypes = new[] { CustomEffectType.FireCell, CustomEffectType.ElectricCell, CustomEffectType.SonicCell, CustomEffectType.AcidCell, CustomEffectType.IceCell };
                    break;
                case 9:
                    chance = 50;
                    damageTypes = new[] { CustomEffectType.FireCell, CustomEffectType.ElectricCell, CustomEffectType.SonicCell, CustomEffectType.AcidCell, CustomEffectType.IceCell };
                    break;
                case 10:
                    chance = 50;
                    damageTypes = new[] { CustomEffectType.FireCell, CustomEffectType.ElectricCell, CustomEffectType.SonicCell, CustomEffectType.AcidCell, CustomEffectType.IceCell, CustomEffectType.DivineCell };
                    break;

                default: return;
            }

            foreach (var effect in damageTypes)
            {
                if (RandomService.D100(1) <= chance)
                {
                    CustomEffectService.ApplyCustomEffect(player, target.Object, effect, RandomService.D6(1), perkLevel, null);
                }
            }

        }

        private static void HandleGrenadeProficiency(NWPlayer oPC, NWObject target)
        {
            NWItem weapon = _.GetSpellCastItem();
            if (weapon.BaseItemType != BASE_ITEM_GRENADE) return;

            int perkLevel = PerkService.GetCreaturePerkLevel(oPC, PerkType.GrenadeProficiency);
            int chance = 10 * perkLevel;
            float duration;

            switch (perkLevel)
            {
                case 1:
                case 2:
                case 3:
                case 4:
                    duration = 6;
                    break;
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                    duration = 9;
                    break;
                default: return;
            }


            if (RandomService.D100(1) <= chance)
            {
                _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, _.EffectKnockdown(), target, duration);
            }
        }

        /// <summary>
        /// Looks at the creature's feats and if any of them are Perks, stores the highest
        /// level as a local variable on the creature. This variable is later used when the
        /// creature actually uses the feat.
        /// Also registers all of the available PerkFeats (highest tier) on the creature's Data.
        /// This data is also used in the AI to make decisions quicker.
        /// </summary>
        /// <param name="self">The creature whose perks we're registering.</param>
        private static void RegisterCreaturePerks(NWCreature self)
        {
            var perkFeatCache = new Dictionary<int, AIPerkDetails>();
            var featIDs = new List<int>();

            // Add all feats the creature has to the list.
            int featCount = NWNXCreature.GetFeatCount(self);
            for (int x = 0; x <= featCount - 1; x++)
            {
                int featID = NWNXCreature.GetFeatByIndex(self, x);
                featIDs.Add(featID);
            }

            bool hasPerkFeat = false;
            // Retrieve perk feat information for only those feats registered as a perk.
            var perkFeats = DataService.PerkFeat.GetAllByIDs(featIDs);

            // Mark the highest perk level on the creature.
            foreach (var perkFeat in perkFeats)
            {
                int level = self.GetLocalInt("PERK_LEVEL_" + perkFeat.PerkID);
                if (level >= perkFeat.PerkLevelUnlocked) continue;

                var perk = DataService.Perk.GetByID(perkFeat.PerkID);
                self.SetLocalInt("PERK_LEVEL_" + perkFeat.PerkID, perkFeat.PerkLevelUnlocked);
                perkFeatCache[perkFeat.PerkID] = new AIPerkDetails(perkFeat.FeatID, perk.ExecutionTypeID);
                hasPerkFeat = true;
            }

            // If a builder sets a perk feat but forgets to set the FP, do it automatically.
            if(hasPerkFeat && self.GetLocalInt("MAX_FP") <= 0)
            {
                int fp = 50;
                fp += (self.IntelligenceModifier + self.WisdomModifier + self.CharismaModifier) * 5;
                SetMaxFP(self, fp);
                SetCurrentFP(self, fp);
            }

            if (hasPerkFeat)
            {
                // Store a new dictionary containing PerkID and FeatID onto the creature's data.
                // This is later used in the AI processing for decision making.
                self.Data["PERK_FEATS"] = perkFeatCache;
            }
        }

        /// <summary>
        /// Sends a message to all nearby creatures within 10 meters.
        /// </summary>
        /// <param name="sender">The sender of the message. Used when determining distance.</param>
        /// <param name="message">The message to send to all nearby creatures.</param>
        private static void SendAOEMessage(NWCreature sender, string message)
        {
            const float MaxDistance = 10.0f;
            int nth = 1;
            NWCreature nearby = _.GetNearestCreature(CREATURE_TYPE_IS_ALIVE, TRUE, sender, nth);
            while (nearby.IsValid && GetDistanceBetween(sender, nearby) <= MaxDistance)
            {
                nearby.SendMessage(message);
                nth++;
                nearby = _.GetNearestCreature(CREATURE_TYPE_IS_ALIVE, TRUE, sender, nth);
            }
        }
    }
}
