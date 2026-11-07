namespace SWLOR.Game.Server.Service.GuiService
{
    /// <summary>
    /// Holds a window's layout change while the player is pressing a control in that window.
    /// </summary>
    /// <remarks>
    /// NUI fires a button's "click" when the mouse button goes down, before it comes back up. A layout
    /// that reaches the client during the press replaces the pressed control, the window never sees the
    /// release, and from then on it ignores every press: its buttons, its text boxes and its close
    /// button stop working until the player relogs. Holding the layout until the "mouseup" arrives
    /// avoids that.
    /// </remarks>
    public class GuiPressLayoutHold
    {
        /// <summary>
        /// Identifies the current press, so a fallback scheduled for one press does not end a later one.
        /// </summary>
        public int PressId { get; private set; }

        /// <summary>Whether the player is pressing a control in the window.</summary>
        public bool IsPressed { get; private set; }

        /// <summary>Whether a layout change is waiting for the release.</summary>
        public bool HasHeldLayout { get; private set; }

        /// <summary>
        /// Records a press. The click and the mousedown events of one press keep the same <see cref="PressId"/>.
        /// </summary>
        public void Press()
        {
            if (IsPressed)
                return;

            IsPressed = true;
            PressId++;
        }

        /// <summary>
        /// Called before a layout change is sent.
        /// </summary>
        /// <returns>True when the change must wait for the release; false when it can be sent now.</returns>
        public bool Hold()
        {
            if (!IsPressed)
                return false;

            HasHeldLayout = true;
            return true;
        }

        /// <summary>
        /// Records the release.
        /// </summary>
        /// <returns>True when a held layout change must be sent now.</returns>
        public bool Release()
        {
            IsPressed = false;
            return TakeHeldLayout();
        }

        /// <summary>
        /// Ends a press whose release never arrived, for example because the player let go outside the window.
        /// </summary>
        /// <param name="pressId">The <see cref="PressId"/> the fallback was scheduled for.</param>
        /// <returns>True when a held layout change must be sent now.</returns>
        public bool Expire(int pressId)
        {
            if (!IsPressed || pressId != PressId)
                return false;

            IsPressed = false;
            return TakeHeldLayout();
        }

        /// <summary>
        /// Forgets any press and held change, for example when the window is opened again.
        /// </summary>
        public void Reset()
        {
            IsPressed = false;
            HasHeldLayout = false;
            PressId++;
        }

        private bool TakeHeldLayout()
        {
            var hadHeldLayout = HasHeldLayout;
            HasHeldLayout = false;
            return hadHeldLayout;
        }
    }
}
