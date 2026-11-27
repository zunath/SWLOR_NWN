using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using NumericsVector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The shape of an ability impact on the ground and the hostile creatures inside it (part 3 of the ability split,
    /// Readmes/MaintainabilityRefactorAbilitySplit.md), moved from <c>AbilityService</c> unchanged: the impact geometry an
    /// activation marker resolves to, line of sight, and the sphere, cone and line tests. Combat owns it, so the melee techniques
    /// and the ability activation checks share it without reaching up to Abilities.
    /// </summary>
    public class CombatImpactGeometryService : ICombatImpactGeometryService
    {
        private readonly ITelegraphService _telegraph;

        public CombatImpactGeometryService(ITelegraphService telegraph)
        {
            _telegraph = telegraph;
        }

        public bool HasAbilityLineOfSight(uint activator, uint target)
        {
            return LineOfSightObject(activator, target) &&
                   LineOfSightVector(GetPosition(activator), GetPosition(target));
        }

        public bool TryGetCombatImpactShape(AbilityTargetingShapeType targetingShape, out CombatImpactAreaShape shape)
        {
            switch (targetingShape)
            {
                case AbilityTargetingShapeType.Sphere:
                case AbilityTargetingShapeType.HSphere:
                    shape = CombatImpactAreaShape.Sphere;
                    return true;
                case AbilityTargetingShapeType.Rect:
                    shape = CombatImpactAreaShape.Line;
                    return true;
                case AbilityTargetingShapeType.Cone:
                    shape = CombatImpactAreaShape.Cone;
                    return true;
                case AbilityTargetingShapeType.None:
                default:
                    shape = default;
                    return false;
            }
        }

        private NumericsVector3 GetAreaImpactPosition(uint activator, uint target, Location targetLocation, bool centerOnActivator = false)
        {
            if (centerOnActivator)
                return GetPosition(activator);

            if (GetIsObjectValid(target))
                return GetPosition(target);

            var targetArea = GetAreaFromLocation(targetLocation);
            return GetIsObjectValid(targetArea)
                ? GetPositionFromLocation(targetLocation)
                : GetPosition(activator);
        }

        private float GetImpactRotationRadians(uint activator, uint target, Location targetLocation)
        {
            var origin = GetPosition(activator);
            var destination = GetIsObjectValid(target)
                ? GetPosition(target)
                : GetIsObjectValid(GetAreaFromLocation(targetLocation))
                    ? GetPositionFromLocation(targetLocation)
                    : origin;
            var delta = destination - origin;

            if (Math.Abs(delta.X) <= 0.01f && Math.Abs(delta.Y) <= 0.01f)
                return DegreesToRadians(GetFacing(activator));

            return (float)Math.Atan2(delta.Y, delta.X);
        }

        private Location GetCombatImpactShapeOrigin(
            uint activator,
            uint target,
            Location targetLocation,
            CombatImpactAreaShape shape,
            bool centerOnActivator)
        {
            return shape == CombatImpactAreaShape.Sphere
                ? Location(GetArea(activator), GetAreaImpactPosition(activator, target, targetLocation, centerOnActivator), 0f)
                : GetLocation(activator);
        }

        public IReadOnlyList<uint> GetHostileTargetsInSphere(uint activator, Location location, float radius)
        {
            return GetHostileCreaturesInCombatImpactShape(
                    activator, OBJECT_INVALID, location, CombatImpactAreaShape.Sphere, radius, 0f, false, false)
                .Where(creature => HasAbilityLineOfSight(activator, creature))
                .ToList();
        }

        public IEnumerable<uint> GetHostileCreaturesInCombatImpactShape(
            uint activator,
            uint target,
            Location targetLocation,
            CombatImpactAreaShape shape,
            float lengthOrRadius,
            float width,
            bool centerOnActivator,
            bool backOffsetOrigin)
        {
            return GetHostileCreaturesInCombatImpactShape(activator,
                ResolveCombatImpactGeometry(activator, target, targetLocation, shape, lengthOrRadius, width,
                    centerOnActivator, backOffsetOrigin));
        }

        public TelegraphGeometry ResolveCombatImpactGeometry(
            uint activator,
            uint target,
            Location targetLocation,
            CombatImpactAreaShape shape,
            float lengthOrRadius,
            float width,
            bool centerOnActivator,
            bool backOffsetOrigin,
            IReadOnlyList<TelegraphGeometry> activationAreaTelegraphs = null)
        {
            var origin = GetCombatImpactShapeOrigin(activator, target, targetLocation, shape, centerOnActivator);
            var rotation = GetImpactRotationRadians(activator, target, targetLocation);
            var originPosition = CombatImpactShapeGeometry.ResolveOrigin(
                GetPositionFromLocation(origin),
                rotation,
                shape,
                backOffsetOrigin);
            var adjustedLength = CombatImpactShapeGeometry.ResolveLength(
                shape,
                lengthOrRadius,
                backOffsetOrigin);
            var telegraphType = shape switch
            {
                CombatImpactAreaShape.Sphere => TelegraphType.Sphere,
                CombatImpactAreaShape.Cone => TelegraphType.Cone,
                CombatImpactAreaShape.Line => TelegraphType.Line,
                _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null)
            };
            var size = new System.Numerics.Vector2(adjustedLength,
                shape == CombatImpactAreaShape.Sphere ? adjustedLength :
                width > 0f ? width : shape == CombatImpactAreaShape.Cone ? adjustedLength : 2f);
            var impact = new TelegraphGeometry(GetAreaFromLocation(origin), telegraphType, originPosition, size, rotation);
            return _telegraph.ResolveImpactGeometry(impact, activationAreaTelegraphs);
        }

        public IEnumerable<uint> GetHostileCreaturesInCombatImpactShape(uint activator, TelegraphGeometry geometry)
        {
            var shape = geometry.Shape switch
            {
                TelegraphType.Sphere => CombatImpactAreaShape.Sphere,
                TelegraphType.Cone => CombatImpactAreaShape.Cone,
                TelegraphType.Line => CombatImpactAreaShape.Line,
                _ => throw new ArgumentOutOfRangeException(nameof(geometry), geometry.Shape, null)
            };
            var maxDistance = GetCombatImpactShapeSearchRadius(shape, geometry.Size.X, geometry.Size.Y);
            var candidates = GetAliveCreaturesInArea(geometry.Area)
                .Select(creature => new
                {
                    Creature = creature,
                    Position = GetPosition(creature)
                })
                .Where(candidate => GetHorizontalDistance(candidate.Position, geometry.Position) <= maxDistance)
                .OrderBy(candidate => GetHorizontalDistance(candidate.Position, geometry.Position));

            foreach (var candidate in candidates)
            {
                if (GetIsReactionTypeHostile(candidate.Creature, activator) &&
                    IsPositionInCombatImpactShape(candidate.Position, geometry.Position, geometry.Rotation, shape, geometry.Size.X, geometry.Size.Y))
                {
                    yield return candidate.Creature;
                }
            }
        }

        private float DegreesToRadians(float degrees)
        {
            return degrees * ((float)Math.PI / 180f);
        }

        private bool IsPositionInCombatImpactShape(
            NumericsVector3 position,
            NumericsVector3 origin,
            float rotation,
            CombatImpactAreaShape shape,
            float lengthOrRadius,
            float width)
        {
            switch (shape)
            {
                case CombatImpactAreaShape.Sphere:
                    return GetHorizontalDistance(position, origin) <= lengthOrRadius;
                case CombatImpactAreaShape.Cone:
                    return IsPositionInCone(position, origin, rotation, lengthOrRadius, width > 0f ? width : lengthOrRadius);
                case CombatImpactAreaShape.Line:
                    return IsPositionInLine(position, origin, rotation, lengthOrRadius, width > 0f ? width : 2.0f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }
        }

        private bool IsPositionInCone(NumericsVector3 position, NumericsVector3 origin, float rotation, float length, float width)
        {
            var toPoint = position - origin;
            var distance = GetHorizontalDistance(position, origin);
            if (distance <= 0.01f)
                return true;

            var direction = new NumericsVector3((float)Math.Cos(rotation), (float)Math.Sin(rotation), 0f);
            var cosAngle = Math.Clamp(NumericsVector3.Dot(toPoint, direction) / distance, -1f, 1f);
            var angleBetween = (float)Math.Acos(cosAngle);
            var halfAngle = (float)Math.Atan(width * 0.5f / length);

            return distance <= length && angleBetween <= halfAngle;
        }

        private float GetCombatImpactShapeSearchRadius(CombatImpactAreaShape shape, float lengthOrRadius, float width)
        {
            switch (shape)
            {
                case CombatImpactAreaShape.Sphere:
                case CombatImpactAreaShape.Cone:
                    return lengthOrRadius;
                case CombatImpactAreaShape.Line:
                    var effectiveWidth = width > 0f ? width : 2.0f;
                    var halfWidth = effectiveWidth * 0.5f;
                    return (float)Math.Sqrt(lengthOrRadius * lengthOrRadius + halfWidth * halfWidth);
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }
        }

        private float GetHorizontalDistance(NumericsVector3 position, NumericsVector3 origin)
        {
            var x = position.X - origin.X;
            var y = position.Y - origin.Y;

            return (float)Math.Sqrt(x * x + y * y);
        }

        private IEnumerable<uint> GetAliveCreaturesInArea(uint area)
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

        private bool IsPositionInLine(NumericsVector3 position, NumericsVector3 origin, float rotation, float length, float width)
        {
            var toPoint = position - origin;
            var rotatedX = toPoint.X * (float)Math.Cos(-rotation) - toPoint.Y * (float)Math.Sin(-rotation);
            var rotatedY = toPoint.X * (float)Math.Sin(-rotation) + toPoint.Y * (float)Math.Cos(-rotation);

            return rotatedX >= 0f &&
                   rotatedX <= length &&
                   Math.Abs(rotatedY) <= width * 0.5f;
        }
    }
}
