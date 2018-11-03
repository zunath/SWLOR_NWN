using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Validator;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Data.Processor
{
    public class CustomEffectProcessor : IDataProcessor<Entity.CustomEffect>
    {
        public IValidator Validator => new CustomEffectValidator();

        public void Process(IDataService data, Entity.CustomEffect dataObject)
        {
        }
    }
}
