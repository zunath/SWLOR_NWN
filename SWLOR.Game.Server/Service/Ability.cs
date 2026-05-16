using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.TelegraphService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using NumericsVector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service
{
    public static class Ability
    {
        private static readonly Dictionary<FeatType, AbilityDetail> _abilities = new();
        private static readonly Dictionary<uint, PlayerAura> _playerAuras = new();
        private static readonly Dictionary<uint, TrackedAbilityImpact> _trackedAbilityImpacts = new();

        private const int MaxNumberOfAuras = 4;
        private const int HostileAbilityBaseEnmity = 100;
        private const int HostileAbilityMissEnmity = 1;

        /// <summary>
        /// When the module caches, abilities will be cached and events will be scheduled.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            CacheAbilities();
        }

        private static void CacheAbilities()
        {
            _abilities.Clear();
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IAbilityListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IAbilityListDefinition) Activator.CreateInstance(type);
                var abilities = instance.BuildAbilities();

                foreach (var (feat, ability) in abilities)
                {
                    _abilities[feat] = ability;
                }
            }

            Console.WriteLine($"Loaded {_abilities.Count} abilities.");
        }

        public static IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails()
        {
            return _abilities;
        }

        /// <summary>
        /// Returns true if a feat is registered to an ability.
        /// Returns false otherwise.
        /// </summary>
        /// <param name="featType">The type of feat to check.</param>
        /// <returns>true if feat is registered to an ability. false otherwise.</returns>
        public static bool IsFeatRegistered(FeatType featType)
        {
            return _abilities.ContainsKey(featType);
        }

        /// <summary>
        /// Retrieves an ability's details by the specified feat type.
        /// If feat does not have an ability, an exception will be thrown.
        /// </summary>
        /// <param name="featType">The type of feat</param>
        /// <returns>The ability detail</returns>
        public static AbilityDetail GetAbilityDetail(FeatType featType)
        {
            if(!_abilities.ContainsKey(featType))
                throw new KeyNotFoundException($"Feat '{featType}' is not registered to an ability.");

            return _abilities[featType];
        }

        public static void BeginAbilityImpact(uint activator, AbilityDetail ability)
        {
            if (!GetIsObjectValid(activator) || ability == null)
                return;

            var abilitySkillType = Combat.GetAbilitySkillType(activator, ability);
            var nextAbilityDamageBonus = Combat.ConsumeNextAbilityDamageBonus(activator, ability.EffectiveLevelPerkType);
            var nextSkillAbilityBonuses = Combat.ConsumeNextSkillAbilityBonuses(activator, abilitySkillType);
            BeginAbilityImpact(
                activator,
                ability,
                nextAbilityDamageBonus + nextSkillAbilityBonuses.DamageBonus,
                nextSkillAbilityBonuses.CriticalRatePercentAdjustment,
                nextSkillAbilityBonuses.DefenseIgnorePercentAdjustment);
        }

        private static void BeginAbilityImpact(
            uint activator,
            AbilityDetail ability,
            int nextAbilityDamageBonus,
            int nextAbilityCriticalRatePercentAdjustment,
            int nextAbilityDefenseIgnorePercentAdjustment = 0)
        {
            if (!GetIsObjectValid(activator) || ability == null)
                return;

            _trackedAbilityImpacts[activator] = new TrackedAbilityImpact(
                ability,
                nextAbilityDamageBonus,
                nextAbilityCriticalRatePercentAdjustment,
                nextAbilityDefenseIgnorePercentAdjustment);
        }

        public static AbilityImpactSummary EndAbilityImpact(uint activator)
        {
            if (!_trackedAbilityImpacts.TryGetValue(activator, out var impact))
                return new AbilityImpactSummary();

            _trackedAbilityImpacts.Remove(activator);
            return impact.Summary;
        }

        private static TrackedAbilityImpact GetTrackedAbilityImpact(uint activator)
        {
            return _trackedAbilityImpacts.TryGetValue(activator, out var impact)
                ? impact
                : null;
        }

        public static float GetActiveForceAffinityMagnitudeMultiplier(uint activator)
        {
            var trackedImpact = GetTrackedAbilityImpact(activator);
            if (trackedImpact?.Ability?.SkillType != SkillType.Force)
                return 1f;

            return Perk.GetForceAffinityMagnitudeMultiplier(
                activator,
                trackedImpact.Ability.EffectiveLevelPerkType);
        }

        public static int ApplyActiveForceAffinityMagnitude(uint activator, int amount)
        {
            var trackedImpact = GetTrackedAbilityImpact(activator);
            if (trackedImpact?.Ability?.SkillType != SkillType.Force)
                return amount;

            return Perk.ApplyForceAffinityMagnitude(
                activator,
                trackedImpact.Ability.EffectiveLevelPerkType,
                amount);
        }

        public static float ApplyActiveForceAffinityMagnitude(uint activator, float amount)
        {
            var trackedImpact = GetTrackedAbilityImpact(activator);
            if (trackedImpact?.Ability?.SkillType != SkillType.Force)
                return amount;

            return Perk.ApplyForceAffinityMagnitude(
                activator,
                trackedImpact.Ability.EffectiveLevelPerkType,
                amount);
        }

        public static int ApplyActiveForceAffinityDurationAdjustment(uint activator, int durationTicks, bool isPermanent)
        {
            if (isPermanent || durationTicks <= 0)
                return durationTicks;

            var adjustedDuration = ApplyActiveForceAffinityMagnitude(activator, durationTicks);
            return Math.Max(1, adjustedDuration);
        }

        public static void ApplyHostileAbilityEnmity(uint activator, uint target, int damage = 0)
        {
            var amount = HostileAbilityBaseEnmity + Math.Max(0, damage);
            Enmity.ModifyEnmity(activator, target, amount);
        }

        private static void ApplyMissedHostileAbilityEnmity(uint activator, uint target)
        {
            Enmity.ModifyEnmity(activator, target, HostileAbilityMissEnmity);
        }

        private static void RecordAbilityImpactShape(uint activator, SkillType skillType, bool isArea)
        {
            var impact = GetTrackedAbilityImpact(activator);
            if (impact == null)
                return;

            impact.RecordShape(skillType, isArea);
        }

        private static void RecordAbilityImpactTarget(uint activator, uint target, SkillType skillType, bool isArea)
        {
            var impact = GetTrackedAbilityImpact(activator);
            if (impact == null || !GetIsObjectValid(target))
                return;

            impact.RecordShape(skillType, isArea);
            impact.RecordTarget(target);
        }

        private static bool IsTrackedAbilityArea(uint activator)
        {
            var impact = GetTrackedAbilityImpact(activator);
            return impact?.Summary.IsAreaAbility ?? false;
        }

        private static bool IsTrackedAbilitySingleTarget(uint activator)
        {
            var impact = GetTrackedAbilityImpact(activator);
            return impact?.Summary.IsSingleTargetAbility ?? false;
        }



        /// <summary>
        /// Checks whether a creature can activate the perk feat.
        /// </summary>
        /// <param name="activator">The activator of the perk feat.</param>
        /// <param name="target">The target of the perk feat.</param>
        /// <param name="abilityType">The type of ability to use.</param>
        /// <param name="effectivePerkLevel">The activator's effective perk level.</param>
        /// <param name="targetLocation">The target location of the perk feat.</param>
        /// <returns>true if successful, false otherwise</returns>
        public static bool CanUseAbility(
            uint activator,
            uint target,
            FeatType abilityType,
            int effectivePerkLevel,
            Location targetLocation)
        {
            var ability = GetAbilityDetail(abilityType);

            // Cannot use this ability in space.
            if (Space.IsPlayerInSpaceMode(activator) &&
                !ability.CanBeUsedInSpace)
            {
                SendMessageToPC(activator, "This ability cannot be used in space.");
                return false;
            }

            // Must have appropriate levels in the perk to use the ability.
            if (effectivePerkLevel <= 0 || ability.AbilityLevel > effectivePerkLevel)
            {
                SendMessageToPC(activator, "You do not meet the prerequisites to use this ability.");
                return false;
            }

            // Activator is dead.
            if (GetCurrentHitPoints(activator) <= 0)
            {
                SendMessageToPC(activator, "You are dead.");
                return false;
            }

            // Not commandable
            if (!GetCommandable(activator))
            {
                SendMessageToPC(activator, "You cannot take actions at this time.");
                return false;
            }

            // Must be within line of sight.
            if (GetIsObjectValid(target) && !LineOfSightObject(activator, target))
            {
                SendMessageToPC(activator, "You cannot see your target.");
                return false;
            }

            // Must not be busy
            if (Activity.IsBusy(activator))
            {
                SendMessageToPC(activator, "You are busy.");
                return false;
            }

            if (Combat.GetAbilitySkillType(activator, ability) == SkillType.Force &&
                Stat.GetStatAdjustment(activator, StatType.ForceAbilityActivationDisabled) > 0)
            {
                SendMessageToPC(activator, "You cannot use Force abilities right now.");
                return false;
            }

            // Target check.
            if (ability.RequiresTarget && !GetIsObjectValid(target))
            {
                SendMessageToPC(activator, "A target is required.");
                return false;
            }

            // Range check. Targetless activations, such as self buffs, queued attacks,
            // and ground-targeted areas, should not validate against a synthetic target.
            if (ability.RequiresTarget &&
                GetIsObjectValid(target) &&
                GetDistanceBetween(activator, target) > ability.MaxRange)
            {
                SendMessageToPC(activator, "You are out of range.  This ability has a range of " + ability.MaxRange + " meters.");
                return false;
            }

            // Hostility check
            if (ability.RequiresTarget &&
                GetIsObjectValid(target) &&
                !GetIsReactionTypeHostile(target, activator) &&
                ability.IsHostileAbility)
            {
                SendMessageToPC(activator, "You may only use this ability on enemies.");
                return false;
            }

            // Perk-specific requirement checks
            foreach (var req in ability.Requirements)
            {
                var requirementError = req.CheckRequirements(activator, ability);
                if (!string.IsNullOrWhiteSpace(requirementError))
                {
                    SendMessageToPC(activator, requirementError);
                    return false;
                }
            }

            // Perk-specific custom validation logic.
            var customValidationResult = ability.CustomValidation == null ? string.Empty : ability.CustomValidation(activator, target, effectivePerkLevel, targetLocation);
            if (!string.IsNullOrWhiteSpace(customValidationResult))
            {
                SendMessageToPC(activator, customValidationResult);
                return false;
            }

            // Check if ability is on a recast timer still.
            var (isOnRecast, timeToWait) = Recast.IsOnRecastDelay(activator, ability.RecastGroup);
            if (isOnRecast)
            {
                SendMessageToPC(activator, $"This ability can be used in {timeToWait}.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Whenever a weapon's OnHit event is fired, add a Leadership combat point if an Aura is active.
        /// </summary>
        [NWNEventHandler(ScriptName.OnItemHit)]
        public static void AddLeadershipCombatPoint()
        {
            var player = OBJECT_SELF;
            var target = GetSpellTargetObject();
            if (!GetIsPC(player) || GetIsDM(player) || !GetIsObjectValid(player))
                return;

            if (GetIsPC(target) || GetIsDM(target))
                return;

            if (!_playerAuras.ContainsKey(player))
                return;

            var aura = _playerAuras[player];

            if (aura.Auras.Count <= 0)
                return;

            CombatPoint.AddCombatPoint(player, target, SkillType.Leadership);
        }

        private static int GetMaxNumberOfAuras(uint activator)
        {
            var social = GetAbilityScore(activator, AbilityType.Social);
            var count = 1 + (social - 10) / 5;

            if (count > MaxNumberOfAuras)
                count = MaxNumberOfAuras;

            return count;
        }

        private static void ApplyAuraEffect(uint source, uint recipient, Type type)
        {
            if (StatusEffect.HasStatusEffect(recipient, type, source) ||
                HasEqualOrStrongerAuraEffect(source, recipient, type))
            {
                return;
            }

            RemoveWeakerDuplicateAuraEffects(source, recipient, type);
            StatusEffect.ApplyStatusEffect(source, recipient, type, 0f);
        }

        private static void RemoveAuraEffect(uint source, uint recipient, Type type, bool sendsWornOffMessage = false)
        {
            StatusEffect.RemoveStatusEffect(recipient, type, source, sendsWornOffMessage);
        }

        private static bool HasEqualOrStrongerAuraEffect(uint source, uint recipient, Type type)
        {
            var sourceSocial = GetAuraSourceSocial(source);
            return StatusEffect.GetCreatureStatusEffects(recipient)
                .GetAllEffects()
                .Any(effect =>
                    effect.GetType() == type &&
                    effect.Source != source &&
                    GetAuraSourceSocial(effect.Source) >= sourceSocial);
        }

        private static void RemoveWeakerDuplicateAuraEffects(uint source, uint recipient, Type type)
        {
            var sourceSocial = GetAuraSourceSocial(source);
            var weakerEffects = StatusEffect.GetCreatureStatusEffects(recipient)
                .GetAllEffects()
                .Where(effect =>
                    effect.GetType() == type &&
                    effect.Source != source &&
                    GetAuraSourceSocial(effect.Source) < sourceSocial)
                .ToList();

            foreach (var weakerEffect in weakerEffects)
            {
                StatusEffect.RemoveStatusEffect(recipient, type, weakerEffect.Source, false);
            }
        }

        private static int GetAuraSourceSocial(uint source)
        {
            return GetIsObjectValid(source)
                ? GetAbilityScore(source, AbilityType.Social)
                : 0;
        }

        public static void ApplyAura(uint activator, Type type, bool targetsSelf, bool targetsParty, bool targetsEnemies)
        {
            if (!_playerAuras.ContainsKey(activator))
                _playerAuras.Add(activator, new PlayerAura());

            var aura = _playerAuras[activator];

            // Safety check - ensure the same aura never enters the cache more than once.
            if (aura.Auras.Exists(x => x.StatusEffect == type))
                return;

            var maxAuras = GetMaxNumberOfAuras(activator);
            var effectName = StatusEffect.GetStatusEffectName(type);

            while (aura.Auras.Count >= maxAuras)
            {
                var removeType = aura.Auras[0].StatusEffect;
                if (aura.Auras[0].TargetsSelf)
                {
                    RemoveAuraEffect(activator, activator, removeType);
                }

                if (aura.Auras[0].TargetsParty)
                {
                    foreach (var member in aura.PartyMembersInRange)
                    {
                        RemoveAuraEffect(activator, member, removeType);
                    }
                }

                if (aura.Auras[0].TargetsEnemies)
                {
                    foreach (var npc in aura.CreaturesInRange)
                    {
                        RemoveAuraEffect(activator, npc, removeType);
                    }
                }

                aura.Auras.RemoveAt(0);
            }

            aura.Auras.Add(new PlayerAuraDetail(type, targetsSelf, targetsParty, targetsEnemies));

            if (targetsSelf)
            {
                ApplyAuraEffect(activator, activator, type);
            }

            if (targetsParty)
            {
                foreach (var member in aura.PartyMembersInRange)
                {
                    if (Party.IsInParty(activator, member))
                        ApplyAuraEffect(activator, member, type);
                }
            }

            if (targetsEnemies)
            {
                foreach (var npc in aura.CreaturesInRange)
                {
                    if (!GetIsDMPossessed(npc) && !GetIsDM(npc) &&
                        (GetIsEnemy(activator, npc) || GetIsEnemy(npc, activator)))
                        ApplyAuraEffect(activator, npc, type);
                }
            }

            SendMessageToPC(activator, ColorToken.Green($"Aura '{effectName}' activated."));
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Sound_Burst), activator);
        }

        public static bool RemoveAura(uint activator, Type type, bool sendsWornOffMessage = false)
        {
            if (!_playerAuras.ContainsKey(activator))
                return false;

            var aura = _playerAuras[activator];
            var existing = aura.Auras.FirstOrDefault(x => x.StatusEffect == type);
            if (existing == null)
                return false;

            if (existing.TargetsSelf)
            {
                RemoveAuraEffect(activator, activator, type, sendsWornOffMessage);
            }

            if (existing.TargetsParty)
            {
                foreach (var member in aura.PartyMembersInRange)
                {
                    RemoveAuraEffect(activator, member, type, sendsWornOffMessage);
                }
            }

            if (existing.TargetsEnemies)
            {
                foreach (var npc in aura.CreaturesInRange)
                {
                    RemoveAuraEffect(activator, npc, type, sendsWornOffMessage);
                }
            }

            aura.Auras.Remove(existing);
            return true;
        }

        public static bool ToggleAura(uint activator, Type type)
        {
            if (!_playerAuras.ContainsKey(activator))
                return true;

            // Aura is active and player wants to deactivate it.
            // Remove it from the list and send a notification message.
            var effectName = StatusEffect.GetStatusEffectName(type);
            if (RemoveAura(activator, type))
            {
                SendMessageToPC(activator, ColorToken.Red($"Aura '{effectName}' deactivated."));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Removes all auras which are currently active on a creature.
        /// </summary>
        /// <param name="activator">The creature who originally activated the auras.</param>
        private static void RemoveAllAuras(uint activator)
        {
            if (!_playerAuras.ContainsKey(activator))
                return;

            var auraDetails = _playerAuras[activator];

            foreach (var aura in auraDetails.Auras)
            {
                if (aura.TargetsSelf)
                {
                    RemoveAuraEffect(activator, activator, aura.StatusEffect, true);
                }

                if (aura.TargetsParty)
                {
                    foreach (var member in auraDetails.PartyMembersInRange)
                    {
                        RemoveAuraEffect(activator, member, aura.StatusEffect);
                    }
                }

                if (aura.TargetsEnemies)
                {
                    foreach (var npc in auraDetails.CreaturesInRange)
                    {
                        RemoveAuraEffect(activator, npc, aura.StatusEffect);
                    }
                }
            }

            _playerAuras.Remove(activator);
        }

        /// <summary>
        /// Removes a creature from all active aura range lists and strips any aura effects they received
        /// as a recipient. Used when a creature leaves the game world in a way that bypasses the normal
        /// AOE exit event (e.g., entering space, being teleported).
        /// </summary>
        /// <param name="target">The creature to remove from all aura ranges.</param>
        public static void RemoveCreatureFromAllAuraRanges(uint target)
        {
            foreach (var (leader, playerAura) in _playerAuras)
            {
                if (playerAura.PartyMembersInRange.Remove(target))
                {
                    foreach (var aura in playerAura.Auras)
                    {
                        if (aura.TargetsParty)
                            RemoveAuraEffect(leader, target, aura.StatusEffect);
                    }
                }

                if (playerAura.CreaturesInRange.Remove(target))
                {
                    foreach (var aura in playerAura.Auras)
                    {
                        if (aura.TargetsEnemies)
                            RemoveAuraEffect(leader, target, aura.StatusEffect);
                    }
                }
            }
        }


        /// <summary>
        /// Refreshes party-aura eligibility for a creature against all active aura leaders.
        /// If the creature is still tracked in a party aura range but is no longer in the leader's party,
        /// remove the cached range entry and strip relevant aura effects.
        /// </summary>
        /// <param name="target">The creature whose party aura eligibility should be refreshed.</param>
        public static void RefreshPartyAuraEligibility(uint target)
        {
            foreach (var (leader, playerAura) in _playerAuras)
            {
                if (!playerAura.PartyMembersInRange.Contains(target))
                    continue;

                if (Party.IsInParty(leader, target))
                    continue;

                playerAura.PartyMembersInRange.Remove(target);

                foreach (var aura in playerAura.Auras)
                {
                    if (aura.TargetsParty)
                        RemoveAuraEffect(leader, target, aura.StatusEffect);
                }
            }
        }

        /// <summary>
        /// Re-applies any aura effects that a creature should be receiving based on their current position
        /// in active aura range lists. Used after in-place resurrection (subdual, revive) where the
        /// AOE enter event may not re-fire for a creature that never physically left the AOE.
        /// </summary>
        /// <param name="target">The creature to re-enroll in active auras.</param>
        public static void ReapplyAuraEffectsForCreature(uint target)
        {
            if (!GetIsObjectValid(target) || GetIsDead(target))
                return;

            foreach (var (leader, playerAura) in _playerAuras)
            {
                if (playerAura.PartyMembersInRange.Contains(target) && Party.IsInParty(leader, target))
                {
                    foreach (var aura in playerAura.Auras)
                    {
                        if (aura.TargetsParty)
                            ApplyAuraEffect(leader, target, aura.StatusEffect);
                    }
                }

                if (playerAura.CreaturesInRange.Contains(target) &&
                    !GetIsDMPossessed(target) && !GetIsDM(target) &&
                    (GetIsEnemy(leader, target) || GetIsEnemy(target, leader)))
                {
                    foreach (var aura in playerAura.Auras)
                    {
                        if (aura.TargetsEnemies)
                            ApplyAuraEffect(leader, target, aura.StatusEffect);
                    }
                }
            }
        }

        private static AreaOfEffect GetAuraAOE(int commandRadiusBonusMeters)
        {
            switch (commandRadiusBonusMeters)
            {
                case >= 4:
                    return AreaOfEffect.AuraUpgrade2;
                case >= 2:
                    return AreaOfEffect.AuraUpgrade1;
                default:
                    return AreaOfEffect.AuraDefault;
            }
        }

        public static void ReapplyPlayerAuraAOE(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            RemoveEffectByTag(player, "AURA_EFFECT");

            AssignCommand(player, () =>
            {
                var commandRadiusBonusMeters = Stat.GetStatAdjustment(player, StatType.LeadershipCommandRadiusBonusMeters);
                var auraAOE = GetAuraAOE(commandRadiusBonusMeters);
                var effect = SupernaturalEffect(EffectAreaOfEffect(auraAOE, "aura_enter", string.Empty, "aura_exit"));
                effect = TagEffect(effect, "AURA_EFFECT");
                ApplyEffectToObject(DurationType.Permanent, effect, player);
            });
        }

        /// <summary>
        /// When a player enters the server, apply the Aura AOE effect.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ApplyAuraAOE()
        {
            var player = GetEnteringObject();
            ReapplyPlayerAuraAOE(player);
        }

        /// <summary>
        /// When a player exits the server, remove all of their Aura effects.
        /// Also removes the player from any aura ranges they are receiving as a recipient.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ClearAurasOnExit()
        {
            var player = GetExitingObject();
            RemoveAllAuras(player);
            RemoveCreatureFromAllAuraRanges(player);
        }

        /// <summary>
        /// When a player dies, remove all of their Aura effects.
        /// Also removes the player from any aura ranges they are receiving as a recipient.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void ClearAurasOnDeath()
        {
            var player = GetLastPlayerDied();
            RemoveAllAuras(player);
            RemoveCreatureFromAllAuraRanges(player);
        }

        /// <summary>
        /// When a player respawns, reapply the aura AOE effect
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleRespawn)]
        public static void ReapplyAuraOnRespawn()
        {
            var player = GetLastRespawnButtonPresser();
            ReapplyPlayerAuraAOE(player);
        }

        /// <summary>
        /// When a player enters space mode, remove all of their Aura effects.
        /// Also removes the player from any aura ranges they are receiving as a recipient.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSpaceEnter)]
        public static void ClearAurasOnSpaceEntry()
        {
            var player = OBJECT_SELF;
            RemoveAllAuras(player);
            RemoveCreatureFromAllAuraRanges(player);
        }

        /// <summary>
        /// Whenever a creature enters the aura, add them to the cache.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAuraEnter)]
        public static void AuraEnter()
        {
            var entering = GetEnteringObject();
            var self = GetAreaOfEffectCreator(OBJECT_SELF);

            if (!_playerAuras.ContainsKey(self))
                _playerAuras.Add(self, new PlayerAura());

            // Party Members
            if (Party.IsInParty(self, entering))
            {
                if (!_playerAuras[self].PartyMembersInRange.Contains(entering))
                    _playerAuras[self].PartyMembersInRange.Add(entering);

                foreach (var detail in _playerAuras[self].Auras)
                {
                    if (detail.TargetsParty)
                    {
                        ApplyAuraEffect(self, entering, detail.StatusEffect);
                    }
                }
            }

            // Enemies
            else if (!GetIsDMPossessed(entering) && !GetIsDM(entering) && (GetIsEnemy(self, entering) || GetIsEnemy(entering, self)))
            {
                if (!_playerAuras[self].CreaturesInRange.Contains(entering))
                    _playerAuras[self].CreaturesInRange.Add(entering);

                foreach (var detail in _playerAuras[self].Auras)
                {
                    if (detail.TargetsEnemies)
                    {
                        ApplyAuraEffect(self, entering, detail.StatusEffect);
                    }
                }
            }
        }

        /// <summary>
        /// Whenever a creature exits the aura, remove it from the cache.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAuraExit)]
        public static void AuraExit()
        {
            var exiting = GetExitingObject();
            var self = GetAreaOfEffectCreator(OBJECT_SELF);

            if (!_playerAuras.ContainsKey(self))
                _playerAuras.Add(self, new PlayerAura());

            if (Party.IsInParty(self, exiting))
            {
                if (!_playerAuras[self].PartyMembersInRange.Contains(exiting))
                    return;

                _playerAuras[self].PartyMembersInRange.Remove(exiting);

                foreach (var detail in _playerAuras[self].Auras)
                {
                    if (detail.TargetsParty)
                    {
                        RemoveAuraEffect(self, exiting, detail.StatusEffect);
                    }
                }
            }

            else if (!GetIsDMPossessed(exiting) && !GetIsDM(exiting) && (GetIsEnemy(self, exiting) || GetIsEnemy(exiting, self)))
            {
                if (!_playerAuras[self].CreaturesInRange.Contains(exiting))
                    return;

                _playerAuras[self].CreaturesInRange.Remove(exiting);

                foreach (var detail in _playerAuras[self].Auras)
                {
                    if (detail.TargetsEnemies)
                    {
                        RemoveAuraEffect(self, exiting, detail.StatusEffect);
                    }
                }
            }
        }

        /// <summary>
        /// Applies the standard hostile combat impact used by weapon and martial abilities.
        /// </summary>
        public static int ApplyCombatImpact(
            uint activator,
            uint target,
            Location targetLocation,
            SkillType skillType,
            int baseDamage,
            int duration,
            Type statusEffect,
            bool isArea,
            IEnumerable<Type> additionalStatusEffects = null,
            Func<IStatusEffect> statusEffectFactory = null,
            CombatDamageType damageType = CombatDamageType.Physical,
            ResistanceType statusResistanceType = ResistanceType.Invalid,
            VisualEffect targetVisualEffect = VisualEffect.None,
            VisualEffect areaVisualEffect = VisualEffect.None,
            Func<uint, int> damagePercentAdjustment = null,
            Func<uint, int> baseDamageAdjustment = null,
            IEnumerable<Func<IStatusEffect>> additionalStatusEffectFactories = null,
            Animation impactAnimation = Animation.Invalid,
            int enmityBonus = 0)
        {
            var totalDamage = 0;
            RecordAbilityImpactShape(activator, skillType, isArea);

            if (isArea)
            {
                var center = GetIsObjectValid(target) ? GetLocation(target) : targetLocation;
                var creature = GetFirstObjectInShape(Shape.Sphere, 5.0f, center, true);
                var creatures = new List<uint>();
                while (GetIsObjectValid(creature))
                {
                    creatures.Add(creature);

                    creature = GetNextObjectInShape(Shape.Sphere, 5.0f, center, true);
                }

                if (creatures.Any(creature => GetIsObjectValid(creature) && GetIsReactionTypeHostile(creature, activator)))
                {
                    if (areaVisualEffect != VisualEffect.None)
                    {
                        ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(areaVisualEffect), center);
                    }
                }

                totalDamage = ApplyCombatImpactToCreatures(
                    activator,
                    creatures,
                    skillType,
                    baseDamage,
                    statusEffect,
                    duration,
                    additionalStatusEffects,
                    statusEffectFactory,
                    damageType,
                    statusResistanceType,
                    targetVisualEffect,
                    damagePercentAdjustment,
                    baseDamageAdjustment,
                    additionalStatusEffectFactories: additionalStatusEffectFactories,
                    enmityBonus: enmityBonus);
            }
            else if (GetIsObjectValid(target))
            {
                totalDamage = ApplyCombatImpactToCreatures(
                    activator,
                    new[] { target },
                    skillType,
                    baseDamage,
                    statusEffect,
                    duration,
                    additionalStatusEffects,
                    statusEffectFactory,
                    damageType,
                    statusResistanceType,
                    targetVisualEffect,
                    damagePercentAdjustment,
                    baseDamageAdjustment,
                    additionalStatusEffectFactories: additionalStatusEffectFactories,
                    enmityBonus: enmityBonus);
            }

            PlayCombatImpactAnimation(activator, impactAnimation);
            return totalDamage;
        }

        /// <summary>
        /// Applies a hostile combat impact after a visible telegraph resolves.
        /// </summary>
        public static void ApplyTelegraphedCombatImpact(
            uint activator,
            uint target,
            Location targetLocation,
            SkillType skillType,
            int baseDamage,
            int duration,
            Type statusEffect,
            CombatImpactAreaShape shape,
            float telegraphDuration,
            float lengthOrRadius,
            float width = 0f,
            IEnumerable<Type> additionalStatusEffects = null,
            bool centerOnActivator = false,
            Func<IStatusEffect> statusEffectFactory = null,
            CombatDamageType damageType = CombatDamageType.Physical,
            ResistanceType statusResistanceType = ResistanceType.Invalid,
            VisualEffect targetVisualEffect = VisualEffect.None,
            VisualEffect areaVisualEffect = VisualEffect.None,
            Func<uint, int> damagePercentAdjustment = null,
            Func<uint, int> baseDamageAdjustment = null,
            Action<AbilityImpactSummary> afterImpactAction = null,
            int maxTargets = 0,
            IEnumerable<Func<IStatusEffect>> additionalStatusEffectFactories = null,
            Animation impactAnimation = Animation.Invalid,
            int enmityBonus = 0)
        {
            RecordAbilityImpactShape(activator, skillType, true);

            if (telegraphDuration <= 0f)
            {
                ApplyCombatImpactInShape(
                    activator,
                    target,
                    targetLocation,
                    skillType,
                    baseDamage,
                    duration,
                    statusEffect,
                    shape,
                    lengthOrRadius,
                    width,
                    additionalStatusEffects,
                    centerOnActivator,
                    statusEffectFactory,
                    damageType,
                    statusResistanceType,
                    targetVisualEffect,
                    areaVisualEffect,
                    damagePercentAdjustment,
                    baseDamageAdjustment,
                    maxTargets,
                    additionalStatusEffectFactories,
                    enmityBonus);
                PlayCombatImpactAnimation(activator, impactAnimation);
                return;
            }

            var areaVisualLocation = Location(
                GetArea(activator),
                shape == CombatImpactAreaShape.Sphere
                    ? GetAreaImpactPosition(activator, target, targetLocation, centerOnActivator)
                    : GetPosition(activator),
                0f);
            var impactRotation = GetImpactRotationRadians(activator, target, targetLocation);
            var trackedImpact = GetTrackedAbilityImpact(activator);
            var action = BuildTelegraphedCombatImpactAction(
                skillType,
                baseDamage,
                duration,
                statusEffect,
                additionalStatusEffects,
                statusEffectFactory,
                shape,
                areaVisualLocation,
                trackedImpact?.Ability,
                trackedImpact?.NextAbilityDamageBonus ?? 0,
                trackedImpact?.NextAbilityCriticalRatePercentAdjustment ?? 0,
                damageType,
                statusResistanceType,
                targetVisualEffect,
                areaVisualEffect,
                damagePercentAdjustment,
                baseDamageAdjustment,
                afterImpactAction,
                maxTargets,
                additionalStatusEffectFactories,
                enmityBonus);

            switch (shape)
            {
                case CombatImpactAreaShape.Sphere:
                    Telegraph.CreateSphereTelegraph(
                        activator,
                        GetPositionFromLocation(areaVisualLocation),
                        lengthOrRadius,
                        telegraphDuration,
                        true,
                        action);
                    break;
                case CombatImpactAreaShape.Cone:
                    Telegraph.CreateConeTelegraph(
                        activator,
                        GetPosition(activator),
                        impactRotation,
                        lengthOrRadius,
                        width > 0f ? width : lengthOrRadius,
                        telegraphDuration,
                        true,
                        action);
                    break;
                case CombatImpactAreaShape.Line:
                    Telegraph.CreateLineTelegraph(
                        activator,
                        GetPosition(activator),
                        impactRotation,
                        lengthOrRadius,
                        width > 0f ? width : 2.0f,
                        telegraphDuration,
                        true,
                        action);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }

            PlayCombatImpactAnimation(activator, impactAnimation);
        }

        private static void ApplyCombatImpactInShape(
            uint activator,
            uint target,
            Location targetLocation,
            SkillType skillType,
            int baseDamage,
            int duration,
            Type statusEffect,
            CombatImpactAreaShape shape,
            float lengthOrRadius,
            float width,
            IEnumerable<Type> additionalStatusEffects,
            bool centerOnActivator,
            Func<IStatusEffect> statusEffectFactory,
            CombatDamageType damageType,
            ResistanceType statusResistanceType,
            VisualEffect targetVisualEffect,
            VisualEffect areaVisualEffect,
            Func<uint, int> damagePercentAdjustment,
            Func<uint, int> baseDamageAdjustment,
            int maxTargets,
            IEnumerable<Func<IStatusEffect>> additionalStatusEffectFactories,
            int enmityBonus)
        {
            RecordAbilityImpactShape(activator, skillType, true);

            var creatures = new List<uint>();
            var origin = shape == CombatImpactAreaShape.Sphere
                ? Location(GetArea(activator), GetAreaImpactPosition(activator, target, targetLocation, centerOnActivator), 0f)
                : GetLocation(activator);
            var maxDistance = shape == CombatImpactAreaShape.Sphere
                ? lengthOrRadius
                : Math.Max(lengthOrRadius, width);

            var nth = 1;
            var creature = GetNearestCreatureToLocation(CreatureType.IsAlive, true, origin, nth);
            var rotation = GetImpactRotationRadians(activator, target, targetLocation);
            var originPosition = GetPositionFromLocation(origin);

            while (GetIsObjectValid(creature) &&
                   GetDistanceBetweenLocations(origin, GetLocation(creature)) <= maxDistance)
            {
                var creaturePosition = GetPosition(creature);
                if (IsPositionInCombatImpactShape(creaturePosition, originPosition, rotation, shape, lengthOrRadius, width))
                {
                    creatures.Add(creature);
                }

                nth++;
                creature = GetNearestCreatureToLocation(CreatureType.IsAlive, true, origin, nth);
            }

            if (creatures.Any(creature => GetIsObjectValid(creature) && GetIsReactionTypeHostile(creature, activator)))
            {
                if (areaVisualEffect != VisualEffect.None)
                {
                    ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(areaVisualEffect), origin);
                }
            }

            ApplyCombatImpactToCreatures(
                activator,
                creatures,
                skillType,
                baseDamage,
                statusEffect,
                duration,
                additionalStatusEffects,
                statusEffectFactory,
                damageType,
                statusResistanceType,
                targetVisualEffect,
                damagePercentAdjustment,
                baseDamageAdjustment,
                maxTargets,
                additionalStatusEffectFactories,
                enmityBonus);
        }

        private static ApplyTelegraphEffect BuildTelegraphedCombatImpactAction(
            SkillType skillType,
            int baseDamage,
            int duration,
            Type statusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            CombatImpactAreaShape shape,
            Location areaVisualLocation,
            AbilityDetail ability,
            int nextAbilityDamageBonus,
            int nextAbilityCriticalRatePercentAdjustment,
            CombatDamageType damageType,
            ResistanceType statusResistanceType,
            VisualEffect targetVisualEffect,
            VisualEffect areaVisualEffect,
            Func<uint, int> damagePercentAdjustment,
            Func<uint, int> baseDamageAdjustment,
            Action<AbilityImpactSummary> afterImpactAction,
            int maxTargets,
            IEnumerable<Func<IStatusEffect>> additionalStatusEffectFactories,
            int enmityBonus)
        {
            return (creator, creatures) =>
            {
                if (!GetIsObjectValid(creator) || GetCurrentHitPoints(creator) <= 0)
                    return;

                var hostileCreatures = creatures
                    .Where(creature => GetIsObjectValid(creature) && GetIsReactionTypeHostile(creature, creator))
                    .ToList();

                if (maxTargets > 0)
                {
                    hostileCreatures = hostileCreatures
                        .OrderBy(creature => GetDistanceBetween(creator, creature))
                        .Take(maxTargets)
                        .ToList();
                }

                if (hostileCreatures.Count <= 0)
                {
                    SendCombatImpactNoTargetsMessage(creator, ability);
                    return;
                }

                if (ability != null)
                {
                    BeginAbilityImpact(creator, ability, nextAbilityDamageBonus, nextAbilityCriticalRatePercentAdjustment);
                    RecordAbilityImpactShape(creator, skillType, true);
                }

                if (areaVisualEffect != VisualEffect.None)
                {
                    ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(areaVisualEffect), areaVisualLocation);
                }

                ApplyCombatImpactToCreatures(
                    creator,
                    hostileCreatures,
                    skillType,
                    baseDamage,
                    statusEffect,
                    duration,
                    additionalStatusEffects,
                    statusEffectFactory,
                    damageType,
                    statusResistanceType,
                    targetVisualEffect,
                    damagePercentAdjustment,
                    baseDamageAdjustment,
                    maxTargets,
                    additionalStatusEffectFactories,
                    enmityBonus);

                if (ability != null)
                {
                    var summary = EndAbilityImpact(creator);
                    Combat.ApplyAbilityImpactEffects(creator, summary);
                    afterImpactAction?.Invoke(summary);
                }
            };
        }

        private static int ApplyCombatImpactToCreatures(
            uint activator,
            IEnumerable<uint> creatures,
            SkillType skillType,
            int baseDamage,
            Type statusEffect,
            int duration,
            IEnumerable<Type> additionalStatusEffects = null,
            Func<IStatusEffect> statusEffectFactory = null,
            CombatDamageType damageType = CombatDamageType.Physical,
            ResistanceType statusResistanceType = ResistanceType.Invalid,
            VisualEffect targetVisualEffect = VisualEffect.None,
            Func<uint, int> damagePercentAdjustment = null,
            Func<uint, int> baseDamageAdjustment = null,
            int maxTargets = 0,
            IEnumerable<Func<IStatusEffect>> additionalStatusEffectFactories = null,
            int enmityBonus = 0)
        {
            var totalDamage = 0;
            var affectedCount = 0;
            var trackedAbility = GetTrackedAbilityImpact(activator)?.Ability;

            foreach (var creature in creatures.Distinct())
            {
                if (!GetIsObjectValid(creature) || !GetIsReactionTypeHostile(creature, activator))
                    continue;

                if (maxTargets > 0 && affectedCount >= maxTargets)
                    break;

                totalDamage += ApplyHostileCombatImpact(
                    activator,
                    creature,
                    skillType,
                    baseDamage,
                    statusEffect,
                    duration,
                    additionalStatusEffects,
                    statusEffectFactory,
                    damageType,
                    statusResistanceType,
                    targetVisualEffect,
                    damagePercentAdjustment,
                    baseDamageAdjustment,
                    additionalStatusEffectFactories,
                    enmityBonus);
                affectedCount++;
            }

            if (affectedCount <= 0)
            {
                SendCombatImpactNoTargetsMessage(activator, trackedAbility);
            }

            return totalDamage;
        }

        private static NumericsVector3 GetAreaImpactPosition(uint activator, uint target, Location targetLocation, bool centerOnActivator = false)
        {
            if (centerOnActivator)
                return GetPosition(activator);

            if (GetIsObjectValid(target))
                return GetPosition(target);

            var targetArea = GetAreaFromLocation(targetLocation);
            return GetIsObjectValid(targetArea)
                ? GetPositionFromLocation(targetLocation)
                : GetPosition(activator);
        }

        private static float GetImpactRotationRadians(uint activator, uint target, Location targetLocation)
        {
            var origin = GetPosition(activator);
            var destination = GetIsObjectValid(target)
                ? GetPosition(target)
                : GetIsObjectValid(GetAreaFromLocation(targetLocation))
                    ? GetPositionFromLocation(targetLocation)
                    : origin;
            var delta = destination - origin;

            if (Math.Abs(delta.X) <= 0.01f && Math.Abs(delta.Y) <= 0.01f)
                return DegreesToRadians(GetFacing(activator));

            return (float)Math.Atan2(delta.Y, delta.X);
        }

        private static float DegreesToRadians(float degrees)
        {
            return degrees * ((float)Math.PI / 180f);
        }

        private static bool IsPositionInCombatImpactShape(
            NumericsVector3 position,
            NumericsVector3 origin,
            float rotation,
            CombatImpactAreaShape shape,
            float lengthOrRadius,
            float width)
        {
            switch (shape)
            {
                case CombatImpactAreaShape.Sphere:
                    return NumericsVector3.Distance(position, origin) <= lengthOrRadius;
                case CombatImpactAreaShape.Cone:
                    return IsPositionInCone(position, origin, rotation, lengthOrRadius, width > 0f ? width : lengthOrRadius);
                case CombatImpactAreaShape.Line:
                    return IsPositionInLine(position, origin, rotation, lengthOrRadius, width > 0f ? width : 2.0f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }
        }

        private static bool IsPositionInCone(NumericsVector3 position, NumericsVector3 origin, float rotation, float length, float width)
        {
            var toPoint = position - origin;
            var distance = toPoint.Length();
            if (distance <= 0.01f)
                return true;

            var direction = new NumericsVector3((float)Math.Cos(rotation), (float)Math.Sin(rotation), 0f);
            var cosAngle = Math.Clamp(NumericsVector3.Dot(toPoint, direction) / distance, -1f, 1f);
            var angleBetween = (float)Math.Acos(cosAngle);
            var halfAngle = (float)Math.Atan(width * 0.5f / length);

            return distance <= length && angleBetween <= halfAngle;
        }

        private static bool IsPositionInLine(NumericsVector3 position, NumericsVector3 origin, float rotation, float length, float width)
        {
            var toPoint = position - origin;
            var rotatedX = toPoint.X * (float)Math.Cos(-rotation) - toPoint.Y * (float)Math.Sin(-rotation);
            var rotatedY = toPoint.X * (float)Math.Sin(-rotation) + toPoint.Y * (float)Math.Cos(-rotation);

            return rotatedX >= 0f &&
                   rotatedX <= length &&
                   Math.Abs(rotatedY) <= width * 0.5f;
        }

        private static void PlayCombatImpactAnimation(uint activator, Animation impactAnimation)
        {
            var trackedAbility = GetTrackedAbilityImpact(activator)?.Ability;
            var animation = impactAnimation == Animation.Invalid
                ? trackedAbility?.ImpactAnimationType ?? Animation.Invalid
                : impactAnimation;

            if (animation == Animation.Invalid)
                animation = Animation.DoubleStrike;

            AssignCommand(activator, () => ActionPlayAnimation(animation));
        }

        private static int ApplyHostileCombatImpact(
            uint activator,
            uint target,
            SkillType skillType,
            int baseDamage,
            Type statusEffect,
            int duration,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            CombatDamageType damageType,
            ResistanceType statusResistanceType,
            VisualEffect targetVisualEffect,
            Func<uint, int> damagePercentAdjustment = null,
            Func<uint, int> baseDamageAdjustment = null,
            IEnumerable<Func<IStatusEffect>> additionalStatusEffectFactories = null,
            int enmityBonus = 0)
        {
            var trackedImpact = GetTrackedAbilityImpact(activator);
            var perkType = trackedImpact?.Ability?.EffectiveLevelPerkType ?? PerkType.Invalid;
            if (!Combat.TryResolveAbilityHit(activator, target, skillType, perkType, out var hitRate))
            {
                SendCombatImpactResultMessage(activator, target, trackedImpact?.Ability, 4, hitRate);
                CombatPoint.AddCombatPoint(activator, target, skillType, 1);
                ApplyMissedHostileAbilityEnmity(activator, target);
                return 0;
            }
            SendCombatImpactResultMessage(activator, target, trackedImpact?.Ability, 1, hitRate);

            var adjustedBaseDamage = Math.Max(0, baseDamage + (baseDamageAdjustment?.Invoke(target) ?? 0));
            var damage = CalculateCombatImpactDamage(activator, target, skillType, adjustedBaseDamage, damageType);
            damage = ApplyDamagePercentAdjustment(target, damage, damagePercentAdjustment);
            if (damage > 0)
            {
                AssignCommand(activator, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, damageType.GetNWScriptDamageType()), target));
                ApplyDarkForceConversion(activator, target, damage);
                Combat.ApplyDamageDealtEffects(activator, target, damage, skillType, damageType);
                StatusEffect.NotifyDamageStatusEffects(activator, target, damage, damageType);
            }

            ApplyHostileAbilityEnmity(activator, target, damage + Math.Max(0, enmityBonus));

            var statusApplied = ApplyCombatImpactStatusEffect(
                activator,
                target,
                statusEffect,
                duration,
                additionalStatusEffects,
                statusEffectFactory,
                additionalStatusEffectFactories,
                statusResistanceType,
                damageType);
            if ((damage > 0 || statusApplied) && targetVisualEffect != VisualEffect.None)
            {
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(targetVisualEffect), target);
            }

            CombatPoint.AddCombatPoint(activator, target, skillType, 3);
            RecordAbilityImpactTarget(activator, target, skillType, false);
            return damage;
        }

        private static void SendCombatImpactResultMessage(
            uint activator,
            uint target,
            AbilityDetail ability,
            int attackResultType,
            int hitRate)
        {
            if (!GetIsObjectValid(activator) || !GetIsObjectValid(target) || ability == null)
                return;

            var combatLogMessage = Combat.BuildAbilityCombatLogMessage(
                activator,
                target,
                ability.Name,
                attackResultType,
                hitRate);
            Messaging.SendMessageNearbyToPlayers(target, combatLogMessage, 60f);
        }

        private static void SendCombatImpactNoTargetsMessage(
            uint activator,
            AbilityDetail ability)
        {
            if (!GetIsObjectValid(activator) || ability == null)
                return;

            var combatLogMessage = Combat.BuildAbilityNoTargetCombatLogMessage(
                activator,
                ability.Name);
            Messaging.SendMessageNearbyToPlayers(activator, combatLogMessage, 60f);
        }

        private static int ApplyDamagePercentAdjustment(
            uint target,
            int damage,
            Func<uint, int> damagePercentAdjustment)
        {
            if (damage <= 0 || damagePercentAdjustment == null)
                return damage;

            var adjustment = damagePercentAdjustment(target);
            if (adjustment == 0)
                return damage;

            return Math.Max(0, damage + (int)Math.Ceiling(damage * (adjustment / 100f)));
        }

        private static void ApplyDarkForceConversion(uint activator, uint target, int damage)
        {
            if (damage <= 0 || !GetIsObjectValid(activator))
                return;

            var trackedImpact = GetTrackedAbilityImpact(activator);
            if (trackedImpact == null ||
                trackedImpact.Ability?.TriggersDarkForceConversion != true)
            {
                return;
            }

            var hpRestorePercent = Stat.GetStatAdjustment(activator, StatType.DarkForceDamageHPPercentRestore);
            if (hpRestorePercent > 0)
            {
                RestoreHPFromDamage(activator, damage, hpRestorePercent);
            }

            var fpRestore = Stat.GetStatAdjustment(activator, StatType.DarkForceDamageFPRestore);
            var hpCostPercent = Stat.GetStatAdjustment(activator, StatType.DarkForceDamageHPCostPercent);
            if (trackedImpact.DarkForceConversionApplied)
                return;

            if (fpRestore <= 0 && hpCostPercent <= 0)
                return;

            trackedImpact.DarkForceConversionApplied = true;

            if (fpRestore > 0)
                Stat.RestoreFP(activator, fpRestore);

            var lowTargetThresholdPercent = Stat.GetStatAdjustment(activator, StatType.DarkForceDamageLowTargetHPThresholdPercent);
            var lowTargetHPCostPercent = Stat.GetStatAdjustment(activator, StatType.DarkForceDamageLowTargetHPCostPercent);
            if (lowTargetThresholdPercent > 0 &&
                lowTargetHPCostPercent > 0 &&
                GetIsObjectValid(target) &&
                GetMaxHitPoints(target) > 0 &&
                GetCurrentHitPoints(target) <= GetMaxHitPoints(target) * lowTargetThresholdPercent / 100)
            {
                hpCostPercent = lowTargetHPCostPercent;
            }

            if (hpCostPercent <= 0)
                return;

            var hpCost = Math.Max(1, GetMaxHitPoints(activator) * hpCostPercent / 100);
            hpCost = Math.Min(hpCost, Math.Max(0, GetCurrentHitPoints(activator) - 1));
            if (hpCost > 0)
                AssignCommand(activator, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(hpCost), activator));
        }

        private static void RestoreHPFromDamage(uint creature, int damage, int percent)
        {
            if (damage <= 0 || percent <= 0)
                return;

            var amount = Math.Max(1, damage * percent / 100);
            amount = Stat.ApplyHealingReceivedAdjustment(creature, amount);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), creature);
        }

        private static int CalculateCombatImpactDamage(
            uint activator,
            uint target,
            SkillType skillType,
            int baseDamage,
            CombatDamageType damageType)
        {
            if (baseDamage <= 0)
                return 0;

            var trackedImpact = GetTrackedAbilityImpact(activator);
            var ability = GetCombatImpactDamageAbility(skillType);
            var perkType = trackedImpact?.Ability?.EffectiveLevelPerkType ?? PerkType.Invalid;
            var idleBonuses = Combat.GetIdleSkillAbilityBonuses(activator, skillType);
            var damage = baseDamage +
                Combat.GetAbilityDamageBonus(activator, skillType) +
                Combat.GetAbilityDamageFlatAdjustment(activator, perkType) +
                idleBonuses.DamageBonus;
            if (trackedImpact != null)
            {
                damage += trackedImpact.NextAbilityDamageBonus;
            }

            var attack = Stat.GetAttack(activator, ability, skillType);
            attack = Combat.ApplyTargetStatusAttackModifiers(activator, target, attack, skillType);
            var attackStat = GetAbilityScore(activator, ability);
            var defenseAbility = damageType.GetDefenseAbilityType();
            var defense = Stat.GetDefense(target, damageType, defenseAbility);
            defense = Combat.ApplyStatusSourceDefenseModifiers(activator, target, defense);
            var defenderStat = GetAbilityScore(target, defenseAbility);
            var defenseIgnorePercent =
                Combat.GetAbilityDefenseIgnorePercentAdjustment(activator, perkType, skillType, target) +
                (trackedImpact?.NextAbilityDefenseIgnorePercentAdjustment ?? 0);
            defense = Combat.ApplyDefenseIgnore(defense, defenseIgnorePercent);
            var criticalRating = Combat.CalculateAbilityCriticalRating(
                activator,
                skillType,
                IsTrackedAbilityArea(activator),
                trackedImpact?.NextAbilityCriticalRatePercentAdjustment ?? 0,
                target);
            var damageRoll = Combat.CalculateDamageWithCriticalMitigation(
                target,
                attack,
                damage,
                attackStat,
                defense,
                defenderStat,
                criticalRating);
            var calculatedDamage = damageRoll.Damage;
            criticalRating = damageRoll.CriticalRating;
            calculatedDamage = Combat.ApplySideAttackDamageModifier(activator, target, skillType, calculatedDamage);
            calculatedDamage = Combat.ApplyTwinBladeAbilityShapeDamageModifier(
                activator,
                skillType,
                calculatedDamage,
                IsTrackedAbilitySingleTarget(activator),
                IsTrackedAbilityArea(activator));
            if (skillType == SkillType.Force)
            {
                calculatedDamage = Perk.ApplyForceAffinityMagnitude(activator, perkType, calculatedDamage);
            }
            calculatedDamage = Combat.ApplyDamageDealtModifiers(activator, target, calculatedDamage, skillType, damageType, true);
            calculatedDamage = Resistance.ApplyResistanceToDamage(target, damageType, calculatedDamage);
            calculatedDamage = Combat.ApplyDamageTakenModifiers(target, calculatedDamage);
            Combat.ApplyDamageReflectionEffects(activator, target, calculatedDamage, damageType);

            if (criticalRating > 0)
            {
                Combat.ApplyCriticalHitEffects(
                    activator,
                    target,
                    calculatedDamage,
                    criticalRating,
                    IsTrackedAbilitySingleTarget(activator),
                    skillType);
                Combat.ApplyCriticalAbilityStatusEffects(activator, target, perkType, damageType);
            }

            return calculatedDamage;
        }

        private static bool ApplyCombatImpactStatusEffect(
            uint activator,
            uint target,
            Type statusEffect,
            int duration,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            IEnumerable<Func<IStatusEffect>> additionalStatusEffectFactories,
            ResistanceType statusResistanceType,
            CombatDamageType sourceDamageType)
        {
            var hasAdditionalStatusEffects = additionalStatusEffects?.Any(x => x != null) ?? false;
            var hasAdditionalStatusEffectFactories = additionalStatusEffectFactories?.Any(x => x != null) ?? false;
            if (duration <= 0 || (statusEffect == null && statusEffectFactory == null && !hasAdditionalStatusEffects && !hasAdditionalStatusEffectFactories))
                return false;

            duration = ApplyAbilityStatusDurationAdjustment(activator, duration);

            var statusApplied = false;
            if (statusEffectFactory != null)
                statusApplied |= ApplyCombatImpactTrackedStatusEffect(activator, target, statusEffectFactory, duration, statusResistanceType, sourceDamageType);
            else if (statusEffect != null)
                statusApplied |= ApplyCombatImpactTrackedStatusEffect(activator, target, statusEffect, duration, statusResistanceType, sourceDamageType);

            if (additionalStatusEffects != null)
            {
                foreach (var additionalStatusEffect in additionalStatusEffects.Where(x => x != null && x != statusEffect).Distinct())
                {
                    statusApplied |= ApplyCombatImpactTrackedStatusEffect(activator, target, additionalStatusEffect, duration, statusResistanceType, sourceDamageType);
                }
            }

            if (additionalStatusEffectFactories != null)
            {
                foreach (var additionalStatusEffectFactory in additionalStatusEffectFactories.Where(x => x != null))
                {
                    statusApplied |= ApplyCombatImpactTrackedStatusEffect(activator, target, additionalStatusEffectFactory, duration, statusResistanceType, sourceDamageType);
                }
            }

            return statusApplied;
        }

        private static int ApplyAbilityStatusDurationAdjustment(uint activator, int duration)
        {
            if (duration <= 0)
                return duration;

            var trackedImpact = GetTrackedAbilityImpact(activator);
            var perkType = trackedImpact?.Ability?.EffectiveLevelPerkType ?? PerkType.Invalid;
            var adjustment = Combat.GetAbilityStatusDurationPercentAdjustment(activator, perkType);
            if (adjustment == 0)
                return duration;

            return Math.Max(1, duration + (int)Math.Ceiling(duration * (adjustment / 100f)));
        }

        private static bool ApplyCombatImpactTrackedStatusEffect(
            uint activator,
            uint target,
            Type type,
            float duration,
            ResistanceType statusResistanceType,
            CombatDamageType sourceDamageType)
        {
            return Resistance.IsValidResistanceType(statusResistanceType)
                ? StatusEffect.ApplyStatusEffect(activator, target, type, duration, statusResistanceType)
                : StatusEffect.ApplyStatusEffect(activator, target, type, duration, sourceDamageType);
        }

        private static bool ApplyCombatImpactTrackedStatusEffect(
            uint activator,
            uint target,
            Func<IStatusEffect> statusEffectFactory,
            float duration,
            ResistanceType statusResistanceType,
            CombatDamageType sourceDamageType)
        {
            var statusEffect = statusEffectFactory?.Invoke();
            if (statusEffect == null)
                return false;

            return Resistance.IsValidResistanceType(statusResistanceType)
                ? StatusEffect.ApplyStatusEffect(activator, target, statusEffect, duration, statusResistanceType)
                : StatusEffect.ApplyStatusEffect(activator, target, statusEffect, duration, sourceDamageType);
        }

        private static AbilityType GetCombatImpactDamageAbility(SkillType skillType)
        {
            switch (skillType)
            {
                case SkillType.Pistol:
                case SkillType.Rifle:
                case SkillType.Throwing:
                case SkillType.Devices:
                    return AbilityType.Perception;
                case SkillType.FirstAid:
                case SkillType.Force:
                    return AbilityType.Willpower;
                default:
                    return AbilityType.Might;
            }
        }

        /// <summary>
        /// Applies a temporary immunity effect to a particular target.
        /// This will add 20 seconds on top of whatever the ability duration length is.
        /// It will NOT remove any existing effects.
        /// </summary>
        /// <param name="target">The target receiving the immunity</param>
        /// <param name="abilityDuration">The length of the ability's duration. This will be added on top of the 20 seconds.</param>
        /// <param name="immunity">The type of immunity to apply.</param>
        public static void ApplyTemporaryImmunity(uint target, float abilityDuration, ImmunityType immunity)
        {
            const float BaseDuration = 20f;
            var duration = BaseDuration + abilityDuration;
            var effectTag = $"ABILITY_TEMP_IMMUNITY_{immunity}";

            // Effect is already in place.
            if (HasEffectByTag(target, effectTag))
                return;

            var effect = EffectImmunity(immunity);
            effect = TagEffect(effect, effectTag);
            ApplyEffectToObject(DurationType.Temporary, effect, target, duration);
        }

        private sealed class TrackedAbilityImpact
        {
            private readonly HashSet<uint> _impactedTargets = new();

            public AbilityDetail Ability { get; }
            public AbilityImpactSummary Summary { get; }
            public int NextAbilityDamageBonus { get; }
            public int NextAbilityCriticalRatePercentAdjustment { get; }
            public int NextAbilityDefenseIgnorePercentAdjustment { get; }
            public bool DarkForceConversionApplied { get; set; }

            public TrackedAbilityImpact(
                AbilityDetail ability,
                int nextAbilityDamageBonus,
                int nextAbilityCriticalRatePercentAdjustment,
                int nextAbilityDefenseIgnorePercentAdjustment)
            {
                Ability = ability;
                NextAbilityDamageBonus = nextAbilityDamageBonus;
                NextAbilityCriticalRatePercentAdjustment = nextAbilityCriticalRatePercentAdjustment;
                NextAbilityDefenseIgnorePercentAdjustment = nextAbilityDefenseIgnorePercentAdjustment;
                Summary = new AbilityImpactSummary
                {
                    SkillType = ability.SkillType,
                    IsAreaAbility = ability.IsAreaAbility,
                    IsSingleTargetAbility = ability.IsSingleTargetAbility
                };
            }

            public void RecordShape(SkillType skillType, bool isArea)
            {
                if (Summary.SkillType == SkillType.Invalid && skillType != SkillType.Invalid)
                {
                    Summary.SkillType = skillType;
                }

                if (isArea)
                {
                    Summary.IsAreaAbility = true;
                    Summary.IsSingleTargetAbility = false;
                }
                else if (!Summary.IsAreaAbility)
                {
                    Summary.IsSingleTargetAbility = true;
                }
            }

            public void RecordTarget(uint target)
            {
                _impactedTargets.Add(target);
                Summary.ImpactedTargetCount = _impactedTargets.Count;

                if (_impactedTargets.Count > 1)
                {
                    Summary.IsAreaAbility = true;
                    Summary.IsSingleTargetAbility = false;
                }
            }
        }
    }
}
