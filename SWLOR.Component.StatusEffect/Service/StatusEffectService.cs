using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Combat.ValueObjects;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Domain.StatusEffect.ValueObjects;
using SWLOR.Shared.Domain.UI.Events;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Component.StatusEffect.Service
{

    public class StatusEffectService : IStatusEffectService
    {
        private readonly IGuiService _guiService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMessagingService _messagingService;

        public StatusEffectService(IGuiService guiService, IServiceProvider serviceProvider, IMessagingService messagingService)
        {
            _guiService = guiService;
            _serviceProvider = serviceProvider;
            _messagingService = messagingService;
        }
        
        // Lazy-loaded service to break circular dependency
        private IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();
        private class StatusEffectGroup
        {
            public uint Source { get; set; }
            public DateTime Expiration { get; set; }
            public FeatType ConcentrationFeatType { get; set; }
            public object EffectData { get; set; }
        }

        private readonly Dictionary<StatusEffectType, StatusEffectDetail> _statusEffects = new();
        private readonly Dictionary<uint, Dictionary<StatusEffectType, StatusEffectGroup>> _creaturesWithStatusEffects = new();
        private readonly Dictionary<uint, Dictionary<StatusEffectType, StatusEffectGroup>> _loggedOutPlayersWithEffects = new();

        private readonly Dictionary<EffectIconType, List<StatusEffectType>> _effectIconToStatusEffects = new();
        private readonly Dictionary<EffectIconType, AbilityType> _abilityIncreaseIconType = new()
        {
            { EffectIconType.AbilityIncreaseSTR, AbilityType.Might },
            { EffectIconType.AbilityDecreaseSTR, AbilityType.Might },
            { EffectIconType.AbilityIncreaseDEX, AbilityType.Perception },
            { EffectIconType.AbilityDecreaseDEX, AbilityType.Perception },
            { EffectIconType.AbilityIncreaseCON, AbilityType.Vitality },
            { EffectIconType.AbilityDecreaseCON, AbilityType.Vitality },
            { EffectIconType.AbilityIncreaseINT, AbilityType.Agility },
            { EffectIconType.AbilityDecreaseINT, AbilityType.Agility },
            { EffectIconType.AbilityIncreaseWIS, AbilityType.Willpower },
            { EffectIconType.AbilityDecreaseWIS, AbilityType.Willpower },
            { EffectIconType.AbilityIncreaseCHA, AbilityType.Social },
            { EffectIconType.AbilityDecreaseCHA, AbilityType.Social },

        };

        private readonly Dictionary<EffectIconType, EffectScriptType> _effectIconToEffectType = new()
        {
            { EffectIconType.Invalid, EffectScriptType.Invalideffect },
            { EffectIconType.DamageResistance, EffectScriptType.DamageResistance },
            { EffectIconType.Regenerate, EffectScriptType.Regenerate },
            { EffectIconType.DamageReduction, EffectScriptType.DamageReduction },
            { EffectIconType.TemporaryHitpoints, EffectScriptType.TemporaryHitpoints },
            { EffectIconType.Entangle, EffectScriptType.Entangle },
            { EffectIconType.Invulnerable, EffectScriptType.Invulnerable },
            { EffectIconType.Fatigue, EffectScriptType.Invalideffect },
            { EffectIconType.Deaf, EffectScriptType.Deaf },
            { EffectIconType.Immunity, EffectScriptType.Immunity },
            { EffectIconType.EnemyAttackBonus, EffectScriptType.EnemyAttackBonus },
            { EffectIconType.Charmed, EffectScriptType.Charmed },
            { EffectIconType.Confused, EffectScriptType.Confused },
            { EffectIconType.Frightened, EffectScriptType.Frightened },
            { EffectIconType.Dominated, EffectScriptType.Dominated },
            { EffectIconType.Paralyze, EffectScriptType.Paralyze },
            { EffectIconType.Dazed, EffectScriptType.Dazed },
            { EffectIconType.Stunned, EffectScriptType.Stunned },
            { EffectIconType.Sleep, EffectScriptType.Sleep },
            { EffectIconType.Poison, EffectScriptType.Poison },
            { EffectIconType.Disease, EffectScriptType.Disease },
            { EffectIconType.Curse, EffectScriptType.Curse },
            { EffectIconType.Silence, EffectScriptType.Silence },
            { EffectIconType.Turned, EffectScriptType.Turned },
            { EffectIconType.Haste, EffectScriptType.Haste },
            { EffectIconType.Slow, EffectScriptType.Slow },
            { EffectIconType.AbilityIncreaseSTR, EffectScriptType.AbilityIncrease },
            { EffectIconType.AbilityDecreaseSTR, EffectScriptType.AbilityDecrease },
            { EffectIconType.AttackIncrease, EffectScriptType.AttackIncrease },
            { EffectIconType.AttackDecrease, EffectScriptType.AttackDecrease },
            { EffectIconType.DamageIncrease, EffectScriptType.DamageIncrease },
            { EffectIconType.DamageDecrease, EffectScriptType.DamageDecrease },
            { EffectIconType.DamageImmunityIncrease, EffectScriptType.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityDecrease, EffectScriptType.DamageImmunityDecrease },
            { EffectIconType.ACIncrease, EffectScriptType.ACIncrease },
            { EffectIconType.ACDecrease, EffectScriptType.ACDecrease },
            { EffectIconType.MovementSpeedIncrease, EffectScriptType.MovementSpeedIncrease },
            { EffectIconType.MovementSpeedDecrease, EffectScriptType.MovementSpeedDecrease },
            { EffectIconType.SavingThrowIncrease, EffectScriptType.SavingThrowDecrease },
            { EffectIconType.SpellResistanceIncrease, EffectScriptType.SpellResistanceIncrease },
            { EffectIconType.SpellResistanceDecrease, EffectScriptType.SpellResistanceDecrease },
            { EffectIconType.SkillIncrease, EffectScriptType.SkillIncrease },
            { EffectIconType.SkillDecrease, EffectScriptType.SkillDecrease },
            { EffectIconType.Invisibility, EffectScriptType.Invisibility },
            { EffectIconType.ImprovedInvisibility, EffectScriptType.ImprovedInvisibility },
            { EffectIconType.Darkness, EffectScriptType.Darkness },
            { EffectIconType.DispelMagicAll, EffectScriptType.DispelMagicAll },
            { EffectIconType.ElementalShield, EffectScriptType.ElementalShield },
            { EffectIconType.LevelDrain, EffectScriptType.NegativeLevel },
            { EffectIconType.Polymorph, EffectScriptType.Polymorph },
            { EffectIconType.Sanctuary, EffectScriptType.Sanctuary },
            { EffectIconType.TrueSeeing, EffectScriptType.TrueSeeing },
            { EffectIconType.SeeInvisibility, EffectScriptType.SeeInvisible },
            { EffectIconType.Timestop, EffectScriptType.Timestop },
            { EffectIconType.Blindness, EffectScriptType.Blindness },
            { EffectIconType.SpellLevelAbsorption, EffectScriptType.SpellLevelAbsorption },
            { EffectIconType.DispelMagicBest, EffectScriptType.DispelMagicBest },
            { EffectIconType.AbilityIncreaseDEX, EffectScriptType.AbilityIncrease },
            { EffectIconType.AbilityDecreaseDEX, EffectScriptType.AbilityDecrease },
            { EffectIconType.AbilityIncreaseCON, EffectScriptType.AbilityIncrease },
            { EffectIconType.AbilityDecreaseCON, EffectScriptType.AbilityDecrease },
            { EffectIconType.AbilityIncreaseINT, EffectScriptType.AbilityIncrease },
            { EffectIconType.AbilityDecreaseINT, EffectScriptType.AbilityDecrease },
            { EffectIconType.AbilityIncreaseWIS, EffectScriptType.AbilityIncrease },
            { EffectIconType.AbilityDecreaseWIS, EffectScriptType.AbilityDecrease },
            { EffectIconType.AbilityIncreaseCHA, EffectScriptType.AbilityIncrease },
            { EffectIconType.AbilityDecreaseCHA, EffectScriptType.AbilityDecrease },
            { EffectIconType.ImmunityAll, EffectScriptType.Immunity },
            { EffectIconType.ImmunityMind, EffectScriptType.Immunity },
            { EffectIconType.ImmunityPoison, EffectScriptType.Immunity },
            { EffectIconType.ImmunityDisease, EffectScriptType.Immunity },
            { EffectIconType.ImmunityFear, EffectScriptType.Immunity },
            { EffectIconType.ImmunityTrap, EffectScriptType.Immunity },
            { EffectIconType.ImmunityParalysis, EffectScriptType.Immunity },
            { EffectIconType.ImmunityBlindness, EffectScriptType.Immunity },
            { EffectIconType.ImmunityDeafness, EffectScriptType.Immunity },
            { EffectIconType.ImmunitySlow, EffectScriptType.Immunity },
            { EffectIconType.ImmunityEntangle, EffectScriptType.Immunity },
            { EffectIconType.ImmunitySilence, EffectScriptType.Immunity },
            { EffectIconType.ImmunityStun, EffectScriptType.Immunity },
            { EffectIconType.ImmunitySleep, EffectScriptType.Immunity },
            { EffectIconType.ImmunityCharm, EffectScriptType.Immunity },
            { EffectIconType.ImmunityDominate, EffectScriptType.Immunity },
            { EffectIconType.ImmunityConfuse, EffectScriptType.Immunity },
            { EffectIconType.ImmunityCurse, EffectScriptType.Immunity },
            { EffectIconType.ImmunityDazed, EffectScriptType.Immunity },
            { EffectIconType.ImmunityAbilityDecrease, EffectScriptType.Immunity },
            { EffectIconType.ImmunityAttackDecrease, EffectScriptType.Immunity },
            { EffectIconType.ImmunityDamageDecrease, EffectScriptType.Immunity },
            { EffectIconType.ImmunityDamageImmunityDecrease, EffectScriptType.Immunity },
            { EffectIconType.ImmunityACDecrease, EffectScriptType.Immunity },
            { EffectIconType.ImmunityMovementSpeedDecrease, EffectScriptType.Immunity },
            { EffectIconType.ImmunitySavingThrowDecrease, EffectScriptType.Immunity },
            { EffectIconType.ImmunitySpellResistanceDecrease, EffectScriptType.Immunity },
            { EffectIconType.ImmunitySkillDecrease, EffectScriptType.Immunity },
            { EffectIconType.ImmunityKnockdown, EffectScriptType.Immunity },
            { EffectIconType.ImmunityNegativeLevel, EffectScriptType.Immunity },
            { EffectIconType.ImmunitySneakAttack, EffectScriptType.Immunity },
            { EffectIconType.ImmunityCriticalHit, EffectScriptType.Immunity },
            { EffectIconType.ImmunityDeathMagic, EffectScriptType.Immunity },
            { EffectIconType.ReflexSaveIncreased, EffectScriptType.SavingThrowIncrease },
            { EffectIconType.FortitudeSaveIncreased, EffectScriptType.SavingThrowIncrease },
            { EffectIconType.WillSaveIncreased, EffectScriptType.SavingThrowIncrease },
            { EffectIconType.Taunted, EffectScriptType.Invalideffect },
            { EffectIconType.SpellImmunity, EffectScriptType.SpellImmunity },
            { EffectIconType.Etherealness, EffectScriptType.Ethereal },
            { EffectIconType.Concealment, EffectScriptType.Concealment },
            { EffectIconType.Petrified, EffectScriptType.Petrify },
            { EffectIconType.EffectSpellFailure, EffectScriptType.SpellFailure },
            { EffectIconType.DamageImmunityMagic, EffectScriptType.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityAcid, EffectScriptType.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityCold, EffectScriptType.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityDivine, EffectScriptType.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityElectrical, EffectScriptType.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityFire, EffectScriptType.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityNegative, EffectScriptType.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityPositive, EffectScriptType.DamageImmunityIncrease },
            { EffectIconType.DamageImmunitySonic, EffectScriptType.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityMagicDecrease, EffectScriptType.DamageImmunityDecrease },
            { EffectIconType.DamageImmunityAcidDecrease, EffectScriptType.DamageImmunityDecrease },
            { EffectIconType.DamageImmunityColdDecrease, EffectScriptType.DamageImmunityDecrease },
            { EffectIconType.DamageImmunityDivineDecrease, EffectScriptType.DamageImmunityDecrease },
            { EffectIconType.DamageImmunityElectricalDecrease, EffectScriptType.DamageImmunityDecrease },
            { EffectIconType.DamageImmunityFireDecrease, EffectScriptType.DamageImmunityDecrease },
            { EffectIconType.DamageImmunityNegativeDecrease, EffectScriptType.DamageImmunityDecrease },
            { EffectIconType.DamageImmunityPositiveDecrease, EffectScriptType.DamageImmunityDecrease },
            { EffectIconType.DamageImmunitySonicDecrease, EffectScriptType.DamageImmunityDecrease },
            { EffectIconType.Charge, EffectScriptType.MovementSpeedIncrease },
            { EffectIconType.Dedication, EffectScriptType.Invalideffect },
            { EffectIconType.FrenziedShout, EffectScriptType.Invalideffect },
            { EffectIconType.Rejuvenation, EffectScriptType.Regenerate },
            { EffectIconType.SoldiersPrecision, EffectScriptType.AttackIncrease },
            { EffectIconType.SoldiersSpeed, EffectScriptType.MovementSpeedIncrease },
            { EffectIconType.SoldiersStrike, EffectScriptType.DamageIncrease },
        };

        /// <summary>
        /// When the module loads, cache all status effects.
        /// </summary>
        public void CacheStatusEffects()
        {
            // Organize perks to make later reads quicker.
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IStatusEffectListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IStatusEffectListDefinition)_serviceProvider.GetRequiredService(type);
                var statusEffects = instance.BuildStatusEffects();

                foreach (var (statusEffectType, detail) in statusEffects)
                {
                    _statusEffects[statusEffectType] = detail;
                    if (!_effectIconToStatusEffects.ContainsKey(detail.EffectIconId))
                        _effectIconToStatusEffects[detail.EffectIconId] = new List<StatusEffectType>();
                    _effectIconToStatusEffects[detail.EffectIconId].Add(statusEffectType);
                }
            }
        }

        /// <summary>
        /// Gives a status effect to a creature.
        /// If creature already has the status effect, and their timer is shorter than length,
        /// it will be extended to the length specified.
        /// </summary>
        /// <param name="source">The source of the status effect.</param>
        /// <param name="target">The creature receiving the status effect.</param>
        /// <param name="statusEffectType">The type of status effect to give.</param>
        /// <param name="length">The amount of time, in seconds, the status effect should last. Set to 0.0f to make it permanent.</param>
        /// <param name="effectData">Effect data used by the effect.</param>
        /// <param name="concentrationFeatType">If status effect is associated with a concentration ability, this will track the feat type used.</param>
        /// <param name="sendApplicationMessage">If true, a message will be sent to nearby players when the status effect is applied.</param>
        public void Apply(
            uint source, 
            uint target, 
            StatusEffectType statusEffectType, 
            float length, 
            object effectData = null,
            FeatType concentrationFeatType = FeatType.Invalid,
            bool sendApplicationMessage = true)
        {
            var statusEffectDetail = _statusEffects[statusEffectType];
            if (!_creaturesWithStatusEffects.ContainsKey(target))
                _creaturesWithStatusEffects[target] = new Dictionary<StatusEffectType, StatusEffectGroup>();

            if (!_creaturesWithStatusEffects[target].ContainsKey(statusEffectType))
                _creaturesWithStatusEffects[target][statusEffectType] = new StatusEffectGroup();

            var expiration = length == 0.0f ? DateTime.MaxValue : DateTime.UtcNow.AddSeconds(length);
            var addIcon = true;

            // If the existing status effect will expire later than this, exit early.
            if (_creaturesWithStatusEffects[target][statusEffectType].Expiration > expiration)
                return;

            // Can't stack - remove the effect then reapply it afterwards.
            if (!statusEffectDetail.CanStack &&
                HasStatusEffect(target, statusEffectType))
            {
                Remove(target, statusEffectType, false, false);
                _creaturesWithStatusEffects[target][statusEffectType] = new StatusEffectGroup();
                addIcon = false;
            }

            // Remove any status effects this effect overrides.
            if (statusEffectDetail.ReplacesEffects != null)
            {
                foreach (var effect in statusEffectDetail.ReplacesEffects)
                {
                    Remove(target, effect, false, false);
                    addIcon = false;
                }
            }

            // Prevent applying the status effect if a more powerful one is already in place.
            if (statusEffectDetail.CannotReplaceEffects != null)
            {
                if (HasStatusEffect(target, statusEffectDetail.CannotReplaceEffects))
                {
                    const string Message = "A more powerful effect already exists.";
                    SendMessageToPC(source, Message);

                    if(source != target)
                        SendMessageToPC(target, Message);
                    return;
                }
            }

            // Set the group details.
            _creaturesWithStatusEffects[target][statusEffectType].Source = source;
            _creaturesWithStatusEffects[target][statusEffectType].Expiration = expiration;
            _creaturesWithStatusEffects[target][statusEffectType].ConcentrationFeatType = concentrationFeatType;
            _creaturesWithStatusEffects[target][statusEffectType].EffectData = effectData;

            // Run the Grant Action, if applicable.
            statusEffectDetail.AppliedAction?.Invoke(source, target, length, effectData);

            // Add the status effect icon if there is one.
            if (addIcon && statusEffectDetail.EffectIconId != EffectIconType.Invalid)
            {
                var iconEffect = EffectIcon(statusEffectDetail.EffectIconId);
                iconEffect = TagEffect(iconEffect, $"EFFECT_ICON_{statusEffectDetail.EffectIconId}");
                
                if(length > 0f)
                    ApplyEffectToObject(DurationType.Temporary, iconEffect, target, length);
                else 
                    ApplyEffectToObject(DurationType.Permanent, iconEffect, target);
            }

            if(sendApplicationMessage)
                _messagingService.SendMessageNearbyToPlayers(target, $"{GetName(target)} receives the effect of {statusEffectDetail.Name}.", 20f);

            _guiService.PublishRefreshEvent(target, new StatusEffectReceivedRefreshEvent());
        }

        /// <summary>
        /// When a player enters the server, if any of their status effects in limbo, re-add them to the
        /// dictionary for processing.
        /// </summary>
        public void PlayerEnter()
        {
            var player = GetEnteringObject();

            if (_loggedOutPlayersWithEffects.ContainsKey(player))
            {
                var effects = _loggedOutPlayersWithEffects[player].ToDictionary(x => x.Key, y => y.Value);
                _creaturesWithStatusEffects[player] = effects;

                _loggedOutPlayersWithEffects.Remove(player);
            }

            ExecuteScript("assoc_stateffect", player);
        }

        /// <summary>
        /// When a player leaves the server, move their status effects to a different dictionary
        /// so they aren't processed unnecessarily.  
        /// </summary>
        public void PlayerExit()
        {
            var player = GetExitingObject();

            if (!_creaturesWithStatusEffects.ContainsKey(player))
                return;

            var effects = _creaturesWithStatusEffects[player].ToDictionary(x => x.Key, y => y.Value);
            _loggedOutPlayersWithEffects[player] = effects;

            _creaturesWithStatusEffects.Remove(player);
        }

        /// <summary>
        /// When the module heartbeat runs, execute and clean up status effects on all creatures.
        /// </summary>
        public void TickStatusEffects()
        {
            var now = DateTime.UtcNow;

            foreach (var (creature, statusEffects) in _creaturesWithStatusEffects.ToDictionary(x => x.Key, y => y.Value))
            {
                // Creature is dead or invalid. Remove its status effects.
                var removeAllEffects = !GetIsObjectValid(creature) || GetIsDead(creature);

                // Iterate over each status effect, cleaning them up if they've expired or executing their tick if applicable.
                foreach (var (statusEffect, group) in statusEffects)
                {
                    var activeConcentration = AbilityService.GetActiveConcentration(group.Source);

                    // Concentration check - If caster is no longer channeling this feat, remove the status effect.
                    if (group.ConcentrationFeatType != FeatType.Invalid)
                    {
                        if (activeConcentration.Feat != group.ConcentrationFeatType)
                        {
                            Remove(creature, statusEffect);
                            continue;
                        }
                    }

                    // Status effect has expired or creature is no longer valid. Remove it.
                    if (removeAllEffects || now > group.Expiration)
                    {
                        Remove(creature, statusEffect);

                        // Concentration - End the ability if this status effect was tied to a concentration ability
                        // and the creature was the target.
                        if (group.ConcentrationFeatType != FeatType.Invalid &&
                            activeConcentration.Feat == group.ConcentrationFeatType &&
                            activeConcentration.Target == creature)
                        {
                            AbilityService.EndConcentrationAbility(group.Source);
                        }

                    }
                    // Otherwise do a Tick.
                    else
                    {
                        var detail = _statusEffects[statusEffect];
                        detail.TickAction?.Invoke(group.Source, creature, group.EffectData);
                    }
                }

                // No more status effects. Remove the creature from the cache.
                if (statusEffects.Count <= 0)
                {
                    _creaturesWithStatusEffects.Remove(creature);
                }
            }
        }

        /// <summary>
        /// When a player dies, remove any status effects which are present.
        /// </summary>
        public void OnPlayerDeath()
        {
            var player = GetLastPlayerDied();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            if (!_creaturesWithStatusEffects.ContainsKey(player))
                return;

            var statusEffects = _creaturesWithStatusEffects[player].Select(s => s.Key);

            foreach (var effect in statusEffects)
            {
                Remove(player, effect);
            }
        }

        private void Remove(uint creature, StatusEffectType statusEffectType, bool showMessage, bool removeIcon)
        {
            if (!HasStatusEffect(creature, statusEffectType, true)) return;

            var effectInstance = _creaturesWithStatusEffects[creature][statusEffectType];
            _creaturesWithStatusEffects[creature].Remove(statusEffectType);

            var statusEffectDetail = _statusEffects[statusEffectType];
            statusEffectDetail.RemoveAction?.Invoke(creature, effectInstance.EffectData);

            if (removeIcon && statusEffectDetail.EffectIconId > 0 && GetIsObjectValid(creature))
            {
                RemoveEffectByTag(creature, $"EFFECT_ICON_{statusEffectDetail.EffectIconId}");
            }

            if(showMessage)
                _messagingService.SendMessageNearbyToPlayers(creature, $"{GetName(creature)}'s {statusEffectDetail.Name} effect has worn off.");

            _guiService.PublishRefreshEvent(creature, new StatusEffectRemovedRefreshEvent());
        }

        /// <summary>
        /// Removes a status effect from a creature.
        /// </summary>
        /// <param name="creature">The creature to remove the status effect from.</param>
        /// <param name="statusEffectType">The type of status effect to remove.</param>
        /// <param name="showMessage">If true, a message will be displayed. Otherwise no message is displayed.</param>
        public void Remove(uint creature, StatusEffectType statusEffectType, bool showMessage = true)
        {
            Remove(creature, statusEffectType, showMessage, true);
        }

        /// <summary>
        /// Removes all status effects from a creature.
        /// </summary>
        /// <param name="creature">The creature to remove all effects from.</param>
        public void RemoveAll(uint creature)
        {
            if (!_creaturesWithStatusEffects.ContainsKey(creature))
                return;

            foreach (var effectType in _creaturesWithStatusEffects[creature].Keys)
            {
                Remove(creature, effectType);
            }
        }

        /// <summary>
        /// Checks if a creature has a status effect.
        /// If ignoreExpiration is true, even if the effect is expired this will return true.
        /// This should only be used within this class to avoid confusion.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="statusEffectType">The status effect type to look for.</param>
        /// <param name="ignoreExpiration">If true, expired effects will return true. Otherwise, expiration will be checked.</param>
        /// <returns>true if creature has status effect, false otherwise</returns>
        private bool HasStatusEffect(uint creature, StatusEffectType statusEffectType, bool ignoreExpiration)
        {
            // Creature doesn't exist in the cache.
            if (!_creaturesWithStatusEffects.ContainsKey(creature))
                return false;

            // Status effect doesn't exist for this creature in the cache.
            if (!_creaturesWithStatusEffects[creature].ContainsKey(statusEffectType))
                return false;

            // Status effect has expired, but hasn't cleaned up yet.
            if (!ignoreExpiration)
            {
                var now = DateTime.UtcNow;
                if (now > _creaturesWithStatusEffects[creature][statusEffectType].Expiration)
                    return false;
            }

            // Status effect hasn't expired.
            return true;
        }

        /// <summary>
        /// Checks if a creature has a status effect.
        /// If no status effect types are specified, false will be returned.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="statusEffectTypes">The status effect types to look for.</param>
        /// <returns>true if creature has status effect, false otherwise</returns>
        public bool HasStatusEffect(uint creature, params StatusEffectType[] statusEffectTypes)
        {
            foreach (var statusEffectType in statusEffectTypes)
            {
                if (HasStatusEffect(creature, statusEffectType, false))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves a status effect detail by its type.
        /// </summary>
        /// <param name="type">The type to search for.</param>
        /// <returns>A status effect detail</returns>
        public StatusEffectDetail GetDetail(StatusEffectType type)
        {
            return _statusEffects[type];
        }

        /// <summary>
        /// Retrieves the effect data associated with a creature's effect.
        /// If creature does not have effect, the default value of T will be returned.
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve from the effect.</typeparam>
        /// <param name="creature">The creature to check.</param>
        /// <param name="effectType">The type of effect.</param>
        /// <returns>An effect data object or a default object of type T</returns>
        public T GetEffectData<T>(uint creature, StatusEffectType effectType)
        {
            if (!_creaturesWithStatusEffects.ContainsKey(creature) ||
                !_creaturesWithStatusEffects[creature].ContainsKey(effectType))
                return default;

            return (T)_creaturesWithStatusEffects[creature][effectType].EffectData;
        }

        /// <summary>
        /// Retrieves the effect duration associated with a creature's effect.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="effectTypes">The type(s) of effect.</param>
        /// <returns>A float time remaining of the status effect</returns>
        public int GetEffectDuration(uint creature, params StatusEffectType[] effectTypes)
        {
            foreach (var effectType in effectTypes)
            {
                if (!_creaturesWithStatusEffects.ContainsKey(creature) ||
                !_creaturesWithStatusEffects[creature].ContainsKey(effectType))
                    continue;

                if (_creaturesWithStatusEffects[creature][effectType].Expiration >= DateTime.MaxValue) return 0;

                var effectTimespan = _creaturesWithStatusEffects[creature][effectType].Expiration - DateTime.UtcNow;

                return (int)effectTimespan.TotalSeconds;
            }

            return 0;
            
        }

        public EffectScriptType GetEffectTypeFromIcon(EffectIconType effectIcon)
        {
            if (!_effectIconToEffectType.TryGetValue(effectIcon, out EffectScriptType effectType))
                return EffectScriptType.Invalideffect;

            return effectType;
        }

        public List<StatusEffectType> GetStatusEffectTypesFromIcon(EffectIconType effectIcon)
        {
            if (!_effectIconToStatusEffects.TryGetValue(effectIcon, out List<StatusEffectType> statusTypes))
                return new List<StatusEffectType>();

            return statusTypes;
        }

        public AbilityType GetAbilityTypeBuffed (EffectIconType effectIcon)
        {
            if (!_abilityIncreaseIconType.TryGetValue(effectIcon, out AbilityType abilityType))
                return AbilityType.Invalid;

            return abilityType;
        }
    }
}
