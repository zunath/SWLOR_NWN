using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// A performer's hot bar and mode feats: the logic that was <see cref="PerformanceService"/>'s own, moved here
    /// unchanged. The feats a performance adds, the bar it swaps in, and the restore of the player's own bar when the
    /// performance ends or a crash left it swapped.
    /// </summary>
    public class PerformanceHotbarService : IPerformanceHotbarService
    {
        private const int FirstBarSlotCount = 12;
        private const int FlourishSlot = 4;
        private const int OptionsSlot = 5;
        private const int StopSlot = 6;

        private readonly FeatType[] _danceToggleFeats =
            { FeatType.Dance1, FeatType.Dance2, FeatType.Dance3, FeatType.Dance4 };

        private readonly FeatType[] _musicToggleFeats =
            { FeatType.PlayInstrument1, FeatType.PlayInstrument2, FeatType.PlayInstrument3, FeatType.PlayInstrument4 };

        private readonly FeatType[] _danceCueFeats =
        {
            FeatType.PerformanceStepLeft, FeatType.PerformanceStepUp,
            FeatType.PerformanceStepDown, FeatType.PerformanceStepRight
        };

        private readonly FeatType[] _musicCueFeats =
        {
            FeatType.PerformanceNoteLow, FeatType.PerformanceNoteMid, FeatType.PerformanceNoteHigh
        };

        private readonly FeatType[] _allModeFeats =
        {
            FeatType.PerformanceStepLeft, FeatType.PerformanceStepUp,
            FeatType.PerformanceStepDown, FeatType.PerformanceStepRight,
            FeatType.PerformanceNoteLow, FeatType.PerformanceNoteMid, FeatType.PerformanceNoteHigh,
            FeatType.PerformanceFlourish, FeatType.PerformanceOptions
        };

        private readonly IDBService _db;
        private readonly ICraftService _craft;
        private readonly IActivityService _activity;
        private readonly IPerformanceRegistryService _sessions;

        public PerformanceHotbarService(
            IDBService db,
            ICraftService craft,
            IActivityService activity,
            IPerformanceRegistryService sessions)
        {
            _db = db;
            _craft = craft;
            _activity = activity;
            _sessions = sessions;
        }

        public bool IsModeFeat(FeatType feat)
        {
            return Array.IndexOf(_allModeFeats, feat) >= 0;
        }

        public bool IsToggleFeat(FeatType feat)
        {
            return Array.IndexOf(_danceToggleFeats, feat) >= 0 || Array.IndexOf(_musicToggleFeats, feat) >= 0;
        }

        public FeatType GetToggleFeat(PerformanceKind kind, int level)
        {
            var toggleFeats = kind == PerformanceKind.Dance ? _danceToggleFeats : _musicToggleFeats;
            return toggleFeats[Math.Clamp(level, 1, toggleFeats.Length) - 1];
        }

        public bool TryGetCue(FeatType feat, out PerformanceCue cue)
        {
            switch (feat)
            {
                case FeatType.PerformanceStepLeft: cue = PerformanceCue.Left; return true;
                case FeatType.PerformanceStepUp: cue = PerformanceCue.Up; return true;
                case FeatType.PerformanceStepDown: cue = PerformanceCue.Down; return true;
                case FeatType.PerformanceStepRight: cue = PerformanceCue.Right; return true;
                case FeatType.PerformanceNoteLow: cue = PerformanceCue.Low; return true;
                case FeatType.PerformanceNoteMid: cue = PerformanceCue.Mid; return true;
                case FeatType.PerformanceNoteHigh: cue = PerformanceCue.High; return true;
                default: cue = PerformanceCue.Left; return false;
            }
        }

        public bool TrySaveHotbar(uint player)
        {
            var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return false;

            dbPlayer.PerformanceSavedHotBar = CreaturePlugin.SerializeQuickbar(player) ?? string.Empty;
            dbPlayer.PerformanceHotBarRestoreSequence = 0;
            _db.Set(dbPlayer);
            return true;
        }

        public void AddModeFeats(uint player, PerformanceKind kind)
        {
            foreach (var feat in GetModeFeats(kind))
                CreaturePlugin.AddFeat(player, feat);
        }

        // Options and Stop keep their slots in both modes; Roleplay mode leaves the cue and Flourish slots empty.
        public void ApplyModeQuickbar(uint player, PerformanceKind kind, FeatType toggleFeat, bool roleplay)
        {
            ClearFirstBar(player);

            if (!roleplay)
            {
                var cueFeats = kind == PerformanceKind.Dance ? _danceCueFeats : _musicCueFeats;
                for (var slot = 0; slot < cueFeats.Length; slot++)
                    PlayerPlugin.SetQuickBarSlot(player, slot, PlayerQuickBarSlot.UseFeat(cueFeats[slot]));

                PlayerPlugin.SetQuickBarSlot(player, FlourishSlot, PlayerQuickBarSlot.UseFeat(FeatType.PerformanceFlourish));
            }

            PlayerPlugin.SetQuickBarSlot(player, OptionsSlot, PlayerQuickBarSlot.UseFeat(FeatType.PerformanceOptions));
            PlayerPlugin.SetQuickBarSlot(player, StopSlot, PlayerQuickBarSlot.UseFeat(toggleFeat));
        }

        public void RemoveModeFeats(uint player)
        {
            foreach (var feat in _allModeFeats)
                CreaturePlugin.RemoveFeat(player, feat);
        }

        private void ClearFirstBar(uint player)
        {
            for (var slot = 0; slot < FirstBarSlotCount; slot++)
                PlayerPlugin.SetQuickBarSlot(player, slot, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
        }

        public void RestorePlayer(uint player)
        {
            RemoveModeFeats(player);

            var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
            {
                ClearFirstBar(player);
                return;
            }

            var saved = dbPlayer.PerformanceSavedHotBar;
            if (string.IsNullOrWhiteSpace(saved) || !CreaturePlugin.DeserializeQuickbar(player, saved))
                ClearFirstBar(player);

            // An export finishes after this script, so only a later login can acknowledge the restored bar.
            dbPlayer.PerformanceHotBarRestoreSequence = AdvanceFileSequence(player);
            _db.Set(dbPlayer);
            PersistCharacter(player);
        }

        // Restores the hot bar and feats after a crash, a failed cleanup, or a stale mode button.
        public void RecoverInterruptedPerformance(uint player, bool atLogin = false)
        {
            if (_sessions.IsPerforming(player))
                return;

            var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return;

            var action = PerformanceHotbarRecovery.Decide(
                dbPlayer.PerformanceSavedHotBar,
                dbPlayer.PerformanceHotBarRestoreSequence,
                LoadedSequence(player),
                atLogin);
            if (action == PerformanceHotbarRecoveryAction.None)
                return;

            if (action == PerformanceHotbarRecoveryAction.Restore)
                RestorePlayer(player);
            else
            {
                dbPlayer.PerformanceSavedHotBar = null;
                dbPlayer.PerformanceHotBarRestoreSequence = 0;
                _db.Set(dbPlayer);
            }

            if (GetBusyType(player) == ActivityStatusType.Performing)
                ClearBusy(player);
        }

        private IEnumerable<FeatType> GetModeFeats(PerformanceKind kind)
        {
            var cueFeats = kind == PerformanceKind.Dance ? _danceCueFeats : _musicCueFeats;
            return cueFeats.Concat(new[] { FeatType.PerformanceFlourish, FeatType.PerformanceOptions });
        }

        // The craft session world's character file checkpoint, behind the converted Craft service, and the Activity
        // service's busy state, one line each so a test can answer them.
        protected virtual int AdvanceFileSequence(uint player) => _craft.SessionWorld.AdvanceFileSequence(player);

        protected virtual int LoadedSequence(uint player) => _craft.SessionWorld.LoadedSequence(player);

        protected virtual void PersistCharacter(uint player) => _craft.SessionWorld.PersistCharacter(player);

        protected virtual ActivityStatusType GetBusyType(uint target) => _activity.GetBusyType(target);

        protected virtual void ClearBusy(uint target) => _activity.ClearBusy(target);
    }
}
