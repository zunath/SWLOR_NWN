namespace SWLOR.Tools.Editor.Messages
{
    public class SettingsLoadedMessage
    {
        public AppSettings Settings { get; set; }

        public SettingsLoadedMessage(AppSettings settings)
        {
            Settings = settings;
        }
    }
}
