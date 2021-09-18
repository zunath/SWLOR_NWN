using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiCheckBox: GuiWidget
    {
        private string Text { get; set; }
        private string TextBindName { get; set; }
        private bool IsTextBound => !string.IsNullOrWhiteSpace(TextBindName);
        
        private bool IsChecked { get; set; }
        private string IsCheckedBindName { get; set; }
        private bool IsCheckedBound => !string.IsNullOrWhiteSpace(IsCheckedBindName);

        public GuiCheckBox SetText(string text)
        {
            Text = text;
            return this;
        }

        public GuiCheckBox BindText(string bindName)
        {
            TextBindName = bindName;
            return this;
        }

        public GuiCheckBox SetIsChecked(bool isChecked)
        {
            IsChecked = isChecked;
            return this;
        }

        public GuiCheckBox BindIsChecked(string bindName)
        {
            IsCheckedBindName = bindName;
            return this;
        }

        public override Json BuildElement()
        {
            var text = IsTextBound ? Nui.Bind(TextBindName) : JsonString(Text);
            var isChecked = IsCheckedBound ? Nui.Bind(IsCheckedBindName) : JsonBool(IsChecked);

            return Nui.Check(text, isChecked);
        }
    }
}
