using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class PerkCategoryProcessor : IDataProcessor<PerkCategory>
    {
        public IValidator Validator => new PerkCategoryValidator();

        public void Process(IDataContext db, PerkCategory dataObject)
        {
        }
    }
}
