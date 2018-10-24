using System.IO;
using Caliburn.Micro;
using Newtonsoft.Json;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.Startup.Contracts;

namespace SWLOR.Tools.Editor.Startup
{
    public class PostBootstrap: IPostBootstrap
    {
        private readonly IEventAggregator _eventAggregator;

        public PostBootstrap(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        public void RunStartUp()
        {
            const string fileName = "./Settings.json";
            AppSettings settings;
            string json;

            if (!File.Exists(fileName))
            {
                settings = new AppSettings();
                json = JsonConvert.SerializeObject(settings);

                File.WriteAllText(fileName, json);
            }

            json = File.ReadAllText(fileName);
            settings = JsonConvert.DeserializeObject<AppSettings>(json);
            _eventAggregator.PublishOnUIThread(new SettingsLoaded(settings));
        }
    }
}
