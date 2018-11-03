using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class GameTopicProcessor : IDataProcessor<GameTopic>
    {
        public IValidator Validator => new GameTopicValidator();

        public void Process(IDataContext db, GameTopic dataObject)
        {
        }
    }
}
