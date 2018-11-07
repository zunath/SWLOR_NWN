using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Data.Processor
{
    public class CraftBlueprintProcessor : IDataProcessor<CraftBlueprint>
    {
        public IValidator Validator => new CraftBlueprintValidator();

        public DatabaseAction Process(IDataService data, CraftBlueprint dataObject)
        {
            return null;
        }
    }
}
