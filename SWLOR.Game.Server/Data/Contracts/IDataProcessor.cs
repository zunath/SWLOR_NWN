using FluentValidation;

namespace SWLOR.Game.Server.Data.Contracts
{
    public interface IDataProcessor<in T>
    {
        IValidator Validator { get; }
        void Process(IDataContext db, T target);
    }
}
