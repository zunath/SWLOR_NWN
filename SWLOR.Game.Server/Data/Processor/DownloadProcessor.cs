using FluentValidation;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Data.Processor
{
    public class DownloadProcessor : IDataProcessor<Download>
    {
        public IValidator Validator => new DownloadValidator();

        public DatabaseAction Process( JObject dataObject)
        {
            return null;
        }
    }
}
