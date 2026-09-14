namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>Which part of the transform gizmo a press landed on.</summary>
    public enum GizmoHandle
    {
        /// <summary>Nothing - the press missed every handle.</summary>
        None,

        /// <summary>An axis arm: drag moves the instance.</summary>
        Axis,

        /// <summary>The ground-plane ring: drag turns the instance.</summary>
        Ring
    }
}
