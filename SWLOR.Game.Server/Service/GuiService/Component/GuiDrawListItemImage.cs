using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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

        public GuiDrawListItemImage()
        {
            Position = new GuiRectangle(0, 0, 0, 0);
        }

        public GuiDrawListItemImage<T> SetResref(string resref)
        {
            Resref = resref;
            return this;
        }

        public GuiDrawListItemImage<T> BindResref<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ResrefBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemImage<T> SetPosition(GuiRectangle position)
        {
            Position = position;
            return this;
        }

        public GuiDrawListItemImage<T> SetPosition(float x, float y, float width, float height)
        {
            Position = new GuiRectangle(x, y, width, height);
            return this;
        }

        public GuiDrawListItemImage<T> BindPosition<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            PositionBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemImage<T> SetAspect(NuiAspect aspect)
        {
            Aspect = aspect;
            return this;
        }

        public GuiDrawListItemImage<T> BindAspect<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            AspectBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemImage<T> SetHorizontalAlign(NuiHorizontalAlign hAlign)
        {
            HorizontalAlign = hAlign;
            return this;
        }

        public GuiDrawListItemImage<T> BindHorizontalAlign<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            HorizontalAlignBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemImage<T> SetVerticalAlign(NuiVerticalAlign vAlign)
        {
            VerticalAlign = vAlign;
            return this;
        }

        public GuiDrawListItemImage<T> BindVerticalAlign<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            VerticalAlignBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public override Json ToJson()
        {
            var isEnabled = IsEnabledBound ? Nui.Bind(IsEnabledBindName) : JsonBool(IsEnabled);
            var resref = IsResrefBound ? Nui.Bind(ResrefBindName) : JsonString(Resref);
            var position = IsPositionBound ? Nui.Bind(PositionBindName) : Position.ToJson();
            var aspect = IsAspectBound ? Nui.Bind(AspectBindName) : JsonInt((int)Aspect);
            var hAlign = IsHorizontalAlignBound ? Nui.Bind(HorizontalAlignBindName) : JsonInt((int)HorizontalAlign);
            var vAlign = IsVerticalAlignBound ? Nui.Bind(VerticalAlignBindName) : JsonInt((int)VerticalAlign);

            return Nui.DrawListImage(isEnabled, resref, position, aspect, hAlign, vAlign);
        }
    }
}
