using System;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
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
        private const string ActiveAbilityName = "ACTIVE_ABILITY";
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
            var target = StringToObject(Events.GetEventData("TARGET_OBJECT_ID"));
            var feat = (FeatType)Convert.ToInt32(Events.GetEventData("FEAT_ID"));
            if (!Ability.IsFeatRegistered(feat)) return;
            var ability = Ability.GetAbilityDetail(feat);
            
            // Creature cannot use the feat.
            var effectivePerkLevel = 
                ability.EffectiveLevelPerkType == PerkType.Invalid 
                    ? 1 // If there's not an associated perk, default level to 1.
                    : Perk.GetEffectivePerkLevel(activator, ability.EffectiveLevelPerkType);
            if (!Ability.CanUseAbility(activator, target, feat, effectivePerkLevel))
            {
                return;
            }

            // Weapon abilities are queued for the next time the activator's attack lands on an enemy.
            if (ability.ActivationType == AbilityActivationType.Weapon)
            {
                Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} readies {ability.Name}.");
                QueueWeaponAbility(activator, ability, feat, effectivePerkLevel);
            }
            // Concentration abilities are triggered once per second.
            else if(ability.ActivationType == AbilityActivationType.Concentration)
            {
                // Using the same concentration feat ends the effect.
                var activeConcentrationAbility = Ability.GetActiveConcentration(activator);
                if(activeConcentrationAbility.Feat == feat)
                {
                    Ability.EndConcentrationAbility(activator);
                }
                else
                {
                    Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} begins concentrating...");
                    Ability.StartConcentrationAbility(activator, feat, ability.ConcentrationStatusEffectType);
                }
                
            }
            // All other abilities are funneled through the same process.
            else
            {
                Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} readies {ability.Name} on {GetName(target)}.");
                ActivateAbility(activator, target, ability, effectivePerkLevel);
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
        /// <param name="ability">The ability details</param>
        /// <param name="effectivePerkLevel">The activator's effective perk level</param>
        private static void ActivateAbility(uint activator, uint target, AbilityDetail ability, int effectivePerkLevel)
        {
            // Activation delay is increased if player is equipped with heavy or light armor.
            float CalculateActivationDelay()
            {
                const float HeavyArmorPenalty = 2.0f;

                var armorPenalty = 1.0f;
                var penaltyMessage = string.Empty;
                for (var slot = 0; slot < NumberOfInventorySlots; slot++)
                {
                    var item = GetItemInSlot((InventorySlot) slot, activator);
                    var armorType = Item.GetArmorType(item);
                    if (armorType == ArmorType.Heavy)
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

                var abilityDelay = ability.ActivationDelay?.Invoke(activator, target, effectivePerkLevel) ?? 0.0f;
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
                    AssignCommand(activator, () => ActionPlayAnimation(ability.AnimationType, 1.0f, delay - 0.2f));
                }
            }

            // Recursive function which checks if player has moved since starting the casting.
            void CheckForActivationInterruption(string activationId, Vector3 originalPosition)
            {
                if (!GetIsPC(activator)) return;

                // Completed abilities should no longer run.
                var status = GetLocalInt(activator, activationId);
                if (status == (int) ActivationStatus.Completed || status == (int)ActivationStatus.Invalid) return;
                
                var currentPosition = GetPosition(activator);

                if (currentPosition.X != originalPosition.X ||
                    currentPosition.Y != originalPosition.Y ||
                    currentPosition.Z != originalPosition.Z)
                {
                    RemoveEffectByTag(activator, "ACTIVATION_VFX");

                    Player.StopGuiTimingBar(activator, string.Empty);
                    SendMessageToPC(activator, "Your ability has been interrupted.");
                    return;
                }

                DelayCommand(0.5f, () => CheckForActivationInterruption(activationId, originalPosition));
            }

            // This method is called after the delay of the ability has finished.
            void CompleteActivation(string id, float abilityRecastDelay)
            {
                DeleteLocalInt(activator, id);

                // Moved during casting or activator died. Cancel the activation.
                if (GetLocalInt(activator, id) == (int) ActivationStatus.Interrupted || GetCurrentHitPoints(activator) <= 0) return;

                ApplyRequirementEffects(activator, ability);
                ability.ImpactAction?.Invoke(activator, target, effectivePerkLevel);
                ApplyRecastDelay(activator, ability.RecastGroup, abilityRecastDelay);
            }

            // Begin the main process
            var activationId = Guid.NewGuid().ToString();
            var activationDelay = CalculateActivationDelay();
            var recastDelay = ability.RecastDelay(activator);
            var position = GetPosition(activator);
            ProcessAnimationAndVisualEffects(activationDelay);
            CheckForActivationInterruption(activationId, position);
            SetLocalInt(activator, activationId, (int)ActivationStatus.Started);

            if (GetIsPC(activator))
            {
                if (activationDelay > 0.0f)
                {
                    Player.StartGuiTimingBar(activator, activationDelay, string.Empty);
                }
            }

            DelayCommand(activationDelay, () => CompleteActivation(activationId, recastDelay));
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
        /// <param name="effectivePerkLevel">The activator's effective perk level</param>
        private static void QueueWeaponAbility(uint activator, AbilityDetail ability, FeatType feat, int effectivePerkLevel)
        {
            var abilityId = Guid.NewGuid().ToString();
            // Assign local variables which will be picked up on the next weapon OnHit event by this player.
            SetLocalInt(activator, ActiveAbilityName, (int)feat);
            SetLocalString(activator, ActiveAbilityIdName, abilityId);
            SetLocalInt(activator, ActiveAbilityFeatIdName, (int)feat);
            SetLocalInt(activator, ActiveAbilityEffectivePerkLevelName, effectivePerkLevel);

            ApplyRequirementEffects(activator, ability);

            var abilityRecastDelay = ability.RecastDelay?.Invoke(activator) ?? 0.0f;
            ApplyRecastDelay(activator, ability.RecastGroup, abilityRecastDelay);

            // Activator must attack within 30 seconds after queueing or else it wears off.
            DelayCommand(30.0f, () =>
            {
                if (GetLocalString(activator, ActiveAbilityIdName) != abilityId) return;

                // Remove the local variables.
                DeleteLocalInt(activator, ActiveAbilityName);
                DeleteLocalString(activator, ActiveAbilityIdName);
                DeleteLocalInt(activator, ActiveAbilityFeatIdName);
                DeleteLocalInt(activator, ActiveAbilityEffectivePerkLevelName);

                // Notify the activator and nearby players
                SendMessageToPC(activator, $"Your weapon ability {ability.Name} is no longer queued.");
                Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} no longer has weapon ability {ability.Name} readied.");
            });
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
            var item = GetSpellCastItem();

            // If this method was triggered by our own armor (from getting hit), return. 
            if (GetBaseItemType(item) == BaseItem.Armor) return;

            var activeWeaponAbility = (FeatType)GetLocalInt(activator, ActiveAbilityName);
            var activeAbilityEffectivePerkLevel = GetLocalInt(activator, ActiveAbilityEffectivePerkLevelName);

            if (!Ability.IsFeatRegistered(activeWeaponAbility)) return;

            var abilityDetail = Ability.GetAbilityDetail(activeWeaponAbility);
            abilityDetail.ImpactAction?.Invoke(activator, target, activeAbilityEffectivePerkLevel);

            DeleteLocalInt(activator, ActiveAbilityName);
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

            DeleteLocalInt(player, ActiveAbilityName);
            DeleteLocalString(player, ActiveAbilityIdName);
            DeleteLocalInt(player, ActiveAbilityFeatIdName);
            DeleteLocalInt(player, ActiveAbilityEffectivePerkLevelName);
        }

        /// <summary>
        /// Applies a recast delay on a specific recast group.
        /// If group is invalid or delay amount is less than or equal to zero, nothing will happen.
        /// </summary>
        /// <param name="activator">The activator of the ability.</param>
        /// <param name="group">The recast group to put this delay under.</param>
        /// <param name="delaySeconds">The number of seconds to delay.</param>
        private static void ApplyRecastDelay(uint activator, RecastGroup group, float delaySeconds)
        {
            if (!GetIsObjectValid(activator) || group == RecastGroup.Invalid || delaySeconds <= 0.0f) return;


            // NPCs and DM-possessed NPCs
            if (!GetIsPC(activator) || GetIsDMPossessed(activator))
            {
                var recastDate = DateTime.UtcNow.AddSeconds(delaySeconds);
                var recastDateString = recastDate.ToString("yyyy-MM-dd hh:mm:ss");
                SetLocalString(activator, $"ABILITY_RECAST_ID_{(int)group}", recastDateString);
            }
            // Players
            else if (GetIsPC(activator) && !GetIsDM(activator))
            {
                var playerId = GetObjectUUID(activator);
                var dbPlayer = DB.Get<Entity.Player>(playerId);

                var recastPercentage = dbPlayer.AbilityRecastReduction * 0.01f;
                if (recastPercentage > 0.5f)
                    recastPercentage = 0.5f;

                delaySeconds -= delaySeconds * recastPercentage;

                var recastDate = DateTime.UtcNow.AddSeconds(delaySeconds);
                dbPlayer.RecastTimes[group] = recastDate;

                DB.Set(playerId, dbPlayer);
            }

        }
    }
}
