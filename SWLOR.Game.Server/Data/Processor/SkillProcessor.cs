using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Data.Processor
{
    public class SkillProcessor : IDataProcessor<Skill>
    {
        public IValidator Validator => new SkillValidator();

        public void Process(IDataService data, Skill dataObject)
        {
        }
    }
}
