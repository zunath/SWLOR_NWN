using FluentValidation;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;

using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Data.Processor
{
    public class KeyItemCategoryProcessor : IDataProcessor<KeyItemCategory>
    {
        public IValidator Validator => new KeyItemCategoryValidator();

        public DatabaseAction Process( JObject dataObject)
        {
            return null;
        }
    }
}
