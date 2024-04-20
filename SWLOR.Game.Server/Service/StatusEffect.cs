using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service
{
    public static class StatusEffect
    {
        private class StatusEffectGroup
        {
            public uint Source { get; set; }
            public DateTime Expiration { get; set; }
            public FeatType ConcentrationFeatType { get; set; }
            public object EffectData { get; set; }
        }

        private static readonly Dictionary<StatusEffectType, StatusEffectDetail> _statusEffects = new();
        private static readonly Dictionary<uint, Dictionary<StatusEffectType, StatusEffectGroup>> _creaturesWithStatusEffects = new();
        private static readonly Dictionary<uint, Dictionary<StatusEffectType, StatusEffectGroup>> _loggedOutPlayersWithEffects = new();

        private static readonly Dictionary<EffectIconType, List<StatusEffectType>> _effectIconToStatusEffects = new();
        private static readonly Dictionary<EffectIconType, AbilityType> _abilityIncreaseIconType = new()
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

        private static readonly Dictionary<EffectIconType, EffectTypeScript> _effectIconToEffectType = new()
        {
            { EffectIconType.Invalid, EffectTypeScript.Invalideffect },
            { EffectIconType.DamageResistance, EffectTypeScript.DamageResistance },
            { EffectIconType.Regenerate, EffectTypeScript.Regenerate },
            { EffectIconType.DamageReduction, EffectTypeScript.DamageReduction },
            { EffectIconType.TemporaryHitpoints, EffectTypeScript.TemporaryHitpoints },
            { EffectIconType.Entangle, EffectTypeScript.Entangle },
            { EffectIconType.Invulnerable, EffectTypeScript.Invulnerable },
            { EffectIconType.Fatigue, EffectTypeScript.Invalideffect },
            { EffectIconType.Deaf, EffectTypeScript.Deaf },
            { EffectIconType.Immunity, EffectTypeScript.Immunity },
            { EffectIconType.EnemyAttackBonus, EffectTypeScript.EnemyAttackBonus },
            { EffectIconType.Charmed, EffectTypeScript.Charmed },
            { EffectIconType.Confused, EffectTypeScript.Confused },
            { EffectIconType.Frightened, EffectTypeScript.Frightened },
            { EffectIconType.Dominated, EffectTypeScript.Dominated },
            { EffectIconType.Paralyze, EffectTypeScript.Paralyze },
            { EffectIconType.Dazed, EffectTypeScript.Dazed },
            { EffectIconType.Stunned, EffectTypeScript.Stunned },
            { EffectIconType.Sleep, EffectTypeScript.Sleep },
            { EffectIconType.Poison, EffectTypeScript.Poison },
            { EffectIconType.Disease, EffectTypeScript.Disease },
            { EffectIconType.Curse, EffectTypeScript.Curse },
            { EffectIconType.Silence, EffectTypeScript.Silence },
            { EffectIconType.Turned, EffectTypeScript.Turned },
            { EffectIconType.Haste, EffectTypeScript.Haste },
            { EffectIconType.Slow, EffectTypeScript.Slow },
            { EffectIconType.AbilityIncreaseSTR, EffectTypeScript.AbilityIncrease },
            { EffectIconType.AbilityDecreaseSTR, EffectTypeScript.AbilityDecrease },
            { EffectIconType.AttackIncrease, EffectTypeScript.AttackIncrease },
            { EffectIconType.AttackDecrease, EffectTypeScript.AttackDecrease },
            { EffectIconType.DamageIncrease, EffectTypeScript.DamageIncrease },
            { EffectIconType.DamageDecrease, EffectTypeScript.DamageDecrease },
            { EffectIconType.DamageImmunityIncrease, EffectTypeScript.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityDecrease, EffectTypeScript.DamageImmunityDecrease },
            { EffectIconType.ACIncrease, EffectTypeScript.ACIncrease },
            { EffectIconType.ACDecrease, EffectTypeScript.ACDecrease },
            { EffectIconType.MovementSpeedIncrease, EffectTypeScript.MovementSpeedIncrease },
            { EffectIconType.MovementSpeedDecrease, EffectTypeScript.MovementSpeedDecrease },
            { EffectIconType.SavingThrowIncrease, EffectTypeScript.SavingThrowDecrease },
            { EffectIconType.SpellResistanceIncrease, EffectTypeScript.SpellResistanceIncrease },
            { EffectIconType.SpellResistanceDecrease, EffectTypeScript.SpellResistanceDecrease },
            { EffectIconType.SkillIncrease, EffectTypeScript.SkillIncrease },
            { EffectIconType.SkillDecrease, EffectTypeScript.SkillDecrease },
            { EffectIconType.Invisibility, EffectTypeScript.Invisibility },
            { EffectIconType.ImprovedInvisibility, EffectTypeScript.ImprovedInvisibility },
            { EffectIconType.Darkness, EffectTypeScript.Darkness },
            { EffectIconType.DispelMagicAll, EffectTypeScript.DispelMagicAll },
            { EffectIconType.ElementalShield, EffectTypeScript.ElementalShield },
            { EffectIconType.LevelDrain, EffectTypeScript.NegativeLevel },
            { EffectIconType.Polymorph, EffectTypeScript.Polymorph },
            { EffectIconType.Sanctuary, EffectTypeScript.Sanctuary },
            { EffectIconType.TrueSeeing, EffectTypeScript.TrueSeeing },
            { EffectIconType.SeeInvisibility, EffectTypeScript.SeeInvisible },
            { EffectIconType.Timestop, EffectTypeScript.Timestop },
            { EffectIconType.Blindness, EffectTypeScript.Blindness },
            { EffectIconType.SpellLevelAbsorption, EffectTypeScript.SpellLevelAbsorption },
            { EffectIconType.DispelMagicBest, EffectTypeScript.DispelMagicBest },
            { EffectIconType.AbilityIncreaseDEX, EffectTypeScript.AbilityIncrease },
            { EffectIconType.AbilityDecreaseDEX, EffectTypeScript.AbilityDecrease },
            { EffectIconType.AbilityIncreaseCON, EffectTypeScript.AbilityIncrease },
            { EffectIconType.AbilityDecreaseCON, EffectTypeScript.AbilityDecrease },
            { EffectIconType.AbilityIncreaseINT, EffectTypeScript.AbilityIncrease },
            { EffectIconType.AbilityDecreaseINT, EffectTypeScript.AbilityDecrease },
            { EffectIconType.AbilityIncreaseWIS, EffectTypeScript.AbilityIncrease },
            { EffectIconType.AbilityDecreaseWIS, EffectTypeScript.AbilityDecrease },
            { EffectIconType.AbilityIncreaseCHA, EffectTypeScript.AbilityIncrease },
            { EffectIconType.AbilityDecreaseCHA, EffectTypeScript.AbilityDecrease },
            { EffectIconType.ImmunityAll, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityMind, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityPoison, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityDisease, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityFear, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityTrap, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityParalysis, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityBlindness, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityDeafness, EffectTypeScript.Immunity },
            { EffectIconType.ImmunitySlow, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityEntangle, EffectTypeScript.Immunity },
            { EffectIconType.ImmunitySilence, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityStun, EffectTypeScript.Immunity },
            { EffectIconType.ImmunitySleep, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityCharm, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityDominate, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityConfuse, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityCurse, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityDazed, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityAbilityDecrease, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityAttackDecrease, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityDamageDecrease, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityDamageImmunityDecrease, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityACDecrease, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityMovementSpeedDecrease, EffectTypeScript.Immunity },
            { EffectIconType.ImmunitySavingThrowDecrease, EffectTypeScript.Immunity },
            { EffectIconType.ImmunitySpellResistanceDecrease, EffectTypeScript.Immunity },
            { EffectIconType.ImmunitySkillDecrease, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityKnockdown, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityNegativeLevel, EffectTypeScript.Immunity },
            { EffectIconType.ImmunitySneakAttack, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityCriticalHit, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityDeathMagic, EffectTypeScript.Immunity },
            { EffectIconType.ReflexSaveIncreased, EffectTypeScript.SavingThrowIncrease },
            { EffectIconType.FortitudeSaveIncreased, EffectTypeScript.SavingThrowIncrease },
            { EffectIconType.WillSaveIncreased, EffectTypeScript.SavingThrowIncrease },
            { EffectIconType.Taunted, EffectTypeScript.Invalideffect },
            { EffectIconType.SpellImmunity, EffectTypeScript.SpellImmunity },
            { EffectIconType.Etherealness, EffectTypeScript.Ethereal },
            { EffectIconType.Concealment, EffectTypeScript.Concealment },
            { EffectIconType.Petrified, EffectTypeScript.Petrify },
            { EffectIconType.EffectSpellFailure, EffectTypeScript.SpellFailure },
            { EffectIconType.DamageImmunityMagic, EffectTypeScript.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityAcid, EffectTypeScript.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityCold, EffectTypeScript.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityDivine, EffectTypeScript.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityElectrical, EffectTypeScript.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityFire, EffectTypeScript.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityNegative, EffectTypeScript.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityPositive, EffectTypeScript.DamageImmunityIncrease },
            { EffectIconType.DamageImmunitySonic, EffectTypeScript.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityMagicDecrease, EffectTypeScript.DamageImmunityDecrease },
            { EffectIconType.DamageImmunityAcidDecrease, EffectTypeScript.DamageImmunityDecrease },
            { EffectIconType.DamageImmunityColdDecrease, EffectTypeScript.DamageImmunityDecrease },
            { EffectIconType.DamageImmunityDivineDecrease, EffectTypeScript.DamageImmunityDecrease },
            { EffectIconType.DamageImmunityElectricalDecrease, EffectTypeScript.DamageImmunityDecrease },
            { EffectIconType.DamageImmunityFireDecrease, EffectTypeScript.DamageImmunityDecrease },
            { EffectIconType.DamageImmunityNegativeDecrease, EffectTypeScript.DamageImmunityDecrease },
            { EffectIconType.DamageImmunityPositiveDecrease, EffectTypeScript.DamageImmunityDecrease },
            { EffectIconType.DamageImmunitySonicDecrease, EffectTypeScript.DamageImmunityDecrease },
        };

        /// <summary>
        /// When the module loads, cache all status effects.
        /// </summary>
        [NWNEventHandler("mod_cache")]
        public static void CacheStatusEffects()
        {
            // Organize perks to make later reads quicker.
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IStatusEffectListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IStatusEffectListDefinition)Activator.CreateInstance(type);
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
        public static void Apply(
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
                Messaging.SendMessageNearbyToPlayers(target, $"{GetName(target)} receives the effect of {statusEffectDetail.Name}.", 20f);

            Gui.PublishRefreshEvent(target, new StatusEffectReceivedRefreshEvent());
        }

        /// <summary>
        /// When a player enters the server, if any of their status effects in limbo, re-add them to the
        /// dictionary for processing.
        /// </summary>
        [NWNEventHandler("mod_enter")]
        public static void PlayerEnter()
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
        [NWNEventHandler("mod_exit")]
        public static void PlayerExit()
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
        [NWNEventHandler("swlor_heartbeat")]
        public static void TickStatusEffects()
        {
            var now = DateTime.UtcNow;

            foreach (var (creature, statusEffects) in _creaturesWithStatusEffects.ToDictionary(x => x.Key, y => y.Value))
            {
                // Creature is dead or invalid. Remove its status effects.
                var removeAllEffects = !GetIsObjectValid(creature) || GetIsDead(creature);

                // Iterate over each status effect, cleaning them up if they've expired or executing their tick if applicable.
                foreach (var (statusEffect, group) in statusEffects)
                {
                    var activeConcentration = Ability.GetActiveConcentration(group.Source);

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
                            Ability.EndConcentrationAbility(group.Source);
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
        [NWNEventHandler("mod_death")]
        public static void OnPlayerDeath()
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

        private static void Remove(uint creature, StatusEffectType statusEffectType, bool showMessage, bool removeIcon)
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
                Messaging.SendMessageNearbyToPlayers(creature, $"{GetName(creature)}'s {statusEffectDetail.Name} effect has worn off.");

            Gui.PublishRefreshEvent(creature, new StatusEffectRemovedRefreshEvent());
        }

        /// <summary>
        /// Removes a status effect from a creature.
        /// </summary>
        /// <param name="creature">The creature to remove the status effect from.</param>
        /// <param name="statusEffectType">The type of status effect to remove.</param>
        /// <param name="showMessage">If true, a message will be displayed. Otherwise no message is displayed.</param>
        public static void Remove(uint creature, StatusEffectType statusEffectType, bool showMessage = true)
        {
            Remove(creature, statusEffectType, showMessage, true);
        }

        /// <summary>
        /// Removes all status effects from a creature.
        /// </summary>
        /// <param name="creature">The creature to remove all effects from.</param>
        public static void RemoveAll(uint creature)
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
        private static bool HasStatusEffect(uint creature, StatusEffectType statusEffectType, bool ignoreExpiration)
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
        public static bool HasStatusEffect(uint creature, params StatusEffectType[] statusEffectTypes)
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
        public static StatusEffectDetail GetDetail(StatusEffectType type)
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
        public static T GetEffectData<T>(uint creature, StatusEffectType effectType)
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
        public static int GetEffectDuration(uint creature, params StatusEffectType[] effectTypes)
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

        public static EffectTypeScript GetEffectTypeFromIcon(EffectIconType effectIcon)
        {
            if (!_effectIconToEffectType.TryGetValue(effectIcon, out EffectTypeScript effectType))
                return EffectTypeScript.Invalideffect;

            return effectType;
        }

        public static List<StatusEffectType> GetStatusEffectTypesFromIcon(EffectIconType effectIcon)
        {
            if (!_effectIconToStatusEffects.TryGetValue(effectIcon, out List<StatusEffectType> statusTypes))
                return new List<StatusEffectType>();

            return statusTypes;
        }

        public static AbilityType GetAbilityTypeBuffed (EffectIconType effectIcon)
        {
            if (!_abilityIncreaseIconType.TryGetValue(effectIcon, out AbilityType abilityType))
                return AbilityType.Invalid;

            return abilityType;
        }
    }
}
