using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The shape of an ability impact on the ground and the hostile creatures inside it (part 3 of the ability split,
    /// Readmes/MaintainabilityRefactorAbilitySplit.md). Static callers reach the container's instance through
    /// <c>GameServices.CombatImpactGeometry</c>.
    /// </summary>
    public interface ICombatImpactGeometryService
    {
        /// <summary>Whether the activator sees the target, by object and by the line between their positions.</summary>
        bool HasAbilityLineOfSight(uint activator, uint target);

        /// <summary>The impact shape of a targeting shape; false for a shape that has none.</summary>
        bool TryGetCombatImpactShape(AbilityTargetingShapeType targetingShape, out CombatImpactAreaShape shape);

        IReadOnlyList<uint> GetHostileTargetsInSphere(uint activator, Location location, float radius);

        /// <summary>The living creatures hostile to the activator inside the impact shape, nearest first.</summary>
        IEnumerable<uint> GetHostileCreaturesInCombatImpactShape(
            uint activator,
            uint target,
            Location targetLocation,
            CombatImpactAreaShape shape,
            float lengthOrRadius,
            float width,
            bool centerOnActivator,
            bool backOffsetOrigin);

        /// <summary>
        /// The impact geometry of a shape around the activator or its target, resolved against the activation markers the
        /// telegraph service shows.
        /// </summary>
        TelegraphGeometry ResolveCombatImpactGeometry(
            uint activator,
            uint target,
            Location targetLocation,
            CombatImpactAreaShape shape,
            float lengthOrRadius,
            float width,
            bool centerOnActivator,
            bool backOffsetOrigin,
            IReadOnlyList<TelegraphGeometry> activationAreaTelegraphs = null);

        /// <summary>The living creatures hostile to the activator inside a resolved impact geometry, nearest first.</summary>
        IEnumerable<uint> GetHostileCreaturesInCombatImpactShape(uint activator, TelegraphGeometry geometry);
    }
}
