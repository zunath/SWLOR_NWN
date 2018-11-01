using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class GameTopicCategoryProcessor : IDataProcessor<GameTopicCategory>
    {
        public IValidator Validator => new GameTopicCategoryValidator();

        public void Process(IDataContext db, GameTopicCategory dataObject)
        {
        }
    }
}
