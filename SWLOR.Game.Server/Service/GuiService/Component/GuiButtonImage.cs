using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiButtonImage<TDataModel> : GuiWidget<TDataModel, GuiButtonImage<TDataModel>>
        where TDataModel: IGuiViewModel
    {
        private string Resref { get; set; }
        private string ResrefBindName { get; set; }
        private bool IsResrefBound => !string.IsNullOrWhiteSpace(ResrefBindName);

        /// <summary>
        /// Sets a static value to the image's Resref property.
        /// </summary>
        /// <param name="resref">The resref of the image, excluding the extension.</param>
        public GuiButtonImage<TDataModel> SetImageResref(string resref)
        {
            Resref = resref;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value to the image's Resref property.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiButtonImage<TDataModel> BindImageResref<TProperty>(Expression<Func<TDataModel, TProperty>> expression)
        {
            ResrefBindName = GuiHelper<TDataModel>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Binds an action to the Click event of the button.
        /// </summary>
        /// <typeparam name="TMethod">The method of the view model.</typeparam>
        /// <param name="expression">Expression to target the method.</param>
        public GuiButtonImage<TDataModel> BindOnClicked<TMethod>(Expression<Func<TDataModel, TMethod>> expression)
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = Guid.NewGuid().ToString();

            Events["click"] = GuiHelper<TDataModel>.GetMethodInfo(expression);

            return this;
        }

        /// <summary>
        /// Builds the GuiButtonImage element.
        /// </summary>
        /// <returns>Json representing the image button element.</returns>
        public override Json BuildElement()
        {
            var resref = IsResrefBound ? Nui.Bind(ResrefBindName) : JsonString(Resref);

            return Nui.ButtonImage(resref);
        }
    }
}
