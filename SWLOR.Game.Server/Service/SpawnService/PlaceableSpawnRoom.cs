namespace SWLOR.Game.Server.Service.SpawnService
{
    /// <summary>
    /// Decides whether a randomly placed placeable (resource node, terminal) can stand at a point without blocking
    /// the way. A ring of points around the spawn point is probed with straight walkable lines: the ground the
    /// node would leave free must stay one connected stretch, so a node never fills a passage, a corridor mouth, or
    /// the gap between two obstacles.
    /// </summary>
    public static class PlaceableSpawnRoom
    {
        /// <summary>
        /// Horizontal radius, in meters, a randomly placed node may block. Resource node and crate walkmeshes are
        /// about 1.94 m squares turned to a random facing, which reach up to 1.41 m from their center.
        /// </summary>
        public const float NodeRadius = 1.5f;

        /// <summary>
        /// Width, in meters, of the walking lane that must stay open between the node and the nearest wall.
        /// </summary>
        public const float WalkingLane = 1.5f;

        /// <summary>
        /// Radius of the probed ring: a creature walking around the node follows it.
        /// </summary>
        public const float RingRadius = NodeRadius + WalkingLane;

        /// <summary>
        /// Number of points on the ring. Neighbors are 1.17 m apart at the ring radius.
        /// </summary>
        public const int RingPoints = 16;

        /// <summary>
        /// Returns true when a node at the point leaves the surrounding ground connected: the ring points reachable
        /// from the center in a straight line form a single stretch, each joined to the next by a clear chord.
        /// Two or more separate stretches mean the node would sit in a passage and cut them apart. A point with no
        /// reachable ring point is a pocket and is refused as well.
        /// </summary>
        /// <param name="x">Spawn point X.</param>
        /// <param name="y">Spawn point Y.</param>
        /// <param name="isClear">Returns true when a creature can walk the straight segment (fromX, fromY) to (toX, toY).</param>
        public static bool LeavesWalkingRoom(float x, float y, Func<float, float, float, float, bool> isClear)
        {
            var ringX = new float[RingPoints];
            var ringY = new float[RingPoints];
            var reachable = new bool[RingPoints];
            var anyReachable = false;
            for (var index = 0; index < RingPoints; index++)
            {
                var angle = index * 2f * MathF.PI / RingPoints;
                ringX[index] = x + RingRadius * MathF.Cos(angle);
                ringY[index] = y + RingRadius * MathF.Sin(angle);
                reachable[index] = isClear(x, y, ringX[index], ringY[index]);
                anyReachable |= reachable[index];
            }

            if (!anyReachable)
                return false;

            // Count the stretches: a reachable point starts one when the step from its predecessor is not walkable.
            var stretchStarts = 0;
            for (var index = 0; index < RingPoints; index++)
            {
                if (!reachable[index])
                    continue;

                var previous = (index + RingPoints - 1) % RingPoints;
                var joined = reachable[previous] &&
                             isClear(ringX[previous], ringY[previous], ringX[index], ringY[index]);
                if (!joined)
                    stretchStarts++;
            }

            // No starts means the whole ring is one closed loop.
            return stretchStarts <= 1;
        }
    }
}
