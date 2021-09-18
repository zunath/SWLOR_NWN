using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Service.GuiService
{
    public abstract class GuiDrawListItem
    {
        public bool IsEnabled { get; set; }
        public string IsEnabledBindName { get; set; }
        public bool IsEnabledBound => !string.IsNullOrWhiteSpace(IsEnabledBindName);

        protected GuiDrawListItem()
        {
            IsEnabled = true;
        }

        public abstract Json ToJson();
    }
}
