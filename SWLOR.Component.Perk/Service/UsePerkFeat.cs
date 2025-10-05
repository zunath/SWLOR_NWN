using Microsoft.Extensions.DependencyInjection;
using System.Numerics;
using SWLOR.Component.Perk.Contracts;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Bioware;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.StatusEffect.Enums;

namespace SWLOR.Component.Perk.Service
{
    public class UsePerkFeat : IUsePerkFeat
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventsPluginService _eventsPlugin;
        private readonly IPlayerPluginService _playerPlugin;
        
        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IAbilityService> _abilityService;
        private readonly Lazy<IPerkService> _perkService;
        private readonly Lazy<IItemService> _itemService;
        private readonly Lazy<IRecastService> _recastService;
        private readonly Lazy<IEnmityService> _enmityService;
        private readonly Lazy<IActivityService> _activityService;
        private readonly Lazy<IMessagingService> _messagingService;
        
        private IAbilityService AbilityService => _abilityService.Value;
        private IPerkService PerkService => _perkService.Value;
        private IItemService ItemService => _itemService.Value;
        private IRecastService RecastService => _recastService.Value;
        private IEnmityService EnmityService => _enmityService.Value;
        private IActivityService ActivityService => _activityService.Value;
        private IMessagingService MessagingService => _messagingService.Value;

        public UsePerkFeat(IServiceProvider serviceProvider, IEventsPluginService eventsPlugin, IPlayerPluginService playerPlugin)
        {
            _serviceProvider = serviceProvider;
            _eventsPlugin = eventsPlugin;
            _playerPlugin = playerPlugin;
            
            // Initialize lazy services
            _abilityService = new Lazy<IAbilityService>(() => _serviceProvider.GetRequiredService<IAbilityService>());
            _perkService = new Lazy<IPerkService>(() => _serviceProvider.GetRequiredService<IPerkService>());
            _itemService = new Lazy<IItemService>(() => _serviceProvider.GetRequiredService<IItemService>());
            _recastService = new Lazy<IRecastService>(() => _serviceProvider.GetRequiredService<IRecastService>());
            _enmityService = new Lazy<IEnmityService>(() => _serviceProvider.GetRequiredService<IEnmityService>());
            _activityService = new Lazy<IActivityService>(() => _serviceProvider.GetRequiredService<IActivityService>());
            _messagingService = new Lazy<IMessagingService>(() => _serviceProvider.GetRequiredService<IMessagingService>());
        }
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
        /// Breaks stealth and invisibility effects if the ability is configured to do so.
        /// </summary>
        /// <param name="activator">The creature using the ability</param>
        /// <param name="ability">The ability details</param>
        private void HandleStealthBreaking(uint activator, AbilityDetail ability)
        {
            if (!ability.BreaksStealth) return;

            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionModeType.Stealth))
                SetActionMode(activator, ActionModeType.Stealth, false);

            // Remove invisibility effects (stealth generator)
            RemoveEffect(activator, EffectScriptType.Invisibility, EffectScriptType.ImprovedInvisibility);
        }

        /// <summary>
        /// When a creature uses any feat, this will check and see if the feat is registered with the perk system.
        /// If it is, requirements to use the feat will be checked and then the ability will activate.
        /// If there are errors at any point in this process, the creature will be notified and the execution will end.
        /// </summary>
        public void UseFeat()
        {
            var activator = OBJECT_SELF;
            var target = StringToObject(_eventsPlugin.GetEventData("TARGET_OBJECT_ID"));
            var targetArea = StringToObject(_eventsPlugin.GetEventData("AREA_OBJECT_ID"));
            var targetPosition = Vector3(
                (float)Convert.ToDouble(_eventsPlugin.GetEventData("TARGET_POSITION_X")),
                (float)Convert.ToDouble(_eventsPlugin.GetEventData("TARGET_POSITION_Y")),
                (float)Convert.ToDouble(_eventsPlugin.GetEventData("TARGET_POSITION_Z"))
            );

            // If we have a valid target, use its position
            if (GetIsObjectValid(target))
            {
                targetPosition = GetPosition(target);
            }

            var targetLocation = Location(targetArea, targetPosition, 0.0f);

            var feat = (FeatType)Convert.ToInt32(_eventsPlugin.GetEventData("FEAT_ID"));
            if (!AbilityService.IsFeatRegistered(feat)) return;
            var ability = AbilityService.GetAbilityDetail(feat);

            // Creature cannot use the feat.
            var effectivePerkLevel =
                ability.EffectiveLevelPerkType == PerkType.Invalid
                    ? 1 // If there's not an associated perk, default level to 1.
                    : PerkService.GetPerkLevel(activator, ability.EffectiveLevelPerkType);

            // Weapon abilities are queued for the next time the activator's attack lands on an enemy.
            if (ability.ActivationType == AbilityActivationType.Weapon)
            {
                if (AbilityService.CanUseAbility(activator, target, feat, effectivePerkLevel, targetLocation))
                {
                    if(ability.DisplaysActivationMessage)
                        MessagingService.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} queues {ability.Name} for the next attack.");
                    QueueWeaponAbility(activator, ability, feat);
                }
            }
            // Concentration abilities are triggered once per tick.
            else if (ability.ActivationType == AbilityActivationType.Concentration)
            {
                // Using the same concentration feat ends the effect.
                var activeConcentrationAbility = AbilityService.GetActiveConcentration(activator);
                if (activeConcentrationAbility.Feat == feat)
                {
                    AbilityService.EndConcentrationAbility(activator);
                }
                else
                {
                    if (AbilityService.CanUseAbility(activator, target, feat, effectivePerkLevel, targetLocation))
                    {
                        ActivateAbility(activator, target, feat, ability, targetLocation);
                    }
                }
            }
            // All other abilities are funneled through the same process.
            else
            {
                if (AbilityService.CanUseAbility(activator, target, feat, effectivePerkLevel, targetLocation))
                {
                    if (GetIsObjectValid(target))
                    {
                        if (ability.DisplaysActivationMessage)
                            MessagingService.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} readies {ability.Name} on {GetName(target)}.");
                    }
                    else
                    {
                        if (ability.DisplaysActivationMessage)
                            MessagingService.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} readies {ability.Name}.");
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
        private void ApplyRequirementEffects(uint activator, AbilityDetail ability)
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
        private void ActivateAbility(
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
                    var item = GetItemInSlot((InventorySlotType)slot, activator);
                    var armorType = ItemService.GetArmorType(item);
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
                if (GetActionMode(activator, ActionModeType.Stealth))
                    SetActionMode(activator, ActionModeType.Stealth, false);

                AssignCommand(activator, () => ClearAllActions());
                BiowarePosition.TurnToFaceObject(target, activator);

                // Display a casting visual effect if one has been specified.
                if (ability.ActivationVisualEffect != VisualEffectType.None)
                {
                    var vfx = TagEffect(EffectVisualEffect(ability.ActivationVisualEffect), "ACTIVATION_VFX");
                    ApplyEffectToObject(DurationType.Temporary, vfx, activator, delay + 0.2f);
                }

                // Casted types play an animation of casting.
                if (ability.ActivationType == AbilityActivationType.Casted &&
                    ability.AnimationType != AnimationType.Invalid)
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
                    _playerPlugin.StopGuiTimingBar(activator, string.Empty);
                    MessagingService.SendMessageNearbyToPlayers(activator, $"{GetName(activator)}'s ability has been interrupted.");
                    SetLocalInt(activator, activationId, (int)ActivationStatus.Interrupted);
                    return;
                }

                DelayCommand(0.5f, () => CheckForActivationInterruption(activationId, originalPosition));
            }

            // This method is called after the delay of the ability has finished.
            void CompleteActivation(string activationId, float abilityRecastDelay)
            {
                ActivityService.ClearBusy(activator);

                // Moved during casting or activator died. Cancel the activation.
                if (GetLocalInt(activator, activationId) == (int)ActivationStatus.Interrupted || GetCurrentHitPoints(activator) <= 0)
                    return;
                

                if (!AbilityService.CanUseAbility(activator, target, feat, ability.AbilityLevel, targetLocation))
                    return;

                DeleteLocalInt(activator, activationId);

                ApplyRequirementEffects(activator, ability);
                HandleStealthBreaking(activator, ability);
                ability.ImpactAction?.Invoke(activator, target, ability.AbilityLevel, targetLocation);
                RecastService.ApplyRecastDelay(activator, ability.RecastGroup, abilityRecastDelay, false);

                if (ability.ConcentrationStatusEffectType != StatusEffectType.Invalid)
                {
                    AbilityService.StartConcentrationAbility(activator, target, feat, ability.ConcentrationStatusEffectType);
                }

                // If this is an attack make the NPC react.
                EnmityService.AttackHighestEnmityTarget(target);
                
                if (!GetIsPC(activator))
                {
                    var combatRoundEndScript = GetEventScript(activator, EventScriptType.Creature_OnEndCombatRound);
                    ExecuteNWScript(combatRoundEndScript, activator);
                }
            }

            // Begin the main process
            var activationId = Guid.NewGuid().ToString();
            var activationDelay = CalculateActivationDelay();
            var recastDelay = ability.RecastDelay?.Invoke(activator) ?? 0f;
            var position = GetPosition(activator);
            ProcessAnimationAndVisualEffects(activationDelay);
            SetLocalInt(activator, activationId, (int)ActivationStatus.Started);
            CheckForActivationInterruption(activationId, position);

            var executeImpact = ability.ActivationAction == null 
                ? true
                : ability.ActivationAction?.Invoke(activator, target, ability.AbilityLevel, targetLocation);

            if (executeImpact == true)
            {
                if (GetIsPC(activator))
                {
                    if (activationDelay > 0.0f)
                    {
                        _playerPlugin.StartGuiTimingBar(activator, activationDelay, string.Empty);
                    }
                }

                ActivityService.SetBusy(activator, ActivityStatusType.AbilityActivation);
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
        private void QueueWeaponAbility(uint activator, AbilityDetail ability, FeatType feat)
        {
            var abilityId = Guid.NewGuid().ToString();
            // Assign local variables which will be picked up on the next weapon OnHit event by this player.
            SetLocalString(activator, ActiveAbilityIdName, abilityId);
            SetLocalInt(activator, ActiveAbilityFeatIdName, (int)feat);
            SetLocalInt(activator, ActiveAbilityEffectivePerkLevelName, ability.AbilityLevel);
            
            ApplyRequirementEffects(activator, ability);

            var abilityRecastDelay = ability.RecastDelay?.Invoke(activator) ?? 0.0f;
            RecastService.ApplyRecastDelay(activator, ability.RecastGroup, abilityRecastDelay, false);

            // Activator must attack within 30 seconds after queueing or else it wears off.
            DelayCommand(30.0f, () =>
            {
                DequeueWeaponAbility(activator, ability.DisplaysActivationMessage);
            });
        }

        public void DequeueWeaponAbility(uint target, bool sendMessage = true)
        {
            var abilityId = GetLocalString(target, ActiveAbilityIdName);
            if (string.IsNullOrWhiteSpace(abilityId))
                return;

            var featId = GetLocalInt(target, ActiveAbilityFeatIdName);
            if (featId == 0)
                return;

            var featType = (FeatType)featId;
            var abilityDetail = AbilityService.GetAbilityDetail(featType);

            // Remove the local variables.
            DeleteLocalString(target, ActiveAbilityIdName);
            DeleteLocalInt(target, ActiveAbilityFeatIdName);
            DeleteLocalInt(target, ActiveAbilityEffectivePerkLevelName);

            // Notify the activator and nearby players
            SendMessageToPC(target, $"Your weapon ability {abilityDetail.Name} is no longer queued.");

            if (sendMessage)
                MessagingService.SendMessageNearbyToPlayers(target, $"{GetName(target)} no longer has weapon ability {abilityDetail.Name} readied.");
        }

        /// <summary>
        /// When a player's weapon hits a target, if an ability is queued, that ability will be executed.
        /// </summary>
        public void ProcessQueuedWeaponAbility()
        {
            var activator = OBJECT_SELF;
            if (!GetIsObjectValid(activator)) return;

            var target = GetSpellTargetObject();
            var targetLocation = GetLocation(target);
            var item = GetSpellCastItem();

            // If this method was triggered by our own armor (from getting hit), return. 
            if (GetBaseItemType(item) == BaseItemType.Armor) return;

            var activeWeaponAbility = (FeatType)GetLocalInt(activator, ActiveAbilityFeatIdName);
            var activeAbilityEffectivePerkLevel = GetLocalInt(activator, ActiveAbilityEffectivePerkLevelName);

            if (!AbilityService.IsFeatRegistered(activeWeaponAbility)) return;

            var abilityDetail = AbilityService.GetAbilityDetail(activeWeaponAbility);
            HandleStealthBreaking(activator, abilityDetail);
            abilityDetail.ImpactAction?.Invoke(activator, target, activeAbilityEffectivePerkLevel, targetLocation);

            DeleteLocalString(activator, ActiveAbilityIdName);
            DeleteLocalInt(activator, ActiveAbilityFeatIdName);
            DeleteLocalInt(activator, ActiveAbilityEffectivePerkLevelName);
        }

        /// <summary>
        /// Whenever a player enters the server, any temporary variables related to ability execution
        /// will be removed from their PC.
        /// </summary>
        public void ClearTemporaryQueuedVariables()
        {
            var player = GetEnteringObject();
            ClearQueuedAbility(player);
        }

        /// <summary>
        /// Whenever a player starts resting, clear any queued abilities.
        /// </summary>
        public void ClearTemporaryQueuedVariablesOnRest()
        {
            ClearQueuedAbility(OBJECT_SELF);
        }

        /// <summary>
        /// Whenever a player equips an item, clear any queued abilities.
        /// </summary>
        public void ClearTemporaryQueuedVariablesOnEquip()
        {
            ClearQueuedAbility(OBJECT_SELF);
        }

        /// <summary>
        /// Clears the queued ability of a player.
        /// </summary>
        /// <param name="player">The player to clear</param>
        private void ClearQueuedAbility(uint player)
        {
            DeleteLocalString(player, ActiveAbilityIdName);
            DeleteLocalInt(player, ActiveAbilityFeatIdName);
            DeleteLocalInt(player, ActiveAbilityEffectivePerkLevelName);
        }
    }
}
