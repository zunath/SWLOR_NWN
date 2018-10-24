namespace SWLOR.Tools.Editor.Messages
{
    public class SettingsLoaded
    {
        public AppSettings Settings { get; set; }

        public SettingsLoaded(AppSettings settings)
        {
            Settings = settings;
        }
    }
}
