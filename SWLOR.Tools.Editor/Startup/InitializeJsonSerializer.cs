using Autofac;
using Newtonsoft.Json;

namespace SWLOR.Tools.Editor.Startup
{
    public class InitializeJsonSerializer: IStartable
    {
        public void Start()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

        }
    }
}
