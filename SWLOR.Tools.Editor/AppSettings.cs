using Newtonsoft.Json;

namespace SWLOR.Tools.Editor
{
    public class AppSettings
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public string DatabaseIPAddress { get; set; }
        public string DatabaseUsername { get; set; }
        public string DatabaseName { get; set; }

        [JsonIgnore]
        public string DatabasePassword { get; set; }

        public AppSettings()
        {
            Width = 800;
            Height = 600;
            X = 0;
            Y = 0;

            DatabaseIPAddress = string.Empty;
            DatabaseUsername = string.Empty;
            DatabaseName = string.Empty;
            DatabasePassword = string.Empty;
        }

    }
}
