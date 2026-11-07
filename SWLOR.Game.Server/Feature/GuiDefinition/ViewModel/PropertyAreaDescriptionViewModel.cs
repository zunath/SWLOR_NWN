using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class PropertyAreaDescriptionViewModel : GuiViewModelBase<PropertyAreaDescriptionViewModel, GuiPayloadBase>
    {
        private string _propertyId;

        public string Description
        {
            get => Get<string>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _propertyId = Property.GetPropertyId(GetArea(Player));
            Description = DB.Get<WorldProperty>(_propertyId)?.CustomDescription ?? string.Empty;
            WatchOnClient(model => model.Description);
        }

        public Action OnClickSave() => () =>
        {
            if (!Property.HasPropertyPermission(Player, _propertyId, PropertyPermissionType.ChangeDescription))
            {
                FloatingTextStringOnCreature("You do not have permission to change this description.", Player, false);
                return;
            }

            var property = DB.Get<WorldProperty>(_propertyId);
            if (property == null)
                return;

            var description = Description ?? string.Empty;
            property.CustomDescription = description.Length > ManageApartmentViewModel.MaxDescriptionLength
                ? description.Substring(0, ManageApartmentViewModel.MaxDescriptionLength)
                : description;
            DB.Set(property);

            FloatingTextStringOnCreature("Area description saved.", Player, false);
            Gui.TogglePlayerWindow(Player, GuiWindowType.PropertyAreaDescription);
        };

        public Action OnClickCancel() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.PropertyAreaDescription);
        };
    }
}
