using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class CraftBlueprintProcessor : IDataProcessor<CraftBlueprint>
    {
        public IValidator Validator => new CraftBlueprintValidator();

        public void Process(IDataContext db, CraftBlueprint dataObject)
        {
        }
    }
}
