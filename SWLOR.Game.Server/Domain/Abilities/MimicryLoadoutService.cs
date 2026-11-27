using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The technique loadout: how many technique slots a player has, equipping and unequipping learned techniques, the slot
    /// budget enforced after progression changes and on login, the saved builds, and the learned and equipped reads. The
    /// technique catalog it reads is <see cref="IMimicryService"/>'s.
    /// </summary>
    public class MimicryLoadoutService : IMimicryLoadoutService
    {
        private readonly IDBService _db;
        private readonly IQuickCastService _quickCast;
        private readonly IGuiService _gui;
        private readonly IPerkService _perk;
        private readonly IStatusEffectService _statusEffect;
        private readonly IMimicryService _mimicry;
        private readonly IAbilityCatalog _catalog;

        public MimicryLoadoutService(
            IDBService db,
            IQuickCastService quickCast,
            IGuiService gui,
            IPerkService perk,
            IStatusEffectService statusEffect,
            IMimicryService mimicry,
            IAbilityCatalog catalog)
        {
            _db = db;
            _quickCast = quickCast;
            _gui = gui;
            _perk = perk;
            _statusEffect = statusEffect;
            _mimicry = mimicry;
            _catalog = catalog;
        }

        private const int BaseSlotsWithAnalyzer = 2;
        private const int SlotsPerAnalyzerMemoryLevel = 2;
        private const int OverclockedAnalyzerSlotBonus = 2;

        private const int TotalHotBarSlots = 36;
        // The first hotbar row is F1 through F12, which is slot indices 0 through 11.
        private const int AutoAddHotBarSlots = 12;

        /// <summary>
        /// Returns the maximum number of technique slots available to a player.
        /// Zero if the player does not have the Combat Analyzer perk.
        /// </summary>
        public int GetMaxSlots(uint player)
        {
            if (!GetIsPC(player))
                return 0;

            var playerId = GetObjectUUID(player);
            return GetMaxSlots(_db.Get<Player>(playerId));
        }

        /// <summary>
        /// Returns the maximum number of technique slots available to a player's database record.
        /// </summary>
        public int GetMaxSlots(Player dbPlayer)
        {
            if (dbPlayer == null)
                return 0;

            var analyzerLevel = dbPlayer.Perks.TryGetValue(PerkType.CombatAnalyzer, out var level) ? level : 0;
            if (analyzerLevel < 1)
                return 0;

            var memoryLevel = dbPlayer.Perks.TryGetValue(PerkType.AnalyzerMemory, out var memoryPerkLevel) ? memoryPerkLevel : 0;
            var capstoneBonus = dbPlayer.Perks.TryGetValue(PerkType.OverclockedAnalyzer, out var capstoneLevel) && capstoneLevel >= 1
                ? OverclockedAnalyzerSlotBonus
                : 0;

            return BaseSlotsWithAnalyzer + memoryLevel * SlotsPerAnalyzerMemoryLevel + capstoneBonus;
        }

        /// <summary>
        /// Returns the number of technique slots currently used by a player's equipped techniques.
        /// </summary>
        public int GetUsedSlots(uint player)
        {
            var playerId = GetObjectUUID(player);
            return GetUsedSlots(_db.Get<Player>(playerId));
        }

        /// <summary>
        /// Returns the number of technique slots currently used by a player database record's equipped techniques.
        /// </summary>
        public int GetUsedSlots(Player dbPlayer)
        {
            if (dbPlayer == null)
                return 0;

            var used = 0;
            foreach (var feat in dbPlayer.EquippedTechniques)
            {
                if (_mimicry.TryGetTechnique(feat, out var detail))
                    used += detail.MimicrySlotCost;
            }

            return used;
        }

        /// <summary>
        /// Determines whether a player can equip a given technique right now.
        /// </summary>
        public bool CanEquip(uint player, FeatType feat, out string error)
        {
            var playerId = GetObjectUUID(player);
            return CanEquip(player, _db.Get<Player>(playerId), feat, out error);
        }

        /// <summary>
        /// Determines whether a player can equip a given technique right now, using an
        /// already-fetched player database record to avoid duplicate round trips.
        /// </summary>
        public bool CanEquip(uint player, Player dbPlayer, FeatType feat, out string error)
        {
            error = string.Empty;

            if (!GetIsPC(player) || GetIsDM(player) || dbPlayer == null)
            {
                error = "Only players may equip techniques.";
                return false;
            }

            if (!_mimicry.TryGetTechnique(feat, out var detail))
            {
                error = "That is not a valid technique.";
                return false;
            }

            if (!dbPlayer.LearnedTechniques.ContainsKey(feat))
            {
                error = "You have not learned that technique.";
                return false;
            }

            var skillRank = dbPlayer.Skills.TryGetValue(SkillType.Mimicry, out var mimicrySkill) ? mimicrySkill.Rank : 0;
            var requiredSkillRank = detail.MimicrySkillRequirement;
            if (skillRank < requiredSkillRank)
            {
                error = $"You need Mimicry skill rank {requiredSkillRank} to equip that technique.";
                return false;
            }

            if (dbPlayer.EquippedTechniques.Contains(feat))
            {
                error = "That technique is already equipped.";
                return false;
            }

            if (GetIsInCombat(player))
            {
                error = "You cannot manage techniques while in combat.";
                return false;
            }

            if (GetUsedSlots(dbPlayer) + detail.MimicrySlotCost > GetMaxSlots(dbPlayer))
            {
                error = "You do not have enough technique slots available.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Equips a learned technique, granting the underlying feat and adding it to the player's hotbar.
        /// </summary>
        public bool EquipTechnique(uint player, FeatType feat)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (!CanEquip(player, dbPlayer, feat, out var error))
            {
                SendMessageToPC(player, ColorToken.Red(error));
                return false;
            }

            dbPlayer.EquippedTechniques.Add(feat);
            _db.Set(dbPlayer);

            GrantTechniqueFeat(player, feat);
            _quickCast.ApplyPreferences(player);
            _gui.PublishRefreshEvent(player, new TechniqueChangedRefreshEvent());

            return true;
        }

        /// <summary>
        /// Unequips a technique, removing the underlying feat and any hotbar slot referencing it.
        /// </summary>
        public bool UnequipTechnique(uint player, FeatType feat)
        {
            if (!GetIsPC(player))
                return false;

            if (GetIsInCombat(player))
            {
                SendMessageToPC(player, ColorToken.Red("You cannot manage techniques while in combat."));
                return false;
            }

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (!dbPlayer.EquippedTechniques.Remove(feat))
                return false;

            _db.Set(dbPlayer);
            RevokeTechniqueFeat(player, feat);
            _quickCast.ApplyPreferences(player);
            _gui.PublishRefreshEvent(player, new TechniqueChangedRefreshEvent());

            return true;
        }

        /// <summary>
        /// Unequips techniques that exceed the player's current Mimicry rank, then removes the
        /// newest-equipped techniques until the remaining loadout fits the current slot budget.
        /// Used after progression changes and on login to keep persisted loadouts valid.
        /// </summary>
        public void EnforceSlotBudget(uint player)
        {
            if (!GetIsPC(player))
                return;

            var playerId = GetObjectUUID(player);
            EnforceSlotBudget(player, _db.Get<Player>(playerId));
        }

        /// <summary>
        /// Enforces Mimicry rank requirements and the slot budget against an already-fetched player
        /// database record, avoiding a duplicate round trip when the caller has one in hand.
        /// </summary>
        public void EnforceSlotBudget(uint player, Player dbPlayer)
        {
            if (!GetIsPC(player) || dbPlayer == null)
                return;

            var playerId = GetObjectUUID(player);
            var maxSlots = GetMaxSlots(dbPlayer);
            var skillRank = dbPlayer.Skills.TryGetValue(SkillType.Mimicry, out var mimicrySkill)
                ? mimicrySkill.Rank
                : 0;
            var changed = false;

            for (var i = dbPlayer.EquippedTechniques.Count - 1; i >= 0; i--)
            {
                var feat = dbPlayer.EquippedTechniques[i];
                if (!_mimicry.TryGetTechnique(feat, out var detail) ||
                    skillRank >= detail.MimicrySkillRequirement)
                    continue;

                dbPlayer.EquippedTechniques.RemoveAt(i);
                changed = true;

                RevokeTechniqueFeat(player, feat);

                Log.WriteStructured(
                    LogGroup.Mimicry,
                    "Technique unequipped by skill requirement enforcement: PlayerId={PlayerId} Technique={Technique} SkillRank={SkillRank} RequiredRank={RequiredRank}",
                    playerId, feat, skillRank, detail.MimicrySkillRequirement);
            }

            for (var i = dbPlayer.EquippedTechniques.Count - 1; i >= 0; i--)
            {
                if (GetUsedSlots(dbPlayer) <= maxSlots)
                    break;

                var feat = dbPlayer.EquippedTechniques[i];
                dbPlayer.EquippedTechniques.RemoveAt(i);
                changed = true;

                RevokeTechniqueFeat(player, feat);

                Log.WriteStructured(
                    LogGroup.Mimicry,
                    "Technique unequipped by slot budget enforcement: PlayerId={PlayerId} Technique={Technique}",
                    playerId, feat);
            }

            if (changed)
            {
                _db.Set(dbPlayer);
                _quickCast.ApplyPreferences(player);
                _gui.PublishRefreshEvent(player, new TechniqueChangedRefreshEvent());
            }
        }

        /// <summary>
        /// Immediately removes equipped techniques whose rank requirement is no longer met when
        /// Mimicry loses a rank. This must run for every Mimicry decay, including rank losses that
        /// do not cross a perk requirement and therefore do not trigger a perk refund callback.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorLoseSkill)]
        public void OnMimicrySkillDecay()
        {
            var skillType = (SkillType)Convert.ToInt32(EventsPlugin.GetEventData("SKILL_TYPE_ID"));
            if (skillType != SkillType.Mimicry)
                return;

            EnforceSlotBudget(OBJECT_SELF);
        }

        /// <summary>
        /// Unequips every equipped technique. Used when the Combat Analyzer perk is refunded.
        /// </summary>
        public void UnequipAllTechniques(uint player)
        {
            if (!GetIsPC(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (dbPlayer.EquippedTechniques.Count <= 0)
                return;

            foreach (var feat in dbPlayer.EquippedTechniques)
            {
                RevokeTechniqueFeat(player, feat);
            }

            var unequippedCount = dbPlayer.EquippedTechniques.Count;
            dbPlayer.EquippedTechniques.Clear();
            _db.Set(dbPlayer);
            _quickCast.ApplyPreferences(player);
            _gui.PublishRefreshEvent(player, new TechniqueChangedRefreshEvent());

            Log.WriteStructured(
                LogGroup.Mimicry,
                "All techniques unequipped by perk refund: PlayerId={PlayerId} Count={Count}",
                playerId, unequippedCount);
        }

        /// <summary>
        /// On login, re-grants every equipped technique's feat (in case it was lost, e.g. a fresh
        /// character load) and enforces rank and slot limits in case progression changed since logout.
        /// </summary>
        [PlayerSession(PlayerReload.LoginOnly,
            "The technique feats and hotbar slots stay in the engine; a rerun would put back techniques the player removed from the hotbar.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void OnPlayerLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            // Character initialization can run migrations that clear native feats later in
            // this same module-enter event. Restore techniques after those handlers finish.
            DelayCommand(0f, () => RestorePlayerTechniques(player));
        }

        private void RestorePlayerTechniques(uint player)
        {
            if (!GetIsObjectValid(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            if (GetPerkLevel(player, PerkType.CombatAnalyzer) < 1)
                return;

            EnforceSlotBudget(player, dbPlayer);

            foreach (var feat in dbPlayer.EquippedTechniques)
            {
                GrantTechniqueFeat(player, feat);
            }
            _quickCast.ApplyPreferences(player);
        }

        /// <summary>
        /// Applies an equipped technique to the player. Active techniques grant the underlying feat
        /// (if missing) and add it to the hotbar, mirroring the grant + hotbar logic used by the perk
        /// system's active ability feats. Trait techniques are passive and are never granted as a
        /// usable feat: their stats and resistances are derived straight from the equipped loadout, so
        /// there is no grant or revoke step for them here, and any stale feat/hotbar entry is stripped
        /// so a trait can never end up castable or on the quickbar.
        /// </summary>
        private void GrantTechniqueFeat(uint player, FeatType feat, bool addToHotbar = true)
        {
            if (!GetIsObjectValid(player))
                return;

            feat = _quickCast.GetOriginalFeat(feat);
            var detail = _mimicry.GetTechniqueDetail(feat);
            if (detail != null && detail.IsMimicryTrait)
            {
                // Passive traits are never usable: they must not be granted as a castable feat nor
                // placed on the hotbar. Strip any feat/hotbar entry an earlier grant (or a prior
                // version that granted the feat unconditionally) left behind. The trait's stats need
                // no grant step - the stat pipeline reads them from the equipped list directly.
                if (_quickCast.KnowsFeat(player, feat))
                    _quickCast.RemoveFeat(player, feat);

                RemoveFeatFromHotBar(player, feat);
                return;
            }

            // Active technique: grant the feat and add it to the hotbar, using the same rules as a
            // perk-purchased active ability (register + ImpactAction gate, first empty auto-add slot).
            if (!_quickCast.KnowsFeat(player, feat))
                CreaturePlugin.AddFeat(player, _quickCast.GetDisplayedFeat(player, feat));

            if (addToHotbar)
                AddFeatToHotBar(player, feat);
        }

        /// <summary>Validates the entire draft before changing any equipped state.</summary>
        public bool ValidateLoadout(Player dbPlayer, IReadOnlyCollection<FeatType> techniques, out string error)
        {
            error = string.Empty;
            if (techniques.Count != techniques.Distinct().Count())
                error = "A build cannot contain duplicate techniques.";
            else
            {
                var rank = dbPlayer.Skills.TryGetValue(SkillType.Mimicry, out var skill) ? skill.Rank : 0;
                var used = 0;
                foreach (var feat in techniques)
                {
                    if (!_mimicry.TryGetTechnique(feat, out var detail))
                    {
                        error = "This build contains an unavailable technique. Remove it before applying.";
                        break;
                    }
                    if (!dbPlayer.LearnedTechniques.ContainsKey(feat))
                    {
                        error = $"You have not learned {detail.Name}.";
                        break;
                    }
                    if (rank < detail.MimicrySkillRequirement)
                    {
                        error = $"{detail.Name} requires Mimicry rank {detail.MimicrySkillRequirement}.";
                        break;
                    }
                    used += detail.MimicrySlotCost;
                }
                if (error.Length == 0 && used > GetMaxSlots(dbPlayer))
                    error = $"This build needs {used} technique slots; you have {GetMaxSlots(dbPlayer)}.";
            }
            return error.Length == 0;
        }

        public void CaptureLoadoutHotbar(uint player, MimicryLoadout loadout)
        {
            loadout.HotbarSlots.Clear();
            for (var slot = 0; slot < TotalHotBarSlots; slot++)
            {
                var entry = PlayerPlugin.GetQuickBarSlot(player, slot);
                if (entry.ObjectType != QuickBarSlotType.Feat)
                    continue;

                var feat = _quickCast.GetOriginalFeat((FeatType)entry.INTParam1);
                if (loadout.Techniques.Contains(feat))
                    loadout.HotbarSlots[slot] = feat;
            }
        }

        public bool ApplyLoadout(uint player, string loadoutId, IReadOnlyCollection<FeatType> techniques, out string error)
        {
            error = string.Empty;
            if (!GetIsPC(player) || GetIsDM(player))
                error = "Only players may apply technique builds.";
            else if (GetIsInCombat(player))
                error = "You cannot apply a technique build while in combat.";
            if (error.Length > 0)
                return false;

            var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
            var incoming = dbPlayer.MimicryLoadouts.FirstOrDefault(x => x.Id == loadoutId);
            if (incoming == null)
            {
                error = "That loadout no longer exists.";
                return false;
            }
            if (!ValidateLoadout(dbPlayer, techniques, out error))
                return false;

            var previous = dbPlayer.EquippedTechniques.ToList();
            var outgoing = dbPlayer.MimicryLoadouts.FirstOrDefault(x => x.Id == dbPlayer.ActiveMimicryLoadoutId);
            // A modified equipped set must not overwrite the saved preset's hotbar layout.
            if (outgoing != null && MimicryLoadouts.Matches(outgoing.Techniques, previous))
                CaptureLoadoutHotbar(player, outgoing);

            // Commit the whole set once. Revoke only outgoing feats so shared stances survive.
            // RecastTimes is intentionally untouched when builds change.
            dbPlayer.EquippedTechniques = techniques.ToList();
            dbPlayer.ActiveMimicryLoadoutId = loadoutId;
            _db.Set(dbPlayer);
            foreach (var feat in previous.Except(techniques))
                RevokeTechniqueFeat(player, feat);
            foreach (var feat in techniques)
                GrantTechniqueFeat(player, feat, false);

            // Restore only empty or Mimicry-owned cells. Other shortcuts always take precedence.
            var owned = previous.Concat(techniques).ToHashSet();
            if (incoming.HotbarSlots.Count > 0)
            {
                foreach (var feat in techniques)
                    RemoveFeatFromHotBar(player, feat);
                foreach (var (slot, savedFeat) in incoming.HotbarSlots)
                {
                    var feat = _quickCast.GetOriginalFeat(savedFeat);
                    if (slot < 0 || slot >= TotalHotBarSlots || !techniques.Contains(feat) ||
                        !_mimicry.TryGetTechnique(feat, out var detail) || detail.IsMimicryTrait)
                        continue;
                    var cell = PlayerPlugin.GetQuickBarSlot(player, slot);
                    if (cell.ObjectType == QuickBarSlotType.Empty ||
                        cell.ObjectType == QuickBarSlotType.Feat && owned.Contains(_quickCast.GetOriginalFeat((FeatType)cell.INTParam1)))
                        PlayerPlugin.SetQuickBarSlot(player, slot, PlayerQuickBarSlot.UseFeat(_quickCast.GetDisplayedFeat(player, feat)));
                }
            }
            foreach (var feat in techniques.Where(feat => !_mimicry.GetTechnique(feat).IsMimicryTrait))
                AddFeatToHotBar(player, feat);
            _quickCast.ApplyPreferences(player);
            _gui.PublishRefreshEvent(player, new TechniqueChangedRefreshEvent());
            return true;
        }

        /// <summary>
        /// Removes the technique's feat, any hotbar slot referencing it, and persistent effects
        /// declared by the ability. Trait stats need no explicit revoke step; they stop applying as
        /// soon as the feat leaves the equipped list.
        /// </summary>
        private void RevokeTechniqueFeat(uint player, FeatType feat)
        {
            if (!GetIsObjectValid(player))
                return;

            feat = _quickCast.GetOriginalFeat(feat);
            var detail = _mimicry.GetTechniqueDetail(feat);
            if (detail != null)
            {
                foreach (var statusEffectType in detail.StatusEffectTypesRemovedOnPerkRefund)
                {
                    RemoveStatusEffect(player, statusEffectType, false);
                }

                foreach (var statusEffectType in detail.SourceOwnedStatusEffectTypesRemovedOnPerkRefund)
                {
                    RemoveStatusEffectsFromAllTargetsBySource(
                        player,
                        statusEffectType,
                        false,
                        detail);
                }
            }

            _quickCast.RemoveFeat(player, feat);
            RemoveFeatFromHotBar(player, feat);
        }

        private bool IsFeatOnHotBar(uint player, FeatType feat)
        {
            feat = _quickCast.GetOriginalFeat(feat);
            for (var slot = 0; slot < TotalHotBarSlots; slot++)
            {
                var quickBarSlot = PlayerPlugin.GetQuickBarSlot(player, slot);
                if (quickBarSlot.ObjectType == QuickBarSlotType.Feat &&
                    _quickCast.GetOriginalFeat((FeatType)quickBarSlot.INTParam1) == feat)
                    return true;
            }

            return false;
        }

        private void AddFeatToHotBar(uint player, FeatType feat)
        {
            feat = _quickCast.GetOriginalFeat(feat);
            if (!IsFeatRegistered(feat) || GetAbilityDetail(feat).ImpactAction == null)
                return;

            if (IsFeatOnHotBar(player, feat))
                return;

            var quickBarSlot = PlayerQuickBarSlot.UseFeat(_quickCast.GetDisplayedFeat(player, feat));

            for (var slot = 0; slot < AutoAddHotBarSlots; slot++)
            {
                if (PlayerPlugin.GetQuickBarSlot(player, slot).ObjectType != QuickBarSlotType.Empty)
                    continue;

                PlayerPlugin.SetQuickBarSlot(player, slot, quickBarSlot);
                return;
            }
        }

        private void RemoveFeatFromHotBar(uint player, FeatType feat)
        {
            feat = _quickCast.GetOriginalFeat(feat);
            for (var slot = 0; slot < TotalHotBarSlots; slot++)
            {
                var quickBarSlot = PlayerPlugin.GetQuickBarSlot(player, slot);
                if (quickBarSlot.ObjectType == QuickBarSlotType.Feat &&
                    _quickCast.GetOriginalFeat((FeatType)quickBarSlot.INTParam1) == feat)
                {
                    PlayerPlugin.SetQuickBarSlot(player, slot, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
                }
            }
        }

        /// <summary>
        /// Returns every technique a player has learned, alongside its cached ability detail.
        /// </summary>
        public List<(FeatType Feat, AbilityDetail Detail)> GetLearnedTechniques(uint player)
        {
            var playerId = GetObjectUUID(player);
            return GetLearnedTechniques(_db.Get<Player>(playerId));
        }

        /// <summary>
        /// Returns every technique a player database record has learned, alongside its cached ability detail.
        /// </summary>
        public List<(FeatType Feat, AbilityDetail Detail)> GetLearnedTechniques(Player dbPlayer)
        {
            var result = new List<(FeatType, AbilityDetail)>();
            if (dbPlayer == null)
                return result;

            foreach (var feat in dbPlayer.LearnedTechniques.Keys)
            {
                if (_mimicry.TryGetTechnique(feat, out var detail))
                    result.Add((feat, detail));
            }

            return result;
        }

        /// <summary>
        /// Returns every technique a player currently has equipped, alongside its cached ability detail.
        /// </summary>
        public List<(FeatType Feat, AbilityDetail Detail)> GetEquippedTechniques(uint player)
        {
            var playerId = GetObjectUUID(player);
            return GetEquippedTechniques(_db.Get<Player>(playerId));
        }

        /// <summary>
        /// Returns every technique a player database record has equipped, alongside its cached ability detail.
        /// </summary>
        public List<(FeatType Feat, AbilityDetail Detail)> GetEquippedTechniques(Player dbPlayer)
        {
            var result = new List<(FeatType, AbilityDetail)>();
            if (dbPlayer == null)
                return result;

            foreach (var feat in dbPlayer.EquippedTechniques)
            {
                if (_mimicry.TryGetTechnique(feat, out var detail))
                    result.Add((feat, detail));
            }

            return result;
        }

        /// <summary>
        /// Returns true when a technique feat is present in a player's equipped loadout.
        /// </summary>
        public bool IsTechniqueEquipped(uint player, FeatType feat)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return false;

            var playerId = GetObjectUUID(player);
            return IsTechniqueEquipped(_db.Get<Player>(playerId), feat);
        }

        /// <summary>
        /// Returns true when a technique feat is present in an already-fetched player record's
        /// equipped loadout.
        /// </summary>
        public bool IsTechniqueEquipped(Player dbPlayer, FeatType feat)
        {
            return dbPlayer?.EquippedTechniques.Contains(_quickCast.GetOriginalFeat(feat)) == true;
        }

        /// <summary>The unconverted <c>Ability</c> registry.</summary>
        protected virtual bool IsFeatRegistered(FeatType feat) => _catalog.IsFeatRegistered(feat);

        /// <summary>The unconverted <c>Ability</c> registry.</summary>
        protected virtual AbilityDetail GetAbilityDetail(FeatType feat) => _catalog.GetAbilityDetail(feat);

        /// <summary>The unconverted <c>Perk</c> service.</summary>
        protected virtual int GetPerkLevel(uint creature, PerkType perkType) => _perk.GetPerkLevel(creature, perkType);

        /// <summary>The unconverted <c>StatusEffect</c> service.</summary>
        protected virtual void RemoveStatusEffect(uint creature, Type statusEffectType, bool sendsWornOffMessage) =>
            _statusEffect.RemoveStatusEffect(creature, statusEffectType, sendsWornOffMessage);

        /// <summary>The unconverted <c>StatusEffect</c> service.</summary>
        protected virtual void RemoveStatusEffectsFromAllTargetsBySource(uint source, Type statusEffectType,
            bool sendsWornOffMessage, AbilityDetail originatingAbility) =>
            _statusEffect.RemoveStatusEffectsFromAllTargetsBySource(source, statusEffectType, sendsWornOffMessage,
                originatingAbility);
    }
}
