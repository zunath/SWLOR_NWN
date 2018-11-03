using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class SkillProcessor : IDataProcessor<Skill>
    {
        public IValidator Validator => new SkillValidator();

        public void Process(IDataContext db, Skill dataObject)
        {
        }
    }
}
