using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Data.Processor
{
    public class SkillCategoryProcessor : IDataProcessor<SkillCategory>
    {
        public IValidator Validator => new SkillCategoryValidator();

        public void Process(IDataService data, SkillCategory dataObject)
        {
        }
    }
}
