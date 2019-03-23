using FluentValidation;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Validator;

using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Data.Processor
{
    public class ModProcessor : IDataProcessor<Entity.Mod>
    {
        public IValidator Validator => new ModValidator();

        public DatabaseAction Process( JObject dataObject)
        {
            return null;
        }
    }
}
