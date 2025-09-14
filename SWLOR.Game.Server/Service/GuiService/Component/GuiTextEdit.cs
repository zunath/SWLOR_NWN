using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.NWN.API;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiTextEdit<T> : GuiWidget<T, GuiTextEdit<T>>
        where T: IGuiViewModel
    {
        private string Placeholder { get; set; }
        private string PlaceholderBindName { get; set; }
        private bool IsPlaceholderBound => !string.IsNullOrWhiteSpace(PlaceholderBindName);
        
        private string Value { get; set; }
        private string ValueBindName { get; set; }
        private bool IsValueBound => !string.IsNullOrWhiteSpace(ValueBindName);

        private int MaxLength { get; set; } = 32;
        private bool IsMultiLine { get; set; }
        private bool HasWordWrap { get; set; } = true;

        /// <summary>
        /// Sets a static value for the placeholder text.
        /// </summary>
        /// <param name="placeholder">The placeholder text</param>
        public GuiTextEdit<T> SetPlaceholder(string placeholder)
        {
            Placeholder = placeholder;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the placeholder text.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiTextEdit<T> BindPlaceholder<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            PlaceholderBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the text editor.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public GuiTextEdit<T> SetValue(string value)
        {
            Value = value;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the text editor.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiTextEdit<T> BindValue<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ValueBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the max length.
        /// </summary>
        /// <param name="maxLength">The max text length.</param>
        public GuiTextEdit<T> SetMaxLength(int maxLength)
        {
            MaxLength = maxLength;
            return this;
        }

        /// <summary>
        /// Sets a static value for whether the text editor is multi-line.
        /// </summary>
        /// <param name="isMultiLine">true if multi-line, false otherwise</param>
        public GuiTextEdit<T> SetIsMultiline(bool isMultiLine)
        {
            IsMultiLine = isMultiLine;
            return this;
        }

        /// <summary>
        /// Sets a static value for whether the text editor has word wrap.
        /// </summary>
        /// <param name="hasWordWrap">true if it has word wrap, false otherwise</param>
        public GuiTextEdit<T> SetHasWordWrap(bool hasWordWrap)
        {
            HasWordWrap = hasWordWrap;
            return this;
        }

        /// <summary>
        /// Builds the GuiTextEdit element.
        /// </summary>
        /// <returns>Json representing the text editor element.</returns>
        public override Json BuildElement()
        {
            var placeholder = IsPlaceholderBound ? Nui.Bind(PlaceholderBindName) : JsonString(Placeholder);
            var value = IsValueBound ? Nui.Bind(ValueBindName) : JsonString(Value);

            return Nui.TextEdit(placeholder, value, MaxLength, IsMultiLine, HasWordWrap);
        }
    }
}
