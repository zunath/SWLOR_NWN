using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class CooldownCategoryProcessor : IDataProcessor<CooldownCategory>
    {
        public IValidator Validator => new CooldownCategoryValidator();

        public void Process(IDataContext db, CooldownCategory dataObject)
        {
        }
    }
}
