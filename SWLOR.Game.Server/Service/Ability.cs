using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
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
        private static readonly Dictionary<uint, ActiveConcentrationAbility> _activeConcentrationAbilities = new();
        private static readonly Dictionary<uint, PlayerAura> _playerAuras = new();

        private const int MaxNumberOfAuras = 4;

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

            // Target check.
            if (ability.RequiresTarget && !GetIsObjectValid(target))
            {
                SendMessageToPC(activator, "A target is required.");
                return false;
            }

            // Range check.
            if (GetDistanceBetween(activator, target) > ability.MaxRange)
            {
                SendMessageToPC(activator, "You are out of range.  This ability has a range of " + ability.MaxRange + " meters.");
                return false;
            }

            // Hostility check
            if (GetIsObjectValid(target) && !GetIsReactionTypeHostile(target, activator) && ability.IsHostileAbility)
            {
                SendMessageToPC(activator, "You may only use this ability on enemies.");
                return false;
            }

            // Perk-specific requirement checks
            foreach (var req in ability.Requirements)
            {
                var requirementError = req.CheckRequirements(activator);
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
        /// Checks whether a creature can activate the perk feat.
        /// </summary>
        /// <param name="activator">The activator of the perk feat.</param>
        /// <param name="abilityType">The type of ability to use.</param>
        /// <returns>true if successful, false otherwise</returns>
        public static bool CanUseConcentration(
            uint activator,
            FeatType abilityType)
        {
            var ability = GetAbilityDetail(abilityType);

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

            // Perk-specific requirement checks
            foreach (var req in ability.Requirements)
            {
                var requirementError = req.CheckRequirements(activator);
                if (!string.IsNullOrWhiteSpace(requirementError))
                {
                    SendMessageToPC(activator, requirementError);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Each tick, creatures with a concentration effect will be processed.
        /// This will drain FP and reapply whatever effect is associated with an ability.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorHeartbeat)]
        public static void ProcessConcentrationEffects()
        {
            var pairs = _activeConcentrationAbilities.ToList();

            foreach (var (creature, concentrationAbility) in pairs)
            {
                // Creature/target is dead or invalid.
                if (!GetIsObjectValid(creature) ||
                    GetIsDead(creature) ||
                    !GetIsObjectValid(concentrationAbility.Target) ||
                    GetIsDead(concentrationAbility.Target))
                {
                    EndConcentrationAbility(creature);
                    continue;
                }

                // Creature and caster are not in the same area.
                if (GetArea(creature) != GetArea(concentrationAbility.Target))
                {
                    EndConcentrationAbility(creature);
                    continue;
                }

                var ability = GetAbilityDetail(concentrationAbility.Feat);

                // Move to next creature if requirements aren't met.
                if (!CanUseConcentration(creature, concentrationAbility.Feat))
                {
                    EndConcentrationAbility(creature);
                    continue;
                }

                // We don't run after activation actions until the second concentration cycle.
                // This is because if a player activates a concentration ability 1 second before the cycle,
                // they get charged for both the activation as well as the concentration cost.
                // The trade off is some abilities will last longer depending on when the player uses them in the cycle.
                // I think this is preferable to punishing the player twice though.
                if (!GetLocalBool(creature, "CONCENTRATION_FIRST_USE"))
                {
                    foreach (var req in ability.Requirements)
                    {
                        req.AfterActivationAction(creature);
                    }
                }
                DeleteLocalBool(creature, "CONCENTRATION_FIRST_USE");
            }
        }

        /// <summary>
        /// Starts a concentration ability on a specified creature.
        /// If there is already a concentration ability active, it will be replaced with this one.
        /// </summary>
        /// <param name="creature">The creature who will perform the concentration.</param>
        /// <param name="target">The target of the concentration effect.</param>
        /// <param name="feat">The type of ability to activate.</param>
        /// <param name="statusEffect">The concentration status effect to apply.</param>
        public static void StartConcentrationAbility(uint creature, uint target, FeatType feat, Type statusEffect)
        {
            EndConcentrationAbility(creature, false);

            _activeConcentrationAbilities[creature] = new ActiveConcentrationAbility(target, feat, statusEffect);
            StatusEffect.ApplyStatusEffect(creature, target, statusEffect, 0.0f);

            Messaging.SendMessageNearbyToPlayers(creature, $"{GetName(creature)} begins concentrating...");
            SetLocalBool(creature, "CONCENTRATION_FIRST_USE", true);
        }

        /// <summary>
        /// Retrieves a creature's active concentration ability.
        /// If no concentration ability is active, Feat.Invalid will be returned.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <returns>The active concentration feat or Feat.Invalid.</returns>
        public static ActiveConcentrationAbility GetActiveConcentration(uint creature)
        {
            if (_activeConcentrationAbilities.ContainsKey(creature))
            {
                return _activeConcentrationAbilities[creature];
            }

            return new ActiveConcentrationAbility(OBJECT_INVALID, FeatType.Invalid, null);
        }

        /// <summary>
        /// Ends a concentration effect on a specified creature.
        /// If creature isn't concentrating, nothing will happen.
        /// </summary>
        /// <param name="creature"></param>
        public static void EndConcentrationAbility(uint creature)
        {
            EndConcentrationAbility(creature, true);
        }

        private static void EndConcentrationAbility(uint creature, bool sendMessage)
        {
            if (_activeConcentrationAbilities.ContainsKey(creature))
            {
                var activeConcentrationEffect = _activeConcentrationAbilities[creature];
                var target = GetIsObjectValid(activeConcentrationEffect.Target)
                    ? activeConcentrationEffect.Target
                    : creature;

                StatusEffect.RemoveStatusEffect(target, activeConcentrationEffect.StatusEffect, creature);
                _activeConcentrationAbilities.Remove(creature);

                if (sendMessage)
                    SendMessageToPC(creature, "You stop concentrating.");

                DeleteLocalBool(creature, "CONCENTRATION_FIRST_USE");
            }
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ClearConcentrationOnExit()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            EndConcentrationAbility(player, false);
            EndConcentrationAbilitiesTargeting(player);
        }

        private static void EndConcentrationAbilitiesTargeting(uint target)
        {
            foreach (var (creature, concentrationAbility) in _activeConcentrationAbilities.ToList())
            {
                if (concentrationAbility.Target != target)
                    continue;

                EndConcentrationAbility(creature);
            }
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
            if (!StatusEffect.HasStatusEffect(recipient, type, source))
                StatusEffect.ApplyStatusEffect(source, recipient, type, 0f);
        }

        private static void RemoveAuraEffect(uint source, uint recipient, Type type, bool sendsWornOffMessage = false)
        {
            StatusEffect.RemoveStatusEffect(recipient, type, source, sendsWornOffMessage);
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

        public static bool ToggleAura(uint activator, Type type)
        {
            if (!_playerAuras.ContainsKey(activator))
                return true;

            // Aura is active and player wants to deactivate it.
            // Remove it from the list and send a notification message.
            var aura = _playerAuras[activator];
            var existing = aura.Auras.FirstOrDefault(x => x.StatusEffect == type);
            if (existing != null)
            {
                var effectName = StatusEffect.GetStatusEffectName(type);

                SendMessageToPC(activator, ColorToken.Red($"Aura '{effectName}' deactivated."));

                if (existing.TargetsSelf)
                {
                    RemoveAuraEffect(activator, activator, type);
                }

                if (existing.TargetsParty)
                {
                    foreach (var member in aura.PartyMembersInRange)
                    {
                        RemoveAuraEffect(activator, member, type);
                    }
                }

                if (existing.TargetsEnemies)
                {
                    foreach (var npc in aura.CreaturesInRange)
                    {
                        RemoveAuraEffect(activator, npc, type);
                    }
                }

                _playerAuras[activator].Auras.Remove(existing);
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

        private static AreaOfEffect GetAuraAOE(int level)
        {
            switch (level)
            {
                case 1:
                    return AreaOfEffect.AuraUpgrade1;
                case 2:
                    return AreaOfEffect.AuraUpgrade2;
                default:
                    return AreaOfEffect.AuraDefault;
            }
        }

        public static void ReapplyPlayerAuraAOE(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            RemoveEffectByTag(player, "AURA_EFFECT");
            var shoutRangeLevel = Perk.GetPerkLevel(player, PerkType.ShoutRange);

            AssignCommand(player, () =>
            {
                var auraAOE = GetAuraAOE(shoutRangeLevel);
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
            int savingThrowDc,
            SavingThrow savingThrow,
            Type statusEffect,
            bool isArea,
            IEnumerable<Type> additionalStatusEffects = null,
            Func<IStatusEffect> statusEffectFactory = null)
        {
            var totalDamage = 0;

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

                totalDamage = ApplyCombatImpactToCreatures(
                    activator,
                    creatures,
                    skillType,
                    baseDamage,
                    savingThrowDc,
                    savingThrow,
                    statusEffect,
                    duration,
                    additionalStatusEffects,
                    statusEffectFactory);
            }
            else if (GetIsObjectValid(target))
            {
                totalDamage = ApplyCombatImpactToCreatures(
                    activator,
                    new[] { target },
                    skillType,
                    baseDamage,
                    savingThrowDc,
                    savingThrow,
                    statusEffect,
                    duration,
                    additionalStatusEffects,
                    statusEffectFactory);
            }

            AssignCommand(activator, () => ActionPlayAnimation(Animation.DoubleStrike));
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
            int savingThrowDc,
            SavingThrow savingThrow,
            Type statusEffect,
            CombatImpactAreaShape shape,
            float telegraphDuration,
            float lengthOrRadius,
            float width = 0f,
            IEnumerable<Type> additionalStatusEffects = null,
            bool centerOnActivator = false,
            Func<IStatusEffect> statusEffectFactory = null)
        {
            if (telegraphDuration <= 0f)
            {
                ApplyCombatImpactInShape(
                    activator,
                    target,
                    targetLocation,
                    skillType,
                    baseDamage,
                    duration,
                    savingThrowDc,
                    savingThrow,
                    statusEffect,
                    shape,
                    lengthOrRadius,
                    width,
                    additionalStatusEffects,
                    centerOnActivator,
                    statusEffectFactory);
                return;
            }

            var action = BuildTelegraphedCombatImpactAction(
                skillType,
                baseDamage,
                duration,
                savingThrowDc,
                savingThrow,
                statusEffect,
                additionalStatusEffects,
                statusEffectFactory);

            switch (shape)
            {
                case CombatImpactAreaShape.Sphere:
                    Telegraph.CreateSphereTelegraph(
                        activator,
                        GetAreaImpactPosition(activator, target, targetLocation, centerOnActivator),
                        lengthOrRadius,
                        telegraphDuration,
                        true,
                        action);
                    break;
                case CombatImpactAreaShape.Cone:
                    Telegraph.CreateConeTelegraph(
                        activator,
                        GetPosition(activator),
                        GetImpactRotationRadians(activator, target, targetLocation),
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
                        GetImpactRotationRadians(activator, target, targetLocation),
                        lengthOrRadius,
                        width > 0f ? width : 2.0f,
                        telegraphDuration,
                        true,
                        action);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }

            AssignCommand(activator, () => ActionPlayAnimation(Animation.DoubleStrike));
        }

        private static void ApplyCombatImpactInShape(
            uint activator,
            uint target,
            Location targetLocation,
            SkillType skillType,
            int baseDamage,
            int duration,
            int savingThrowDc,
            SavingThrow savingThrow,
            Type statusEffect,
            CombatImpactAreaShape shape,
            float lengthOrRadius,
            float width,
            IEnumerable<Type> additionalStatusEffects,
            bool centerOnActivator,
            Func<IStatusEffect> statusEffectFactory)
        {
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

            ApplyCombatImpactToCreatures(
                activator,
                creatures,
                skillType,
                baseDamage,
                savingThrowDc,
                savingThrow,
                statusEffect,
                duration,
                additionalStatusEffects,
                statusEffectFactory);
        }

        private static ApplyTelegraphEffect BuildTelegraphedCombatImpactAction(
            SkillType skillType,
            int baseDamage,
            int duration,
            int savingThrowDc,
            SavingThrow savingThrow,
            Type statusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory)
        {
            return (creator, creatures) =>
            {
                if (!GetIsObjectValid(creator) || GetCurrentHitPoints(creator) <= 0)
                    return;

                ApplyCombatImpactToCreatures(
                    creator,
                    creatures,
                    skillType,
                    baseDamage,
                    savingThrowDc,
                    savingThrow,
                    statusEffect,
                    duration,
                    additionalStatusEffects,
                    statusEffectFactory);
            };
        }

        private static int ApplyCombatImpactToCreatures(
            uint activator,
            IEnumerable<uint> creatures,
            SkillType skillType,
            int baseDamage,
            int savingThrowDc,
            SavingThrow savingThrow,
            Type statusEffect,
            int duration,
            IEnumerable<Type> additionalStatusEffects = null,
            Func<IStatusEffect> statusEffectFactory = null)
        {
            var totalDamage = 0;

            foreach (var creature in creatures.Distinct())
            {
                if (!GetIsObjectValid(creature) || !GetIsReactionTypeHostile(creature, activator))
                    continue;

                totalDamage += ApplyHostileCombatImpact(
                    activator,
                    creature,
                    skillType,
                    baseDamage,
                    savingThrowDc,
                    savingThrow,
                    statusEffect,
                    duration,
                    additionalStatusEffects,
                    statusEffectFactory);
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

        private static int ApplyHostileCombatImpact(
            uint activator,
            uint target,
            SkillType skillType,
            int baseDamage,
            int savingThrowDc,
            SavingThrow savingThrow,
            Type statusEffect,
            int duration,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory)
        {
            var damage = CalculateCombatImpactDamage(activator, target, skillType, baseDamage);
            if (damage > 0)
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);
                Enmity.ModifyEnmity(activator, target, damage + 100);
            }

            ApplyCombatImpactStatusEffect(activator, target, savingThrowDc, savingThrow, statusEffect, duration, additionalStatusEffects, statusEffectFactory);
            CombatPoint.AddCombatPoint(activator, target, skillType, 3);
            return damage;
        }

        private static int CalculateCombatImpactDamage(uint activator, uint target, SkillType skillType, int baseDamage)
        {
            if (baseDamage <= 0)
                return 0;

            var ability = GetCombatImpactDamageAbility(skillType);
            var damage = baseDamage + Combat.GetAbilityDamageBonus(activator, skillType);
            var attack = Stat.GetAttack(activator, ability, skillType);
            var attackStat = GetAbilityScore(activator, ability);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityModifier(AbilityType.Vitality, target);
            var calculatedDamage = Combat.CalculateDamage(attack, damage, attackStat, defense, defenderStat, 0);
            return Combat.ApplyDamageTakenModifiers(target, calculatedDamage);
        }

        private static void ApplyCombatImpactStatusEffect(
            uint activator,
            uint target,
            int savingThrowDc,
            SavingThrow savingThrow,
            Type statusEffect,
            int duration,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory)
        {
            var hasAdditionalStatusEffects = additionalStatusEffects?.Any(x => x != null) ?? false;
            if (duration <= 0 || (statusEffect == null && statusEffectFactory == null && !hasAdditionalStatusEffects))
                return;

            if (savingThrowDc > 0 && !SavingThrowFailed(activator, target, savingThrow, savingThrowDc))
                return;

            if (statusEffectFactory != null)
                ApplyCombatImpactTrackedStatusEffect(activator, target, statusEffectFactory, duration);
            else if (statusEffect != null)
                ApplyCombatImpactTrackedStatusEffect(activator, target, statusEffect, duration);

            if (additionalStatusEffects != null)
            {
                foreach (var additionalStatusEffect in additionalStatusEffects.Where(x => x != null && x != statusEffect).Distinct())
                {
                    ApplyCombatImpactTrackedStatusEffect(activator, target, additionalStatusEffect, duration);
                }
            }

        }

        private static void ApplyCombatImpactTrackedStatusEffect(
            uint activator,
            uint target,
            Type type,
            float duration)
        {
            StatusEffect.ApplyStatusEffect(activator, target, type, duration);
        }

        private static void ApplyCombatImpactTrackedStatusEffect(
            uint activator,
            uint target,
            Func<IStatusEffect> statusEffectFactory,
            float duration)
        {
            var statusEffect = statusEffectFactory?.Invoke();
            if (statusEffect == null)
                return;

            StatusEffect.ApplyStatusEffect(activator, target, statusEffect, duration);
        }

        private static bool SavingThrowFailed(uint activator, uint target, SavingThrow savingThrow, int dc)
        {
            dc = Combat.CalculateSavingThrowDC(activator, savingThrow, dc);
            switch (savingThrow)
            {
                case SavingThrow.Fortitude:
                    return FortitudeSave(target, dc, SavingThrowType.None, activator) == SavingThrowResultType.Failed;
                case SavingThrow.Reflex:
                    return ReflexSave(target, dc, SavingThrowType.None, activator) == SavingThrowResultType.Failed;
                default:
                    return WillSave(target, dc, SavingThrowType.None, activator) == SavingThrowResultType.Failed;
            }
        }

        private static AbilityType GetCombatImpactDamageAbility(SkillType skillType)
        {
            switch (skillType)
            {
                case SkillType.Vibroknife:
                case SkillType.Lightsaber:
                case SkillType.Saberstaff:
                case SkillType.Katar:
                case SkillType.Pistol:
                case SkillType.Rifle:
                case SkillType.Devices:
                case SkillType.FirstAid:
                    return AbilityType.Perception;
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
    }
}
