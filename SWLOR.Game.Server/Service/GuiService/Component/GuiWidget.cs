using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public abstract class GuiWidget<TDataModel, TDerived> : IGuiWidget
        where TDataModel: IGuiDataModel
        where TDerived: GuiWidget<TDataModel, TDerived>
    {
        public string Id { get; protected set; }
        private float Width { get; set; }
        private float Height { get; set; }
        private float AspectRatio { get; set; }
        private float Margin { get; set; }
        private float Padding { get; set; }
        private List<GuiDrawList<TDataModel>> DrawLists { get; set; }

        private bool IsEnabled { get; set; }
        private string IsEnabledBindName { get; set; }
        private bool IsEnableBound => !string.IsNullOrWhiteSpace(IsEnabledBindName);
        
        private bool IsVisible { get; set; }
        private string IsVisibleBindName { get; set; }
        private bool IsVisibleBound => !string.IsNullOrWhiteSpace(IsVisibleBindName);
        
        private string Tooltip { get; set; }
        private string TooltipBindName { get; set; }
        private bool IsTooltipBound => !string.IsNullOrWhiteSpace(TooltipBindName);
        
        private GuiColor? Color { get; set; }
        private string ColorBindName { get; set; }
        private bool IsColorBound => !string.IsNullOrWhiteSpace(ColorBindName);

        public Dictionary<string, GuiEventDelegate> Events { get; private set; }

        public abstract Json BuildElement();

        public TDerived SetId(string id)
        {
            Id = id;
            return (TDerived)this;
        }

        public TDerived SetWidth(float width)
        {
            Width = width;
            return (TDerived)this;
        }

        public TDerived SetHeight(float height)
        {
            Height = height;
            return (TDerived)this;
        }

        public TDerived SetAspectRatio(float aspectRatio)
        {
            AspectRatio = aspectRatio;
            return (TDerived)this;
        }

        public TDerived SetMargin(float margin)
        {
            Margin = margin;
            return (TDerived)this;
        }

        public TDerived SetPadding(float padding)
        {
            Padding = padding;
            return (TDerived)this;
        }

        public TDerived SetIsEnabled(bool isEnabled)
        {
            IsEnabled = isEnabled;
            return (TDerived)this;
        }

        public TDerived BindIsEnabled<TProperty>(Expression<Func<TDataModel, TProperty>> expression)
        {
            IsEnabledBindName = GuiHelper<TDataModel>.GetPropertyName(expression);
            return (TDerived)this;
        }


        public TDerived SetIsVisible(bool isVisible)
        {
            IsVisible = isVisible;
            return (TDerived)this;
        }

        public TDerived BindIsVisible<TProperty>(Expression<Func<TDataModel, TProperty>> expression)
        {
            IsVisibleBindName = GuiHelper<TDataModel>.GetPropertyName(expression);
            return (TDerived)this;
        }

        public TDerived SetTooltip(string tooltip)
        {
            Tooltip = tooltip;
            return (TDerived)this;
        }

        public TDerived BindTooltip<TProperty>(Expression<Func<TDataModel, TProperty>> expression)
        {
            TooltipBindName = GuiHelper<TDataModel>.GetPropertyName(expression);
            return (TDerived)this;
        }

        public TDerived SetColor(GuiColor color)
        {
            Color = color;
            return (TDerived)this;
        }

        public TDerived SetColor(int red, int green, int blue, int alpha = 255)
        {
            Color = new GuiColor(red, green, blue, alpha);
            return (TDerived)this;
        }

        public TDerived BindColor<TProperty>(Expression<Func<TDataModel, TProperty>> expression)
        {
            ColorBindName = GuiHelper<TDataModel>.GetPropertyName(expression);
            return (TDerived)this;
        }

        public TDerived AddDrawList(Action<GuiDrawList<TDataModel>> drawList)
        {
            var newDrawList = new GuiDrawList<TDataModel>();
            DrawLists.Add(newDrawList);
            drawList(newDrawList);
            return (TDerived)this;
        }

        public TDerived OnMouseDown(GuiEventDelegate mouseDownAction)
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = Guid.NewGuid().ToString();

            Events["mousedown"] = mouseDownAction;

            return (TDerived)this;
        }

        public TDerived OnMouseUp(GuiEventDelegate mouseUpAction)
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = Guid.NewGuid().ToString();

            Events["mouseup"] = mouseUpAction;

            return (TDerived)this;
        }

        protected GuiWidget()
        {
            IsEnabled = true;
            IsVisible = true;
            DrawLists = new List<GuiDrawList<TDataModel>>();
            Events = new Dictionary<string, GuiEventDelegate>();
        }

        public virtual Json ToJson()
        {
            var element = BuildElement();

            // Nui Id
            if (!string.IsNullOrWhiteSpace(Id))
            {
                element = Nui.Id(element, Id);
            }

            // Width
            if (Width > 0f)
            {
                element = Nui.Width(element, Width);
            }

            // Height
            if (Height > 0f)
            {
                element = Nui.Height(element, Height);
            }

            // Aspect Ratio
            if (AspectRatio > 0f)
            {
                element = Nui.Aspect(element, AspectRatio);
            }

            // Is Enabled (Can be bound)
            if (IsEnableBound)
            {
                var binding = Nui.Bind(IsEnabledBindName);
                element = Nui.Enabled(element, binding);
            }
            else
            {
                element = Nui.Enabled(element, JsonBool(IsEnabled));
            }

            // Is Visible (Can be bound)
            if (IsVisibleBound)
            {
                var binding = Nui.Bind(IsVisibleBindName);
                element = Nui.Visible(element, binding);
            }
            else
            {
                element = Nui.Visible(element, JsonBool(IsVisible));
            }

            // Margin
            if (Margin > 0f)
            {
                element = Nui.Margin(element, Margin);
            }

            // Padding
            if (Padding > 0f)
            {
                element = Nui.Padding(element, Padding);
            }

            // Tooltip (Can be bound)
            if (IsTooltipBound)
            {
                var binding = Nui.Bind(TooltipBindName);
                element = Nui.Tooltip(element, binding);
            }
            else if(!string.IsNullOrWhiteSpace(Tooltip))
            {
                element = Nui.Tooltip(element, JsonString(Tooltip));
            }

            // Color
            if (IsColorBound)
            {
                var binding = Nui.Bind(ColorBindName);
                element = Nui.StyleForegroundColor(element, binding);
            }
            else if (Color != null)
            {
                element = Nui.StyleForegroundColor(element, Color.ToJson());
            }

            // Draw Lists
            foreach (var drawList in DrawLists)
            {
                element = drawList.ToJson(element);
            }

            return element;
        }
    }
}
