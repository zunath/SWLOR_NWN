using FluentValidation;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Data.Contracts
{
    public interface IDataProcessor<in T>
    {
        IValidator Validator { get; }
        void Process(IDataService db, T target);
    }
}
