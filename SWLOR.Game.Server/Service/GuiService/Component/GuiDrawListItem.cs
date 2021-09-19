using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public abstract class GuiDrawListItem<TDataModel, TDerived>
        where TDataModel: IGuiViewModel
        where TDerived : GuiDrawListItem<TDataModel, TDerived>
    {
        protected bool IsEnabled { get; set; }
        protected string IsEnabledBindName { get; set; }
        protected bool IsEnabledBound => !string.IsNullOrWhiteSpace(IsEnabledBindName);

        public TDerived SetIsEnabled(bool isEnabled)
        {
            IsEnabled = true;
            return (TDerived)this;
        }

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
