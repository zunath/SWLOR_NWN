using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// What a pilot's Ordnance perks add to a ship's launchers (plan section 6, Appendix A), ported from the ground
    /// Throwing Ordnance line: the Proximity Warheads splash on the launcher's own hits, and what an area launcher
    /// ability leaves behind (fragmentation, Exposed after repeated hits, Cluster Warheads damage and Bombardier's
    /// Rhythm). Every value is a space-only stat (<c>SpaceLauncher*</c>), so the ground Throwing stats, which pick
    /// their skill through selector stats, never mix with a pilot's ground perks.
    /// </summary>
    public static class SpaceOrdnance
    {
        /// <summary>Radius of the Proximity Warheads splash: the ground's 3 m scaled to space.</summary>
        public static readonly float SplashRadius = SpaceRadii.FromGround(3f);

        /// <summary>The target and up to four other ships (Proximity Warheads).</summary>
        public const int SplashMaximumTargets = 5;

        /// <summary>The group that identifies the Saturation Strike stats among a ship's temporary modifiers.</summary>
        public const string FragmentationZoneGroup = "SpaceSaturationStrikeZones";

        private const int StaleSequenceMinutes = 10;

        private static readonly Dictionary<(uint Attacker, uint Target), SpaceHitSequenceState> _hitSequences = new();

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void Subscribe()
        {
            SpaceCombatEvents.ShotLanded -= OnShotLanded;
            SpaceCombatEvents.ShotLanded += OnShotLanded;
        }

        // Proximity Warheads

        private static void OnShotLanded(SpaceShotEvent e)
        {
            // The ground splash belongs to the weapon's auto-attacks, so only the launcher's own cycles trigger it.
            if (e.Family != SpaceWeaponFamily.Launcher || e.FromAbility || !GetIsObjectValid(e.Attacker))
                return;

            // A crew member's launcher shot reads their own Proximity Warheads perk; the ship deals the splash.
            var chance = Stat.GetStatAdjustment(e.StatSource, StatType.SpaceLauncherSplashChance);
            var damage = Stat.GetStatAdjustment(e.StatSource, StatType.SpaceLauncherSplashDamage);
            if (chance <= 0 || damage <= 0 || Random.D100(1) > chance)
                return;

            ApplySplash(e.Attacker, e.Target, damage);
        }

        private static void ApplySplash(uint attacker, uint target, int damage)
        {
            var targets = new List<uint>();
            if (GetIsObjectValid(target) && !GetIsDead(target))
                targets.Add(target);

            targets.AddRange(Space.GetEnemyShipsNear(attacker, GetLocation(target), SplashRadius)
                .Where(x => x != target)
                .Take(SplashMaximumTargets - 1));

            foreach (var ship in targets)
                ApplyTriggeredDamage(attacker, ship, damage);
        }

        // Area launcher abilities

        /// <summary>
        /// Runs what an area launcher ability's perks add once its shots have landed: fragmentation on every ship it
        /// hit, the Exposed sequence, the Cluster Warheads hit and Bombardier's Rhythm.
        /// </summary>
        public static void OnAreaAbilityResolved(uint activator, IEnumerable<SpaceFireResult> results)
        {
            var hits = results
                .Where(x => x.IsHit && GetIsObjectValid(x.Target))
                .Select(x => x.Target)
                .Distinct()
                .ToList();
            if (hits.Count == 0)
                return;

            ApplyFragmentation(activator, hits);
            ApplyHitSequence(activator, hits);
            ApplyCluster(activator, hits);
            ApplyMinimumTargetsStamina(activator, hits.Count);
        }

        private static void ApplyFragmentation(uint activator, IEnumerable<uint> hits)
        {
            var damage = Stat.GetStatAdjustment(activator, StatType.SpaceLauncherAreaFragmentationDamage);
            var pulse = Stat.GetStatAdjustment(activator, StatType.SpaceLauncherAreaFragmentationPulseSeconds);
            var duration = Stat.GetStatAdjustment(activator, StatType.SpaceLauncherAreaFragmentationDurationSeconds);
            if (damage <= 0 || pulse <= 0 || duration <= 0)
                return;

            foreach (var ship in hits)
                Space.ApplyStatusToShip(activator, ship, new FragmentationStatusEffect(damage, pulse), duration);
        }

        private static void ApplyHitSequence(uint activator, IEnumerable<uint> hits)
        {
            var required = Stat.GetStatAdjustment(activator, StatType.SpaceLauncherAreaHitSequenceCountRequired);
            var window = Stat.GetStatAdjustment(activator, StatType.SpaceLauncherAreaHitSequenceWindowSeconds);
            var exposedSeconds = Stat.GetStatAdjustment(activator, StatType.SpaceLauncherAreaHitSequenceExposedDurationSeconds);
            if (required <= 0 || window <= 0 || exposedSeconds <= 0)
                return;

            var now = DateTime.UtcNow;
            PruneHitSequences(now);
            foreach (var ship in hits)
            {
                var key = (activator, ship);
                _hitSequences.TryGetValue(key, out var current);
                var count = SpaceHitSequences.NextCount(current, now, window);
                if (SpaceHitSequences.IsTriggered(count, required))
                {
                    _hitSequences.Remove(key);
                    Space.ApplyStatusToShip(activator, ship, typeof(ExposedStatusEffect), exposedSeconds);
                    continue;
                }

                _hitSequences[key] = new SpaceHitSequenceState { Count = count, LastHit = now };
            }
        }

        private static void ApplyCluster(uint activator, IReadOnlyList<uint> hits)
        {
            var damage = Stat.GetStatAdjustment(activator, StatType.SpaceLauncherAreaClusterDamageBonus);
            var maximumTargets = Stat.GetStatAdjustment(activator, StatType.SpaceLauncherAreaClusterMaximumTargets);
            if (damage <= 0 || maximumTargets <= 0)
                return;

            // The extra hit goes to a ship near the first one hit, preferring a ship the ability did not reach.
            var reference = hits[0];
            var nearby = Space.GetEnemyShipsNear(activator, GetLocation(reference), SpaceRadii.NearTarget)
                .Where(x => x != reference)
                .ToList();
            foreach (var ship in nearby.Where(x => !hits.Contains(x)).Concat(nearby.Where(hits.Contains)).Take(maximumTargets))
                ApplyTriggeredDamage(activator, ship, damage);
        }

        private static void ApplyMinimumTargetsStamina(uint activator, int enemiesHit)
        {
            var threshold = Stat.GetStatAdjustment(activator, StatType.SpaceLauncherAreaMinTargetsStaminaRestoreThreshold);
            var restore = Stat.GetStatAdjustment(activator, StatType.SpaceLauncherAreaMinTargetsStaminaRestore);
            if (threshold <= 0 || restore <= 0 || enemiesHit < threshold)
                return;

            var ability = Ability.GetActiveAbilityImpactSummary(activator)?.Ability;
            Combat.RestoreAbilityHitStamina(activator, ability, restore, sendFeedback: false);
        }

        // Saturation Strike

        /// <summary>
        /// Starts the fragmentation zones of Saturation Strike: for <paramref name="zoneSeconds"/>, the pilot's area
        /// launcher abilities add this fragmentation to the Shrapnel Warheads' (the stats add, as the ground's do).
        /// </summary>
        public static void StartFragmentationZones(
            uint activator,
            int damage,
            int pulseSeconds,
            int durationSeconds,
            int zoneSeconds)
        {
            TemporaryStatModifier.Replace(
                activator, StatType.SpaceLauncherAreaFragmentationDamage, damage, zoneSeconds, FragmentationZoneGroup);
            TemporaryStatModifier.Replace(
                activator, StatType.SpaceLauncherAreaFragmentationPulseSeconds, pulseSeconds, zoneSeconds, FragmentationZoneGroup);
            TemporaryStatModifier.Replace(
                activator, StatType.SpaceLauncherAreaFragmentationDurationSeconds, durationSeconds, zoneSeconds, FragmentationZoneGroup);
        }

        /// <summary>Forgets a pilot's hit counts and zones when they leave space, so none follow them onto the ground.</summary>
        public static void Clear(uint ship)
        {
            foreach (var key in _hitSequences.Keys.Where(x => x.Attacker == ship || x.Target == ship).ToList())
                _hitSequences.Remove(key);

            foreach (var stat in new[]
                     {
                         StatType.SpaceLauncherAreaFragmentationDamage,
                         StatType.SpaceLauncherAreaFragmentationPulseSeconds,
                         StatType.SpaceLauncherAreaFragmentationDurationSeconds
                     })
            {
                TemporaryStatModifier.Consume(ship, stat, FragmentationZoneGroup);
            }
        }

        private static void PruneHitSequences(DateTime now)
        {
            foreach (var key in _hitSequences
                         .Where(x => (now - x.Value.LastHit).TotalMinutes > StaleSequenceMinutes)
                         .Select(x => x.Key)
                         .ToList())
            {
                _hitSequences.Remove(key);
            }
        }

        /// <summary>
        /// A flat instance of damage a perk triggers on a ship: it goes to the ship's shield and hull and adds
        /// threat for the pilot, and it raises no on-hit events.
        /// </summary>
        private static void ApplyTriggeredDamage(uint attacker, uint ship, int damage)
        {
            if (damage > 0 && GetIsObjectValid(ship) && !GetIsDead(ship))
                Space.TryApplyStatusDamage(attacker, ship, damage);
        }
    }
}
