using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class CraftDeviceProcessor : IDataProcessor<CraftDevice>
    {
        public IValidator Validator => new CraftDeviceValidator();

        public void Process(IDataContext db, CraftDevice dataObject)
        {
        }
    }
}
