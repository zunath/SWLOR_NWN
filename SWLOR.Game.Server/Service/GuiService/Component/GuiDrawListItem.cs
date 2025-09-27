using System;
using System.Linq.Expressions;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public abstract class GuiDrawListItem<TDataModel, TDerived>
        where TDataModel: IGuiViewModel
        where TDerived : GuiDrawListItem<TDataModel, TDerived>
    {
        protected bool IsEnabled { get; set; }
        protected string IsEnabledBindName { get; set; }
        protected bool IsEnabledBound => !string.IsNullOrWhiteSpace(IsEnabledBindName);

        /// <summary>
        /// Sets a static value to determine whether the item is enabled.
        /// </summary>
        /// <param name="isEnabled">true if enabled, false otherwise</param>
        public TDerived SetIsEnabled(bool isEnabled)
        {
            IsEnabled = isEnabled;
            return (TDerived)this;
        }

        /// <summary>
        /// Binds a dynamic value which determines whether the item is enabled.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public TDerived BindIsEnabled<TProperty>(Expression<Func<TDataModel, TProperty>> expression)
        {
            IsEnabledBindName = GuiHelper<TDataModel>.GetPropertyName(expression);
            return (TDerived)this;
        }

        protected GuiDrawListItem()
        {
            IsEnabled = true;
        }

        public abstract Json ToJson();
    }
}
