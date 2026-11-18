using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Native;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// What survives logout, a planned restart and a hot reload (part 7 of the status effect split): the effects a player keeps while
    /// away, the stances and toggles saved for a restart and the settling of the carried effects after a reload. The trackers, the
    /// removal and the application stay in <see cref="StatusEffectService"/>; this part calls them through
    /// <see cref="IStatusEffectService"/>. The native effect read sits behind a one-line <c>protected virtual</c> method whose body is
    /// the original call.
    /// </summary>
    public class StatusEffectSessionService : IStatusEffectSessionService, ILoggedOutStatusEffectSource, IReloadStateAdopter
    {
        private const string StatusEffectTag = "STATUS_EFFECT";

        // The adoption order is by class name: StatusEffectService (the trackers it settles here) is read before this class.
        [ReloadState(ReloadStateKind.Carried)]
        [ReloadCarriedFrom("SWLOR.Game.Server.Domain.Character.StatusEffectService")]
        private readonly Dictionary<string, LoggedOutStatusEffects> _loggedOutPlayerEffects = new();
        private readonly IStatusEffectService _statusEffect;
        private readonly IStatusEffectApplicationService _statusEffectApplication;
        private readonly IStatusEffectCatalogService _statusCatalog;
        private readonly IDBService _db;
        private readonly IAbilityCatalog _catalog;

        public StatusEffectSessionService(
            IStatusEffectService statusEffect,
            IStatusEffectApplicationService statusEffectApplication,
            IStatusEffectCatalogService statusCatalog,
            IDBService db,
            IAbilityCatalog catalog)
        {
            _statusEffect = statusEffect;
            _statusEffectApplication = statusEffectApplication;
            _statusCatalog = statusCatalog;
            _db = db;
            _catalog = catalog;
        }

        /// <summary>
        /// Gives the status effects this service as a holder of logged-out effects. A reload runs boot handlers that rerun before it
        /// adopts the carried status effects, so the list is full before the first removal by source.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void RegisterLoggedOutSource()
        {
            _statusEffect.RegisterLoggedOutSource(this);
        }

        public IEnumerable<CreatureStatusEffect> GetLoggedOutEffects()
        {
            return _loggedOutPlayerEffects.Values.Select(loggedOut => loggedOut.Effects);
        }

        /// <summary>
        /// When a player enters the server, apply the NWN effect system
        /// </summary>
        [PlayerSession(PlayerReload.LoginOnly,
            "Moves the effects held since logout back to the player; a reload carries the status effects themselves, and a rerun would take the food bonus off max HP.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void OnPlayerEnter()
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
                    _statusEffect.TrackCreatureStatusEffects(player, effects);
                    _statusEffect.ReapplyNWNEffects(player, effects);
                    NotifyStatusEffectsRestored(player, effects);
                }
            }

            _statusEffect.ReapplyFoodHP(player);
            DelayCommand(1f, () => RestoreSavedToggles(player));
        }

        /// <summary>
        /// After a hot reload has carried the status effects into this build (<see cref="IReloadStateAdopter"/>), for every creature
        /// still in the world: a status whose native effect is gone is settled now, as the old build's settle closure (dropped by the
        /// reload) would have settled it; every other status restarts what it keeps going through its own closures
        /// (<see cref="StatusEffectBase.ResumeAfterReload"/>, the rest movement check); and the movement rate is recomputed, as the
        /// closure after a removed stat change would have done (online players too, whose last status may have just ended).
        /// </summary>
        public void OnCarriedStateAdopted()
        {
            var creatures = new HashSet<uint>(_statusEffect.GetTrackedCreatures());
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
                creatures.Add(player);

            foreach (var creature in creatures)
            {
                if (!GetIsObjectValid(creature))
                    continue;

                if (_statusEffect.TryGetCreatureStatusEffects(creature, out var effects))
                {
                    foreach (var effect in effects.GetAllEffects())
                    {
                        if (!MayHaveEffectTag(creature, effect.Id) || !HasEffectByTag(creature, effect.Id))
                            _statusEffect.RemoveStatusEffectById(creature, effect.Id, true, false);
                        else
                            (effect as StatusEffectBase)?.ResumeAfterReload(creature);
                    }
                }

                _statusEffect.ApplyCreatureMovementRate(creature);
            }
        }

        /// <summary>
        /// Saves the stances and toggles of everyone online or holding effects from an earlier logout. Those effects are only kept in
        /// memory, so without this a server restart loses them.
        /// </summary>
        public void SaveTogglesForRestart()
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (!GetIsDM(player) && _statusEffect.TryGetCreatureStatusEffects(player, out var effects))
                    SaveToggles(GetObjectUUID(player), effects);
            }

            foreach (var (playerId, loggedOut) in _loggedOutPlayerEffects)
            {
                SaveToggles(playerId, loggedOut.Effects);
            }
        }

        private void SaveToggles(string playerId, CreatureStatusEffect effects)
        {
            var toggles = effects.GetAllEffects()
                .Where(effect => effect.RestoresAfterServerRestart && effect.DurationTicks < 0)
                .Select(effect => effect.GetType().Name)
                .Distinct()
                .ToList();
            if (toggles.Count == 0)
                return;

            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            dbPlayer.TogglesToRestore = toggles;
            _db.Set(dbPlayer);
        }

        private void RestoreSavedToggles(uint player)
        {
            if (!GetIsObjectValid(player))
                return;

            var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
            if (dbPlayer?.TogglesToRestore == null)
                return;

            var toggles = dbPlayer.TogglesToRestore;
            dbPlayer.TogglesToRestore = null;
            _db.Set(dbPlayer);

            if (GetCurrentHitPoints(player) <= 0)
                return;

            foreach (var toggle in toggles)
            {
                var type = _statusCatalog.FindDefinitionByName(toggle);
                var ability = type == null ? null : FindKnownToggleAbility(player, type);
                if (ability == null || _statusEffect.HasStatusEffect(player, type) || !_statusCatalog.TryCreateStatusEffect(type, out var statusEffect))
                    continue;

                statusEffect.OriginatingAbility = ability;
                _statusEffect.RemoveOtherStanceStatuses(player, type);
                _statusEffectApplication.ApplyStatusEffect(player, player, statusEffect, 0f);
            }
        }

        // A deploy can change perks while the server is down, so a toggle only returns if the player still knows its ability.
        private AbilityDetail FindKnownToggleAbility(uint player, Type statusEffectType)
        {
            foreach (var (feat, ability) in GetAllAbilityDetails())
            {
                if (ability.StatusEffectTypesRemovedOnPerkRefund.Contains(statusEffectType) && QuickCastFeatMap.KnowsFeat(player, feat))
                    return ability;
            }

            return null;
        }

        /// <summary>
        /// When a player exits the server, hold their status effects until they return.
        /// </summary>
        [PlayerSession(PlayerReload.LogoutOnly,
            "Holds the effects for the player's return and takes them off the creature; a reload carries the status effects of every creature instead.")]
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public void OnPlayerExit()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            _statusEffect.RemoveStatusEffectsFromAllTargetsWhenSourceExits(player);

            var playerId = GetObjectUUID(player);
            if (_statusEffect.TryGetCreatureStatusEffects(player, out var effects) &&
                effects.GetAllEffects().Count > 0)
            {
                _statusEffect.RemoveNonPersistentStatusEffects(player, effects);

                if (effects.GetAllEffects().Count > 0)
                {
                    _loggedOutPlayerEffects[playerId] = new LoggedOutStatusEffects(player, effects, Clock.UtcNow);
                    _statusEffect.RemoveTrackedNWNEffects(player, effects);
                }
                else
                {
                    RemoveEffectByTag(player, StatusEffectTag);
                }
            }

            _statusEffect.RemoveCreature(player);
        }

        private void ReassignSelfSourcedEffects(uint creature, LoggedOutStatusEffects loggedOutEffects)
        {
            foreach (var statusEffect in loggedOutEffects.Effects.GetAllEffects()
                         .Where(effect => effect.Source == loggedOutEffects.Creature))
            {
                statusEffect.ReassignSource(creature);
            }
        }

        private void ReconcileLoggedOutEffects(uint creature, LoggedOutStatusEffects loggedOutEffects)
        {
            var currentTime = Clock.UtcNow;
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
                _statusEffect.NotifyStatusEffectRemoved(creature, statusEffect);
                RemoveEffectByTag(creature, statusEffect.Id);
            }
        }

        private void NotifyStatusEffectsRestored(uint creature, CreatureStatusEffect effects)
        {
            foreach (var handler in effects.GetAllEffects().OfType<IStatusEffectRestoredHandler>().ToList())
            {
                handler.AfterRestored(creature);
            }
        }

        // A native read of the object's applied effects.
        protected virtual bool MayHaveEffectTag(uint creature, string tag) => AppliedEffects.MayHaveTag(creature, tag);

        protected virtual IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails() => _catalog.GetAllAbilityDetails();

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
