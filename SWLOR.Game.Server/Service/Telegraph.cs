using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.TelegraphService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public static class Telegraph
    {
        public const int MaxRenderCount = 16;
        private const int TelegraphSizeScale = 10;
        private const int MaxPackedTelegraphSize = 255;
        private const int MaxPackedTelegraphRotation = 1023;
        private const float TwoPi = (float)(Math.PI * 2.0);

        private static readonly Dictionary<uint, Dictionary<string, ActiveTelegraph>> _telegraphsByArea = new();
        private static readonly Dictionary<string, ActiveTelegraph> _allTelegraphs = new();

        /// <summary>
        /// Creates a new telegraph effect at the specified location.
        /// </summary>
        /// <param name="creator">The creature creating the telegraph</param>
        /// <param name="position">Position of the telegraph</param>
        /// <param name="rotation">Rotation of the telegraph in radians</param>
        /// <param name="size">Size of the telegraph (X = length/radius, Y = width for cone/line)</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="isHostile">Whether this telegraph is hostile</param>
        /// <param name="type">Shape type of the telegraph</param>
        /// <param name="action">Action to execute when telegraph completes</param>
        /// <returns>Unique ID for the telegraph</returns>
        public static string CreateTelegraph(
            uint creator,
            Vector3 position,
            float rotation,
            Vector2 size,
            float duration,
            bool isHostile,
            TelegraphType type,
            ApplyTelegraphEffect action,
            bool isPersistentAreaIndicator = false)
        {
            var data = new TelegraphData
            {
                Creator = creator,
                Shape = type,
                Position = position,
                Rotation = rotation,
                Size = size,
                Duration = duration,
                IsHostile = isHostile,
                IsPersistentAreaIndicator = isPersistentAreaIndicator,
                Action = action
            };

            return RunTelegraphEffect(creator, data);
        }

        /// <summary>
        /// Cancels a telegraph effect before it completes.
        /// </summary>
        /// <param name="telegraphId">ID of the telegraph to cancel</param>
        public static void CancelTelegraph(string telegraphId)
        {
            if (!_allTelegraphs.ContainsKey(telegraphId))
                return;

            var telegraph = _allTelegraphs[telegraphId];

            if (_telegraphsByArea.ContainsKey(telegraph.Area) && _telegraphsByArea[telegraph.Area].ContainsKey(telegraphId))
            {
                _telegraphsByArea[telegraph.Area].Remove(telegraphId);
            }

            _allTelegraphs.Remove(telegraphId);
            UpdateShadersForArea(telegraph.Area);
            // RemoveEffectByLinkId is not available in SWLOR, effects are removed automatically
        }

        /// <summary>
        /// Gets all active telegraphs in a specific area.
        /// </summary>
        /// <param name="area">Area to check</param>
        /// <returns>Dictionary of telegraph IDs to telegraph data</returns>
        public static Dictionary<string, ActiveTelegraph> GetTelegraphsInArea(uint area)
        {
            return _telegraphsByArea.ContainsKey(area)
                ? new Dictionary<string, ActiveTelegraph>(_telegraphsByArea[area])
                : new Dictionary<string, ActiveTelegraph>();
        }

        public static IReadOnlyList<TelegraphGeometry> CaptureGeometry(IEnumerable<string> telegraphIds)
        {
            return telegraphIds
                .Where(_allTelegraphs.ContainsKey)
                .Select(id => _allTelegraphs[id])
                .Select(telegraph => new TelegraphGeometry(
                    telegraph.Area,
                    telegraph.Data.Shape,
                    telegraph.Data.Position,
                    telegraph.Data.Size,
                    telegraph.Data.Rotation))
                .ToArray();
        }

        public static bool ShouldShowImpactFlash(
            TelegraphGeometry impact,
            IReadOnlyList<TelegraphGeometry> activationTelegraphs)
        {
            return activationTelegraphs == null || !activationTelegraphs.Any(impact.Matches);
        }

        /// <summary>
        /// Checks if a creature is within a telegraph's area of effect.
        /// </summary>
        /// <param name="creature">Creature to check</param>
        /// <param name="telegraphId">ID of the telegraph</param>
        /// <returns>True if creature is within the telegraph</returns>
        public static bool IsCreatureInTelegraph(uint creature, string telegraphId)
        {
            if (!_allTelegraphs.ContainsKey(telegraphId))
                return false;

            var telegraph = _allTelegraphs[telegraphId];
            return GetArea(creature) == telegraph.Area &&
                   IsInTelegraph(creature, telegraph.Data);
        }

        private static string RunTelegraphEffect(uint telegrapher, TelegraphData data)
        {
            var area = GetArea(telegrapher);
            if (!_telegraphsByArea.ContainsKey(area))
                _telegraphsByArea[area] = new Dictionary<string, ActiveTelegraph>();

            var effect = EffectRunScript(
                ScriptName.TelegraphEffect,
                ScriptName.TelegraphEffect,
                string.Empty);

            OnApply(telegrapher, data, effect);
            ApplyEffectToObject(DurationType.Temporary, effect, telegrapher, data.Duration);
            UpdateShadersForArea(area);

            return GetEffectLinkId(effect);
        }

        private static void OnApply(uint telegrapher, TelegraphData data, Effect effect)
        {
            var area = GetArea(telegrapher);
            var telegraphId = GetEffectLinkId(effect);

            var start = GetMicrosecondCounter();
            var end = (int)(start + data.Duration * 1000 * 1000);
            var telegraph = new ActiveTelegraph(area, start, end, data);

            if (!_telegraphsByArea.ContainsKey(area))
                _telegraphsByArea[area] = new Dictionary<string, ActiveTelegraph>();

            _telegraphsByArea[area][telegraphId] = telegraph;
            _allTelegraphs[telegraphId] = telegraph;
        }

        public static void OnRemoved(uint telegrapher, string telegraphId)
        {
            // Resolve the area from the telegraph's own record rather than the telegrapher's current
            // area. A creature that moved (or zoned) between creation and expiry would otherwise miss
            // the lookup, leaking the entry and leaving a stale shape rendered for everyone in it.
            if (!_allTelegraphs.TryGetValue(telegraphId, out var telegraph))
                return;

            var area = telegraph.Area;

            if (!_telegraphsByArea.ContainsKey(area))
                return;

            if (!_telegraphsByArea[area].ContainsKey(telegraphId))
                return;

            RunTelegraphAction(area, telegraph.Data);

            _telegraphsByArea[area].Remove(telegraphId);
            _allTelegraphs.Remove(telegraphId);
            UpdateShadersForArea(area);
        }

        [NWNEventHandler(ScriptName.TelegraphEffect)]
        public static void OnTelegraphEffect()
        {
            var scriptType = (RunScriptEffectScriptType)GetLastRunScriptEffectScriptType();
            if (scriptType != RunScriptEffectScriptType.OnRemoved)
                return;

            var effect = GetLastRunScriptEffect();
            OnRemoved(OBJECT_SELF, GetEffectLinkId(effect));
        }

        private static void RunTelegraphAction(uint area, TelegraphData data)
        {
            var action = data.Action;
            if (action != null)
            {
                var maxDistance = CalculateMaxCreatureDistance(data.Shape, data.Size);
                var creatureList = new List<uint>();
                var candidates = GetAliveCreaturesInArea(area)
                    .Select(creature => new
                    {
                        Creature = creature,
                        Position = GetPosition(creature)
                    })
                    .Where(candidate => GetHorizontalDistance(candidate.Position, data.Position) <= maxDistance)
                    .OrderBy(candidate => GetHorizontalDistance(candidate.Position, data.Position));

                foreach (var candidate in candidates)
                {
                    if (IsPositionInTelegraph(candidate.Position, data))
                    {
                        creatureList.Add(candidate.Creature);
                    }
                }

                action(data.Creator, creatureList);
            }
        }

        private static float CalculateMaxCreatureDistance(TelegraphType shape, Vector2 size)
        {
            switch (shape)
            {
                case TelegraphType.None:
                    return 0f;
                case TelegraphType.Sphere:
                    return size.X; // Sphere radius
                case TelegraphType.Cone:
                    return size.X; // Cone length
                case TelegraphType.Line:
                    var halfWidth = size.Y * 0.5f;
                    return (float)Math.Sqrt(size.X * size.X + halfWidth * halfWidth);
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }
        }

        private static bool IsInTelegraph(uint creature, TelegraphData data)
        {
            return IsPositionInTelegraph(GetPosition(creature), data);
        }

        private static bool IsPositionInTelegraph(Vector3 position, TelegraphData data)
        {
            switch (data.Shape)
            {
                case TelegraphType.Sphere:
                    return IsPositionInSphere(position, data);
                case TelegraphType.Cone:
                    return IsPositionInCone(position, data);
                case TelegraphType.Line:
                    return IsPositionInLine(position, data);
                default:
                    return false;
            }
        }

        private static bool IsPositionInSphere(Vector3 position, TelegraphData data)
        {
            var radius = data.Size.X;
            var distance = GetHorizontalDistance(position, data.Position);
            return distance <= radius;
        }

        private static bool IsPositionInCone(Vector3 position, TelegraphData data)
        {
            var directionX = (float)Math.Cos(data.Rotation);
            var directionY = (float)Math.Sin(data.Rotation);

            var toPointX = position.X - data.Position.X;
            var toPointY = position.Y - data.Position.Y;
            var distance = GetHorizontalDistance(position, data.Position);
            if (distance <= 0.01f)
                return true;

            // Compute the actual cone angle dynamically
            var halfAngle = (float)Math.Atan(data.Size.Y * 0.5f / data.Size.X);

            // Angle between the direction and the point
            var dotProduct = toPointX * directionX + toPointY * directionY;
            var cosAngle = Math.Clamp(dotProduct / distance, -1f, 1f);
            var angleBetween = (float)Math.Acos(cosAngle);

            return distance <= data.Size.X && angleBetween <= halfAngle;
        }

        private static bool IsPositionInLine(Vector3 position, TelegraphData data)
        {
            var toPoint = position - data.Position;

            // Compute rotated position relative to the telegraph's orientation
            var rotatedPos = new Vector2(
                toPoint.X * (float)Math.Cos(-data.Rotation) - toPoint.Y * (float)Math.Sin(-data.Rotation),
                toPoint.X * (float)Math.Sin(-data.Rotation) + toPoint.Y * (float)Math.Cos(-data.Rotation)
            );

            var distAlongLine = rotatedPos.X;
            var distFromCenter = (float)Math.Abs(rotatedPos.Y);

            return distAlongLine >= 0f && distAlongLine <= data.Size.X // Within length
                   && distFromCenter <= data.Size.Y * 0.5f; // Within width
        }

        private static float GetHorizontalDistance(Vector3 position, Vector3 origin)
        {
            var x = position.X - origin.X;
            var y = position.Y - origin.Y;

            return (float)Math.Sqrt(x * x + y * y);
        }

        private static IEnumerable<uint> GetAliveCreaturesInArea(uint area)
        {
            if (!GetIsObjectValid(area))
                yield break;

            for (var creature = GetFirstObjectInArea(area, ObjectType.Creature);
                 GetIsObjectValid(creature);
                 creature = GetNextObjectInArea(area, ObjectType.Creature))
            {
                if (!GetIsDead(creature) && GetCurrentHitPoints(creature) > 0)
                    yield return creature;
            }
        }

        private static float DegreesToRadians(float degrees)
        {
            return degrees * ((float)Math.PI / 180f);
        }

        private static TelegraphColorType DetermineTelegraphColorType(uint player, uint telegrapher, bool isHostile)
        {
            var isOwnTelegraph = player == telegrapher;
            var isPartyMemberTelegraph = !isOwnTelegraph && Party.IsInParty(player, telegrapher);

            return SelectTelegraphColorType(isOwnTelegraph, isPartyMemberTelegraph, isHostile);
        }

        private static TelegraphColorType SelectTelegraphColorType(
            bool isOwnTelegraph,
            bool isPartyMemberTelegraph,
            bool isHostile)
        {
            // The player's own placement stays recognizable, while beneficial effects remain green
            // regardless of who created them. Party gray distinguishes allied offensive placement
            // from hostile red telegraphs created outside the party.
            if (isOwnTelegraph)
                return TelegraphColorType.Self;

            if (!isHostile)
                return TelegraphColorType.Beneficial;

            if (isPartyMemberTelegraph)
                return TelegraphColorType.PartyMember;

            return TelegraphColorType.Hostile;
        }

        private static int PackTelegraphData(TelegraphType shapeType, TelegraphColorType colorType, Vector2 size, float rotation)
        {
            var sizeX = Math.Clamp((int)Math.Round(size.X * TelegraphSizeScale), 0, MaxPackedTelegraphSize);
            var sizeY = Math.Clamp((int)Math.Round(size.Y * TelegraphSizeScale), 0, MaxPackedTelegraphSize);
            var normalizedRotation = NormalizeRotation(rotation);
            var packedRotation = Math.Clamp((int)Math.Round(normalizedRotation / TwoPi * MaxPackedTelegraphRotation), 0, MaxPackedTelegraphRotation);

            return ((int)shapeType & 0x7) |
                   (((int)colorType & 0x3) << 3) |
                   (sizeX << 5) |
                   (sizeY << 13) |
                   (packedRotation << 21);
        }

        private static float NormalizeRotation(float rotation)
        {
            var normalized = rotation % TwoPi;
            return normalized < 0f
                ? normalized + TwoPi
                : normalized;
        }

        /// <summary>
        /// Clears all telegraphs from memory. Used for cleanup.
        /// </summary>
        public static void ClearAllTelegraphs()
        {
            _telegraphsByArea.Clear();
            _allTelegraphs.Clear();
            UpdateShadersForAllPlayers();
        }

        /// <summary>
        /// Updates shader uniforms for all players to display telegraphs.
        /// </summary>
        public static void UpdateShadersForAllPlayers()
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                UpdateShaderForPlayer(player);
            }
        }

        /// <summary>
        /// Refreshes active telegraphs for specific player viewers after relationship state changes.
        /// Party membership affects hostile telegraph colors, so existing packed uniforms must be
        /// rebuilt even when no telegraph was created or removed.
        /// </summary>
        /// <param name="players">Players whose telegraph uniforms should be refreshed.</param>
        public static void UpdateShadersForPlayers(IEnumerable<uint> players)
        {
            foreach (var player in players.Distinct())
            {
                if (!GetIsObjectValid(player) || (!GetIsPC(player) && !GetIsDM(player)))
                    continue;

                UpdateShaderForPlayer(player);
            }
        }

        /// <summary>
        /// Updates shader uniforms only for players standing in the given area. Telegraph shader slots
        /// are per-area, so a telegraph created or removed in one area cannot affect players elsewhere.
        /// Instant abilities now flash their shape on every use, so this runs far more often than the
        /// original create/destroy-only path and must not touch every player on the server.
        /// </summary>
        private static void UpdateShadersForArea(uint area)
        {
            if (!GetIsObjectValid(area))
                return;

            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetArea(player) == area)
                    UpdateShaderForPlayer(player);
            }
        }

        /// <summary>
        /// Pushes the current telegraph state to a player entering an area. Without this, a player who
        /// zones in (or logs in) while a telegraph is already running sees nothing until some unrelated
        /// telegraph happens to be created or removed in that same area.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void OnAreaEnter()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) && !GetIsDM(player))
                return;

            UpdateShaderForPlayer(player);
        }

        /// <summary>
        /// Updates shader uniforms for a specific player to display telegraphs.
        /// </summary>
        /// <param name="player">Player to update shaders for</param>
        private static void UpdateShaderForPlayer(uint player)
        {
            var area = GetArea(player);
            if (!_telegraphsByArea.ContainsKey(area))
            {
                ResetTelegraphShaderSlots(player, 0);
                return;
            }

            var telegraphs = SelectTelegraphsForRendering(_telegraphsByArea[area].Values);

            var i = 0;
            foreach (var telegraph in telegraphs)
            {
                var data = telegraph.Data;
                var position = data.Position;
                var size = data.Size;

                var colorType = DetermineTelegraphColorType(player, data.Creator, data.IsHostile);
                var packed = PackTelegraphData(data.Shape, colorType, size, data.Rotation);

                SetShaderUniformInt(
                    player,
                    ShaderUniformType.Type1 + i,
                    packed);

                SetShaderUniformVec(
                    player,
                    ShaderUniformType.Type1 + i,
                    position.X,
                    position.Y,
                    position.Z,
                    0f);

                i++;
            }

            ResetTelegraphShaderSlots(player, telegraphs.Length);
        }

        private static ActiveTelegraph[] SelectTelegraphsForRendering(IEnumerable<ActiveTelegraph> telegraphs)
        {
            return telegraphs
                .OrderBy(telegraph => telegraph.Data.IsPersistentAreaIndicator)
                .Take(MaxRenderCount)
                .ToArray();
        }

        private static void ResetTelegraphShaderSlots(uint player, int startIndex)
        {
            for (var x = startIndex; x < MaxRenderCount; ++x)
            {
                var uniformIndex = ShaderUniformType.Type1 + x;
                SetShaderUniformInt(player, uniformIndex, (int)TelegraphType.None);
            }
        }

        /// <summary>
        /// Creates a simple sphere telegraph.
        /// </summary>
        /// <param name="creator">Creature creating the telegraph</param>
        /// <param name="position">Center position</param>
        /// <param name="radius">Radius of the sphere</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="isHostile">Whether this telegraph is hostile</param>
        /// <param name="action">Action to execute when telegraph completes</param>
        /// <returns>Telegraph ID</returns>
        public static string CreateSphereTelegraph(
            uint creator,
            Vector3 position,
            float radius,
            float duration,
            bool isHostile,
            ApplyTelegraphEffect action,
            bool isPersistentAreaIndicator = false)
        {
            return CreateTelegraph(
                creator,
                position,
                0f,
                new Vector2(radius, radius),
                duration,
                isHostile,
                TelegraphType.Sphere,
                action,
                isPersistentAreaIndicator);
        }

        /// <summary>
        /// Creates a cone telegraph.
        /// </summary>
        /// <param name="creator">Creature creating the telegraph</param>
        /// <param name="position">Base position of the cone</param>
        /// <param name="rotation">Direction the cone faces (in radians)</param>
        /// <param name="length">Length of the cone</param>
        /// <param name="width">Width of the cone at the end</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="isHostile">Whether this telegraph is hostile</param>
        /// <param name="action">Action to execute when telegraph completes</param>
        /// <returns>Telegraph ID</returns>
        public static string CreateConeTelegraph(
            uint creator,
            Vector3 position,
            float rotation,
            float length,
            float width,
            float duration,
            bool isHostile,
            ApplyTelegraphEffect action)
        {
            return CreateTelegraph(
                creator,
                position,
                rotation,
                new Vector2(length, width),
                duration,
                isHostile,
                TelegraphType.Cone,
                action);
        }

        /// <summary>
        /// Creates a line telegraph.
        /// </summary>
        /// <param name="creator">Creature creating the telegraph</param>
        /// <param name="position">Start position of the line</param>
        /// <param name="rotation">Direction the line faces (in radians)</param>
        /// <param name="length">Length of the line</param>
        /// <param name="width">Width of the line</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="isHostile">Whether this telegraph is hostile</param>
        /// <param name="action">Action to execute when telegraph completes</param>
        /// <returns>Telegraph ID</returns>
        public static string CreateLineTelegraph(
            uint creator,
            Vector3 position,
            float rotation,
            float length,
            float width,
            float duration,
            bool isHostile,
            ApplyTelegraphEffect action)
        {
            return CreateTelegraph(
                creator,
                position,
                rotation,
                new Vector2(length, width),
                duration,
                isHostile,
                TelegraphType.Line,
                action);
        }

        /// <summary>
        /// Creates a telegraph at a creature's position.
        /// </summary>
        /// <param name="creator">Creature creating the telegraph</param>
        /// <param name="target">Target creature to center the telegraph on</param>
        /// <param name="type">Type of telegraph</param>
        /// <param name="size">Size of the telegraph</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="isHostile">Whether this telegraph is hostile</param>
        /// <param name="action">Action to execute when telegraph completes</param>
        /// <returns>Telegraph ID</returns>
        public static string CreateTelegraphAtCreature(
            uint creator,
            uint target,
            TelegraphType type,
            Vector2 size,
            float duration,
            bool isHostile,
            ApplyTelegraphEffect action)
        {
            var position = GetPosition(target);
            var rotation = DegreesToRadians(GetFacing(target));

            return CreateTelegraph(
                creator,
                position,
                rotation,
                size,
                duration,
                isHostile,
                type,
                action);
        }

        /// <summary>
        /// Creates a telegraph in front of a creature.
        /// </summary>
        /// <param name="creator">Creature creating the telegraph</param>
        /// <param name="target">Target creature to position the telegraph in front of</param>
        /// <param name="distance">Distance in front of the target</param>
        /// <param name="type">Type of telegraph</param>
        /// <param name="size">Size of the telegraph</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="isHostile">Whether this telegraph is hostile</param>
        /// <param name="action">Action to execute when telegraph completes</param>
        /// <returns>Telegraph ID</returns>
        public static string CreateTelegraphInFrontOfCreature(
            uint creator,
            uint target,
            float distance,
            TelegraphType type,
            Vector2 size,
            float duration,
            bool isHostile,
            ApplyTelegraphEffect action)
        {
            var position = GetPosition(target);
            var rotation = DegreesToRadians(GetFacing(target));

            // Calculate position in front of the creature
            var frontPosition = new Vector3(
                position.X + (float)Math.Cos(rotation) * distance,
                position.Y + (float)Math.Sin(rotation) * distance,
                position.Z
            );

            return CreateTelegraph(
                creator,
                frontPosition,
                rotation,
                size,
                duration,
                isHostile,
                type,
                action);
        }
    }
}
