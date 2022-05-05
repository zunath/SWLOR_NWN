using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiImage<T> : GuiWidget<T, GuiImage<T>>
        where T: IGuiViewModel
    {
        private string Resref { get; set; }
        private string ResrefBindName { get; set; }
        private bool IsResrefBound => !string.IsNullOrWhiteSpace(ResrefBindName);
        
        private NuiAspect Aspect { get; set; }
        private string AspectBindName { get; set; }
        private bool IsAspectBound => !string.IsNullOrWhiteSpace(AspectBindName);
        
        private NuiHorizontalAlign HorizontalAlign { get; set; }
        private string HorizontalAlignBindName { get; set; }
        private bool IsHorizontalAlignBound => !string.IsNullOrWhiteSpace(HorizontalAlignBindName);
        
        private NuiVerticalAlign  VerticalAlign { get; set; }
        private string VerticalAlignBindName { get; set; }
        private bool IsVerticalAlignBound => !string.IsNullOrWhiteSpace(VerticalAlignBindName);

        /// <summary>
        /// Sets a static value for the image resref.
        /// </summary>
        /// <param name="resref">The resref value to set.</param>
        public GuiImage<T> SetResref(string resref)
        {
            Resref = resref;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value to the image resref.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiImage<T> BindResref<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ResrefBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the aspect of the image.
        /// </summary>
        /// <param name="aspect">The aspect to set.</param>
        public GuiImage<T> SetAspect(NuiAspect aspect)
        {
            Aspect = aspect;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the aspect of the image.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiImage<T> BindAspect<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            AspectBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets the horizontal alignment of the image.
        /// </summary>
        /// <param name="hAlign">The horizontal alignment to set.</param>
        public GuiImage<T> SetHorizontalAlign(NuiHorizontalAlign hAlign)
        {
            HorizontalAlign = hAlign;
            return this;
        }

        /// <summary>
        /// Binds a dynamic horizontal alignment for the image.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiImage<T> BindHorizontalAlign<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            HorizontalAlignBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets the vertical alignment of the image.
        /// </summary>
        /// <param name="vAlign">The vertical alignment to set.</param>
        public GuiImage<T> SetVerticalAlign(NuiVerticalAlign vAlign)
        {
            VerticalAlign = vAlign;
            return this;
        }

        /// <summary>
        /// Binds a dynamic vertical alignment for the image.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiImage<T> BindVerticalAlign<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            VerticalAlignBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Builds the GuIImage element.
        /// </summary>
        /// <returns>Json representing the image element.</returns>
        public override Json BuildElement()
        {
            var resref = IsResrefBound ? Nui.Bind(ResrefBindName) : JsonString(Resref);
            var aspect = IsAspectBound ? Nui.Bind(AspectBindName) : JsonInt((int) Aspect);
            var hAlign = IsHorizontalAlignBound ? Nui.Bind(HorizontalAlignBindName) : JsonInt((int) HorizontalAlign);
            var vAlign = IsVerticalAlignBound ? Nui.Bind(VerticalAlignBindName) : JsonInt((int) VerticalAlign);

            return Nui.Image(resref, aspect, hAlign, vAlign);
        }
    }
}
