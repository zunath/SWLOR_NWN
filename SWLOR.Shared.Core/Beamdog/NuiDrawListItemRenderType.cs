namespace SWLOR.Shared.Core.Beamdog
{
    public enum NuiDrawListItemRenderType
    {
        // Always render draw list item (default).
        Always = 0,
        // Only render when NOT hovering.
        MouseOff = 1,
        // Only render when mouse is hovering.
        MouseHover = 2,
        // Only render while LMB is held down.
        MouseLeft = 3,
        // Only render while RMB is held down.
        MouseRight = 4,
        // Only render while MMB is held down.
        MouseMiddle = 5,
    }
}
