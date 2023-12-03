using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public abstract class GuiWidget<TDataModel, TDerived> : IGuiWidget
        where TDataModel: IGuiViewModel
        where TDerived: GuiWidget<TDataModel, TDerived>
    {
        public string Id { get; protected set; }
        public string VisibilityOverrideBindName { get; set; }
        public List<IGuiWidget> Elements { get; }
        protected float Width { get; private set; }
        protected float Height { get; private set; }
        private float AspectRatio { get; set; }
        private float Margin { get; set; } = -1f;
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
        
        private string DisabledTooltip { get; set; }
        private string DisabledTooltipBindName { get; set; }
        private bool IsDisabledTooltipBound => !string.IsNullOrWhiteSpace(DisabledTooltipBindName);

        private bool IsEncouraged { get; set; }
        private string IsEncouragedBindName { get; set; }
        private bool IsEncouragedBound { get; set; }

        private GuiColor? Color { get; set; }
        private string ColorBindName { get; set; }
        private bool IsColorBound => !string.IsNullOrWhiteSpace(ColorBindName);

        public Dictionary<string, GuiMethodDetail> Events { get; private set; }

        public abstract Json BuildElement();

        /// <summary>
        /// Sets a static Id for the element.
        /// </summary>
        /// <param name="id">The Id to set.</param>
        public TDerived SetId(string id)
        {
            Id = id;
            return (TDerived)this;
        }

        /// <summary>
        /// Sets a static width for the element.
        /// </summary>
        /// <param name="width">The width to set.</param>
        public TDerived SetWidth(float width)
        {
            Width = width;
            return (TDerived)this;
        }

        /// <summary>
        /// Sets a static height for the element.
        /// </summary>
        /// <param name="height">The height to set.</param>
        public TDerived SetHeight(float height)
        {
            Height = height;
            return (TDerived)this;
        }

        /// <summary>
        /// Sets a static aspect ratio for the element.
        /// </summary>
        /// <param name="aspectRatio">The aspect ratio to set.</param>
        public TDerived SetAspectRatio(float aspectRatio)
        {
            AspectRatio = aspectRatio;
            return (TDerived)this;
        }

        /// <summary>
        /// Sets a static margin for the element.
        /// </summary>
        /// <param name="margin">The margin to set.</param>
        public TDerived SetMargin(float margin)
        {
            Margin = margin;
            return (TDerived)this;
        }

        /// <summary>
        /// Sets a static padding for the element.
        /// </summary>
        /// <param name="padding">The padding to set.</param>
        public TDerived SetPadding(float padding)
        {
            Padding = padding;
            return (TDerived)this;
        }

        /// <summary>
        /// Sets a static value for whether the element is enabled.
        /// </summary>
        /// <param name="isEnabled">true if enabled, false otherwise</param>
        public TDerived SetIsEnabled(bool isEnabled)
        {
            IsEnabled = isEnabled;
            return (TDerived)this;
        }

        /// <summary>
        /// Binds a dynamic value which determines whether the element is enabled or not.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public TDerived BindIsEnabled<TProperty>(Expression<Func<TDataModel, TProperty>> expression)
        {
            IsEnabledBindName = GuiHelper<TDataModel>.GetPropertyName(expression);
            return (TDerived)this;
        }

        /// <summary>
        /// Sets a static value for whether the element is visible.
        /// </summary>
        /// <param name="isVisible">true if visible, false otherwise</param>
        public TDerived SetIsVisible(bool isVisible)
        {
            IsVisible = isVisible;
            return (TDerived)this;
        }

        /// <summary>
        /// Binds a dynamic value which determines whether the element is visible or not.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public TDerived BindIsVisible<TProperty>(Expression<Func<TDataModel, TProperty>> expression)
        {
            IsVisibleBindName = GuiHelper<TDataModel>.GetPropertyName(expression);
            return (TDerived)this;
        }

        /// <summary>
        /// Sets a static value for the tooltip text.
        /// </summary>
        /// <param name="tooltip">The tooltip text to set.</param>
        public TDerived SetTooltip(string tooltip)
        {
            Tooltip = tooltip;
            return (TDerived)this;
        }

        /// <summary>
        /// Binds a dynamic value for the tooltip text.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public TDerived BindTooltip<TProperty>(Expression<Func<TDataModel, TProperty>> expression)
        {
            TooltipBindName = GuiHelper<TDataModel>.GetPropertyName(expression);
            return (TDerived)this;
        }

        /// <summary>
        /// Sets a static value for the disabled tooltip text.
        /// </summary>
        /// <param name="disabledTooltip">The disabled tooltip text to set.</param>
        public TDerived SetDisabledTooltip(string disabledTooltip)
        {
            DisabledTooltip = disabledTooltip;
            return (TDerived)this;
        }

        /// <summary>
        /// Binds a dynamic value for the disabled tooltip text.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public TDerived BindDisabledTooltip<TProperty>(Expression<Func<TDataModel, TProperty>> expression)
        {
            DisabledTooltipBindName = GuiHelper<TDataModel>.GetPropertyName(expression);
            return (TDerived)this;
        }

        /// <summary>
        /// Sets a static value for whether the element is encouraged.
        /// </summary>
        /// <param name="isEncouraged">true if encouraged, false otherwise</param>
        public TDerived SetIsEncouraged(bool isEncouraged)
        {
            IsEncouraged = isEncouraged;
            return (TDerived)this;
        }

        /// <summary>
        /// Binds a dynamic value which determines whether the element is encouraged or not.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public TDerived BindIsEncouraged<TProperty>(Expression<Func<TDataModel, TProperty>> expression)
        {
            IsEncouragedBindName = GuiHelper<TDataModel>.GetPropertyName(expression);
            return (TDerived)this;
        }

        /// <summary>
        /// Sets a static value for the Color property.
        /// </summary>
        /// <param name="color">The color to set.</param>
        public TDerived SetColor(GuiColor color)
        {
            Color = color;
            return (TDerived)this;
        }

        /// <summary>
        /// Sets a static value for the Color property.
        /// </summary>
        /// <param name="red">The amount of red to use. 0-255</param>
        /// <param name="green">The amount of green to use. 0-255</param>
        /// <param name="blue">The amount of blue to use. 0-255</param>
        /// <param name="alpha">The amount of alpha to use. 0-255</param>
        public TDerived SetColor(byte red, byte green, byte blue, byte alpha = 255)
        {
            Color = new GuiColor(red, green, blue, alpha);
            return (TDerived)this;
        }

        /// <summary>
        /// Binds a dynamic color to this element.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public TDerived BindColor<TProperty>(Expression<Func<TDataModel, TProperty>> expression)
        {
            ColorBindName = GuiHelper<TDataModel>.GetPropertyName(expression);
            return (TDerived)this;
        }

        /// <summary>
        /// Adds a list of elements to draw.
        /// </summary>
        /// <param name="drawList">The list of elements to draw.</param>
        public TDerived AddDrawList(Action<GuiDrawList<TDataModel>> drawList)
        {
            var newDrawList = new GuiDrawList<TDataModel>();
            DrawLists.Add(newDrawList);
            drawList(newDrawList);
            return (TDerived)this;
        }

        /// <summary>
        /// Binds an action to the Mouse Down event of the element.
        /// Fires when the user's mouse is pressed down on the element.
        /// </summary>
        /// <typeparam name="TMethod">The method of the view model.</typeparam>
        /// <param name="expression">Expression to target the method.</param>
        public TDerived BindOnMouseDown<TMethod>(Expression<Func<TDataModel, TMethod>> expression)
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = Guid.NewGuid().ToString();

            Events["mousedown"] = GuiHelper<TDataModel>.GetMethodInfo(expression);

            return (TDerived)this;
        }

        /// <summary>
        /// Binds an action to the Mouse Up event of the element.
        /// Fires when the user's mouse is released on the element.
        /// </summary>
        /// <typeparam name="TMethod">The method of the view model.</typeparam>
        /// <param name="expression">Expression to target the method.</param>
        public TDerived BindOnMouseUp<TMethod>(Expression<Func<TDataModel, TMethod>> expression)
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = Guid.NewGuid().ToString();

            Events["mouseup"] = GuiHelper<TDataModel>.GetMethodInfo(expression);

            return (TDerived)this;
        }

        protected GuiWidget()
        {
            IsEnabled = true;
            IsVisible = true;
            DrawLists = new List<GuiDrawList<TDataModel>>();
            Events = new Dictionary<string, GuiMethodDetail>();
            Elements = new List<IGuiWidget>();
        }

        /// <summary>
        /// Converts the widget to Json which is readable by NWN.
        /// </summary>
        /// <returns>Json representing this element.</returns>
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

            // Visibility override - workaround for the NUI vector issue reported here:
            // https://github.com/Beamdog/nwn-issues/issues/427
            if (!string.IsNullOrWhiteSpace(VisibilityOverrideBindName))
            {
                var binding = Nui.Bind(VisibilityOverrideBindName);
                element = Nui.Visible(element, binding);
            }

            // Margin
            if (Margin > -1f)
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

            // Disabled Tooltip (Can be bound)
            if (IsDisabledTooltipBound)
            {
                var binding = Nui.Bind(DisabledTooltipBindName);
                element = Nui.DisabledTooltip(element, binding);
            }
            else if (!string.IsNullOrWhiteSpace(DisabledTooltip))
            {
                element = Nui.DisabledTooltip(element, JsonString(DisabledTooltip));
            }

            // Is Encouraged (Can be bound)
            if (IsEncouragedBound)
            {
                var binding = Nui.Bind(IsEncouragedBindName);
                element = Nui.Encouraged(element, binding);
            }
            else if (!string.IsNullOrWhiteSpace(IsEncouragedBindName))
            {
                element = Nui.Encouraged(element, JsonBool(IsEncouraged));
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
