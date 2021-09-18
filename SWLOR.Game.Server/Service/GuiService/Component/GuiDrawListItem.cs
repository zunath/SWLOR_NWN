using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public abstract class GuiDrawListItem
    {
        protected bool IsEnabled { get; set; }
        protected string IsEnabledBindName { get; set; }
        protected bool IsEnabledBound => !string.IsNullOrWhiteSpace(IsEnabledBindName);

        public GuiDrawListItem SetIsEnabled(bool isEnabled)
        {
            IsEnabled = true;
            return this;
        }

        public GuiDrawListItem BindIsEnabled(string bindName)
        {
            IsEnabledBindName = bindName;
            return this;
        }

        protected GuiDrawListItem()
        {
            IsEnabled = true;
        }

        public abstract Json ToJson();
    }
}
