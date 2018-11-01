using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class QuestProcessor : IDataProcessor<Quest>
    {
        public IValidator Validator => new QuestValidator();

        public void Process(IDataContext db, Quest dataObject)
        {
        }
    }
}
