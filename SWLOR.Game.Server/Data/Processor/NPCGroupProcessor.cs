using FluentValidation;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Data.Processor
{
    public class NPCGroupProcessor : IDataProcessor<NPCGroup>
    {
        public IValidator Validator => new NPCGroupValidator();

        public DatabaseAction Process( JObject dataObject)
        {
            return null;
        }
    }
}
