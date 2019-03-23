using FluentValidation;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Data.Processor
{
    public class SkillProcessor : IDataProcessor<Skill>
    {
        public IValidator Validator => new SkillValidator();

        public DatabaseAction Process( JObject dataObject)
        {
            return null;
        }
    }
}
