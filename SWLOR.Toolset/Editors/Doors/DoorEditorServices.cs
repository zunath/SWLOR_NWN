using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Doors;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Editors.Triggers;
using SWLOR.Toolset.Workspace;
using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Doors
{
    /// <summary>Module/game-data services needed when a door editor is embedded in an area.</summary>
    public sealed record DoorEditorServices(
        string HeaderOwner,
        Func<BehaviorTagScope, string, string?>? ResolveTag,
        Func<string, IReadOnlyList<BehaviorChoice>>? ResolveChoices,
        IReadOnlyList<DoorAppearanceChoice> Appearances,
        ResourceIndex? ResourceIndex,
        Func<JsonGffStruct, BlueprintModelRenderResult>? ResolveModel,
        ThumbnailService? Thumbnails = null,
        ChoicePreviewService? ChoicePreviews = null,
        Services.IEditorPromptService? Prompts = null);
}
