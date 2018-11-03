using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class BuildingStyleProcessor: IDataProcessor<BuildingStyle>
    {
        public IValidator Validator => new BuildingStyleValidator();

        public void Process(IDataContext db, BuildingStyle dataObject)
        {
        }
    }
}
