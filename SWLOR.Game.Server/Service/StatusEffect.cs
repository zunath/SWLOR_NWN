using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public static class StatusEffect
    {
        private const string StatusEffectTag = "STATUS_EFFECT";
        private const float Interval = 1f;

        public static bool TryApplyNativeKnockdown(uint creature, float duration)
        {
            if (!GetIsObjectValid(creature) || duration <= 0f ||
                Stat.GetStatAdjustment(creature, StatType.KnockdownImmunity) > 0)
                return false;

            ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), creature, duration);
            return true;
        }

        private static readonly Dictionary<uint, CreatureStatusEffect> _creatureEffects = new();
        private static readonly Dictionary<string, LoggedOutStatusEffects> _loggedOutPlayerEffects = new();
        private static readonly Dictionary<Type, StatusEffectMetadata> _statusEffects = new();
        private static readonly HashSet<string> _suppressedStatusEffectRemovalIds = new();
        private static readonly Dictionary<uint, int> _nativeAttackSwingDepth = new();
        private static readonly Dictionary<uint, List<Action>> _deferredNativeAttackStatusEffects = new();
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
            { EffectIconType.ImmunitySpellResistanceDecrease, EffectTypeScript.Immunity },
            { EffectIconType.ImmunitySkillDecrease, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityKnockdown, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityNegativeLevel, EffectTypeScript.Immunity },
            { EffectIconType.ImmunitySneakAttack, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityCriticalHit, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityDeathMagic, EffectTypeScript.Immunity },
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
            { EffectIconType.Charge, EffectTypeScript.MovementSpeedIncrease },
            { EffectIconType.Dedication, EffectTypeScript.Invalideffect },
            { EffectIconType.FrenziedShout, EffectTypeScript.Invalideffect },
            { EffectIconType.Rejuvenation, EffectTypeScript.Regenerate },
            { EffectIconType.SoldiersPrecision, EffectTypeScript.AttackIncrease },
            { EffectIconType.SoldiersSpeed, EffectTypeScript.MovementSpeedIncrease },
            { EffectIconType.SoldiersStrike, EffectTypeScript.DamageIncrease },
        };

        /// <summary>
        /// When the module caches, status effects will be discovered and cached.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            CacheStatusEffects();
        }

        private static void CacheStatusEffects()
        {
            _statusEffects.Clear();

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(StatusEffectBase).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var effect = (StatusEffectBase)Activator.CreateInstance(type);

                _statusEffects[type] = new StatusEffectMetadata(
                    () => (IStatusEffect)Activator.CreateInstance(type),
                    effect.Name,
                    effect.Frequency,
                    effect.SourceType,
                    effect.Categories);
            }

            Console.WriteLine($"Loaded {_statusEffects.Count} status effects.");
        }

        private static bool TryCreateStatusEffect(Type statusEffectClass, out IStatusEffect statusEffect)
        {
            if (statusEffectClass != null &&
                _statusEffects.TryGetValue(statusEffectClass, out var metadata))
            {
                statusEffect = metadata.Create();
                return true;
            }

            statusEffect = null;
            return false;
        }

        /// <summary>
        /// When a player enters the server, apply the NWN effect system
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void OnPlayerEnter()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            if (_loggedOutPlayerEffects.TryGetValue(playerId, out var loggedOutEffects))
            {
                _loggedOutPlayerEffects.Remove(playerId);
                ReassignSelfSourcedEffects(player, loggedOutEffects);
                ReconcileLoggedOutEffects(player, loggedOutEffects);

                var effects = loggedOutEffects.Effects;
                if (effects.GetAllEffects().Count > 0)
                {
                    _creatureEffects[player] = effects;
                    ReapplyNWNEffects(player, effects);
                    NotifyStatusEffectsRestored(player, effects);
                }
            }

            Stat.ReapplyFoodHP(player);
        }

        /// <summary>
        /// When a player exits the server, hold their status effects until they return.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void OnPlayerExit()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            RemoveStatusEffectsFromAllTargetsWhenSourceExits(player);

            var playerId = GetObjectUUID(player);
            if (_creatureEffects.TryGetValue(player, out var effects) &&
                effects.GetAllEffects().Count > 0)
            {
                RemoveNonPersistentStatusEffects(player, effects);

                if (effects.GetAllEffects().Count > 0)
                {
                    _loggedOutPlayerEffects[playerId] = new LoggedOutStatusEffects(player, effects, DateTime.UtcNow);
                    RemoveTrackedNWNEffects(player, effects);
                }
                else
                {
                    RemoveEffectByTag(player, StatusEffectTag);
                }
            }

            RemoveCreature(player);
        }

        public static void ClearStatusEffectsOnDeath(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            RemoveAllStatusEffects(player);
            RemoveCreature(player);
        }

        /// <summary>
        /// Handle when a status effect is applied (called by NWN script)
        /// </summary>
        [NWNEventHandler(ScriptName.OnApplyStatusEffect)]
        public static void OnApplyStatusEffect()
        {
            OnApplyNWNStatusEffect(OBJECT_SELF);
        }

        /// <summary>
        /// Handle when a status effect is removed (called by NWN script)
        /// </summary>
        [NWNEventHandler(ScriptName.OnRemoveStatusEffect)]
        public static void OnRemoveStatusEffect()
        {
            OnRemoveNWNStatusEffect(OBJECT_SELF);
        }

        /// <summary>
        /// Handle status effect interval processing (called by NWN script)
        /// </summary>
        [NWNEventHandler(ScriptName.OnStatusEffectInterval)]
        public static void OnStatusEffectInterval()
        {
            OnNWNStatusEffectInterval(OBJECT_SELF);
        }

        private static CreatureStatusEffect EnsureCreatureStatusEffectTracker(uint creature)
        {
            if (!_creatureEffects.TryGetValue(creature, out var effects))
            {
                effects = new CreatureStatusEffect();
                _creatureEffects[creature] = effects;
            }

            return effects;
        }

        public static void OnApplyNWNStatusEffect(uint player)
        {
            if (!_creatureEffects.ContainsKey(player))
                _creatureEffects[player] = new CreatureStatusEffect();
        }

        public static void OnRemoveNWNStatusEffect(uint player)
        {
            var tag = GetLastStatusEffectTag();
            if (string.IsNullOrWhiteSpace(tag) || tag == StatusEffectTag)
            {
                if (_creatureEffects.ContainsKey(player))
                    _creatureEffects.Remove(player);

                return;
            }

            if (_suppressedStatusEffectRemovalIds.Contains(tag))
                return;

            // NWN can deliver the old timer's removal callback after a duration extension
            // has already replaced it with another native effect carrying the same tag.
            // Wait until native removal settles, then only clear statuses without a timer.
            DelayCommand(0f, () =>
            {
                if (!HasEffectByTag(player, tag))
                    RemoveStatusEffectById(player, tag, true, false);
            });
        }

        private static string GetLastStatusEffectTag()
        {
            var effect = GetLastRunScriptEffect();
            var tag = GetEffectTag(effect);
            return !string.IsNullOrWhiteSpace(tag)
                ? tag
                : GetEffectString(effect, 0);
        }

        public static void OnNWNStatusEffectInterval(uint creature)
        {
            var tag = GetLastStatusEffectTag();
            if (string.IsNullOrWhiteSpace(tag) || tag == StatusEffectTag)
            {
                TickAllStatusEffects(creature);
                return;
            }

            TickStatusEffectById(creature, tag);
        }

        private static void TickAllStatusEffects(uint creature)
        {
            // Clean up invalid creatures when we encounter them
            if (!GetIsObjectValid(creature) || GetIsDead(creature))
            {
                if (_creatureEffects.ContainsKey(creature))
                {
                    _creatureEffects.Remove(creature);
                }
                RemoveEffectByTag(creature, StatusEffectTag);
                return;
            }

            if (!_creatureEffects.ContainsKey(creature))
            {
                RemoveEffectByTag(creature, StatusEffectTag);
                return;
            }

            var effects = _creatureEffects[creature];

            foreach (var effect in effects.GetAllTickEffects())
            {
                if (effect.ActivationType != StatusEffectActivationType.Tick)
                    continue;

                TickStatusEffect(creature, effect);
            }
        }

        private static void TickStatusEffectById(uint creature, string statusEffectId)
        {
            if (!GetIsObjectValid(creature) || GetIsDead(creature))
            {
                RemoveStatusEffectById(creature, statusEffectId, false, true);
                return;
            }

            if (!_creatureEffects.TryGetValue(creature, out var effects))
            {
                RemoveEffectByTag(creature, statusEffectId);
                return;
            }

            var effect = effects.GetAllTickEffects().FirstOrDefault(x => x.Id == statusEffectId);
            if (effect == null)
            {
                RemoveEffectByTag(creature, statusEffectId);
                return;
            }

            TickStatusEffect(creature, effect);
        }

        private static void TickStatusEffect(uint creature, IStatusEffect effect)
        {
            if (effect.ActivationType != StatusEffectActivationType.Tick)
                return;

            if (effect.IsFlaggedForRemoval)
            {
                RemoveStatusEffectById(creature, effect.Id, true, true);
            }
            else
            {
                effect.TickEffect(creature);

                if (effect.IsFlaggedForRemoval)
                {
                    RemoveStatusEffectById(creature, effect.Id, true, true);
                }
            }
        }

        public static CreatureStatusEffect GetCreatureStatusEffects(uint creature)
        {
            return !_creatureEffects.ContainsKey(creature)
                ? new CreatureStatusEffect()
                : _creatureEffects[creature];
        }

        public static IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat)
        {
            return _creatureEffects.TryGetValue(creature, out var effects)
                ? effects.GetStatSources(payloadStat)
                : Array.Empty<StatAdjustmentSource>();
        }

        public static int GetStatAdjustment(uint creature, StatType stat)
        {
            return _creatureEffects.TryGetValue(creature, out var effects) &&
                   effects.StatGroup.Stats.TryGetValue(stat, out var value)
                ? value
                : 0;
        }

        public static bool HasAnyActiveEffect(uint creature, IReadOnlySet<Type> effectTypes)
        {
            return _creatureEffects.TryGetValue(creature, out var effects) && effects.HasAnyActiveEffect(effectTypes);
        }

        public static void ApplyPermanentStatusEffect<T>(uint source, uint creature)
            where T: IStatusEffect
        {
            ApplyStatusEffectInternal((IStatusEffect)Activator.CreateInstance(typeof(T)), source, creature, -1, true);
        }

        public static void ApplyPermanentStatusEffect(Type type, uint source, uint creature)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            ApplyStatusEffectInternal((IStatusEffect)Activator.CreateInstance(type), source, creature, -1, true);
        }

        private static bool ApplyStatusEffectInternal(
            IStatusEffect statusEffect,
            uint source,
            uint creature,
            int durationTicks,
            bool isPermanent,
            ResistanceType resistanceOverride = ResistanceType.Invalid,
            CombatDamageType sourceDamageType = CombatDamageType.Invalid,
            Type replacedStatusEffectType = null)
        {
            if (TryDeferNativeAttackStatusEffect(
                    statusEffect,
                    source,
                    creature,
                    durationTicks,
                    isPermanent,
                    resistanceOverride,
                    sourceDamageType,
                    replacedStatusEffectType))
            {
                return true;
            }

            durationTicks = ApplyOutgoingStatusDurationAdjustments(statusEffect, source, durationTicks, isPermanent);
            ApplyOutgoingStatusStatAdjustments(statusEffect, source);

            var resistanceType = ResolveResistanceType(statusEffect, resistanceOverride, sourceDamageType);
            var durationResistanceMessage = string.Empty;
            if (!isPermanent &&
                durationTicks > 0 &&
                GetIsObjectValid(source) &&
                GetIsObjectValid(creature) &&
                GetIsReactionTypeHostile(creature, source))
            {
                if (Resistance.IsValidResistanceType(resistanceType))
                {
                    if (Resistance.HasImmunity(creature, resistanceType))
                    {
                        SendMessageToPC(source, "Your ability was resisted.");
                        return false;
                    }

                    var durationTicksBeforeResistance = durationTicks;
                    durationTicks = Resistance.CalculateResistedTicks(creature, resistanceType, durationTicks);
                    durationResistanceMessage = BuildDurationResistanceMessage(
                        resistanceType,
                        statusEffect.Name,
                        durationTicksBeforeResistance,
                        durationTicks,
                        statusEffect.Frequency);
                }
            }

            if (!isPermanent && durationTicks <= 0)
            {
                SendMessageToPC(source, "Your ability was resisted.");
                return false;
            }

            var existingEffects = replacedStatusEffectType == null ? null : GetCreatureStatusEffects(creature);
            var replacedEffects = existingEffects?.GetAllEffects()
                .Where(effect => effect.GetType() == replacedStatusEffectType).ToArray() ?? Array.Empty<IStatusEffect>();
            if (replacedStatusEffectType != null && replacedEffects.Length == 0)
                return false;

            // A conversion validates without counting the effect it replaces as active
            // control. Preserve its native effect, expiration and stats on rejection.
            string canApply;
            foreach (var effect in replacedEffects)
                existingEffects.Remove(effect);
            try { canApply = statusEffect.CanApply(creature); }
            finally
            {
                foreach (var effect in replacedEffects)
                    existingEffects.Add(effect);
            }
            if (!string.IsNullOrWhiteSpace(canApply))
            {
                var message = $"Effect failed to apply: {canApply}";
                SendStatusEffectFailure(source, creature, message);
                return false;
            }

            foreach (var morePowerful in statusEffect.MorePowerfulEffectTypes)
            {
                if (HasEffect(morePowerful, creature))
                {
                    var message = "A more powerful effect is active.";
                    SendStatusEffectFailure(source, creature, message);
                    return false;
                }
            }

            if (replacedStatusEffectType != null)
                RemoveStatusEffect(replacedStatusEffectType, creature, OBJECT_INVALID, false, true, true);

            var creatureEffects = EnsureCreatureStatusEffectTracker(creature);

            RemoveOtherCommandStatuses(creature, statusEffect.GetType(), source);

            switch (statusEffect.StackingType)
            {
                case StatusEffectStackType.Disabled:
                case StatusEffectStackType.Invalid:
                    RemoveStatusEffect(statusEffect.GetType(), creature, OBJECT_INVALID, false, true, true);
                    break;
                case StatusEffectStackType.StackFromMultipleSources:
                    RemoveStatusEffect(statusEffect.GetType(), creature, source, false, true, true);
                    break;
            }

            foreach (var lessPowerful in statusEffect.LessPowerfulEffectTypes)
            {
                RemoveStatusEffect(lessPowerful, creature, OBJECT_INVALID, false, true, true);
            }

            statusEffect.AssignResistanceType(resistanceType);
            statusEffect.ApplyEffect(source, creature, durationTicks);
            if (statusEffect.IsFlaggedForRemoval)
            {
                statusEffect.RemoveEffect(creature);
                if (_creatureEffects.TryGetValue(creature, out creatureEffects) &&
                    creatureEffects.GetAllEffects().Count <= 0)
                {
                    RemoveCreature(creature);
                }

                return false;
            }

            creatureEffects = EnsureCreatureStatusEffectTracker(creature);
            creatureEffects.Add(statusEffect);

            if (statusEffect is ILeadershipDamageReductionStatusEffect)
            {
                ReconcileLeadershipDamageReduction(creatureEffects);
            }

            if (HasStatAdjustment(statusEffect))
            {
                Stat.ApplyCreatureMovementRate(creature);
            }

            ApplyTrackedNWNEffect(creature, statusEffect, durationTicks, isPermanent);
            Combat.ApplyStatusAppliedTargetStaminaDrain(source, creature, statusEffect.Categories);
            PublishStatusEffectReceivedRefresh(creature);

            if (!string.IsNullOrWhiteSpace(durationResistanceMessage) &&
                (GetIsPC(source) || GetIsDM(source)))
            {
                SendMessageToPC(source, durationResistanceMessage);
            }

            if (statusEffect.SendsApplicationMessage)
            {
                Messaging.SendMessageNearbyToPlayers(creature, receiver =>
                {
                    var name = PlayerName.GetDisplayName(receiver, creature);
                    var effectName = statusEffect.Name;
                    var applicationMessage = $"{name} receives the effect of {effectName}";
                    if (GetIsObjectValid(source) && source != creature)
                    {
                        var sourceName = PlayerName.GetDisplayName(receiver, source);
                        if (!string.IsNullOrWhiteSpace(sourceName))
                        {
                            applicationMessage = $"{applicationMessage} from {sourceName}";
                        }
                    }

                    return applicationMessage;
                });
            }

            return true;
        }

        public static string BuildDurationResistanceMessage(
            ResistanceType resistanceType,
            string effectName,
            int originalDurationTicks,
            int adjustedDurationTicks,
            float frequency)
        {
            if (resistanceType == ResistanceType.Invalid ||
                originalDurationTicks <= 0 ||
                adjustedDurationTicks <= 0 ||
                originalDurationTicks == adjustedDurationTicks)
            {
                return string.Empty;
            }

            var direction = adjustedDurationTicks < originalDurationTicks
                ? "reduced"
                : "increased";
            var secondsPerTick = Math.Max(1f, frequency);
            var originalSeconds = FormatDurationSeconds(originalDurationTicks * secondsPerTick);
            var adjustedSeconds = FormatDurationSeconds(adjustedDurationTicks * secondsPerTick);
            var displayedEffectName = string.IsNullOrWhiteSpace(effectName) ? "the effect" : effectName;

            return $"{resistanceType} Resistance {direction} {displayedEffectName} duration from {originalSeconds} to {adjustedSeconds}.";
        }

        private static string FormatDurationSeconds(float seconds)
        {
            var roundedSeconds = Math.Round(seconds);
            return Math.Abs(seconds - roundedSeconds) < 0.01f
                ? $"{(int)roundedSeconds}s"
                : $"{seconds:0.#}s";
        }

        private static int ApplyOutgoingStatusDurationAdjustments(
            IStatusEffect statusEffect,
            uint source,
            int durationTicks,
            bool isPermanent)
        {
            if (isPermanent || durationTicks <= 0 || !GetIsObjectValid(source))
                return durationTicks;

            var percentAdjustment = 0;
            if ((statusEffect.Categories & StatusEffectCategory.Debuff) == StatusEffectCategory.Debuff)
            {
                percentAdjustment += Stat.GetStatAdjustment(source, StatType.OutgoingDebuffDurationPercentAdjustment);
            }

            if ((statusEffect.Categories & StatusEffectCategory.Control) == StatusEffectCategory.Control)
            {
                percentAdjustment += Stat.GetStatAdjustment(source, StatType.OutgoingControlDurationPercentAdjustment);
            }

            if ((statusEffect.Categories & StatusEffectCategory.ForceDisruption) == StatusEffectCategory.ForceDisruption)
            {
                percentAdjustment += Stat.GetStatAdjustment(source, StatType.OutgoingForceDisruptionDurationPercentAdjustment);
            }

            if (percentAdjustment != 0)
            {
                durationTicks = Math.Max(1, durationTicks + (int)Math.Ceiling(durationTicks * (percentAdjustment / 100f)));
            }

            if ((statusEffect.Categories & StatusEffectCategory.Bleeding) == StatusEffectCategory.Bleeding)
            {
                var bonusSeconds = Stat.GetStatAdjustment(source, StatType.OutgoingBleedingDurationBonusSeconds);
                if (bonusSeconds > 0)
                {
                    durationTicks += Math.Max(1, (int)Math.Ceiling(bonusSeconds / Math.Max(1f, statusEffect.Frequency)));
                }
            }

            return durationTicks;
        }

        private static void ApplyOutgoingStatusStatAdjustments(IStatusEffect statusEffect, uint source)
        {
            if (!GetIsObjectValid(source))
                return;

            if ((statusEffect.Categories & StatusEffectCategory.ForceDisruption) == StatusEffectCategory.ForceDisruption)
            {
                var forceDefenseAdjustment = Stat.GetStatAdjustment(source, StatType.OutgoingForceDisruptionForceDefensePercentAdjustment);
                if (forceDefenseAdjustment != 0)
                {
                    statusEffect.StatGroup.Stats[StatType.ForceDefensePercentAdjustment] += forceDefenseAdjustment;
                }
            }
        }

        private static ResistanceType ResolveResistanceType(
            IStatusEffect statusEffect,
            ResistanceType resistanceOverride,
            CombatDamageType sourceDamageType)
        {
            if (Resistance.IsValidResistanceType(resistanceOverride))
                return resistanceOverride;

            if (Resistance.IsValidResistanceType(statusEffect.ResistanceType))
                return statusEffect.ResistanceType;

            if (sourceDamageType.TryGetElementalResistanceType(out var elementalSourceType))
                return elementalSourceType;

            if (sourceDamageType.TryGetSourceResistanceType(out var sourceResistanceType))
                return sourceResistanceType;

            return ResistanceType.Invalid;
        }

        private static void SendStatusEffectFailure(uint source, uint creature, string message)
        {
            var recipient = GetIsObjectValid(source) && (GetIsPC(source) || GetIsDM(source))
                ? source
                : creature;

            SendMessageToPC(recipient, message);
        }

        private static void ReapplyNWNEffects(uint creature, CreatureStatusEffect effects)
        {
            var shouldReapplyMovementRate = false;

            foreach (var statusEffect in effects.GetAllEffects())
            {
                statusEffect.ReapplyEffect(creature);
                ApplyTrackedNWNEffect(creature, statusEffect, statusEffect.DurationTicks, statusEffect.DurationTicks < 0);

                if (HasStatAdjustment(statusEffect))
                {
                    shouldReapplyMovementRate = true;
                }
            }

            if (shouldReapplyMovementRate)
            {
                Stat.ApplyCreatureMovementRate(creature);
            }
        }

        private static void ReassignSelfSourcedEffects(uint creature, LoggedOutStatusEffects loggedOutEffects)
        {
            foreach (var statusEffect in loggedOutEffects.Effects.GetAllEffects()
                         .Where(effect => effect.Source == loggedOutEffects.Creature))
            {
                statusEffect.ReassignSource(creature);
            }
        }

        private static void ReconcileLoggedOutEffects(uint creature, LoggedOutStatusEffects loggedOutEffects)
        {
            var currentTime = DateTime.UtcNow;
            if (currentTime < loggedOutEffects.LoggedOutAt)
            {
                currentTime = loggedOutEffects.LoggedOutAt;
            }

            var effects = loggedOutEffects.Effects;

            foreach (var statusEffect in effects.GetAllEffects())
            {
                statusEffect.ReconcileElapsedTime(currentTime);

                if (!statusEffect.IsFlaggedForRemoval)
                    continue;

                statusEffect.RemoveEffect(creature);
                effects.Remove(statusEffect);
                NotifyStatusEffectRemoved(creature, statusEffect);
                RemoveEffectByTag(creature, statusEffect.Id);
            }
        }

        private static void RemoveNonPersistentStatusEffects(uint creature, CreatureStatusEffect effects)
        {
            foreach (var statusEffect in effects.GetAllEffects().Where(effect => !effect.PersistsOnLogout))
            {
                RemoveStatusEffect(creature, statusEffect.GetType(), statusEffect.Source, false);
            }
        }

        private static void RemoveTrackedNWNEffects(uint creature, CreatureStatusEffect effects)
        {
            foreach (var statusEffect in effects.GetAllEffects())
            {
                statusEffect.RemoveNativeEffects(creature);
                RemoveNativeStatusEffect(creature, statusEffect.Id);
            }

            RemoveEffectByTag(creature, StatusEffectTag);
        }

        public static bool ApplyStatusEffect<T>(
            uint source,
            uint creature,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid)
            where T: IStatusEffect
        {
            return ApplyStatusEffect(source, creature, (IStatusEffect)Activator.CreateInstance(typeof(T)), durationSeconds, resistanceOverride);
        }

        public static bool ApplyStatusEffect<T>(
            uint source,
            uint creature,
            float durationSeconds,
            CombatDamageType sourceDamageType)
            where T : IStatusEffect
        {
            return ApplyStatusEffect(source, creature, (IStatusEffect)Activator.CreateInstance(typeof(T)), durationSeconds, sourceDamageType);
        }

        public static bool ApplyStatusEffect(
            uint source,
            uint creature,
            IStatusEffect statusEffect,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid)
        {
            var durationTicks = durationSeconds <= 0f
                ? -1
                : Math.Max(1, (int)Math.Ceiling(durationSeconds / Math.Max(1f, statusEffect.Frequency)));

            return ApplyStatusEffectInternal(
                statusEffect,
                source,
                creature,
                durationTicks,
                durationSeconds <= 0f,
                resistanceOverride);
        }

        public static bool ApplyStatusEffect(
            uint source,
            uint creature,
            IStatusEffect statusEffect,
            float durationSeconds,
            CombatDamageType sourceDamageType)
        {
            var durationTicks = durationSeconds <= 0f
                ? -1
                : Math.Max(1, (int)Math.Ceiling(durationSeconds / Math.Max(1f, statusEffect.Frequency)));

            return ApplyStatusEffectInternal(
                statusEffect,
                source,
                creature,
                durationTicks,
                durationSeconds <= 0f,
                ResistanceType.Invalid,
                sourceDamageType);
        }

        public static bool ApplyStatusEffect(
            uint source,
            uint creature,
            Type statusEffectClass,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid)
        {
            if (!TryCreateStatusEffect(statusEffectClass, out var statusEffect))
                throw new KeyNotFoundException($"Status effect '{statusEffectClass?.Name ?? "null"}' is not registered.");

            var frequency = statusEffect.Frequency;
            var durationTicks = durationSeconds <= 0f
                ? -1
                : Math.Max(1, (int)Math.Ceiling(durationSeconds / Math.Max(1f, frequency)));

            return ApplyStatusEffectInternal(
                statusEffect,
                source,
                creature,
                durationTicks,
                durationSeconds <= 0f,
                resistanceOverride);
        }

        public static bool ApplyStatusEffect(
            uint source,
            uint creature,
            Type statusEffectClass,
            float durationSeconds,
            CombatDamageType sourceDamageType,
            Type replacedStatusEffectType = null)
        {
            if (!TryCreateStatusEffect(statusEffectClass, out var statusEffect))
                throw new KeyNotFoundException($"Status effect '{statusEffectClass?.Name ?? "null"}' is not registered.");

            var frequency = statusEffect.Frequency;
            var durationTicks = durationSeconds <= 0f
                ? -1
                : Math.Max(1, (int)Math.Ceiling(durationSeconds / Math.Max(1f, frequency)));

            return ApplyStatusEffectInternal(
                statusEffect,
                source,
                creature,
                durationTicks,
                durationSeconds <= 0f,
                ResistanceType.Invalid,
                sourceDamageType,
                replacedStatusEffectType);
        }

        private static void ApplyTrackedNWNEffect(uint creature, IStatusEffect statusEffect, int durationTicks, bool isPermanent)
        {
            if (HasEffectByTag(creature, statusEffect.Id))
                return;

            var effect = BuildNativeStatusEffect(statusEffect);

            if (isPermanent || durationTicks < 0)
            {
                ApplyEffectToObject(DurationType.Permanent, effect, creature);
                return;
            }

            var durationSeconds = GetStatusEffectDurationSeconds(statusEffect, durationTicks);
            ApplyEffectToObject(DurationType.Temporary, effect, creature, durationSeconds);
        }

        private static Effect BuildNativeStatusEffect(IStatusEffect statusEffect)
        {
            var intervalScript = statusEffect.ActivationType == StatusEffectActivationType.Tick
                ? ScriptName.OnStatusEffectInterval
                : string.Empty;

            var effect = EffectRunScript(
                ScriptName.OnApplyStatusEffect,
                ScriptName.OnRemoveStatusEffect,
                intervalScript,
                Interval,
                statusEffect.Id);
            effect = TagEffect(effect, statusEffect.Id);

            if (statusEffect.Icon != EffectIconType.Invalid)
            {
                var iconEffect = TagEffect(EffectIcon(statusEffect.Icon), statusEffect.Id);
                effect = LinkEffect(effect, iconEffect);
            }

            var abilityStatEffects = BuildAbilityStatEffects(statusEffect);
            effect = LinkEffect(effect, abilityStatEffects);
            effect = TagEffect(effect, statusEffect.Id);
            return SupernaturalEffect(effect);
        }

        private static float GetStatusEffectDurationSeconds(IStatusEffect statusEffect, int durationTicks)
        {
            var logicalDurationSeconds = durationTicks * Math.Max(1f, statusEffect.Frequency);

            // NWN may remove an effect before delivering an interval callback scheduled for the
            // exact same timestamp. Ticking effects therefore keep one logical tick of native
            // lifetime grace so a late callback can catch up and the final logical tick can remove
            // the effect.
            // Passive effects have no interval callback and retain their exact duration.
            return statusEffect.ActivationType == StatusEffectActivationType.Tick
                ? logicalDurationSeconds + Math.Max(Interval, statusEffect.Frequency)
                : logicalDurationSeconds;
        }

        private static Effect LinkEffect(Effect linkedEffect, Effect effect)
        {
            return effect == null
                ? linkedEffect
                : EffectLinkEffects(linkedEffect, effect);
        }

        private static Effect BuildAbilityStatEffects(IStatusEffect statusEffect)
        {
            Effect linkedEffect = null;

            foreach (var (ability, amount) in statusEffect.StatGroup.Abilities)
            {
                if (ability == AbilityType.Invalid || amount == 0)
                    continue;

                var effect = amount > 0
                    ? EffectAbilityIncrease(ability, amount)
                    : EffectAbilityDecrease(ability, Math.Abs(amount));
                effect = TagEffect(effect, statusEffect.Id);

                linkedEffect = linkedEffect == null
                    ? effect
                    : EffectLinkEffects(linkedEffect, effect);
            }

            return linkedEffect;
        }

        public static bool HasStatusEffectDefinition(Type statusEffectClass)
        {
            return statusEffectClass != null && _statusEffects.ContainsKey(statusEffectClass);
        }

        public static bool HasStatusEffect(uint creature, Type statusEffectClass)
        {
            return _creatureEffects.ContainsKey(creature) &&
                   _creatureEffects[creature].GetAllEffects().Any(x => x.GetType() == statusEffectClass);
        }

        public static IStatusEffect GetStatusEffect(uint creature, Type statusEffectClass)
        {
            return _creatureEffects.ContainsKey(creature)
                ? _creatureEffects[creature].GetAllEffects().FirstOrDefault(x => x.GetType() == statusEffectClass)
                : null;
        }

        public static IStatusEffect GetStatusEffect(uint creature, Type statusEffectClass, uint source)
        {
            return _creatureEffects.ContainsKey(creature)
                ? _creatureEffects[creature]
                    .GetAllEffects()
                    .FirstOrDefault(x => x.GetType() == statusEffectClass && x.Source == source)
                : null;
        }

        public static bool ExtendStatusEffectDuration(
            uint creature,
            Type statusEffectClass,
            uint source,
            float durationSeconds)
        {
            if (!_creatureEffects.TryGetValue(creature, out var creatureEffects) ||
                statusEffectClass == null ||
                durationSeconds <= 0f)
            {
                return false;
            }

            var extended = false;
            foreach (var statusEffect in creatureEffects.GetAllEffects())
            {
                if (statusEffect.GetType() != statusEffectClass)
                    continue;

                if (source != OBJECT_INVALID && statusEffect.Source != source)
                    continue;

                var ticks = Math.Max(1, (int)Math.Ceiling(durationSeconds / Math.Max(1f, statusEffect.Frequency)));
                statusEffect.ExtendDurationTicks(ticks);
                RemoveNativeStatusEffect(creature, statusEffect.Id);
                ApplyTrackedNWNEffect(creature, statusEffect, statusEffect.DurationTicks, statusEffect.DurationTicks < 0);
                extended = true;
            }

            return extended;
        }

        public static bool RefreshStatusEffectDuration(
            uint creature,
            Type statusEffectClass,
            uint source,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid,
            CombatDamageType sourceDamageType = CombatDamageType.Invalid)
        {
            if (!_creatureEffects.TryGetValue(creature, out var creatureEffects) ||
                statusEffectClass == null ||
                durationSeconds <= 0f)
            {
                return false;
            }

            var refreshed = false;
            foreach (var statusEffect in creatureEffects.GetAllEffects())
            {
                if (statusEffect.GetType() != statusEffectClass)
                    continue;

                if (source != OBJECT_INVALID && statusEffect.Source != source)
                    continue;

                var ticks = Math.Max(1, (int)Math.Ceiling(durationSeconds / Math.Max(1f, statusEffect.Frequency)));
                var resistanceType = ResolveResistanceType(statusEffect, resistanceOverride, sourceDamageType);
                if (Resistance.IsValidResistanceType(resistanceType) &&
                    GetIsObjectValid(source) &&
                    GetIsObjectValid(creature) &&
                    GetIsReactionTypeHostile(creature, source))
                {
                    if (Resistance.HasImmunity(creature, resistanceType))
                    {
                        SendMessageToPC(source, "Your ability was resisted.");
                        continue;
                    }

                    ticks = Resistance.CalculateResistedTicks(creature, resistanceType, ticks);
                }

                if (ticks <= 0)
                {
                    SendMessageToPC(source, "Your ability was resisted.");
                    continue;
                }

                statusEffect.SetDurationTicks(Math.Max(statusEffect.DurationTicks, ticks));
                RemoveNativeStatusEffect(creature, statusEffect.Id);
                ApplyTrackedNWNEffect(creature, statusEffect, statusEffect.DurationTicks, statusEffect.DurationTicks < 0);
                refreshed = true;
            }

            return refreshed;
        }

        public static bool HasStatusEffect<T>(uint creature)
            where T : IStatusEffect
        {
            return HasStatusEffect(creature, typeof(T));
        }

        public static bool HasStatusEffect(uint creature, Type statusEffectClass, uint source)
        {
            return _creatureEffects.ContainsKey(creature) &&
                   _creatureEffects[creature].GetAllEffects()
                       .Any(x => x.GetType() == statusEffectClass && x.Source == source);
        }

        public static bool HasStatusEffect(uint creature, params Type[] statusEffectClasses)
        {
            return statusEffectClasses.Any(type => HasStatusEffect(creature, type));
        }

        public static bool HasStatusEffectCategory(uint creature, StatusEffectCategory category)
        {
            return _creatureEffects.ContainsKey(creature) &&
                   _creatureEffects[creature]
                       .GetAllEffects()
                       .Any(effect => (effect.Categories & category) == category);
        }

        public static bool HasStatusEffectCategory(Type statusEffectClass, StatusEffectCategory category)
        {
            if (statusEffectClass == null || category == StatusEffectCategory.None)
                return false;

            return _statusEffects.TryGetValue(statusEffectClass, out var statusEffect) &&
                   (statusEffect.Categories & category) == category;
        }

        public static bool HasCleanseableStatusEffect(uint creature, StatusEffectCleanseType cleanseType)
        {
            return GetCreatureStatusEffects(creature)
                .GetAllEffects()
                .Any(effect => HasCleanseType(effect, cleanseType));
        }

        public static void RemoveCleanseableStatusEffects(
            uint creature,
            StatusEffectCleanseType cleanseType,
            bool sendsWornOffMessage = true)
        {
            var effects = GetCreatureStatusEffects(creature)
                .GetAllEffects()
                .Where(effect => HasCleanseType(effect, cleanseType))
                .ToList();

            foreach (var effect in effects)
            {
                RemoveStatusEffect(creature, effect.GetType(), effect.Source, sendsWornOffMessage);
            }
        }

        public static bool RemoveFirstCleanseableStatusEffect(
            uint creature,
            StatusEffectCleanseType cleanseType,
            bool sendsWornOffMessage = true)
        {
            var effect = GetCreatureStatusEffects(creature)
                .GetAllEffects()
                .FirstOrDefault(effect => HasCleanseType(effect, cleanseType));

            if (effect == null)
                return false;

            RemoveStatusEffect(creature, effect.GetType(), effect.Source, sendsWornOffMessage);
            return true;
        }

        public static bool RemoveFirstStatusEffect(
            uint creature,
            IEnumerable<Type> statusEffectClasses,
            bool sendsWornOffMessage = true)
        {
            var types = statusEffectClasses?
                .Where(type => type != null)
                .ToHashSet() ?? new HashSet<Type>();

            if (types.Count <= 0)
                return false;

            var effect = GetCreatureStatusEffects(creature)
                .GetAllEffects()
                .FirstOrDefault(effect => types.Contains(effect.GetType()));

            if (effect == null)
                return false;

            RemoveStatusEffect(creature, effect.GetType(), effect.Source, sendsWornOffMessage);
            return true;
        }

        public static bool RemoveFirstStatusEffectByCategory(
            uint creature,
            StatusEffectCategory category,
            bool sendsWornOffMessage = true)
        {
            var effect = GetCreatureStatusEffects(creature)
                .GetAllEffects()
                .FirstOrDefault(effect => (effect.Categories & category) == category);

            if (effect == null)
                return false;

            RemoveStatusEffect(creature, effect.GetType(), effect.Source, sendsWornOffMessage);
            return true;
        }

        public static bool RemoveFirstBeneficialCombatStatusEffect(
            uint creature,
            bool sendsWornOffMessage = true)
        {
            var effect = GetCreatureStatusEffects(creature)
                .GetAllEffects()
                .FirstOrDefault(IsBeneficialCombatStatusEffect);

            if (effect == null)
                return false;

            RemoveStatusEffect(creature, effect.GetType(), effect.Source, sendsWornOffMessage);
            return true;
        }

        public static void ConsumeStatusEffectStat(uint creature, StatType statType)
        {
            GetCreatureStatusEffects(creature).ConsumeStat(statType);
        }

        public static void RemoveStatusEffectsWithStat(
            uint creature,
            StatType statType,
            bool sendsWornOffMessage = true)
        {
            var effects = GetCreatureStatusEffects(creature)
                .GetAllEffects()
                .Where(effect => effect.StatGroup.Stats.TryGetValue(statType, out var value) && value != 0)
                .ToList();

            foreach (var effect in effects)
            {
                RemoveStatusEffect(creature, effect.GetType(), effect.Source, sendsWornOffMessage);
            }
        }

        public static void RemoveStatusEffectFromAllTargetsBySource(
            Type statusEffectClass,
            uint source,
            bool sendsWornOffMessage = true)
        {
            if (statusEffectClass == null || !GetIsObjectValid(source))
                return;

            var targets = _creatureEffects
                .Where(entry => entry.Value
                    .GetAllEffects()
                    .Any(effect => effect.GetType() == statusEffectClass && effect.Source == source))
                .Select(entry => entry.Key)
                .ToList();

            foreach (var target in targets)
            {
                RemoveStatusEffect(target, statusEffectClass, source, sendsWornOffMessage);
            }
        }

        public static void RemoveStatusEffectsFromAllTargetsBySource(
            uint source,
            Type statusEffectType,
            bool sendsWornOffMessage = true)
        {
            if (!GetIsObjectValid(source) || statusEffectType == null)
                return;

            var effectsByTarget = _creatureEffects
                .Select(entry => new
                {
                    Target = entry.Key,
                    Effects = entry.Value.GetAllEffects()
                        .Where(effect => statusEffectType.IsAssignableFrom(effect.GetType()) && effect.Source == source)
                        .Select(effect => effect.GetType())
                        .Distinct()
                        .ToList()
                })
                .Where(entry => entry.Effects.Count > 0)
                .ToList();

            foreach (var entry in effectsByTarget)
            {
                foreach (var effectType in entry.Effects)
                    RemoveStatusEffect(entry.Target, effectType, source, sendsWornOffMessage);
            }

            foreach (var loggedOutEffects in _loggedOutPlayerEffects.Values)
            {
                var sourceOwnedEffects = loggedOutEffects.Effects.GetAllEffects()
                    .Where(effect =>
                        statusEffectType.IsAssignableFrom(effect.GetType()) &&
                        effect.Source == source)
                    .ToList();
                foreach (var effect in sourceOwnedEffects)
                    loggedOutEffects.Effects.Remove(effect);
            }
        }

        private static void RemoveStatusEffectsFromAllTargetsWhenSourceExits(uint source)
        {
            if (!GetIsObjectValid(source))
                return;

            var effectsByTarget = _creatureEffects
                .Select(entry => new
                {
                    Target = entry.Key,
                    EffectTypes = entry.Value.GetAllEffects()
                        .Where(effect => effect.Source == source && effect is IRemoveWhenSourceExits)
                        .Select(effect => effect.GetType())
                        .Distinct()
                        .ToList()
                })
                .Where(entry => entry.EffectTypes.Count > 0)
                .ToList();

            foreach (var entry in effectsByTarget)
            {
                foreach (var effectType in entry.EffectTypes)
                    RemoveStatusEffect(entry.Target, effectType, source, false);
            }

            foreach (var loggedOutEffects in _loggedOutPlayerEffects.Values)
            {
                var sourceOwnedEffects = loggedOutEffects.Effects.GetAllEffects()
                    .Where(effect => effect.Source == source && effect is IRemoveWhenSourceExits)
                    .ToList();
                foreach (var effect in sourceOwnedEffects)
                    loggedOutEffects.Effects.Remove(effect);
            }
        }

        public static void RemoveStatusEffectsWithNegativeStat(
            uint creature,
            StatType statType,
            bool sendsWornOffMessage = true)
        {
            var effects = GetCreatureStatusEffects(creature)
                .GetAllEffects()
                .Where(effect => effect.StatGroup.Stats.TryGetValue(statType, out var value) && value < 0)
                .ToList();

            foreach (var effect in effects)
            {
                RemoveStatusEffect(creature, effect.GetType(), effect.Source, sendsWornOffMessage);
            }
        }

        public static uint GetStatusEffectSourceWithStat(uint creature, StatType statType)
        {
            var effect = GetCreatureStatusEffects(creature)
                .GetAllEffects()
                .Where(effect =>
                    GetIsObjectValid(effect.Source) &&
                    effect.StatGroup.Stats.TryGetValue(statType, out var value) &&
                    value != 0)
                .OrderByDescending(effect => Math.Abs(effect.StatGroup.Stats[statType]))
                .FirstOrDefault();

            return effect?.Source ?? OBJECT_INVALID;
        }

        private static bool IsBeneficialCombatStatusEffect(IStatusEffect effect)
        {
            if ((effect.Categories & StatusEffectCategory.Buff) == StatusEffectCategory.Buff)
                return true;

            if (effect.PersistsOnLogout ||
                effect.CleanseTypes != StatusEffectCleanseType.None ||
                (effect.Categories & (StatusEffectCategory.Debuff | StatusEffectCategory.Control | StatusEffectCategory.Bleeding)) != 0)
            {
                return false;
            }

            return effect.StatGroup.Abilities.Any(x => x.Value > 0) ||
                   effect.StatGroup.Resists.Any(x => x.Value > 0) ||
                   effect.StatGroup.Stats.Any(x => Stat.IsBeneficialStatAdjustment(x.Key, x.Value));
        }

        public static bool HasCleanseType(IStatusEffect effect, StatusEffectCleanseType cleanseType)
        {
            return (effect.CleanseTypes & cleanseType) == cleanseType;
        }

        public static void RemoveStatusEffect(
            uint creature,
            Type statusEffectClass,
            bool sendsWornOffMessage = true,
            bool removeNativeEffect = true)
        {
            RemoveStatusEffect(statusEffectClass, creature, OBJECT_INVALID, sendsWornOffMessage, removeNativeEffect);
        }

        public static void RemoveStatusEffect(
            uint creature,
            Type statusEffectClass,
            uint source,
            bool sendsWornOffMessage = true,
            bool removeNativeEffect = true)
        {
            RemoveStatusEffect(statusEffectClass, creature, source, sendsWornOffMessage, removeNativeEffect);
        }

        public static string GetStatusEffectName(Type statusEffectClass)
        {
            return statusEffectClass != null && _statusEffects.TryGetValue(statusEffectClass, out var statusEffect)
                ? statusEffect.Name
                : statusEffectClass?.Name ?? "Unknown";
        }

        public static StatusEffectSourceType GetStatusEffectSourceType(Type statusEffectClass)
        {
            return statusEffectClass != null && _statusEffects.TryGetValue(statusEffectClass, out var statusEffect)
                ? statusEffect.SourceType
                : StatusEffectSourceType.Invalid;
        }

        public static void RemoveOtherStanceStatuses(
            uint creature,
            Type statusEffectType,
            bool sendsWornOffMessage = false,
            bool removeNativeEffect = true)
        {
            if (GetStatusEffectSourceType(statusEffectType) != StatusEffectSourceType.Stance)
                return;

            RemoveStatusEffectBySourceType(
                creature,
                StatusEffectSourceType.Stance,
                sendsWornOffMessage,
                statusEffectType,
                removeNativeEffect);
        }

        /// <summary>
        /// Enforces command exclusivity for Leadership-style party-buff commands (Press the
        /// Attack, Cleanse Order, Decisive Command): applying one removes any other
        /// <see cref="StatusEffectSourceType.Command"/>-classified effect that the same source
        /// leader previously applied to this target. Effects applied by other leaders, and
        /// non-Command effects such as auras, are left untouched. Newest application wins.
        /// </summary>
        public static void RemoveOtherCommandStatuses(
            uint creature,
            Type statusEffectType,
            uint source,
            bool sendsWornOffMessage = false,
            bool removeNativeEffect = true)
        {
            if (GetStatusEffectSourceType(statusEffectType) != StatusEffectSourceType.Command)
                return;

            RemoveStatusEffectBySourceType(
                creature,
                StatusEffectSourceType.Command,
                sendsWornOffMessage,
                statusEffectType,
                removeNativeEffect,
                source,
                isReplacement: true);
        }

        public static List<Type> GetStatusEffectsFromIcon(EffectIconType effectIcon)
        {
            return _statusEffects
                .Where(x => x.Value.Create().Icon == effectIcon)
                .Select(x => x.Key)
                .ToList();
        }

        public static List<Type> GetStatusEffectsFromIcon(uint creature, EffectIconType effectIcon)
        {
            var activeStatusTypes = GetCreatureStatusEffects(creature)
                .GetAllEffects()
                .Where(effect => effect.Icon == effectIcon)
                .Select(effect => effect.GetType())
                .ToList();

            return activeStatusTypes.Count > 0
                ? activeStatusTypes
                : GetStatusEffectsFromIcon(effectIcon);
        }

        public static int GetEffectDuration(uint creature, params Type[] effectTypes)
        {
            if (!_creatureEffects.ContainsKey(creature))
                return 0;

            var effect = _creatureEffects[creature]
                .GetAllEffects()
                .FirstOrDefault(x => effectTypes.Contains(x.GetType()));

            if (effect == null || effect.DurationTicks < 0)
                return 0;

            var nativeDuration = GetNativeStatusEffectDuration(creature, effect.Id);
            return nativeDuration > 0
                ? nativeDuration
                : (int)Math.Ceiling(effect.DurationTicks * effect.Frequency);
        }

        private static int GetNativeStatusEffectDuration(uint creature, string statusEffectId)
        {
            var remaining = 0;

            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                if (GetEffectTag(effect) != statusEffectId)
                    continue;

                remaining = Math.Max(remaining, GetEffectDurationRemaining(effect));
            }

            return remaining;
        }

        public static EffectTypeScript GetEffectTypeFromIcon(EffectIconType effectIcon)
        {
            return _effectIconToEffectType.TryGetValue(effectIcon, out var effectType)
                ? effectType
                : EffectTypeScript.Invalideffect;
        }

        public static AbilityType GetAbilityTypeBuffed(EffectIconType effectIcon)
        {
            return _abilityIncreaseIconType.TryGetValue(effectIcon, out var abilityType)
                ? abilityType
                : AbilityType.Invalid;
        }

        private static void RemoveStatusEffect(
            Type type,
            uint creature,
            uint source,
            bool sendsWornOffMessage = true,
            bool removeNativeEffect = true,
            bool isReplacement = false)
        {
            if (!_creatureEffects.TryGetValue(creature, out var creatureEffects))
                return;

            // RemoveStatusEffectInstance only emits the worn-off message for the final remaining
            // instance of a given type, so removing every instance here yields exactly one message.
            var statusEffects = creatureEffects.GetAllEffects();
            foreach (var statusEffect in statusEffects)
            {
                if (statusEffect.GetType() != type)
                    continue;

                if (source != OBJECT_INVALID && statusEffect.Source != source)
                    continue;

                RemoveStatusEffectInstance(
                    creature,
                    creatureEffects,
                    statusEffect,
                    sendsWornOffMessage,
                    removeNativeEffect,
                    isReplacement);
            }

            RemoveCreatureIfEmpty(creature, creatureEffects, removeNativeEffect);
        }

        private static void RemoveStatusEffectById(
            uint creature,
            string statusEffectId,
            bool sendsWornOffMessage = true,
            bool removeNativeEffect = true)
        {
            if (!_creatureEffects.TryGetValue(creature, out var creatureEffects))
            {
                if (removeNativeEffect)
                    RemoveNativeStatusEffect(creature, statusEffectId);

                return;
            }

            var statusEffect = creatureEffects.GetAllEffects().FirstOrDefault(x => x.Id == statusEffectId);
            if (statusEffect == null)
            {
                if (removeNativeEffect)
                    RemoveNativeStatusEffect(creature, statusEffectId);

                return;
            }

            RemoveStatusEffectInstance(creature, creatureEffects, statusEffect, sendsWornOffMessage, removeNativeEffect);
            RemoveCreatureIfEmpty(creature, creatureEffects, removeNativeEffect);
        }

        private static void RemoveStatusEffectInstance(
            uint creature,
            CreatureStatusEffect creatureEffects,
            IStatusEffect statusEffect,
            bool sendsWornOffMessage,
            bool removeNativeEffect,
            bool isReplacement = false)
        {
            if (sendsWornOffMessage &&
                statusEffect.SendsWornOffMessage &&
                IsLastInstanceOfType(creatureEffects, statusEffect))
            {
                var effectName = statusEffect.Name;
                Messaging.SendMessageNearbyToPlayers(creature,
                    receiver => $"{PlayerName.GetDisplayName(receiver, creature)}'s {effectName} effect has worn off.");
            }

            if (removeNativeEffect)
                RemoveNativeStatusEffect(creature, statusEffect.Id);

            statusEffect.RemoveEffect(creature, isReplacement);
            creatureEffects.Remove(statusEffect);
            NotifyStatusEffectRemoved(creature, statusEffect);

            if (statusEffect is ILeadershipDamageReductionStatusEffect)
            {
                ReconcileLeadershipDamageReduction(creatureEffects);
            }

            if (HasStatAdjustment(statusEffect))
            {
                DelayCommand(0.1f, () => Stat.ApplyCreatureMovementRate(creature));
            }

            if (!isReplacement)
            {
                PublishStatusEffectRemovedRefresh(creature);
            }
        }

        /// <summary>
        /// Returns true when the supplied status effect is the only remaining instance of its
        /// runtime type on the creature. Stacking effects (e.g. Suppression) apply many separate
        /// instances that each expire on their own timer; gating the "worn off" message on the
        /// final instance collapses what would otherwise be one message per stack into a single
        /// notification when the effect fully wears off.
        /// </summary>
        private static bool IsLastInstanceOfType(CreatureStatusEffect creatureEffects, IStatusEffect statusEffect)
        {
            var type = statusEffect.GetType();
            return creatureEffects.GetAllEffects().Count(effect => effect.GetType() == type) <= 1;
        }

        private static void NotifyStatusEffectRemoved(uint creature, IStatusEffect statusEffect)
        {
            if (statusEffect is IStatusEffectRemovedHandler handler)
                handler.AfterRemoved(creature);
        }

        private static void NotifyStatusEffectsRestored(uint creature, CreatureStatusEffect effects)
        {
            foreach (var handler in effects.GetAllEffects().OfType<IStatusEffectRestoredHandler>().ToList())
            {
                handler.AfterRestored(creature);
            }
        }

        /// <summary>
        /// Enforces take-the-max behavior across the Leadership damage-reduction family
        /// independently for each damage channel. A physical/Force-only effect can supersede
        /// Hold the Line in those channels without disabling Hold the Line's protection against
        /// elemental and other damage. Weaker members remain applied so their unrelated stats
        /// continue to function, and every channel is recomputed after application or removal.
        /// </summary>
        private static void ReconcileLeadershipDamageReduction(CreatureStatusEffect creatureEffects)
        {
            var family = creatureEffects.GetAllEffects()
                .Where(effect => effect is ILeadershipDamageReductionStatusEffect)
                .ToList();

            if (family.Count == 0)
                return;

            var winnerByStat = family
                .SelectMany(effect =>
                    ((ILeadershipDamageReductionStatusEffect)effect).LeadershipDamageReductionStats.Keys)
                .Distinct()
                .ToDictionary(
                    statType => statType,
                    statType => family
                        .Where(effect =>
                            ((ILeadershipDamageReductionStatusEffect)effect)
                            .LeadershipDamageReductionStats.ContainsKey(statType))
                        .OrderBy(effect =>
                            ((ILeadershipDamageReductionStatusEffect)effect)
                            .LeadershipDamageReductionStats[statType])
                        .First());

            foreach (var effect in family)
            {
                var leadershipDamageReductionEffect = (ILeadershipDamageReductionStatusEffect)effect;
                ApplyLeadershipDamageReductionContribution(
                    creatureEffects,
                    effect,
                    leadershipDamageReductionEffect,
                    winnerByStat);
            }
        }

        private static void ApplyLeadershipDamageReductionContribution(
            CreatureStatusEffect creatureEffects,
            IStatusEffect effect,
            ILeadershipDamageReductionStatusEffect leadershipDamageReductionEffect,
            IReadOnlyDictionary<StatType, IStatusEffect> winnerByStat)
        {
            var isAlreadyCorrect = leadershipDamageReductionEffect.LeadershipDamageReductionStats.All(pair =>
                effect.StatGroup.Stats.TryGetValue(pair.Key, out var current) &&
                current == (ReferenceEquals(winnerByStat[pair.Key], effect) ? pair.Value : 0));

            if (isAlreadyCorrect)
                return;

            creatureEffects.Remove(effect);

            foreach (var (statType, nominalValue) in leadershipDamageReductionEffect.LeadershipDamageReductionStats)
            {
                effect.StatGroup.Stats[statType] = ReferenceEquals(winnerByStat[statType], effect)
                    ? nominalValue
                    : 0;
            }

            creatureEffects.Add(effect);
        }

        private static bool HasStatAdjustment(IStatusEffect statusEffect)
        {
            return statusEffect.StatGroup.Stats.Any(stat => stat.Value != 0);
        }

        private static void PublishStatusEffectReceivedRefresh(uint creature)
        {
            Gui.PublishCharacterSheetRefreshEvent(creature, new StatusEffectReceivedRefreshEvent());
        }

        private static void PublishStatusEffectRemovedRefresh(uint creature)
        {
            Gui.PublishCharacterSheetRefreshEvent(creature, new StatusEffectRemovedRefreshEvent());
        }

        private static void RemoveNativeStatusEffect(uint creature, string statusEffectId)
        {
            _suppressedStatusEffectRemovalIds.Add(statusEffectId);
            RemoveEffectByTag(creature, statusEffectId);
            _suppressedStatusEffectRemovalIds.Remove(statusEffectId);
        }

        public static void RemoveStatusEffect<T>(uint creature)
            where T: IStatusEffect
        {
            var type = typeof(T);
            RemoveStatusEffect(type, creature);
        }

        public static void RemoveStatusEffect(Type type, uint creature)
        {
            RemoveStatusEffect(type, creature, OBJECT_INVALID);
        }

        public static void RemoveStatusEffectBySourceType(
            uint creature,
            StatusEffectSourceType sourceType,
            bool sendsWornOffMessage = true,
            Type excludedStatusEffectType = null,
            bool removeNativeEffect = true,
            uint filterBySource = OBJECT_INVALID,
            bool isReplacement = false)
        {
            var creatureEffects = GetCreatureStatusEffects(creature);
            var effects = creatureEffects.GetAllBySourceType(sourceType);
            foreach (var effect in effects)
            {
                if (excludedStatusEffectType != null && effect.GetType() == excludedStatusEffectType)
                    continue;

                if (filterBySource != OBJECT_INVALID && effect.Source != filterBySource)
                    continue;

                RemoveStatusEffect(
                    effect.GetType(),
                    creature,
                    effect.Source,
                    sendsWornOffMessage,
                    removeNativeEffect,
                    isReplacement);
            }
        }

        public static bool HasEffect(Type type, uint creature)
        {
            if (!_creatureEffects.ContainsKey(creature))
                return false;

            return _creatureEffects[creature].HasEffect(type);
        }

        public static bool HasEffect<T>(uint creature)
            where T : IStatusEffect
        {
            return HasEffect(typeof(T), creature);
        }

        private static void RemoveStatusEffect(IStatusEffect statusEffect, uint creature, uint source = OBJECT_INVALID)
        {
            RemoveStatusEffect(statusEffect.GetType(), creature, source);
        }

        public static T GetStatusEffect<T>(uint creature)
            where T : class, IStatusEffect
        {
            return !_creatureEffects.ContainsKey(creature)
                ? null
                : _creatureEffects[creature].GetEffect<T>();
        }


        public static void RemoveAllStatusEffects(uint creature)
        {
            if (!_creatureEffects.ContainsKey(creature))
                return;

            var effects = _creatureEffects[creature].GetAllEffects();
            foreach (var effect in effects)
            {
                RemoveStatusEffect(effect.GetType(), creature);
            }
        }

        /// <summary>
        /// Removes a creature from the status effect system entirely
        /// </summary>
        public static void RemoveCreature(uint creature, bool removeNativeEffect = true)
        {
            if (_creatureEffects.ContainsKey(creature))
            {
                _creatureEffects.Remove(creature);
            }

            if (removeNativeEffect)
            {
                RemoveEffectByTag(creature, StatusEffectTag);
            }
        }

        private static void RemoveCreatureIfEmpty(
            uint creature,
            CreatureStatusEffect effects,
            bool removeNativeEffect = true)
        {
            if (effects.GetAllEffects().Count > 0)
                return;

            RemoveCreature(creature, removeNativeEffect);
        }

        [NWNEventHandler(ScriptName.OnSWLORDamage)]
        public static void OnDealtDamage()
        {
            var attacker = OBJECT_SELF;
            var defender = StringToObject(EventsPlugin.GetEventData("DEFENDER"));
            var damage = Convert.ToInt32(EventsPlugin.GetEventData("DAMAGE"));
            var damageTypeText = EventsPlugin.GetEventData("DAMAGE_TYPE");
            var damageType = int.TryParse(damageTypeText, out var parsedDamageType)
                ? (CombatDamageType)parsedDamageType
                : CombatDamageType.Physical;

            NotifyDamageStatusEffects(attacker, defender, damage, damageType);

            var effects = GetCreatureStatusEffects(attacker);

            foreach (var effect in effects.GetAllOnHitEffects())
            {
                effect.OnHitEffect(attacker, defender, damage);
            }
        }

        public static void NotifyDamageStatusEffects(
            uint attacker,
            uint defender,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct)
        {
            if (damage <= 0 || !GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return;

            foreach (var effect in GetCreatureStatusEffects(attacker).GetAllEffects())
            {
                effect.OnDamageDealtEffect(attacker, defender, damage, damageType, deliveryType);
            }

            foreach (var effect in GetCreatureStatusEffects(defender).GetAllEffects())
            {
                effect.OnDamageTakenEffect(defender, attacker, damage, damageType, deliveryType);
            }
        }

        /// <summary>
        /// Notifies status effects that must validate their state before an originating hit is
        /// applied. Ordinary damage reactions remain in NotifyDamageStatusEffects so they observe
        /// the defender's post-hit state whenever the damage path supports it.
        /// </summary>
        public static void NotifyPreDamageStatusEffects(
            uint attacker,
            uint defender,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct)
        {
            if (damage <= 0 || !GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return;

            foreach (var effect in GetCreatureStatusEffects(defender)
                         .GetAllEffects()
                         .OfType<IPreDamageStatusEffect>()
                         .ToList())
            {
                effect.OnBeforeDamageTaken(defender, attacker, damage, damageType, deliveryType);
            }
        }

        /// <summary>
        /// Notifies limited-attack status effects once per originating hostile attack, including
        /// misses and deflections. Effects exhausted by the attempt are removed immediately so
        /// they cannot affect another attack before the next status-effect tick.
        /// </summary>
        public static void NotifyAttackAttemptStatusEffects(
            uint attacker,
            SkillType skillType,
            AbilityImpactSummary abilityImpact = null)
        {
            if (!GetIsObjectValid(attacker))
                return;

            var effects = GetCreatureStatusEffects(attacker)
                .GetAllEffects()
                .OfType<IAttackAttemptStatusEffect>()
                .ToList();

            foreach (var effect in effects)
            {
                effect.OnAttackAttemptedEffect(attacker, skillType, abilityImpact);
            }

            foreach (var effect in effects.Where(effect => effect.IsFlaggedForRemoval))
            {
                RemoveStatusEffect(
                    attacker,
                    effect.GetType(),
                    effect.Source,
                    effect.SendsWornOffMessage);
            }
        }

        /// <summary>
        /// Marks the start of a synchronous native swing. Limited attack-timing effects granted
        /// while its already-scheduled rolls resolve are deferred until the outermost swing ends.
        /// </summary>
        public static void BeginNativeAttackSwing(uint attacker)
        {
            _nativeAttackSwingDepth.TryGetValue(attacker, out var depth);
            _nativeAttackSwingDepth[attacker] = depth + 1;
        }

        /// <summary>
        /// Ends a synchronous native swing and applies limited attack-timing effects that were
        /// granted by its precomputed rolls.
        /// </summary>
        public static void EndNativeAttackSwing(uint attacker)
        {
            if (!_nativeAttackSwingDepth.TryGetValue(attacker, out var depth))
                return;

            if (depth > 1)
            {
                _nativeAttackSwingDepth[attacker] = depth - 1;
                return;
            }

            _nativeAttackSwingDepth.Remove(attacker);
            if (!_deferredNativeAttackStatusEffects.Remove(attacker, out var deferredEffects))
                return;

            foreach (var applyDeferredEffect in deferredEffects)
            {
                applyDeferredEffect();
            }
        }

        private static bool TryDeferNativeAttackStatusEffect(
            IStatusEffect statusEffect,
            uint source,
            uint creature,
            int durationTicks,
            bool isPermanent,
            ResistanceType resistanceOverride,
            CombatDamageType sourceDamageType,
            Type replacedStatusEffectType)
        {
            if ((statusEffect is not ILimitedAttackDelayReductionStatusEffect &&
                 statusEffect is not ILimitedAttackNoDelayStatusEffect) ||
                !_nativeAttackSwingDepth.ContainsKey(creature))
            {
                return false;
            }

            if (!_deferredNativeAttackStatusEffects.TryGetValue(creature, out var deferredEffects))
            {
                deferredEffects = new List<Action>();
                _deferredNativeAttackStatusEffects[creature] = deferredEffects;
            }

            deferredEffects.Add(() => ApplyStatusEffectInternal(
                statusEffect,
                source,
                creature,
                durationTicks,
                isPermanent,
                resistanceOverride,
                sourceDamageType,
                replacedStatusEffectType));
            return true;
        }

        /// <summary>
        /// Gets the attack-delay reduction and earliest remaining charge count supplied by active
        /// limited-attack effects that apply to the requested skill.
        /// </summary>
        public static bool TryGetLimitedAttackDelayReduction(
            uint attacker,
            SkillType skillType,
            out int reductionPercent,
            out int remainingAttacks)
        {
            if (Combat.IsAttackDelayReductionSuppressed(attacker))
            {
                reductionPercent = 0;
                remainingAttacks = 0;
                return false;
            }

            var effects = GetCreatureStatusEffects(attacker)
                .GetAllEffects()
                .OfType<ILimitedAttackDelayReductionStatusEffect>()
                .Where(effect => effect.AppliesToSkill(skillType) && effect.RemainingAttacks > 0)
                .ToList();

            reductionPercent = effects.Sum(effect => effect.AttackDelayReductionPercent);
            remainingAttacks = effects.Count == 0
                ? 0
                : effects.Min(effect => effect.RemainingAttacks);
            return effects.Count > 0 && reductionPercent > 0;
        }

        /// <summary>
        /// Gets the earliest remaining charge count supplied by active limited no-delay effects
        /// that apply to the requested attack skill.
        /// </summary>
        public static bool TryGetLimitedAttackNoDelay(
            uint attacker,
            SkillType skillType,
            out int remainingAttacks)
        {
            if (Combat.IsAttackDelayReductionSuppressed(attacker))
            {
                remainingAttacks = 0;
                return false;
            }

            var effects = GetCreatureStatusEffects(attacker)
                .GetAllEffects()
                .OfType<ILimitedAttackNoDelayStatusEffect>()
                .Where(effect => effect.AppliesToSkill(skillType) && effect.RemainingAttacks > 0)
                .ToList();

            remainingAttacks = effects.Count == 0
                ? 0
                : effects.Min(effect => effect.RemainingAttacks);
            return effects.Count > 0;
        }

        public static void OnGuardedHit(uint defender, uint attacker, int preventedDamage)
        {
            if (!_creatureEffects.TryGetValue(defender, out var creatureEffects))
                return;

            foreach (var effect in creatureEffects
                         .GetAllEffects()
                         .OfType<IGuardedHitStatusEffect>()
                         .ToList())
            {
                effect.OnGuardedHitEffect(defender, attacker, preventedDamage);
            }
        }

        private readonly struct StatusEffectMetadata
        {
            public Func<IStatusEffect> Create { get; }
            public string Name { get; }
            public float Frequency { get; }
            public StatusEffectSourceType SourceType { get; }
            public StatusEffectCategory Categories { get; }

            public StatusEffectMetadata(
                Func<IStatusEffect> create,
                string name,
                float frequency,
                StatusEffectSourceType sourceType,
                StatusEffectCategory categories)
            {
                Create = create;
                Name = name;
                Frequency = frequency;
                SourceType = sourceType;
                Categories = categories;
            }
        }

        private sealed class LoggedOutStatusEffects
        {
            public uint Creature { get; }
            public CreatureStatusEffect Effects { get; }
            public DateTime LoggedOutAt { get; }

            public LoggedOutStatusEffects(uint creature, CreatureStatusEffect effects, DateTime loggedOutAt)
            {
                Creature = creature;
                Effects = effects;
                LoggedOutAt = loggedOutAt;
            }
        }
    }
}
