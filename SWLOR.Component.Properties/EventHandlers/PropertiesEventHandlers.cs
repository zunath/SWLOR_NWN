using SWLOR.Shared.Domain.Properties.Contracts;
using SWLOR.Shared.Domain.Properties.Events;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.Area;
using SWLOR.Shared.Events.Events.NWNX;

namespace SWLOR.Component.Properties.EventHandlers
{
    /// <summary>
    /// Event handlers for Properties-related game events.
    /// This class handles the infrastructure layer of receiving game events and delegating to the appropriate services.
    /// </summary>
    public class PropertiesEventHandlers
    {
        private readonly IPropertyService _propertyService;

        public PropertiesEventHandlers(IPropertyService propertyService)
        {
            _propertyService = propertyService;
        }

        /// <summary>
        /// When the module loads, cache all relevant data into memory.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheData()
        {
            _propertyService.CachePropertyTypes();
            _propertyService.CachePropertyLayoutTypes();
            _propertyService.CachePermissions();
            _propertyService.CacheStructures();
            _propertyService.CacheInstanceTemplates();
            _propertyService.CacheStructuresByPropertyType();
        }

        /// <summary>
        /// When the module loads, clean up any deleted data, refreshes permissions and then load properties.
        /// </summary>
        [ScriptHandler<OnModuleLoad>]
        public void OnModuleLoad()
        {
            _propertyService.RefreshPermissions();
            _propertyService.ProcessCities();
            _propertyService.CleanUpData();
            _propertyService.LoadProperties();
        }

        /// <summary>
        /// When an apartment terminal is used, open the Apartment NUI
        /// </summary>
        [ScriptHandler<OnApartmentTerminal>]
        public void StartApartmentConversation()
        {
            _propertyService.StartApartmentConversation();
        }

        /// <summary>
        /// When a player enters a property instance, add them to the list of players.
        /// </summary>
        [ScriptHandler<OnAreaEnter>]
        public void EnterPropertyInstance()
        {
            _propertyService.EnterPropertyInstance();
        }

        /// <summary>
        /// When a player exits a property instance, remove them from the list of players.
        /// </summary>
        [ScriptHandler<OnAreaExit>]
        public void ExitPropertyInstance()
        {
            _propertyService.ExitPropertyInstance();
        }

        /// <summary>
        /// When the property menu feat is used, open the GUI window.
        /// </summary>
        [ScriptHandler<OnFeatUseBefore>]
        public void PropertyMenu()
        {
            _propertyService.PropertyMenu();
        }

        /// <summary>
        /// Before an item is used, if it is a structure item, place it at the specified location.
        /// </summary>
        [ScriptHandler<OnItemUseBefore>]
        public void PlaceStructure()
        {
            _propertyService.PlaceStructure();
        }

        /// <summary>
        /// When a building entrance is used, port the player inside the instance if they have permission
        /// or display an error message saying they don't have permission to enter.
        /// </summary>
        [ScriptHandler<OnEnterProperty>]
        public void EnterBuilding()
        {
            _propertyService.EnterBuilding();
        }

        /// <summary>
        /// When the Citizenship terminal is used, open the Manage Citizenship UI.
        /// </summary>
        [ScriptHandler<OnOpenCitizenship>]
        public void OpenCitizenshipMenu()
        {
            _propertyService.OpenCitizenshipMenu();
        }

        /// <summary>
        /// When the City Management terminal is used, open the City Management UI.
        /// </summary>
        [ScriptHandler<OnOpenCityManage>]
        public void OpenCityManagementMenu()
        {
            _propertyService.OpenCityManagementMenu();
        }
    }
}
