using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Triggers
{
    /// <summary>Module services needed when a trigger editor is embedded in an area.</summary>
    public sealed record TriggerEditorServices(
        string HeaderOwner,
        TransitionDestinationResolver? ResolveDestination,
        Func<string, IReadOnlyList<BehaviorChoice>>? ResolveChoices = null,
        ChoicePreviewService? ChoicePreviews = null);
}
