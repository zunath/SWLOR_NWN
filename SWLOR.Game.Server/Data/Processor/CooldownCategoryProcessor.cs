using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Data.Processor
{
    public class CooldownCategoryProcessor : IDataProcessor<CooldownCategory>
    {
        public IValidator Validator => new CooldownCategoryValidator();

        public void Process(IDataService data, CooldownCategory dataObject)
        {
        }
    }
}
