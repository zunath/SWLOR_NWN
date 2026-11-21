using SWLOR.Game.Server.Domain.Foundation;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The property terminals and menus: the apartment, citizenship and city management terminals and the property menu feat,
    /// which open their windows. Split from <c>PropertyService</c>; the members moved unchanged.
    /// </summary>
    public interface IPropertyTerminalService
    {
        /// <summary>
        /// When an apartment terminal is used, open the Apartment NUI
        /// </summary>
        void StartApartmentConversation();

        /// <summary>
        /// When the property menu feat is used, open the GUI window.
        /// </summary>
        void PropertyMenu();

        /// <summary>
        /// When the Citizenship terminal is used, open the Manage Citizenship UI.
        /// </summary>
        void OpenCitizenshipMenu();

        /// <summary>
        /// When the City Management terminal is used, open the City Management UI.
        /// </summary>
        void OpenCityManagementMenu();
    }
}
