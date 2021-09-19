using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiButtonImage<T> : GuiWidget<T, GuiButtonImage<T>>
        where T: IGuiDataModel
    {
        private string Resref { get; set; }
        private string ResrefBindName { get; set; }
        private bool IsResrefBound => !string.IsNullOrWhiteSpace(ResrefBindName);

        public GuiButtonImage<T> SetResref(string resref)
        {
            Resref = resref;
            return this;
        }

        public GuiButtonImage<T> BindResref<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ResrefBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public override Json BuildElement()
        {
            var resref = IsResrefBound ? Nui.Bind(ResrefBindName) : JsonString(Resref);

            return Nui.ButtonImage(resref);
        }
    }
}
