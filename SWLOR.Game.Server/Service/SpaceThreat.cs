using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using System.Linq;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Threat rules in space (plan, shared rules for every space style). Threat for weapon damage is in
    /// <see cref="CompleteWeaponShot"/>; this partial holds repair threat and the goad.
    /// </summary>
    public static partial class Space
    {
        /// <summary>
        /// Repairs generate threat like ground healing: half of what was actually restored, added for the
        /// repairer to every enemy that holds the repaired ship on its threat table.
        /// </summary>
        /// <param name="source">The ship doing the repair.</param>
        /// <param name="repaired">The ship that was repaired (it may be the source).</param>
        /// <param name="restored">Points actually restored, after the pool's maximum.</param>
        public static void ApplyRepairEnmity(uint source, uint repaired, int restored)
        {
            RecordIncursionRepair(source, repaired, restored);
            var amount = SpaceCombatMath.RepairEnmity(restored);
            if (amount <= 0 || !GetIsObjectValid(source) || !GetIsObjectValid(repaired))
                return;

            foreach (var enemy in Enmity.GetEnmityTowardsAllEnemies(repaired).Keys)
            {
                if (Enmity.HasNonProximityEnmity(repaired, enemy))
                    Enmity.ModifyEnmity(source, enemy, amount);
            }
        }

        /// <summary>
        /// Goads one enemy ship into attacking the source: its threat catches up to the highest visible rival plus
        /// <paramref name="bonus"/> (stat-adjusted), once, with no target lock (<see cref="Enmity.TryTaunt"/>).
        /// Fails when the target is not an enemy ship in space.
        /// </summary>
        public static bool Goad(uint source, uint target, int bonus)
        {
            if (!IsShipInSpace(source) || !IsShipInSpace(target) || !IsShipEnemy(source, target))
                return false;

            return Enmity.TryTaunt(source, target, bonus);
        }

        /// <summary>
        /// Goads a target and every other enemy ship within <paramref name="radius"/> meters of it, up to
        /// <paramref name="maximum"/> ships in all. Returns the ships that were goaded, the target first.
        /// </summary>
        public static List<uint> GoadArea(uint source, uint target, float radius, int maximum, int bonus)
        {
            var goaded = new List<uint>();
            if (!GetIsObjectValid(target) || maximum <= 0)
                return goaded;

            if (Goad(source, target, bonus))
                goaded.Add(target);

            foreach (var nearby in GetEnemyShipsNear(source, GetLocation(target), radius, maximum))
            {
                if (goaded.Count >= maximum)
                    break;

                if (nearby != target && Goad(source, nearby, bonus))
                    goaded.Add(nearby);
            }

            return goaded;
        }

        /// <summary>
        /// The living enemy ships of <paramref name="source"/> within a radius of a point, nearest to the point first.
        /// </summary>
        public static List<uint> GetEnemyShipsNear(uint source, Location center, float radius, int maximum = int.MaxValue)
        {
            var ships = new List<uint>();
            foreach (var nearby in ObjectSnapshot.InShape(Shape.Sphere, radius, center, true, ObjectType.Creature))
            {
                if (!GetIsObjectValid(nearby) || nearby == source || GetIsDead(nearby) ||
                    !IsShipEnemy(source, nearby) || !IsShipInSpace(nearby))
                {
                    continue;
                }

                ships.Add(nearby);
            }

            return ships
                .OrderBy(x => GetDistanceBetweenLocations(center, GetLocation(x)))
                .Take(maximum)
                .ToList();
        }
    }
}
