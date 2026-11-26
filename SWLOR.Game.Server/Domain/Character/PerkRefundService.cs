using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// What a perk refund takes away: the status effects the perk's abilities left on a creature, and the refund of a perk level
    /// when a skill decays below what the perk needs. They moved here from <see cref="PerkService"/> unchanged except where they
    /// read the catalog tables, which they now ask the stay-part for (<see cref="IPerkService.GetPerksWithSkillRequirement"/>).
    /// Calls into the hot bar, the ability catalog and the stat-driven trackers sit behind one-line <c>protected virtual</c>
    /// methods whose bodies are the original calls.
    /// </summary>
    public class PerkRefundService : IPerkRefundService
    {
        private readonly IDBService _db;
        private readonly IFeatHotBar _quickCast;
        private readonly IStatusEffectService _statusEffect;
        private readonly IAbilityCatalog _catalog;
        private readonly IPerkService _perk;
        private readonly IPerkGrantedFeatService _perkGrantedFeat;
        private readonly IStatDrivenTrackerRefresh _trackers;

        public PerkRefundService(
            IDBService db,
            IFeatHotBar quickCast,
            IStatusEffectService statusEffect,
            IAbilityCatalog catalog,
            IPerkService perk,
            IPerkGrantedFeatService perkGrantedFeat,
            IStatDrivenTrackerRefresh trackers)
        {
            _db = db;
            _quickCast = quickCast;
            _statusEffect = statusEffect;
            _catalog = catalog;
            _perk = perk;
            _perkGrantedFeat = perkGrantedFeat;
            _trackers = trackers;
        }

        /// <summary>
        /// Removes declared self effects and effects granted by the refunded perk's abilities.
        /// Source-owned effects retain their ability identity even when another perk uses the same status class.
        /// </summary>
        public void RemoveStatusEffectsOnPerkRefund(uint creature, PerkType perkType)
        {
            if (perkType == PerkType.Invalid || !GetIsObjectValid(creature))
                return;

            var statusEffectTypes = GetAllAbilityDetails()
                .Values
                .Where(ability => ability.EffectiveLevelPerkType == perkType)
                .SelectMany(ability => ability.StatusEffectTypesRemovedOnPerkRefund)
                .Distinct()
                .ToList();

            foreach (var statusEffectType in statusEffectTypes)
            {
                _statusEffect.RemoveStatusEffect(creature, statusEffectType, false);
            }

            var sourceOwnedStatusEffects = GetSourceOwnedStatusEffectRefunds(
                GetAllAbilityDetails().Values, perkType);

            foreach (var sourceOwnedStatusEffect in sourceOwnedStatusEffects)
            {
                _statusEffect.RemoveStatusEffectsFromAllTargetsBySource(
                    creature,
                    sourceOwnedStatusEffect.StatusEffectType,
                    false,
                    sourceOwnedStatusEffect.Ability);
            }

            RefreshStatDrivenTrackerEffects(creature);
        }

        /// <summary>
        /// Selects distinct ability/status pairs for a perk refund. Deduplication retains separate
        /// abilities that share a status class so each cleanup uses its own originating ability.
        /// </summary>
        public IReadOnlyList<(AbilityDetail Ability, Type StatusEffectType)> GetSourceOwnedStatusEffectRefunds(
            IEnumerable<AbilityDetail> abilities, PerkType perkType)
        {
            return abilities
                .Where(ability => ability.EffectiveLevelPerkType == perkType)
                .SelectMany(ability => ability.SourceOwnedStatusEffectTypesRemovedOnPerkRefund
                    .Select(statusEffectType => (Ability: ability, StatusEffectType: statusEffectType)))
                .Distinct()
                .ToArray();
        }

        /// <summary>
        /// When a skill receives decay, any perks tied to that skill should be checked.
        /// If the player no longer meets the requirements for those perks, they should be reduced in level.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorLoseSkill)]
        public void RemovePerkLevelOnSkillDecay()
        {
            var skillType = (SkillType)Convert.ToInt32(EventsPlugin.GetEventData("SKILL_TYPE_ID"));

            // Early exit - if no perks are tied to this skill, then it doesn't matter. There's nothing to remove.
            var possiblePerks = _perk.GetPerksWithSkillRequirement(skillType);
            if (possiblePerks.Count <= 0)
                return;

            var player = OBJECT_SELF;
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            foreach (var perkType in possiblePerks)
            {
                // Player doesn't have this perk. Move to the next.
                if (!dbPlayer.Perks.ContainsKey(perkType))
                    continue;

                var perkDetail = _perk.GetPerkDetails(perkType);
                var effectiveLevel = _perk.GetPlayerEffectivePerkLevel(player, perkType);
                var currentLevel = dbPlayer.Perks[perkType];

                // Player didn't suffer a reduction in effective level. Move to the next.
                if (effectiveLevel == currentLevel)
                    continue;

                // Found at least one perk level that needs to be removed.
                for (var level = currentLevel; level > effectiveLevel; level--)
                {
                    var perkLevel = perkDetail.PerkLevels[level];
                    dbPlayer.UnallocatedSP += perkLevel.Price;

                    foreach (var feat in perkLevel.GrantedFeats)
                    {
                        _quickCast.RemoveFeat(player, feat);
                    }

                    Log.Write(LogGroup.PerkRefund, $"AUTOMATIC DECAY REFUND - {playerId} - Refunded Date {Clock.UtcNow} - Level {perkLevel} - PerkID {perkType}");
                    FloatingTextStringOnCreature($"Perk '{perkDetail.Name}' level {level} was refunded because your skill fell under the minimum requirements. You reclaimed {perkLevel.Price} SP.", player, false);
                }

                dbPlayer.Perks[perkType] = effectiveLevel;
                if (effectiveLevel > 0)
                {
                    _perkGrantedFeat.SyncGrantedFeats(player, perkType, effectiveLevel, true);
                }

                _db.Set(dbPlayer);

                RemoveStatusEffectsOnPerkRefund(player, perkType);

                foreach (var refundTrigger in perkDetail.RefundedTriggers)
                {
                    refundTrigger(player);
                }
            }

            ExportSingleCharacter(player);
        }

        protected virtual IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails()
        {
            return _catalog.GetAllAbilityDetails();
        }

        protected virtual void RefreshStatDrivenTrackerEffects(uint creature)
        {
            _trackers.RefreshStatDrivenTrackerEffects(creature);
        }
    }
}
