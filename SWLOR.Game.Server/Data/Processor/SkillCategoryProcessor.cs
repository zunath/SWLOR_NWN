using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class SkillCategoryProcessor : IDataProcessor<SkillCategory>
    {
        public IValidator Validator => new SkillCategoryValidator();

        public void Process(IDataContext db, SkillCategory dataObject)
        {
        }
    }
}
