using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>One uninterrupted mastery attempt; incomplete attempts deliberately do not persist.</summary>
    public sealed class TwinPistolTrialProgress
    {
        private uint _target;
        private int _mainHits;
        private int _companionHits;
        private DateTime _firingStarted;
        private DateTime _sequenceStarted;
        private int _sequenceStep;

        public void RecordTargetChange(uint target)
        {
            if (_target == target)
                return;
            _target = target;
            _mainHits = 0;
            _companionHits = 0;
            _firingStarted = default;
        }

        public bool RecordOrdinaryHit(DateTime now, uint target, bool companion)
        {
            if (_target != target || now >= _firingStarted.AddSeconds(30))
            {
                _target = target;
                _mainHits = 0;
                _companionHits = 0;
                _firingStarted = now;
            }
            if (companion)
                _companionHits++;
            else
                _mainHits++;
            return _mainHits >= 6 && _companionHits >= 6;
        }

        /// <summary>
        /// Tempo Shot, then Lead Shot, then Closing Shot spending at least two Cadence, each landing a hit,
        /// all within 18 seconds of the Tempo Shot. Other abilities in between do not break the sequence.
        /// </summary>
        public bool RecordAbility(DateTime now, bool anyHit, PerkType perk, int cadenceSpent)
        {
            if (now >= _sequenceStarted.AddSeconds(18))
                _sequenceStep = 0;
            if (!anyHit)
                return false;
            if (perk == PerkType.TempoShot)
            {
                _sequenceStarted = now;
                _sequenceStep = 1;
                return false;
            }
            if (perk == PerkType.LeadShot && _sequenceStep == 1)
            {
                _sequenceStep = 2;
                return false;
            }
            return perk == PerkType.ClosingShot && _sequenceStep == 2 && cadenceSpent >= 2;
        }
    }
}
