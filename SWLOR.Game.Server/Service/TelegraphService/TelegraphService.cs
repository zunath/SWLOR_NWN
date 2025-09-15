using System;
using System.Collections.Generic;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service.TelegraphService
{
    public static class Telegraph
    {
        private const float TargetFPS = 30f;
        public const int MaxRenderCount = 8;
        
        // Color constants for telegraph rendering
        private static readonly Vector3 _hostileTelegraphColor = new(1.0f, 0.0f, 0.0f); // Red
        private static readonly Vector3 _selfTelegraphColor = new(0.0f, 0.0f, 1.0f); // Blue
        private static readonly Vector3 _friendlyTelegraphColor = new(0.0f, 1.0f, 0.0f); // Green
        private static readonly Vector3 _enemyBeneficialTelegraphColor = new(0.66f, 0.66f, 0.66f); // Gray
        
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
            ApplyTelegraphEffect action)
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
            return IsInTelegraph(creature, telegraph.Data);
        }

        private static string RunTelegraphEffect(uint telegrapher, TelegraphData data)
        {
            var area = GetArea(telegrapher);
            if (!_telegraphsByArea.ContainsKey(area))
                _telegraphsByArea[area] = new Dictionary<string, ActiveTelegraph>();

            var telegraphId = Guid.NewGuid().ToString();
            var effect = EffectRunScript(
                TelegraphEvents.TelegraphEffectScript,
                TelegraphEvents.TelegraphEffectScript,
                string.Empty);
            
            OnApply(telegrapher, data, telegraphId);
            ApplyEffectToObject(DurationType.Temporary, effect, telegrapher, data.Duration);

            return telegraphId;
        }

        private static void OnApply(uint telegrapher, TelegraphData data, string telegraphId)
        {
            var area = GetArea(telegrapher);

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
            var area = GetArea(telegrapher);

            if (!_telegraphsByArea.ContainsKey(area))
                return;

            if (!_telegraphsByArea[area].ContainsKey(telegraphId))
                return;

            RunTelegraphAction(area, _telegraphsByArea[area][telegraphId].Data);

            _telegraphsByArea[area].Remove(telegraphId);
            _allTelegraphs.Remove(telegraphId);
        }

        private static void RunTelegraphAction(uint area, TelegraphData data)
        {
            var action = data.Action;
            if (action != null)
            {
                var location = Location(area, data.Position, data.Rotation);
                var maxDistance = CalculateMaxCreatureDistance(data.Shape, data.Size);
                var creatureList = new List<uint>();

                var nth = 1;
                var nearest = GetNearestCreatureToLocation(CreatureType.IsAlive, true, location, nth);
                while (GetIsObjectValid(nearest) &&
                       GetDistanceBetweenLocations(location, GetLocation(nearest)) <= maxDistance)
                {
                    if (IsInTelegraph(nearest, data))
                    {
                        creatureList.Add(nearest);
                    }

                    nth++;
                    nearest = GetNearestCreatureToLocation(CreatureType.IsAlive, true, location, nth);
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
                    return size.X; // Line length
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }
        }

        private static bool IsInTelegraph(uint creature, TelegraphData data)
        {
            switch (data.Shape)
            {
                case TelegraphType.Sphere:
                    return IsCreatureInSphere(creature, data);
                case TelegraphType.Cone:
                    return IsCreatureInCone(creature, data);
                case TelegraphType.Line:
                    return IsCreatureInLine(creature, data);
                default:
                    return false;
            }
        }

        private static bool IsCreatureInSphere(uint creature, TelegraphData data)
        {
            var position = GetPosition(creature);
            var radius = data.Size.X;
            var dx = position.X - data.Position.X;
            var dy = position.Y - data.Position.Y;
            var dz = position.Z - data.Position.Z;
            var distance = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            return distance <= radius;
        }

        private static bool IsCreatureInCone(uint creature, TelegraphData data)
        {
            var position = GetPosition(creature);

            var directionX = (float)Math.Cos(data.Rotation);
            var directionY = (float)Math.Sin(data.Rotation);
            var directionZ = 0f;
            
            var toPointX = position.X - data.Position.X;
            var toPointY = position.Y - data.Position.Y;
            var toPointZ = position.Z - data.Position.Z;
            var distance = (float)Math.Sqrt(toPointX * toPointX + toPointY * toPointY + toPointZ * toPointZ);

            // Compute the actual cone angle dynamically
            var halfAngle = (float)Math.Atan((data.Size.Y * 0.5f) / data.Size.X);

            // Angle between the direction and the point
            var dotProduct = toPointX * directionX + toPointY * directionY + toPointZ * directionZ;
            var angleBetween = (float)Math.Acos(dotProduct / distance);

            return (distance <= data.Size.X) && (angleBetween <= halfAngle);
        }

        private static bool IsCreatureInLine(uint creature, TelegraphData data)
        {
            var position = GetPosition(creature);
            var toPoint = position - data.Position;

            // Compute rotated position relative to the telegraph's orientation
            var rotatedPos = new Vector2(
                toPoint.X * (float)Math.Cos(-data.Rotation) - toPoint.Y * (float)Math.Sin(-data.Rotation),
                toPoint.X * (float)Math.Sin(-data.Rotation) + toPoint.Y * (float)Math.Cos(-data.Rotation)
            );

            var distAlongLine = rotatedPos.X;
            var distFromCenter = (float)Math.Abs(rotatedPos.Y);

            return (distAlongLine >= 0f && distAlongLine <= data.Size.X) // Within length
                   && (distFromCenter <= data.Size.Y * 0.5f); // Within width
        }

        private static TelegraphColorType DetermineTelegraphColorType(uint player, uint telegrapher, bool isHostile)
        {
            if (player == telegrapher)
                return TelegraphColorType.Self;

            return isHostile
                ? (GetIsReactionTypeFriendly(player, telegrapher) == 1 ? TelegraphColorType.Friendly : TelegraphColorType.Hostile)
                : (GetIsReactionTypeFriendly(player, telegrapher) == 1 ? TelegraphColorType.Friendly : TelegraphColorType.EnemyBeneficial);
        }

        private static int PackShapeAndColor(TelegraphType shapeType, TelegraphColorType colorType)
        {
            return ((int)shapeType << 8) | (int)colorType;
        }

        /// <summary>
        /// Clears all telegraphs from memory. Used for cleanup.
        /// </summary>
        public static void ClearAllTelegraphs()
        {
            _telegraphsByArea.Clear();
            _allTelegraphs.Clear();
        }

        /// <summary>
        /// Initializes the telegraph system on module load.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void OnModuleLoad()
        {
            UpdateShaderLerpTimer();
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
        /// Updates shader uniforms for a specific player to display telegraphs.
        /// </summary>
        /// <param name="player">Player to update shaders for</param>
        private static void UpdateShaderForPlayer(uint player)
        {
            var area = GetArea(player);
            if (!_telegraphsByArea.ContainsKey(area))
                return;

            var telegraphs = _telegraphsByArea[area];
            var telegraphCountToRender = telegraphs.Count > MaxRenderCount 
                ? MaxRenderCount 
                : telegraphs.Count;
            var telegraphCountToReset = MaxRenderCount - telegraphCountToRender;

            var i = 0;
            foreach (var (_, telegraph) in telegraphs)
            {
                if (i >= MaxRenderCount)
                    break;

                var data = telegraph.Data;
                var position = data.Position;
                var size = data.Size;

                var colorType = DetermineTelegraphColorType(player, data.Creator, data.IsHostile);
                var packed = PackShapeAndColor(data.Shape, colorType);

                SetShaderUniformInt(
                    player, 
                    ShaderUniformType.Type1 + i, 
                    packed);

                SetShaderUniformVec(
                    player, 
                    ShaderUniformType.Type1 + (i * 2) + 0, 
                    position.X, 
                    position.Y, 
                    position.Z, 
                    telegraph.Data.Rotation);
                SetShaderUniformVec(
                    player, 
                    ShaderUniformType.Type1 + (i * 2) + 1, 
                    telegraph.Start, 
                    telegraph.End, 
                    size.X, 
                    size.Y);

                i++;
            }

            for (var x = 0; x < telegraphCountToReset; ++x)
            {
                var uniformIndex = ShaderUniformType.Type1 + telegraphCountToRender + x;
                SetShaderUniformInt(player, uniformIndex, (int)TelegraphType.None);
            }
        }

        /// <summary>
        /// Updates shader uniforms at the target FPS rate.
        /// This method is called recursively to maintain smooth telegraph rendering.
        /// </summary>
        private static void UpdateShaderLerpTimer()
        {
            var counter = GetMicrosecondCounter();

            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                SetShaderUniformInt(player, ShaderUniformType.Type16, counter);
            }

            DelayCommand(1.0f / TargetFPS, UpdateShaderLerpTimer);
        }
    }
}
