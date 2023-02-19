using System;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.StatusEffectDefinition.StatusEffectData;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatusEffectService;
using Item = SWLOR.Game.Server.Service.Item;

namespace SWLOR.Game.Server.Feature
{
    public static class UsePerkFeat
    {
        private enum ActivationStatus
        {
            Invalid = 0,
            Started = 1,
            Interrupted = 2,
            Completed = 3
        }

        // Variable names for queued abilities.
        private const string ActiveAbilityIdName = "ACTIVE_ABILITY_ID";
        private const string ActiveAbilityFeatIdName = "ACTIVE_ABILITY_FEAT_ID";
        private const string ActiveAbilityEffectivePerkLevelName = "ACTIVE_ABILITY_EFFECTIVE_PERK_LEVEL";

        /// <summary>
        /// When a creature uses any feat, this will check and see if the feat is registered with the perk system.
        /// If it is, requirements to use the feat will be checked and then the ability will activate.
        /// If there are errors at any point in this process, the creature will be notified and the execution will end.
        /// </summary>
        [NWNEventHandler("feat_use_bef")]
        public static void UseFeat()
        {
            var activator = OBJECT_SELF;
            var target = StringToObject(EventsPlugin.GetEventData("TARGET_OBJECT_ID"));
            var targetArea = StringToObject(EventsPlugin.GetEventData("AREA_OBJECT_ID"));
            var targetPosition = Vector3(
                (float)Convert.ToDouble(EventsPlugin.GetEventData("TARGET_POSITION_X")),
                (float)Convert.ToDouble(EventsPlugin.GetEventData("TARGET_POSITION_Y")),
                (float)Convert.ToDouble(EventsPlugin.GetEventData("TARGET_POSITION_Z"))
            );

            // If we have a valid target, use its position
            if (GetIsObjectValid(target))
            {
                targetPosition = GetPosition(target);
            }

            var targetLocation = Location(targetArea, targetPosition, 0.0f);

            var feat = (FeatType)Convert.ToInt32(EventsPlugin.GetEventData("FEAT_ID"));
            if (!Ability.IsFeatRegistered(feat)) return;
            var ability = Ability.GetAbilityDetail(feat);

            // Creature cannot use the feat.
            var effectivePerkLevel =
                ability.EffectiveLevelPerkType == PerkType.Invalid
                    ? 1 // If there's not an associated perk, default level to 1.
                    : Perk.GetEffectivePerkLevel(activator, ability.EffectiveLevelPerkType);

            // Weapon abilities are queued for the next time the activator's attack lands on an enemy.
            if (ability.ActivationType == AbilityActivationType.Weapon)
            {
                if (Ability.CanUseAbility(activator, target, feat, effectivePerkLevel, targetLocation))
                {
                    if(ability.DisplaysActivationMessage)
                        Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} queues {ability.Name} for the next attack.");
                    QueueWeaponAbility(activator, ability, feat);
                }
            }
            // Concentration abilities are triggered once per tick.
            else if (ability.ActivationType == AbilityActivationType.Concentration)
            {
                // Using the same concentration feat ends the effect.
                var activeConcentrationAbility = Ability.GetActiveConcentration(activator);
                if (activeConcentrationAbility.Feat == feat)
                {
                    Ability.EndConcentrationAbility(activator);
                }
                else
                {
                    if (Ability.CanUseAbility(activator, target, feat, effectivePerkLevel, targetLocation))
                    {
                        ActivateAbility(activator, target, feat, ability, targetLocation);
                    }
                }
            }
            // All other abilities are funneled through the same process.
            else
            {
                if (Ability.CanUseAbility(activator, target, feat, effectivePerkLevel, targetLocation))
                {
                    if (GetIsObjectValid(target))
                    {
                        if (ability.DisplaysActivationMessage)
                            Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} readies {ability.Name} on {GetName(target)}.");
                    }
                    else
                    {
                        if (ability.DisplaysActivationMessage)
                            Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} readies {ability.Name}.");
                    }
                    
                    ActivateAbility(activator, target, feat, ability, targetLocation);
                }
            }
        }

        /// <summary>
        /// Applies effects to the activator for each requirement.
        /// Depending on the ability type, this may be called before or after the ability has finished.
        /// </summary>
        /// <param name="activator">The activator of the ability.</param>
        /// <param name="ability">The ability details</param>
        private static void ApplyRequirementEffects(uint activator, AbilityDetail ability)
        {
            foreach (var req in ability.Requirements)
            {
                req.AfterActivationAction(activator);
            }
        }


        /// <summary>
        /// Handles casting abilities. These can be combat-related or casting-related and may or may not have a casting delay.
        /// Requirement reductions (FP, STM, etc) are applied after the casting has completed.
        /// In the event there is no casting delay, the reductions are applied immediately.
        /// </summary>
        /// <param name="activator">The creature activating the ability.</param>
        /// <param name="target">The target of the ability</param>
        /// <param name="feat">The type of feat associated with this ability.</param>
        /// <param name="ability">The ability details</param>
        /// <param name="targetLocation">The targeted location</param>
        private static void ActivateAbility(
            uint activator,
            uint target,
            FeatType feat,
            AbilityDetail ability,
            Location targetLocation)
        {
            // Activation delay is increased if player is equipped with heavy or light armor.
            float CalculateActivationDelay()
            {
                const float HeavyArmorPenalty = 2.0f;

                var armorPenalty = 1.0f;
                var penaltyMessage = string.Empty;
                for (var slot = 0; slot < NumberOfInventorySlots; slot++)
                {
                    var item = GetItemInSlot((InventorySlot)slot, activator);
                    var armorType = Item.GetArmorType(item);
                    if (armorType == ArmorType.Heavy && !ability.IgnoreHeavyArmorPenalty)
                    {
                        armorPenalty = HeavyArmorPenalty;
                        penaltyMessage = "Heavy armor slows your activation speed by 100%.";
                    }

                    // If we found heavy armor, we can exit early. Anything else requires us to iterate over the rest of the items.
                    if (armorPenalty >= HeavyArmorPenalty) break;
                }

                // Notify player if needed.
                if (!string.IsNullOrWhiteSpace(penaltyMessage))
                {
                    SendMessageToPC(activator, penaltyMessage);
                }

                var abilityDelay = ability.ActivationDelay?.Invoke(activator, target, ability.AbilityLevel) ?? 0.0f;
                return abilityDelay * armorPenalty;
            }

            // Handles displaying animation and visual effects.
            void ProcessAnimationAndVisualEffects(float delay)
            {
                // Force out of stealth
                if (GetActionMode(activator, ActionMode.Stealth))
                    SetActionMode(activator, ActionMode.Stealth, false);

                AssignCommand(activator, () => ClearAllActions());
                BiowarePosition.TurnToFaceObject(target, activator);

                // Display a casting visual effect if one has been specified.
                if (ability.ActivationVisualEffect != VisualEffect.None)
                {
                    var vfx = TagEffect(EffectVisualEffect(ability.ActivationVisualEffect), "ACTIVATION_VFX");
                    ApplyEffectToObject(DurationType.Temporary, vfx, activator, delay + 0.2f);
                }

                // Casted types play an animation of casting.
                if (ability.ActivationType == AbilityActivationType.Casted &&
                    ability.AnimationType != Animation.Invalid)
                {
                    var animationLength = delay - 0.2f;
                    if (animationLength < 0f)
                        animationLength = 0f;

                    AssignCommand(activator, () => ActionPlayAnimation(ability.AnimationType, 1.0f, animationLength));
                }
            }

            // Recursive function which checks if player has moved since starting the casting.
            void CheckForActivationInterruption(string activationId, Vector3 originalPosition)
            {
                if (!GetIsPC(activator)) return;

                // Completed abilities should no longer run.
                var status = GetLocalInt(activator, activationId);
                if (status == (int)ActivationStatus.Completed || status == (int)ActivationStatus.Invalid) return;

                var currentPosition = GetPosition(activator);

                if (currentPosition.X != originalPosition.X ||
                    currentPosition.Y != originalPosition.Y ||
                    currentPosition.Z != originalPosition.Z)
                {
                    RemoveEffectByTag(activator, "ACTIVATION_VFX");

                    PlayerPlugin.StopGuiTimingBar(activator, string.Empty);
                    Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)}'s ability has been interrupted.");
                    return;
                }

                DelayCommand(0.5f, () => CheckForActivationInterruption(activationId, originalPosition));
            }

            // This method is called after the delay of the ability has finished.
            void CompleteActivation(string id, float abilityRecastDelay)
            {
                DeleteLocalInt(activator, id);
                Activity.ClearBusy(activator);

                // Moved during casting or activator died. Cancel the activation.
                if (GetLocalInt(activator, id) == (int)ActivationStatus.Interrupted || GetCurrentHitPoints(activator) <= 0)
                    return;
                
                if (!Ability.CanUseAbility(activator, target, feat, ability.AbilityLevel, targetLocation))
                    return;
                
                ApplyRequirementEffects(activator, ability);
                ability.ImpactAction?.Invoke(activator, target, ability.AbilityLevel, targetLocation);
                Recast.ApplyRecastDelay(activator, ability.RecastGroup, abilityRecastDelay, false);

                if (ability.ConcentrationStatusEffectType != StatusEffectType.Invalid)
                {
                    Ability.StartConcentrationAbility(activator, target, feat, ability.ConcentrationStatusEffectType);
                }

                // If this is an attack make the NPC react.
                Enmity.AttackHighestEnmityTarget(target);
                
                if (!GetIsPC(activator))
                {
                    var combatRoundEndScript = GetEventScript(activator, EventScript.Creature_OnEndCombatRound);
                    ExecuteScript(combatRoundEndScript, activator);
                }
            }

            // Begin the main process
            var activationId = Guid.NewGuid().ToString();
            var activationDelay = CalculateActivationDelay();
            var recastDelay = ability.RecastDelay?.Invoke(activator) ?? 0f;
            var position = GetPosition(activator);
            ProcessAnimationAndVisualEffects(activationDelay);
            CheckForActivationInterruption(activationId, position);
            SetLocalInt(activator, activationId, (int)ActivationStatus.Started);

            var executeImpact = ability.ActivationAction == null 
                ? true
                : ability.ActivationAction?.Invoke(activator, target, ability.AbilityLevel, targetLocation);

            if (executeImpact == true)
            {
                if (GetIsPC(activator))
                {
                    if (activationDelay > 0.0f)
                    {
                        PlayerPlugin.StartGuiTimingBar(activator, activationDelay, string.Empty);
                    }
                }

                Activity.SetBusy(activator, ActivityStatusType.AbilityActivation);
                DelayCommand(activationDelay, () => CompleteActivation(activationId, recastDelay));

                // If currently attacking a target, re-attack it after the end of the activation period.
                // This mitigates the issue where a melee fighter's combat is disrupted for using an ability.
                if (GetCurrentAction(activator) == ActionType.AttackObject)
                {
                    var attackTarget = GetAttackTarget(activator);
                    DelayCommand(activationDelay + 0.1f, () =>
                    {
                        AssignCommand(activator, () => ActionAttack(attackTarget));
                    });
                }
            }
        }

        /// <summary>
        /// Handles queuing a weapon ability for the activator's next attack.
        /// Local variables are set on the activator which are picked up the next time the activator's weapon hits a target.
        /// If the activator does not hit a target within 30 seconds, the queued ability wears off automatically.
        /// Requirement reductions (FP, STM, etc) are applied as soon as the ability is queued.
        /// </summary>
        /// <param name="activator">The creature activating the ability.</param>
        /// <param name="ability">The ability details</param>
        /// <param name="feat">The feat being activated</param>
        private static void QueueWeaponAbility(uint activator, AbilityDetail ability, FeatType feat)
        {
            var abilityId = Guid.NewGuid().ToString();
            // Assign local variables which will be picked up on the next weapon OnHit event by this player.
            SetLocalString(activator, ActiveAbilityIdName, abilityId);
            SetLocalInt(activator, ActiveAbilityFeatIdName, (int)feat);
            SetLocalInt(activator, ActiveAbilityEffectivePerkLevelName, ability.AbilityLevel);
            
            ApplyRequirementEffects(activator, ability);

            var abilityRecastDelay = ability.RecastDelay?.Invoke(activator) ?? 0.0f;
            Recast.ApplyRecastDelay(activator, ability.RecastGroup, abilityRecastDelay, false);

            // Activator must attack within 30 seconds after queueing or else it wears off.
            DelayCommand(30.0f, () =>
            {
                DequeueWeaponAbility(activator, ability.DisplaysActivationMessage);
            });
        }

        public static void DequeueWeaponAbility(uint target, bool sendMessage = true)
        {
            var abilityId = GetLocalString(target, ActiveAbilityIdName);
            if (string.IsNullOrWhiteSpace(abilityId))
                return;

            var featType = (FeatType)GetLocalInt(target, ActiveAbilityIdName);
            var abilityDetail = Ability.GetAbilityDetail(featType);

            // Remove the local variables.
            DeleteLocalString(target, ActiveAbilityIdName);
            DeleteLocalInt(target, ActiveAbilityFeatIdName);
            DeleteLocalInt(target, ActiveAbilityEffectivePerkLevelName);

            // Notify the activator and nearby players
            SendMessageToPC(target, $"Your weapon ability {abilityDetail.Name} is no longer queued.");

            if (sendMessage)
                Messaging.SendMessageNearbyToPlayers(target, $"{GetName(target)} no longer has weapon ability {abilityDetail.Name} readied.");
        }

        /// <summary>
        /// When a player's weapon hits a target, if an ability is queued, that ability will be executed.
        /// </summary>
        [NWNEventHandler("item_on_hit")]
        public static void ProcessQueuedWeaponAbility()
        {
            var activator = OBJECT_SELF;
            if (!GetIsObjectValid(activator)) return;

            var target = GetSpellTargetObject();
            var targetLocation = GetLocation(target);
            var item = GetSpellCastItem();

            // If this method was triggered by our own armor (from getting hit), return. 
            if (GetBaseItemType(item) == BaseItem.Armor) return;

            var activeWeaponAbility = (FeatType)GetLocalInt(activator, ActiveAbilityFeatIdName);
            var activeAbilityEffectivePerkLevel = GetLocalInt(activator, ActiveAbilityEffectivePerkLevelName);

            if (!Ability.IsFeatRegistered(activeWeaponAbility)) return;

            var abilityDetail = Ability.GetAbilityDetail(activeWeaponAbility);
            abilityDetail.ImpactAction?.Invoke(activator, target, activeAbilityEffectivePerkLevel, targetLocation);

            DeleteLocalString(activator, ActiveAbilityIdName);
            DeleteLocalInt(activator, ActiveAbilityFeatIdName);
            DeleteLocalInt(activator, ActiveAbilityEffectivePerkLevelName);
        }

        /// <summary>
        /// Whenever a player enters the server, any temporary variables related to ability execution
        /// will be removed from their PC.
        /// </summary>
        [NWNEventHandler("mod_enter")]
        public static void ClearTemporaryQueuedVariables()
        {
            var player = GetEnteringObject();

            ClearQueuedAbility(player);
        }

        /// <summary>
        /// Whenever a player starts resting, clear any queued abilities.
        /// </summary>
        [NWNEventHandler("rest_started")]
        public static void ClearTemporaryQueuedVariablesOnRest()
        {
            ClearQueuedAbility(OBJECT_SELF);
        }

        /// <summary>
        /// Whenever a player equips an item, clear any queued abilities.
        /// </summary>
        [NWNEventHandler("item_eqp_bef")]
        public static void ClearTemporaryQueuedVariablesOnEquip()
        {
            ClearQueuedAbility(OBJECT_SELF);
        }

        /// <summary>
        /// Clears the queued ability of a player.
        /// </summary>
        /// <param name="player">The player to clear</param>
        private static void ClearQueuedAbility(uint player)
        {
            DeleteLocalString(player, ActiveAbilityIdName);
            DeleteLocalInt(player, ActiveAbilityFeatIdName);
            DeleteLocalInt(player, ActiveAbilityEffectivePerkLevelName);
        }
    }
}
