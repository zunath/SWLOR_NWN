using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// Player auras and the Leadership combat point (part 8 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md),
    /// moved from <c>AbilityService</c> unchanged: everything that reads or writes the carried aura lists, with the eight event
    /// handlers that keep them. The party reads go through <see cref="IPartyRoster"/>, which Combat owns, so the part needs no
    /// Social service; the death service tells it about a revive through <see cref="ICreatureRevivedListener"/>. The read of the
    /// command radius bonus sits behind a <c>protected virtual</c> method so a test can answer it.
    /// </summary>
    public class AbilityAuraService : IAbilityAuraService
    {
        private readonly IStatusEffectService _statusEffect;
        private readonly IStatusEffectApplicationService _statusEffectApplication;
        private readonly IStatusEffectCatalogService _statusCatalog;
        private readonly IPartyRoster _party;
        private readonly IStatService _stat;
        private readonly ICombatPointService _combatPoint;

        public AbilityAuraService(
            IStatusEffectService statusEffect,
            IStatusEffectApplicationService statusEffectApplication,
            IStatusEffectCatalogService statusCatalog,
            IPartyRoster party,
            IStatService stat,
            ICombatPointService combatPoint)
        {
            _statusEffect = statusEffect;
            _statusEffectApplication = statusEffectApplication;
            _statusCatalog = statusCatalog;
            _party = party;
            _stat = stat;
            _combatPoint = combatPoint;
        }

        [ReloadState(ReloadStateKind.Carried)]
        [ReloadCarriedFrom("SWLOR.Game.Server.Domain.Abilities.AbilityService")]
        private readonly Dictionary<uint, PlayerAura> _playerAuras = new();

        private const int MaxNumberOfAuras = 4;

        /// <summary>
        /// Whenever a weapon's OnHit event is fired, add a Leadership combat point if an Aura is active.
        /// </summary>
        [NWNEventHandler(ScriptName.OnItemHit)]
        public void AddLeadershipCombatPoint()
        {
            RecordLeadershipWeaponHit(OBJECT_SELF, GetSpellTargetObject());
        }

        public void RecordLeadershipWeaponHit(uint player, uint target)
        {
            if (!GetIsPC(player) || GetIsDM(player) || !GetIsObjectValid(player))
                return;

            if (GetIsPC(target) || GetIsDM(target))
                return;

            if (!_playerAuras.ContainsKey(player))
                return;

            var aura = _playerAuras[player];

            if (aura.Auras.Count <= 0)
                return;

            _combatPoint.AddCombatPoint(player, target, SkillType.Leadership);
        }

        private int GetMaxNumberOfAuras(uint activator)
        {
            var social = GetAbilityScore(activator, AbilityType.Social);
            var count = 1 + (social - 10) / 5;

            if (count < 1)
                count = 1;

            if (count > MaxNumberOfAuras)
                count = MaxNumberOfAuras;

            return count;
        }

        private void ApplyAuraEffect(uint source, uint recipient, Type type, VisualEffect successfulImpactVisualEffect = VisualEffect.None)
        {
            if (_statusEffect.HasStatusEffect(recipient, type, source) ||
                HasEqualOrStrongerAuraEffect(source, recipient, type))
            {
                return;
            }

            RemoveWeakerDuplicateAuraEffects(source, recipient, type);
            var detail = _playerAuras.TryGetValue(source, out var playerAura)
                ? playerAura.Auras.FirstOrDefault(x => x.StatusEffect == type)
                : null;
            var statusEffect = _statusCatalog.CreateStatusEffect(type);
            if (statusEffect is IAuraStatusEffect aura && detail != null)
                aura.AnnouncesApplication = !detail.AnnouncedTo.Contains(recipient);

            if (!_statusEffectApplication.ApplyStatusEffect(source, recipient, statusEffect, 0f))
                return;

            detail?.AnnouncedTo.Add(recipient);
            if (successfulImpactVisualEffect != VisualEffect.None)
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(successfulImpactVisualEffect), recipient);
        }

        private void RemoveAuraEffect(uint source, uint recipient, Type type, bool sendsWornOffMessage = false)
        {
            _statusEffect.RemoveStatusEffect(recipient, type, source, sendsWornOffMessage);
            RestoreStrongestAuraEffect(recipient, type, source);
        }

        /// <summary>
        /// A stronger source replaces weaker copies of the same aura on a recipient. Once a source's copy
        /// is removed, the strongest other source still covering the recipient applies its copy again.
        /// </summary>
        private void RestoreStrongestAuraEffect(uint recipient, Type type, uint removedSource)
        {
            if (!GetIsObjectValid(recipient) || GetIsDead(recipient) || _statusEffect.HasStatusEffect(recipient, type))
                return;

            var strongestSource = OBJECT_INVALID;
            PlayerAuraDetail strongestDetail = null;
            var strongestSocial = int.MinValue;
            foreach (var (source, playerAura) in _playerAuras)
            {
                if (source == removedSource)
                    continue;

                var detail = playerAura.Auras.FirstOrDefault(x => x.StatusEffect == type);
                if (detail == null || !IsAuraRecipient(source, playerAura, detail, recipient))
                    continue;

                var social = GetAuraSourceSocial(source);
                if (social <= strongestSocial)
                    continue;

                strongestSource = source;
                strongestDetail = detail;
                strongestSocial = social;
            }

            if (strongestDetail != null)
                ApplyAuraEffect(strongestSource, recipient, type, strongestDetail.SuccessfulImpactVisualEffect);
        }

        private bool IsAuraRecipient(uint source, PlayerAura playerAura, PlayerAuraDetail detail, uint recipient)
        {
            if (detail.TargetsSelf && source == recipient)
                return true;

            if (detail.TargetsParty && playerAura.PartyMembersInRange.Contains(recipient) && _party.IsInParty(source, recipient))
                return true;

            return detail.TargetsEnemies &&
                   playerAura.CreaturesInRange.Contains(recipient) &&
                   !GetIsDMPossessed(recipient) && !GetIsDM(recipient) &&
                   (GetIsEnemy(source, recipient) || GetIsEnemy(recipient, source));
        }

        private bool HasEqualOrStrongerAuraEffect(uint source, uint recipient, Type type)
        {
            var sourceSocial = GetAuraSourceSocial(source);
            return _statusEffect.GetCreatureStatusEffects(recipient)
                .GetAllEffects()
                .Any(effect =>
                    effect.GetType() == type &&
                    effect.Source != source &&
                    GetAuraSourceSocial(effect.Source) >= sourceSocial);
        }

        private void RemoveWeakerDuplicateAuraEffects(uint source, uint recipient, Type type)
        {
            var sourceSocial = GetAuraSourceSocial(source);
            var weakerEffects = _statusEffect.GetCreatureStatusEffects(recipient)
                .GetAllEffects()
                .Where(effect =>
                    effect.GetType() == type &&
                    effect.Source != source &&
                    GetAuraSourceSocial(effect.Source) < sourceSocial)
                .ToList();

            foreach (var weakerEffect in weakerEffects)
            {
                _statusEffect.RemoveStatusEffect(recipient, type, weakerEffect.Source, false);
            }
        }

        private int GetAuraSourceSocial(uint source)
        {
            return GetIsObjectValid(source)
                ? GetAbilityScore(source, AbilityType.Social)
                : 0;
        }

        public void ApplyAura(uint activator, Type type, bool targetsSelf, bool targetsParty, bool targetsEnemies, VisualEffect successfulImpactVisualEffect = VisualEffect.None)
        {
            if (!_playerAuras.ContainsKey(activator))
                _playerAuras.Add(activator, new PlayerAura());

            var aura = _playerAuras[activator];

            // Safety check - ensure the same aura never enters the cache more than once.
            if (aura.Auras.Exists(x => x.StatusEffect == type))
                return;

            var maxAuras = GetMaxNumberOfAuras(activator);
            var effectName = _statusCatalog.GetStatusEffectName(type);

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

            aura.Auras.Add(new PlayerAuraDetail(type, targetsSelf, targetsParty, targetsEnemies, successfulImpactVisualEffect));

            if (targetsSelf)
            {
                ApplyAuraEffect(activator, activator, type, successfulImpactVisualEffect);
            }

            if (targetsParty)
            {
                foreach (var member in aura.PartyMembersInRange)
                {
                    if (_party.IsInParty(activator, member))
                        ApplyAuraEffect(activator, member, type, successfulImpactVisualEffect);
                }
            }

            if (targetsEnemies)
            {
                foreach (var npc in aura.CreaturesInRange)
                {
                    if (!GetIsDMPossessed(npc) && !GetIsDM(npc) &&
                        (GetIsEnemy(activator, npc) || GetIsEnemy(npc, activator)))
                        ApplyAuraEffect(activator, npc, type, successfulImpactVisualEffect);
                }
            }

            SendMessageToPC(activator, ColorToken.Green($"Aura '{effectName}' activated."));
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Sound_Burst), activator);
        }

        /// <summary>
        /// Retrieves the creatures a self-and-party aura applies to: the activator and the party
        /// members currently inside its aura range.
        /// </summary>
        /// <param name="activator">The creature whose aura is checked.</param>
        /// <returns>The activator followed by its party members in aura range.</returns>
        public List<uint> GetSelfAndPartyAuraRecipients(uint activator)
        {
            var recipients = new List<uint> { activator };
            if (_playerAuras.TryGetValue(activator, out var aura))
                recipients.AddRange(aura.PartyMembersInRange.Where(member => _party.IsInParty(activator, member)));

            return recipients;
        }

        public bool RemoveAura(uint activator, Type type, bool sendsWornOffMessage = false)
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

        public bool ToggleAura(uint activator, Type type)
        {
            if (!_playerAuras.ContainsKey(activator))
                return true;

            // Aura is active and player wants to deactivate it.
            // Remove it from the list and send a notification message.
            var effectName = _statusCatalog.GetStatusEffectName(type);
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
        private void RemoveAllAuras(uint activator)
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
        /// Deactivates every aura the player is currently projecting and strips any aura effects they are
        /// receiving as a recipient. Used by flows that must return a player to a clean state, such as the
        /// character rebuilder before a reset.
        /// </summary>
        public void ClearAllPlayerAuras(uint player)
        {
            RemoveAllAuras(player);
            RemoveCreatureFromAllAuraRanges(player);
        }

        /// <summary>
        /// Removes a creature from all active aura range lists and strips any aura effects they received
        /// as a recipient. Used when a creature leaves the game world in a way that bypasses the normal
        /// AOE exit event (e.g., entering space, being teleported).
        /// </summary>
        /// <param name="target">The creature to remove from all aura ranges.</param>
        public void RemoveCreatureFromAllAuraRanges(uint target)
        {
            // Leave every range first so removing one aura copy cannot restore another source's copy.
            var removedEffects = new List<(uint Leader, Type StatusEffect)>();
            foreach (var (leader, playerAura) in _playerAuras)
            {
                if (playerAura.PartyMembersInRange.Remove(target))
                {
                    foreach (var aura in playerAura.Auras)
                    {
                        if (aura.TargetsParty)
                            removedEffects.Add((leader, aura.StatusEffect));
                    }
                }

                if (playerAura.CreaturesInRange.Remove(target))
                {
                    foreach (var aura in playerAura.Auras)
                    {
                        if (aura.TargetsEnemies)
                            removedEffects.Add((leader, aura.StatusEffect));
                    }
                }
            }

            foreach (var (leader, statusEffect) in removedEffects)
            {
                RemoveAuraEffect(leader, target, statusEffect);
            }

            foreach (var playerAura in _playerAuras.Values)
            {
                foreach (var aura in playerAura.Auras)
                    aura.AnnouncedTo.Remove(target);
            }
        }


        /// <summary>
        /// Refreshes party-aura eligibility for a creature against all active aura leaders.
        /// If the creature is still tracked in a party aura range but is no longer in the leader's party,
        /// remove the cached range entry and strip relevant aura effects.
        /// </summary>
        /// <param name="target">The creature whose party aura eligibility should be refreshed.</param>
        public void RefreshPartyAuraEligibility(uint target)
        {
            foreach (var (leader, playerAura) in _playerAuras)
            {
                if (!playerAura.PartyMembersInRange.Contains(target))
                    continue;

                if (_party.IsInParty(leader, target))
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
        /// Enrolls a creature that just joined a party in the party auras it is already standing in,
        /// and enrolls the party members already standing in its own aura. The aura enter event only
        /// fires on entry, so without this they would wait until they leave and re-enter the aura.
        /// </summary>
        /// <param name="member">The creature that joined the party.</param>
        public void EnrollPartyMemberInAuraRanges(uint member)
        {
            var area = GetArea(member);
            if (!GetIsObjectValid(area))
                return;

            foreach (var aoe in ObjectSnapshot.InArea(area, ObjectType.AreaOfEffect))
            {
                var leader = GetAreaOfEffectCreator(aoe);
                if (leader != member && !_party.IsInParty(leader, member))
                    continue;

                if (GetEventScript(aoe, EventScript.AreaOfEffect_OnObjectEnter) != ScriptName.OnAuraEnter)
                    continue;

                var creaturesInside = ObjectSnapshot.Capture(
                    () => GetFirstInPersistentObject(aoe),
                    () => GetNextInPersistentObject(aoe),
                    GetIsObjectValid);

                foreach (var creature in creaturesInside)
                {
                    if (creature == leader || GetIsDead(creature))
                        continue;

                    if (leader == member ? _party.IsInParty(leader, creature) : creature == member)
                        AddPartyMemberToAuraRange(leader, creature);
                }
            }
        }

        private void AddPartyMemberToAuraRange(uint leader, uint member)
        {
            if (!_playerAuras.ContainsKey(leader))
                _playerAuras.Add(leader, new PlayerAura());

            if (!_playerAuras[leader].PartyMembersInRange.Contains(member))
                _playerAuras[leader].PartyMembersInRange.Add(member);

            foreach (var detail in _playerAuras[leader].Auras)
            {
                if (detail.TargetsParty)
                {
                    ApplyAuraEffect(leader, member, detail.StatusEffect, detail.SuccessfulImpactVisualEffect);
                }
            }
        }

        /// <summary>
        /// Re-applies any aura effects that a creature should be receiving based on their current position
        /// in active aura range lists. Used after in-place resurrection (subdual, revive) where the
        /// AOE enter event may not re-fire for a creature that never physically left the AOE.
        /// </summary>
        /// <param name="target">The creature to re-enroll in active auras.</param>
        public void ReapplyAuraEffectsForCreature(uint target)
        {
            if (!GetIsObjectValid(target) || GetIsDead(target))
                return;

            foreach (var (leader, playerAura) in _playerAuras)
            {
                if (playerAura.PartyMembersInRange.Contains(target) && _party.IsInParty(leader, target))
                {
                    foreach (var aura in playerAura.Auras)
                    {
                        if (aura.TargetsParty)
                            ApplyAuraEffect(leader, target, aura.StatusEffect, aura.SuccessfulImpactVisualEffect);
                    }
                }

                if (playerAura.CreaturesInRange.Contains(target) &&
                    !GetIsDMPossessed(target) && !GetIsDM(target) &&
                    (GetIsEnemy(leader, target) || GetIsEnemy(target, leader)))
                {
                    foreach (var aura in playerAura.Auras)
                    {
                        if (aura.TargetsEnemies)
                            ApplyAuraEffect(leader, target, aura.StatusEffect, aura.SuccessfulImpactVisualEffect);
                    }
                }
            }
        }

        private AreaOfEffect GetAuraAOE(int commandRadiusBonusMeters)
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

        public void ReapplyPlayerAuraAOE(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            RemoveEffectByTag(player, "AURA_EFFECT");

            AssignCommand(player, () =>
            {
                var commandRadiusBonusMeters = GetStatAdjustment(player, StatType.LeadershipCommandRadiusBonusMeters);
                var auraAOE = GetAuraAOE(commandRadiusBonusMeters);
                var effect = SupernaturalEffect(EffectAreaOfEffect(auraAOE, "aura_enter", string.Empty, "aura_exit"));
                effect = TagEffect(effect, "AURA_EFFECT");
                ApplyEffectToObject(DurationType.Permanent, effect, player);
            });
        }

        /// <summary>
        /// When a player enters the server, apply the Aura AOE effect.
        /// </summary>
        [PlayerSession(PlayerReload.LoginOnly,
            "The aura's area of effect stays on the player; the aura lists are carried (AbilityAuraService._playerAuras).")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void ApplyAuraAOE()
        {
            var player = GetEnteringObject();
            ReapplyPlayerAuraAOE(player);
        }

        /// <summary>
        /// When a player exits the server, remove all of their Aura effects.
        /// Also removes the player from any aura ranges they are receiving as a recipient.
        /// </summary>
        [PlayerSession(PlayerReload.LogoutOnly,
            "Takes the player's auras off everyone; the aura lists are carried.")]
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public void ClearAurasOnExit()
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
        public void ClearAurasOnDeath()
        {
            var player = GetLastPlayerDied();
            RemoveAllAuras(player);
            RemoveCreatureFromAllAuraRanges(player);
        }

        /// <summary>
        /// When a player respawns, reapply the aura AOE effect
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleRespawn)]
        public void ReapplyAuraOnRespawn()
        {
            var player = GetLastRespawnButtonPresser();
            ReapplyPlayerAuraAOE(player);
        }

        /// <summary>
        /// When a player enters space mode, remove all of their Aura effects.
        /// Also removes the player from any aura ranges they are receiving as a recipient.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSpaceEnter)]
        public void ClearAurasOnSpaceEntry()
        {
            var player = OBJECT_SELF;
            RemoveAllAuras(player);
            RemoveCreatureFromAllAuraRanges(player);
        }

        /// <summary>
        /// Whenever a creature enters the aura, add them to the cache.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAuraEnter)]
        public void AuraEnter()
        {
            var entering = GetEnteringObject();
            var self = GetAreaOfEffectCreator(OBJECT_SELF);

            if (!_playerAuras.ContainsKey(self))
                _playerAuras.Add(self, new PlayerAura());

            // Party Members
            if (_party.IsInParty(self, entering))
            {
                AddPartyMemberToAuraRange(self, entering);
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
                        ApplyAuraEffect(self, entering, detail.StatusEffect, detail.SuccessfulImpactVisualEffect);
                    }
                }
            }
        }

        /// <summary>
        /// Whenever a creature exits the aura, remove it from the cache.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAuraExit)]
        public void AuraExit()
        {
            var exiting = GetExitingObject();
            var self = GetAreaOfEffectCreator(OBJECT_SELF);

            if (!_playerAuras.ContainsKey(self))
                _playerAuras.Add(self, new PlayerAura());

            if (_party.IsInParty(self, exiting))
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
        /// A creature brought back where it fell (a subdual) never re-enters the aura it stands in, so its aura effects are
        /// applied again from the range lists.
        /// </summary>
        public void OnCreatureRevived(uint creature) => ReapplyAuraEffectsForCreature(creature);

        protected virtual int GetStatAdjustment(uint creature, StatType stat) => _stat.GetStatAdjustment(creature, stat);
    }
}
