using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Workspace
{
    /// <summary>
    /// A blueprint's optional drawable geometry plus editor-only classification that must survive
    /// when the geometry cannot be loaded. Transition doors use the classification to draw their
    /// fixed doorway marker even when <see cref="Model"/> is null.
    /// </summary>
    public readonly record struct BlueprintModelRenderResult(
        RenderModel? Model,
        bool IsDoorTransition);
}
