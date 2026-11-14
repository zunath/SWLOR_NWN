namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>One toggled module that is on, as the Ship HUD lists it.</summary>
    public class ShipHudModuleRow
    {
        /// <summary>The hotbar feat slot number (1-30).</summary>
        public int Slot { get; set; }

        /// <summary>The slot's row and number, such as "High 2".</summary>
        public string SlotLabel { get; set; }

        /// <summary>The crew seat (or droid post) that runs the module when it is not a pilot module, else null.</summary>
        public string SeatLabel { get; set; }

        public string Name { get; set; }

        /// <summary>What the module is aimed at right now.</summary>
        public string TargetText { get; set; }

        /// <summary>How far through the current cycle the module is (0 to 1).</summary>
        public float Progress { get; set; }

        /// <summary>Whether the module keeps a target the pilot can change (support modules).</summary>
        public bool CanRetarget { get; set; }
    }
}
