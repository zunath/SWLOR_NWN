namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>One uninterrupted mastery attempt; incomplete attempts deliberately do not persist.</summary>
    public sealed class TwinPistolTrialProgress
    {
        private uint _target;
        private int _mainHits;
        private int _companionHits;
        private DateTime _firingStarted;
        private DateTime _movementStarted;
        private bool _slideLanded;
        private bool _rushLanded;

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

        public bool RecordAbility(DateTime now, bool anyHit, float distanceTravelled, TwinPistolAbilitySpec spec, int momentumSpent)
        {
            if (now >= _movementStarted.AddSeconds(18))
            {
                _slideLanded = false;
                _rushLanded = false;
                _movementStarted = now;
            }
            if (!anyHit || !spec.IsCrossfire)
                return false;
            if (distanceTravelled >= 2f && spec.GeneratesMomentum)
            {
                if (!_slideLanded && !_rushLanded)
                    _movementStarted = now;
                _slideLanded |= spec.Movement == TwinPistolMovementType.Retreat;
                _rushLanded |= spec.Movement == TwinPistolMovementType.FreeDestination;
            }
            return _slideLanded && _rushLanded && spec.SpendsMomentum &&
                   spec.Movement == TwinPistolMovementType.Approach && momentumSpent >= 2;
        }
    }
}
