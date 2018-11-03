using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Data.Processor
{
    public class DownloadProcessor : IDataProcessor<Download>
    {
        public IValidator Validator => new DownloadValidator();

        public void Process(IDataService data, Download dataObject)
        {
        }
    }
}
