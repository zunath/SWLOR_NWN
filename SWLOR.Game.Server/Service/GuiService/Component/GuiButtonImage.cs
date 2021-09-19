using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiButtonImage<TDataModel> : GuiWidget<TDataModel, GuiButtonImage<TDataModel>>
        where TDataModel: IGuiViewModel
    {
        private string Resref { get; set; }
        private string ResrefBindName { get; set; }
        private bool IsResrefBound => !string.IsNullOrWhiteSpace(ResrefBindName);

        public GuiButtonImage<TDataModel> SetResref(string resref)
        {
            Resref = resref;
            return this;
        }

        public GuiButtonImage<TDataModel> BindResref<TProperty>(Expression<Func<TDataModel, TProperty>> expression)
        {
            ResrefBindName = GuiHelper<TDataModel>.GetPropertyName(expression);
            return this;
        }

        public GuiButtonImage<TDataModel> OnClicked(GuiEventDelegate<IGuiViewModel> clickAction)
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = Guid.NewGuid().ToString();

            Events["click"] = clickAction;

            return this;
        }

        public override Json BuildElement()
        {
            var resref = IsResrefBound ? Nui.Bind(ResrefBindName) : JsonString(Resref);

            return Nui.ButtonImage(resref);
        }
    }
}
