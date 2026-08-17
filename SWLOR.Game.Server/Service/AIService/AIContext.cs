using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.AIService
{
    public sealed class AIContext
    {
        private uint _currentEnmityTarget;
        private bool _currentEnmityTargetLoaded;
        private int? _selfHealthPercent;

        public uint Self { get; }
        public AITriggerType Trigger { get; }
        public uint EventTarget { get; }
        public AIProfile Profile { get; }
        public AIState State { get; }
        public IReadOnlyList<uint> Allies { get; }
        public uint EvaluatedTarget { get; private set; }

        public AIContext(
            uint self,
            AITriggerType trigger,
            uint eventTarget,
            AIProfile profile,
            AIState state,
            IReadOnlyList<uint> allies)
        {
            Self = self;
            Trigger = trigger;
            EventTarget = eventTarget;
            Profile = profile;
            State = state;
            Allies = allies ?? new List<uint>();
            EvaluatedTarget = OBJECT_INVALID;
        }

        public uint CurrentEnmityTarget
        {
            get
            {
                if (_currentEnmityTargetLoaded)
                    return _currentEnmityTarget;

                _currentEnmityTargetLoaded = true;
                _currentEnmityTarget = Enmity.GetHighestEnmityTarget(Self);

                return _currentEnmityTarget;
            }
        }

        public uint Master => GetMaster(Self);

        public int SelfHealthPercent => _selfHealthPercent ??= GetHealthPercent(Self);

        public int TargetHealthPercent => GetIsObjectValid(EvaluatedTarget)
            ? GetHealthPercent(EvaluatedTarget)
            : 100;

        public bool IsOutsideHomeRadius
        {
            get
            {
                var home = HomeLocation;
                return GetIsObjectValid(GetAreaFromLocation(home)) &&
                       (GetAreaFromLocation(home) != GetArea(Self) ||
                        GetDistanceBetweenLocations(GetLocation(Self), home) > 15f);
            }
        }

        public Location HomeLocation => GetLocalLocation(Self, "HOME_LOCATION");

        public float DistanceToMaster => GetIsObjectValid(Master)
            ? GetDistanceBetween(Self, Master)
            : 999f;

        public float ElapsedCombatSeconds => State.CombatStartedTime == default
            ? 0f
            : (float)(DateTime.UtcNow - State.CombatStartedTime).TotalSeconds;

        public bool HasFeat(SWLOR.NWN.API.NWScript.Enum.FeatType feat)
        {
            return GetHasFeat(feat, Self);
        }

        public void SetEvaluatedTarget(uint target)
        {
            EvaluatedTarget = target;
        }

        public int CountHostilesNearTarget(float radius)
        {
            var origin = GetIsObjectValid(EvaluatedTarget)
                ? GetLocation(EvaluatedTarget)
                : GetLocation(Self);

            var count = 0;
            var creature = GetFirstObjectInShape(Shape.Sphere, radius, origin, true, ObjectType.Creature);
            while (GetIsObjectValid(creature))
            {
                if (creature != Self &&
                    GetIsEnemy(creature, Self) &&
                    GetCurrentHitPoints(creature) > 0)
                {
                    count++;
                }

                creature = GetNextObjectInShape(Shape.Sphere, radius, origin, true, ObjectType.Creature);
            }

            return count;
        }

        public int CountHostilesInAbilityArea(AbilityDetail ability)
        {
            if (ability?.Targeting == null)
                return CountHostilesNearTarget(ability?.MaxRange ?? 0f);

            var target = GetIsObjectValid(EvaluatedTarget)
                ? EvaluatedTarget
                : Self;

            return Ability.GetHostileCreaturesInTargetingArea(
                    Self,
                    target,
                    GetLocation(target),
                    ability.Targeting)
                .Count;
        }

        public uint GetLowestHealthAlly(bool includeSelf, float maxRange)
        {
            var candidates = Allies
                .Where(GetIsObjectValid)
                .Where(x => GetCurrentHitPoints(x) > 0)
                .ToList();

            if (includeSelf && !candidates.Contains(Self))
                candidates.Add(Self);

            var master = Master;
            if (GetIsObjectValid(master) && !candidates.Contains(master))
                candidates.Add(master);

            var best = OBJECT_INVALID;
            var bestHealth = 101;

            foreach (var candidate in candidates)
            {
                if (candidate != Self && maxRange > 0f && GetDistanceBetween(Self, candidate) > maxRange)
                    continue;

                var health = GetHealthPercent(candidate);
                if (health < bestHealth)
                {
                    best = candidate;
                    bestHealth = health;
                }
            }

            return best;
        }

        private static int GetHealthPercent(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return 100;

            var max = Math.Max(1, GetMaxHitPoints(creature));
            var current = Math.Max(0, GetCurrentHitPoints(creature));
            return current * 100 / max;
        }
    }
}
