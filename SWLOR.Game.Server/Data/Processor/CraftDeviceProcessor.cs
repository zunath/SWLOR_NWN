using FluentValidation;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Data.Processor
{
    public class CraftDeviceProcessor : IDataProcessor<CraftDevice>
    {
        public IValidator Validator => new CraftDeviceValidator();

        public DatabaseAction Process( JObject dataObject)
        {
            return null;
        }
    }
}
