using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service
{
    public static class Ability
    {
        private static readonly Dictionary<FeatType, AbilityDetail> _abilities = new();
        private static readonly Dictionary<uint, ActiveConcentrationAbility> _activeConcentrationAbilities = new();
        private static readonly Dictionary<AbilityToggleType, Action<uint, bool>> _toggleActions = new();
        private static readonly Dictionary<uint, PlayerAura> _playerAuras = new();

        private const int MaxNumberOfAuras = 4;

        /// <summary>
        /// When the module caches, abilities will be cached and events will be scheduled.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            CacheAbilities();
            CacheToggleActions();
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

        private static void CacheToggleActions()
        {
            // If more toggle actions are added, it will make sense to promote this to a full fledged builder.
            // Until then, it can live here.
            _toggleActions[AbilityToggleType.Dash] = (player, isEnabled) =>
            {
                string message;
                message = isEnabled 
                    ? ColorToken.Green("Dash enabled") 
                    : ColorToken.Red("Dash disabled");

                Stat.ApplyPlayerMovementRate(player);
                SendMessageToPC(player, message);
            };
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
        /// <param name="statusEffectType">The concentration status effect to apply.</param>
        public static void StartConcentrationAbility(uint creature, uint target, FeatType feat, StatusEffectType statusEffectType)
        {
            _activeConcentrationAbilities[creature] = new ActiveConcentrationAbility(target, feat, statusEffectType);
            StatusEffect.Apply(creature, target, statusEffectType, 0.0f, null, feat);

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

            return new ActiveConcentrationAbility(OBJECT_INVALID, FeatType.Invalid, StatusEffectType.Invalid);
        }
        
        /// <summary>
        /// Ends a concentration effect on a specified creature.
        /// If creature isn't concentrating, nothing will happen.
        /// </summary>
        /// <param name="creature"></param>
        public static void EndConcentrationAbility(uint creature)
        {
            if (_activeConcentrationAbilities.ContainsKey(creature))
            {
                var activeConcentrationEffect = _activeConcentrationAbilities[creature];
                StatusEffect.Remove(creature, activeConcentrationEffect.StatusEffectType);
                _activeConcentrationAbilities.Remove(creature);

                SendMessageToPC(creature, "You stop concentrating.");
                DeleteLocalBool(creature, "CONCENTRATION_FIRST_USE");
            }
        }

        /// <summary>
        /// Toggles an ability on or off for a given player.
        /// If additional logic is defined in an AbilityToggleDefinition, that will be run after this is performed.
        /// </summary>
        /// <param name="player">The player to toggle on or off.</param>
        /// <param name="toggleType">The type of toggle to turn on or off.</param>
        /// <param name="isToggled">true if the ability should be enabled, false otherwise</param>
        public static void ToggleAbility(uint player, AbilityToggleType toggleType, bool isToggled)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (dbPlayer.AbilityToggles == null)
                dbPlayer.AbilityToggles = new Dictionary<AbilityToggleType, bool>();

            if (!dbPlayer.AbilityToggles.ContainsKey(toggleType))
                dbPlayer.AbilityToggles[toggleType] = false;

            var runLogic = dbPlayer.AbilityToggles[toggleType] != isToggled;
            dbPlayer.AbilityToggles[toggleType] = isToggled;

            DB.Set(dbPlayer);

            if (runLogic &&
                _toggleActions.ContainsKey(toggleType))
            {
                _toggleActions[toggleType](player, isToggled);
            }
        }

        /// <summary>
        /// Retrieves whether a player has a specific toggle type enabled.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="toggleType">The type of toggle to check</param>
        /// <returns>true if the ability is toggled on, false otherwise</returns>
        public static bool IsAbilityToggled(uint player, AbilityToggleType toggleType)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return false;

            var playerId = GetObjectUUID(player);
            return IsAbilityToggled(playerId, toggleType);
        }

        /// <summary>
        /// Retrieves whether  a player has a specific toggle type enabled.
        /// </summary>
        /// <param name="playerId">The player Id to check</param>
        /// <param name="toggleType">The type of toggle to check</param>
        /// <returns>true if the ability is toggled on, false otherwise</returns>
        public static bool IsAbilityToggled(string playerId, AbilityToggleType toggleType)
        {
            var dbPlayer = DB.Get<Player>(playerId);

            if (dbPlayer == null)
                return false;

            if (dbPlayer.AbilityToggles == null)
                dbPlayer.AbilityToggles = new Dictionary<AbilityToggleType, bool>();

            if (!dbPlayer.AbilityToggles.ContainsKey(toggleType))
                dbPlayer.AbilityToggles[toggleType] = false;

            return dbPlayer.AbilityToggles[toggleType];
        }

        /// <summary>
        /// Determines if any ability is toggled by a player.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <returns>true if any ability is toggled, false otherwise</returns>
        public static bool IsAnyAbilityToggled(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (dbPlayer == null)
                return false;

            if (dbPlayer.AbilityToggles == null)
                return false;

            foreach (var toggle in dbPlayer.AbilityToggles.Values)
            {
                if (toggle)
                    return true;
            }

            return false;
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

        public static void ApplyAura(uint activator, StatusEffectType type, bool targetsSelf, bool targetsParty, bool targetsEnemies)
        {
            if (!_playerAuras.ContainsKey(activator))
                _playerAuras.Add(activator, new PlayerAura());

            var aura = _playerAuras[activator];

            // Safety check - ensure the same aura never enters the cache more than once.
            if (aura.Auras.Exists(x => x.Type == type))
                return;

            var maxAuras = GetMaxNumberOfAuras(activator);
            var detail = StatusEffect.GetDetail(type);

            while (aura.Auras.Count >= maxAuras)
            {
                var removeType = aura.Auras[0].Type;
                if (aura.Auras[0].TargetsSelf)
                {
                    StatusEffect.Remove(activator, removeType, false);
                }

                if (aura.Auras[0].TargetsParty)
                {
                    foreach (var member in aura.PartyMembersInRange)
                    {
                        StatusEffect.Remove(member, removeType, false);
                    }
                }

                if (aura.Auras[0].TargetsEnemies)
                {
                    foreach (var npc in aura.CreaturesInRange)
                    {
                        StatusEffect.Remove(npc, removeType, false);
                    }
                }

                aura.Auras.RemoveAt(0);
            }

            aura.Auras.Add(new PlayerAuraDetail(type, targetsSelf, targetsParty, targetsEnemies));

            if (targetsSelf)
            {
                StatusEffect.Apply(activator, activator, type, 0f, activator);
            }

            SendMessageToPC(activator, ColorToken.Green($"Aura '{detail.Name}' activated."));
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Sound_Burst), activator);
        }

        public static bool ToggleAura(uint activator, StatusEffectType type)
        {
            if (!_playerAuras.ContainsKey(activator))
                return true;

            // Aura is active and player wants to deactivate it.
            // Remove it from the list and send a notification message.
            var aura = _playerAuras[activator];
            var existing = aura.Auras.FirstOrDefault(x => x.Type == type);
            if (existing != null)
            {
                var statusEffect = StatusEffect.GetDetail(type);

                SendMessageToPC(activator, ColorToken.Red($"Aura '{statusEffect.Name}' deactivated."));

                if (existing.TargetsSelf)
                {
                    StatusEffect.Remove(activator, type, false);
                }

                if (existing.TargetsParty)
                {
                    foreach (var member in aura.PartyMembersInRange)
                    {
                        StatusEffect.Remove(member, type, false);
                    }
                }

                if (existing.TargetsEnemies)
                {
                    foreach (var npc in aura.CreaturesInRange)
                    {
                        StatusEffect.Remove(npc, type, false);
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
                    StatusEffect.Remove(activator, aura.Type);
                }

                if (aura.TargetsParty)
                {
                    foreach (var member in auraDetails.PartyMembersInRange)
                    {
                        StatusEffect.Remove(member, aura.Type, false);
                    }
                }

                if (aura.TargetsEnemies)
                {
                    foreach (var npc in auraDetails.CreaturesInRange)
                    {
                        StatusEffect.Remove(npc, aura.Type, false);
                    }
                }
            }

            _playerAuras.Remove(activator);
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
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ClearAurasOnExit()
        {
            var player = GetExitingObject();
            RemoveAllAuras(player);
        }

        /// <summary>
        /// When a player dies, remove all of their Aura effects.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void ClearAurasOnDeath()
        {
            var player = GetLastPlayerDied();
            RemoveAllAuras(player);
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
        /// </summary>
        [NWNEventHandler(ScriptName.OnSpaceEnter)]
        public static void ClearAurasOnSpaceEntry()
        {
            var player = OBJECT_SELF;
            RemoveAllAuras(player);
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
                if (_playerAuras[self].PartyMembersInRange.Contains(entering))
                    return;

                _playerAuras[self].PartyMembersInRange.Add(entering);

                foreach (var detail in _playerAuras[self].Auras)
                {
                    if (detail.TargetsParty)
                    {
                        StatusEffect.Apply(self, entering, detail.Type, 0f, self);
                    }
                }
            }

            // Enemies
            else if (!GetIsDMPossessed(entering) && !GetIsDM(entering) && (GetIsEnemy(self, entering) || GetIsEnemy(entering, self)))
            {
                if (_playerAuras[self].CreaturesInRange.Contains(entering))
                    return;

                _playerAuras[self].CreaturesInRange.Add(entering);

                foreach (var detail in _playerAuras[self].Auras)
                {
                    if (detail.TargetsEnemies)
                    {
                        StatusEffect.Apply(self, entering, detail.Type, 0f, self);
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
                        StatusEffect.Remove(exiting, detail.Type, false);
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
                        StatusEffect.Remove(exiting, detail.Type, false);
                    }
                }
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
