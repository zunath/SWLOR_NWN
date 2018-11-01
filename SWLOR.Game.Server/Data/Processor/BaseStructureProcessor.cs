using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class BaseStructureProcessor : IDataProcessor<BaseStructure>
    {
        public IValidator Validator => new BaseStructureValidator();

        public void Process(IDataContext db, BaseStructure dataObject)
        {
        }
    }
}
