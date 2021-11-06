using System;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CraftViewModel: GuiViewModelBase<CraftViewModel, CraftPayload>
    {
        private RecipeType _recipe;

        public string WindowTitle
        {
            get => Get<string>();
            set => Set(value);
        }

        protected override void Initialize(CraftPayload initialPayload)
        {
            _recipe = initialPayload.Recipe;

            ChangePartialView("MyView", "SetUpPartial");
        }

        public Action ChangeToSetUpPartial() => () =>
        {
            ChangePartialView("MyView", "SetUpPartial");
        };

        public Action ChangeToCraftPartial() => () =>
        {
            ChangePartialView("MyView", "CraftPartial");
        };
    }
}
