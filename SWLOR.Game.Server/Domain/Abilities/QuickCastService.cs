using System.Collections.Generic;
using NWNXLib = NWN.Native.API.NWNXLib;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// Persisted targeting preferences apply to all eligible native ability buttons. The logic that used to be static in
    /// <c>Service.QuickCast</c> is moved here unchanged; the set of players being updated is this instance's own. The
    /// facade keeps the event handlers and forwards every call to the container's instance. Calls into the unconverted
    /// <c>Ability</c> and the feature code sit behind one-line <c>protected virtual</c> methods whose bodies are the
    /// original calls.
    /// </summary>
    public class QuickCastService : IQuickCastService
    {
        private readonly HashSet<uint> _applying = new();

        private readonly IDBService _db;
        private readonly Lazy<IAbilityService> _ability;

        public QuickCastService(IDBService db, Lazy<IAbilityService> ability)
        {
            _db = db;
            _ability = ability;
        }

        public FeatType GetOriginalFeat(FeatType feat) => QuickCastFeatMap.GetOriginal(feat);

        private PlayerSettings GetSettings(uint player)
        {
            if (!GetIsObjectValid(player) || !GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return null;
            var id = GetObjectUUID(player);
            return string.IsNullOrWhiteSpace(id) ? null : _db.Get<Player>(id)?.Settings;
        }

        public bool IsEnabled(PlayerSettings settings, QuickCastTargetType type) => type switch
        {
            QuickCastTargetType.Self => settings?.SelfQuickCastEnabled == true,
            QuickCastTargetType.CurrentEnemy => settings?.EnemyQuickCastEnabled == true,
            _ => false
        };

        public bool IsEnabled(uint player, QuickCastTargetType type) => IsEnabled(GetSettings(player), type);

        public bool UsesAutomaticTarget(FeatType original, PlayerSettings settings)
        {
            if (settings == null || !IsFeatRegistered(original))
                return false;
            var ability = GetAbilityDetail(original);
            return IsEnabled(settings, QuickCastTargetType.Self) && QuickCastPolicy.IsEligible(ability, QuickCastTargetType.Self) ||
                   IsEnabled(settings, QuickCastTargetType.CurrentEnemy) && QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy);
        }

        public FeatType GetDisplayedFeat(uint player, FeatType feat)
        {
            var original = GetOriginalFeat(feat);
            return QuickCastFeatMap.TryGetAlias(original, out var alias) && UsesAutomaticTarget(original, GetSettings(player))
                ? alias : original;
        }

        public bool KnowsFeat(uint creature, FeatType feat)
        {
            var original = GetOriginalFeat(feat);
            return CreaturePlugin.GetKnowsFeat(creature, original) ||
                   QuickCastFeatMap.TryGetAlias(original, out var alias) && CreaturePlugin.GetKnowsFeat(creature, alias);
        }

        public void RemoveFeat(uint creature, FeatType feat)
        {
            var original = GetOriginalFeat(feat);
            CreaturePlugin.RemoveFeat(creature, original);
            if (QuickCastFeatMap.TryGetAlias(original, out var alias))
                CreaturePlugin.RemoveFeat(creature, alias);
        }

        public void ApplyPreferences(uint player)
        {
            var settings = GetSettings(player);
            if (settings == null || !_applying.Add(player))
                return;
            try
            {
                foreach (var (alias, original) in QuickCastFeatMap.Originals)
                {
                    var hasOriginal = CreaturePlugin.GetKnowsFeat(player, original);
                    var hasAlias = CreaturePlugin.GetKnowsFeat(player, alias);
                    if (!hasOriginal && !hasAlias)
                        continue;
                    var automatic = UsesAutomaticTarget(original, settings);
                    var desired = automatic ? alias : original;
                    var other = automatic ? original : alias;
                    if (!(automatic ? hasAlias : hasOriginal))
                        CreaturePlugin.AddFeat(player, desired);
                    if (automatic ? hasOriginal : hasAlias)
                        CreaturePlugin.RemoveFeat(player, other);
                }

                // Saved layouts and newly dragged feats may still carry either representation.
                // Only ability IDs change; labels, other shortcuts and slot positions are preserved.
                for (var index = 0; index < 36; index++)
                {
                    var slot = PlayerPlugin.GetQuickBarSlot(player, index);
                    if (slot.ObjectType != QuickBarSlotType.Feat)
                        continue;

                    var current = (FeatType)slot.INTParam1;
                    var original = GetOriginalFeat(current);
                    if (!QuickCastFeatMap.TryGetAlias(original, out var alias))
                        continue;
                    var desired = KnowsFeat(player, original) && UsesAutomaticTarget(original, settings) ? alias : original;
                    if (current == desired)
                        continue;
                    slot.INTParam1 = (int)desired;
                    PlayerPlugin.SetQuickBarSlot(player, index, slot);
                }
            }
            finally { _applying.Remove(player); }
        }

        public void ApplyOnEnter() => ApplyPreferences(GetEnteringObject());

        public void ApplyOnQuickbarEdit() => ApplyPreferences(OBJECT_SELF);

        public bool TryCast(uint player, FeatType alias, out string error)
        {
            var settings = GetSettings(player);
            if (settings == null || !QuickCastFeatMap.IsAlias(alias))
            {
                error = "Automatic targeting is only available for your player character's abilities.";
                return false;
            }
            var feat = GetOriginalFeat(alias);
            if (!IsFeatRegistered(feat) || !KnowsFeat(player, feat))
            {
                error = "You do not know this ability.";
                return false;
            }
            var ability = GetAbilityDetail(feat);
            var type = ability.IsHostileAbility ? QuickCastTargetType.CurrentEnemy : QuickCastTargetType.Self;
            if (!IsEnabled(settings, type) || !QuickCastPolicy.IsEligible(ability, type))
            {
                error = "Automatic targeting is disabled for this ability. Check Settings > Targeting.";
                return false;
            }

            if (HasCastingBlockingEffect(player))
            {
                error = "You cannot use an ability while incapacitated.";
                return false;
            }

            var target = player;
            if (type == QuickCastTargetType.CurrentEnemy)
            {
                // Includes the attack resumed after a previous ability, before the engine restores the
                // native attack target at the start of its next round.
                target = GetQueuedAttackTarget(player);
                if (!GetIsObjectValid(target) || target == player || GetIsDead(target) || GetCurrentHitPoints(target) <= 0 ||
                    GetArea(target) != GetArea(player) || !GetIsReactionTypeHostile(target, player))
                {
                    error = "Attack a living enemy first to use current-enemy targeting.";
                    return false;
                }
            }
            error = string.Empty;
            return UseAbility(player, target, feat);
        }

        /// <summary>The unconverted <c>Ability</c> registry.</summary>
        protected virtual bool IsFeatRegistered(FeatType feat) => _ability.Value.IsFeatRegistered(feat);

        /// <summary>The unconverted <c>Ability</c> registry.</summary>
        protected virtual AbilityDetail GetAbilityDetail(FeatType feat) => _ability.Value.GetAbilityDetail(feat);

        /// <summary>Reads the creature's applied effects from the native engine.</summary>
        protected virtual bool HasCastingBlockingEffect(uint player)
        {
            var creature = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(player).AsNWSCreature();
            foreach (var effect in creature.m_appliedEffects)
            {
                if (QuickCastPolicy.BlocksCasting((EffectTypeScript)effect.GetScriptEffectType(1)))
                    return true;
            }

            return false;
        }

        /// <summary>Reads the queued attack from the native engine.</summary>
        protected virtual uint GetQueuedAttackTarget(uint player) => ActionInterruption.GetQueuedAttackTarget(player);

        /// <summary>Starts the ability through the feature pipeline.</summary>
        protected virtual bool UseAbility(uint player, uint target, FeatType feat) =>
            UsePerkFeat.TryUseAbility(player, target, feat, GetLocation(target));
    }
}
