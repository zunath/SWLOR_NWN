namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>
    /// What a transition's destination tag reaches in the module, from the tag index: where the one door
    /// or waypoint defining it lives, or the other kind of object when only that carries the tag.
    /// </summary>
    /// <remarks>
    /// The index keeps one area per tag, so SWLOR never reports a tag as ambiguous; it did not before
    /// either, and waypoints legitimately share tags.
    /// </remarks>
    public static class SwlorTransitionDestinations
    {
        public static TransitionDestinationResult Resolve(ModuleTagIndex index, BehaviorTagScope scope, string tag)
        {
            ArgumentNullException.ThrowIfNull(index);
            if (scope == BehaviorTagScope.None)
                return TransitionDestinationResult.TypeUnset;

            if (string.IsNullOrWhiteSpace(tag))
                return TransitionDestinationResult.NotFound;

            if (scope == BehaviorTagScope.Item)
            {
                var itemResRef = index.FindItemBlueprintDefiningTag(tag);
                return itemResRef == null
                    ? TransitionDestinationResult.NotFound
                    : TransitionDestinationResult.Resolved($"item blueprint {itemResRef}");
            }

            var doorArea = index.FindAreaDefiningTag(tag, ResourceType.Utd);
            var waypointArea = index.FindAreaDefiningTag(tag, ResourceType.Utw);
            switch (scope)
            {
                case BehaviorTagScope.Door:
                    if (doorArea != null)
                        return TransitionDestinationResult.Resolved($"door in {doorArea}");
                    return waypointArea != null
                        ? TransitionDestinationResult.WrongType(BehaviorTagScope.Waypoint)
                        : TransitionDestinationResult.NotFound;
                case BehaviorTagScope.Waypoint:
                    if (waypointArea != null)
                        return TransitionDestinationResult.Resolved($"waypoint in {waypointArea}");
                    return doorArea != null
                        ? TransitionDestinationResult.WrongType(BehaviorTagScope.Door)
                        : TransitionDestinationResult.NotFound;
                default:
                    if (doorArea != null)
                        return TransitionDestinationResult.Resolved($"door in {doorArea}");
                    return waypointArea != null
                        ? TransitionDestinationResult.Resolved($"waypoint in {waypointArea}")
                        : TransitionDestinationResult.NotFound;
            }
        }

        /// <summary>
        /// Adapts a resolver that only says where a tag is defined: a location is a resolved destination,
        /// none is a missing one, and a destination with no kind chosen is an unset type.
        /// </summary>
        public static TransitionDestinationResolver FromLocations(Func<BehaviorTagScope, string, string?> locate)
        {
            ArgumentNullException.ThrowIfNull(locate);
            return (scope, tag) => scope == BehaviorTagScope.None
                ? TransitionDestinationResult.TypeUnset
                : TransitionDestinationResult.FromLocation(locate(scope, tag));
        }
    }
}
