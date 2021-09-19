using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public abstract class GuiDrawListItem<T>
        where T: IGuiDataModel
    {
        protected bool IsEnabled { get; set; }
        protected string IsEnabledBindName { get; set; }
        protected bool IsEnabledBound => !string.IsNullOrWhiteSpace(IsEnabledBindName);

        public GuiDrawListItem<T> SetIsEnabled(bool isEnabled)
        {
            IsEnabled = true;
            return this;
        }

        public GuiDrawListItem<T> BindIsEnabled<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            IsEnabledBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        protected GuiDrawListItem()
        {
            IsEnabled = true;
        }

        public abstract Json ToJson();
    }
}
