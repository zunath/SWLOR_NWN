using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Auras for ships in space. The ground aura is an engine area-of-effect on the player, which space entry
    /// removes (Ability.ClearAurasOnSpaceEntry), so a space aura is a timer: every
    /// <see cref="SpaceRadii.AuraRefreshSeconds"/> seconds each commander's aura gives its status to the party
    /// ships within command range (the commander's own ship too) and takes it from ships that left the range or
    /// the party. An aura ends when the commander leaves space, dies or switches it off.
    ///
    /// Command range is <see cref="SpaceRadii.CommandRange"/> (30 m) plus the ground command-radius bonus scaled
    /// by <see cref="SpaceRadii.GroundToSpaceFactor"/> plus any bonus registered with
    /// <see cref="RegisterRangeBonus"/>, for example the Command Presence trait.
    /// </summary>
    public static class SpaceAuras
    {
        private sealed class SpaceAura
        {
            public Type StatusEffect { get; init; }
            public bool TargetsSelf { get; init; }
            public HashSet<uint> Recipients { get; } = new();
            public HashSet<uint> AnnouncedTo { get; } = new();
        }

        private static readonly Dictionary<uint, List<SpaceAura>> _auras = new();
        private static readonly List<Func<uint, float>> _rangeBonuses = new();

        /// <summary>Starts the refresh timer. Space.LoadSpaceSystem calls it.</summary>
        public static void ScheduleRefresh()
        {
            Scheduler.ScheduleRepeating(Refresh, TimeSpan.FromSeconds(SpaceRadii.AuraRefreshSeconds));
        }

        /// <summary>
        /// Adds meters to a ship's command range. The function gets the commander's ship and returns the bonus in
        /// space meters (0 for none); perks that widen command range register one.
        /// </summary>
        public static void RegisterRangeBonus(Func<uint, float> bonusMeters)
        {
            _rangeBonuses.Add(bonusMeters);
        }

        /// <summary>The ship's command range in meters.</summary>
        public static float GetCommandRange(uint ship)
        {
            var extra = _rangeBonuses.Sum(bonus => bonus(ship));
            return SpaceRadii.GetCommandRange(
                Stat.GetStatAdjustment(ship, StatType.LeadershipCommandRadiusBonusMeters),
                extra);
        }

        /// <summary>
        /// Switches an aura on for a commander's ship. The aura reaches party ships within command range, and the
        /// commander's own ship when <paramref name="targetsSelf"/> is set. The oldest aura is dropped when the
        /// commander already holds as many as their Social allows. Returns false when the ship is not in space.
        /// </summary>
        public static bool ApplyAura(uint source, Type statusEffect, bool targetsSelf = true)
        {
            if (!Space.IsShipInSpace(source) || statusEffect == null)
                return false;

            if (!_auras.TryGetValue(source, out var auras))
                _auras[source] = auras = new List<SpaceAura>();

            if (auras.Any(x => x.StatusEffect == statusEffect))
                return true;

            var limit = SpaceAuraRules.MaxAuras(GetAbilityScore(source, AbilityType.Social));
            while (auras.Count >= limit)
            {
                RemoveRecipients(source, auras[0]);
                auras.RemoveAt(0);
            }

            var aura = new SpaceAura { StatusEffect = statusEffect, TargetsSelf = targetsSelf };
            auras.Add(aura);
            Reconcile(source, aura);
            return true;
        }

        /// <summary>Switches one aura off and takes it from every ship that holds it. False when it was not on.</summary>
        public static bool RemoveAura(uint source, Type statusEffect)
        {
            if (!_auras.TryGetValue(source, out var auras))
                return false;

            var aura = auras.FirstOrDefault(x => x.StatusEffect == statusEffect);
            if (aura == null)
                return false;

            RemoveRecipients(source, aura);
            auras.Remove(aura);
            if (auras.Count == 0)
                _auras.Remove(source);

            return true;
        }

        /// <summary>Switches off every aura a ship projects. Leaving space calls it.</summary>
        public static void RemoveAuras(uint source)
        {
            if (!_auras.TryGetValue(source, out var auras))
                return;

            foreach (var aura in auras)
                RemoveRecipients(source, aura);

            _auras.Remove(source);
        }

        /// <summary>Whether a commander's ship projects this aura.</summary>
        public static bool HasAura(uint source, Type statusEffect)
        {
            return _auras.TryGetValue(source, out var auras) && auras.Any(x => x.StatusEffect == statusEffect);
        }

        /// <summary>Gives every aura its status to the ships in range and takes it from the ships that left.</summary>
        public static void Refresh()
        {
            foreach (var source in _auras.Keys.ToList())
            {
                if (!_auras.TryGetValue(source, out var auras))
                    continue;

                if (!Space.IsShipInSpace(source) || GetIsDead(source))
                {
                    RemoveAuras(source);
                    continue;
                }

                foreach (var aura in auras.ToList())
                {
                    try
                    {
                        Reconcile(source, aura);
                    }
                    catch (Exception ex)
                    {
                        Log.Write(LogGroup.Error, $"Space aura refresh failed: {ex}");
                    }
                }
            }
        }

        private static void Reconcile(uint source, SpaceAura aura)
        {
            var inRange = Party.GetAllPartyMembersWithinRange(source, GetCommandRange(source))
                .Where(x => Space.IsShipInSpace(x) && !GetIsDead(x) && (aura.TargetsSelf || x != source) &&
                            (x == source || !Space.IsShipHostile(source, x)));

            var (add, remove) = SpaceAuraRules.Reconcile(aura.Recipients, inRange);
            foreach (var ship in remove)
            {
                aura.Recipients.Remove(ship);
                StatusEffect.RemoveStatusEffect(ship, aura.StatusEffect, source, false);
            }

            foreach (var ship in add)
            {
                if (TryGive(source, ship, aura))
                    aura.Recipients.Add(ship);
            }
        }

        private static bool TryGive(uint source, uint ship, SpaceAura aura)
        {
            if (StatusEffect.HasStatusEffect(ship, aura.StatusEffect, source))
                return true;

            var sourceSocial = GetAbilityScore(source, AbilityType.Social);
            var others = StatusEffect.GetCreatureStatusEffects(ship)
                .GetAllEffects()
                .Where(effect => effect.GetType() == aura.StatusEffect && effect.Source != source)
                .Select(effect => GetIsObjectValid(effect.Source) ? GetAbilityScore(effect.Source, AbilityType.Social) : 0)
                .ToList();
            if (SpaceAuraRules.HasEqualOrStrongerCopy(sourceSocial, others))
                return false;

            var statusEffect = StatusEffect.CreateStatusEffect(aura.StatusEffect);
            if (statusEffect is AuraStatusEffectBase auraEffect)
                auraEffect.AnnouncesApplication = !aura.AnnouncedTo.Contains(ship);

            if (!StatusEffect.ApplyStatusEffect(source, ship, statusEffect, 0f))
                return false;

            aura.AnnouncedTo.Add(ship);
            return true;
        }

        private static void RemoveRecipients(uint source, SpaceAura aura)
        {
            foreach (var ship in aura.Recipients)
            {
                if (GetIsObjectValid(ship))
                    StatusEffect.RemoveStatusEffect(ship, aura.StatusEffect, source, false);
            }

            aura.Recipients.Clear();
        }
    }
}
