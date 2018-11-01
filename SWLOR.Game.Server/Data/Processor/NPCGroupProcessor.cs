using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class NPCGroupProcessor : IDataProcessor<NPCGroup>
    {
        public IValidator Validator => new NPCGroupValidator();

        public void Process(IDataContext db, NPCGroup dataObject)
        {
        }
    }
}
