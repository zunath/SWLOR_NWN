using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class CraftBlueprintCategoryProcessor : IDataProcessor<CraftBlueprintCategory>
    {
        public IValidator Validator => new CraftBlueprintCategoryValidator();

        public void Process(IDataContext db, CraftBlueprintCategory dataObject)
        {
        }
    }
}
