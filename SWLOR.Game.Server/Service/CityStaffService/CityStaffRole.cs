namespace SWLOR.Game.Server.Service.CityStaffService
{
    /// <summary>
    /// A member of a city building's staff. Saved on the staff creature's blueprint as <c>CITY_STAFF_ROLE</c>, so the
    /// numbers never change. The Starport's dockmaster is not here: it is the existing dockhand, who keeps the Starport
    /// dialog.
    /// </summary>
    public enum CityStaffRole
    {
        Invalid = 0,

        /// <summary>City Hall: general goods.</summary>
        Clerk = 1,

        /// <summary>Cantina: food and drink.</summary>
        Bartender = 2,

        /// <summary>Medical Center: medical supplies.</summary>
        Medic = 3,

        /// <summary>Bank: opens the citizen's bank account.</summary>
        Teller = 4
    }
}
