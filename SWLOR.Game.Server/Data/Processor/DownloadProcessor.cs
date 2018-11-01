using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class DownloadProcessor : IDataProcessor<Download>
    {
        public IValidator Validator => new DownloadValidator();

        public void Process(IDataContext db, Download dataObject)
        {
        }
    }
}
