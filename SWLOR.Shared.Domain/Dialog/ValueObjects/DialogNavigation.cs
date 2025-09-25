namespace SWLOR.Shared.Dialog.Model
{
    public class DialogNavigation
    {
        public string PageName { get; set; }
        public string DialogName { get; set; }

        public DialogNavigation(string pageName, string dialogName)
        {
            PageName = pageName;
            DialogName = dialogName;
        }
    }
}
