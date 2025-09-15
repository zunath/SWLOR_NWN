using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiDrawListItemImage<T>: GuiDrawListItem<T, GuiDrawListItemImage<T>>, IGuiDrawListItem
        where T: IGuiViewModel
    {
        private string Resref { get; set; }
        private string ResrefBindName { get; set; }
        private bool IsResrefBound => !string.IsNullOrWhiteSpace(ResrefBindName);
        
        private GuiRectangle Position { get; set; }
        private string PositionBindName { get; set; }
        private bool IsPositionBound => !string.IsNullOrWhiteSpace(PositionBindName);
        
        private NuiAspect Aspect { get; set; }
        private string AspectBindName { get; set; }
        private bool IsAspectBound => !string.IsNullOrWhiteSpace(AspectBindName);
        
        private NuiHorizontalAlign HorizontalAlign { get; set; }
        private string HorizontalAlignBindName { get; set; }
        private bool IsHorizontalAlignBound => !string.IsNullOrWhiteSpace(HorizontalAlignBindName);
        
        private NuiVerticalAlign VerticalAlign { get; set; }
        private string VerticalAlignBindName { get; set; }
        private bool IsVerticalAlignBound => !string.IsNullOrWhiteSpace(VerticalAlignBindName);

        private GuiRectangle DrawTextureRegion { get; set; }
        private string DrawTextureRegionBindName { get; set; }
        private bool IsDrawTextureRegionBound => !string.IsNullOrWhiteSpace(DrawTextureRegionBindName);

        public GuiDrawListItemImage()
        {
            Position = new GuiRectangle(0, 0, 0, 0);
        }

        /// <summary>
        /// Sets a static value for the image Resref.
        /// </summary>
        /// <param name="resref">The resref to use for the image.</param>
        public GuiDrawListItemImage<T> SetResref(string resref)
        {
            Resref = resref;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the image Resref.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemImage<T> BindResref<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ResrefBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the position of the image.
        /// </summary>
        /// <param name="position">The position to set.</param>
        public GuiDrawListItemImage<T> SetPosition(GuiRectangle position)
        {
            Position = position;
            return this;
        }

        /// <summary>
        /// Sets a static value for the position of the image.
        /// </summary>
        /// <param name="x">The X coordinate</param>
        /// <param name="y">The Y coordinate</param>
        /// <param name="width">The width of the image</param>
        /// <param name="height">The height of the image</param>
        public GuiDrawListItemImage<T> SetPosition(float x, float y, float width, float height)
        {
            Position = new GuiRectangle(x, y, width, height);
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the position of the image.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemImage<T> BindPosition<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            PositionBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the Aspect of the image.
        /// </summary>
        /// <param name="aspect">The aspect to set.</param>
        public GuiDrawListItemImage<T> SetAspect(NuiAspect aspect)
        {
            Aspect = aspect;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the Aspect of the image.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemImage<T> BindAspect<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            AspectBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the horizontal alignment of the image.
        /// </summary>
        /// <param name="hAlign">The horizontal alignment</param>
        public GuiDrawListItemImage<T> SetHorizontalAlign(NuiHorizontalAlign hAlign)
        {
            HorizontalAlign = hAlign;
            return this;
        }

        /// <summary>
        /// Binds a horizontal alignment for the image.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemImage<T> BindHorizontalAlign<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            HorizontalAlignBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the vertical alignment of the image.
        /// </summary>
        /// <param name="vAlign">The vertical alignment</param>
        public GuiDrawListItemImage<T> SetVerticalAlign(NuiVerticalAlign vAlign)
        {
            VerticalAlign = vAlign;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the vertical alignment of the image.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemImage<T> BindVerticalAlign<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            VerticalAlignBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the draw texture region of the image.
        /// </summary>
        /// <param name="rect">The draw texture region</param>
        public GuiDrawListItemImage<T> SetDrawTextureRegion(GuiRectangle rect)
        {
            DrawTextureRegion = rect;
            return this;
        }

        /// <summary>
        /// Sets a static value for the draw texture region of the image.
        /// </summary>
        /// <param name="x">The x position</param>
        /// <param name="y">The y position</param>
        /// <param name="width">The width</param>
        /// <param name="height">The height</param>
        public GuiDrawListItemImage<T> SetDrawTextureRegion(int x, int y, int width, int height)
        {
            DrawTextureRegion = new GuiRectangle(x, y, width, height);
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the draw texture region of the image.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemImage<T> BindDrawTextureRegion<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            DrawTextureRegionBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Builds a GuiDrawListItemImage element.
        /// </summary>
        /// <returns>Json representing the image draw list item.</returns>
        public override Json ToJson()
        {
            var isEnabled = IsEnabledBound ? Nui.Bind(IsEnabledBindName) : JsonBool(IsEnabled);
            var resref = IsResrefBound ? Nui.Bind(ResrefBindName) : JsonString(Resref);
            var position = IsPositionBound ? Nui.Bind(PositionBindName) : Position.ToJson();
            var aspect = IsAspectBound ? Nui.Bind(AspectBindName) : JsonInt((int)Aspect);
            var hAlign = IsHorizontalAlignBound ? Nui.Bind(HorizontalAlignBindName) : JsonInt((int)HorizontalAlign);
            var vAlign = IsVerticalAlignBound ? Nui.Bind(VerticalAlignBindName) : JsonInt((int)VerticalAlign);
            var drawTextureRegion = IsDrawTextureRegionBound ? Nui.Bind(DrawTextureRegionBindName) : DrawTextureRegion?.ToJson();

            var image = Nui.DrawListImage(isEnabled, resref, position, aspect, hAlign, vAlign);

            if (drawTextureRegion != null)
            {
                image = Nui.DrawListImageRegion(image, drawTextureRegion);
            }

            return image;
        }
    }
}
