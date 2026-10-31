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
    ///
    /// A crew member who switches an aura on from a station is its user: the aura stays the ship's (one per ship, the
    /// ship is the status source, and removing it by the ship clears it), but its limit, range, strength and the
    /// strongest-copy comparison read the user, and it ends when the user leaves the seat.
    /// </summary>
    public static class SpaceAuras
    {
        private sealed class SpaceAura
        {
            public Type StatusEffect { get; init; }
            public bool TargetsSelf { get; init; }

            /// <summary>The crew member who switched it on from a station, or OBJECT_INVALID for the pilot's own.</summary>
            public uint User { get; init; } = OBJECT_INVALID;
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

        /// <summary>
        /// The ship's command range in meters, from the stats of whoever commands from it: the crew member using an
        /// ability from a station, otherwise the pilot (<see cref="SpaceCrew.GetAbilityUser"/>).
        /// </summary>
        public static float GetCommandRange(uint ship)
        {
            var extra = _rangeBonuses.Sum(bonus => bonus(ship));
            return SpaceRadii.GetCommandRange(
                Stat.GetStatAdjustment(SpaceCrew.GetAbilityUser(ship), StatType.LeadershipCommandRadiusBonusMeters),
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

            // A crew member switching it on from a station is its user, and their Social sets the limit.
            var user = SpaceCrew.GetAbilityUser(source);
            var limit = SpaceAuraRules.MaxAuras(GetAbilityScore(user, AbilityType.Social));
            while (auras.Count >= limit)
            {
                RemoveRecipients(source, auras[0]);
                auras.RemoveAt(0);
            }

            var aura = new SpaceAura
            {
                StatusEffect = statusEffect,
                TargetsSelf = targetsSelf,
                User = SpaceAuraRules.RecordedUser(source, user),
            };
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

        /// <summary>Ends the auras a crew member switched on from a ship's station. Leaving the seat calls it.</summary>
        public static void RemoveUserAuras(uint ship, uint user)
        {
            if (!_auras.TryGetValue(ship, out var auras))
                return;

            foreach (var aura in auras.Where(x => x.User == user).ToList())
                RemoveAura(ship, aura.StatusEffect);
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
                    // A station user's aura ends once they leave the seat (logged out, left the ship or the station).
                    var seated = SpaceCrew.TryGetSeat(aura.User, out var seatShip, out _) && seatShip == source;
                    if (SpaceAuraRules.UserLeft(aura.User, seated))
                    {
                        RemoveAura(source, aura.StatusEffect);
                        continue;
                    }

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
            // The refresh runs outside the ability's use, so the user's station use is set up again for it: range,
            // Social and Leadership then read the user (SpaceCrew.GetAbilityUser), while the ship stays the source.
            using var userScope = BeginUserScope(source, aura);
            // A ship whose copy of this aura is gone (a stronger commander's copy replaced it) is given it again below
            // once it can be, so it keeps the buff in this aura after the stronger one ends.
            foreach (var ship in SpaceAuraRules.LostCopies(aura.Recipients, x => StatusEffect.HasStatusEffect(x, aura.StatusEffect, source)))
                aura.Recipients.Remove(ship);

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

            var sourceSocial = GetAbilityScore(GetCommander(source, aura.StatusEffect), AbilityType.Social);
            var others = StatusEffect.GetCreatureStatusEffects(ship)
                .GetAllEffects()
                .Where(effect => effect.GetType() == aura.StatusEffect && effect.Source != source)
                .Select(effect => GetIsObjectValid(effect.Source)
                    ? GetAbilityScore(GetCommander(effect.Source, aura.StatusEffect), AbilityType.Social)
                    : 0)
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

        /// <summary>Whose stats a ship's copy of an aura carries: the aura's recorded user, otherwise the ship.</summary>
        private static uint GetCommander(uint ship, Type statusEffect)
        {
            var aura = _auras.TryGetValue(ship, out var auras) ? auras.FirstOrDefault(x => x.StatusEffect == statusEffect) : null;
            return SpaceAuraRules.Commander(ship, aura?.User ?? OBJECT_INVALID);
        }

        /// <summary>The user's station use, set up again while their aura refreshes; nothing for the pilot's own aura.</summary>
        private static SpaceCrewFireScope BeginUserScope(uint ship, SpaceAura aura)
        {
            if (SpaceAuraRules.Commander(ship, aura.User) == ship ||
                !SpaceCrew.TryGetSeat(aura.User, out var seatShip, out var seat) || seatShip != ship)
            {
                return SpaceCrew.BeginStationFire(null);
            }

            return SpaceCrew.BeginStationFire(new SpaceCrewFireContext
            {
                Ship = ship,
                CrewMember = aura.User,
                Seat = seat,
                IsAbility = true,
            });
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
