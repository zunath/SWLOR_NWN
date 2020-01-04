using System;

namespace SWLOR.Game.Server
{
    public class ApplicationSettings
    {
        public string LogDirectory { get; }
        public int ImpoundPruneDays { get; }

        private static ApplicationSettings _settings;
        public static ApplicationSettings Get()
        {
            if (_settings == null)
                _settings = new ApplicationSettings();

            return _settings;
        }

        private ApplicationSettings()
        {
            LogDirectory = Environment.GetEnvironmentVariable("SWLOR_LOG_DIRECTORY");
            ImpoundPruneDays = Convert.ToInt32(Environment.GetEnvironmentVariable("SWLOR_IMPOUND_PRUNE_DAYS"));
        }

    }
}