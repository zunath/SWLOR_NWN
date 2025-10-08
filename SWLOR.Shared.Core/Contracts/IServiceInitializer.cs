namespace SWLOR.Shared.Core.Contracts
{
    public interface IServiceInitializer
    {
        /// <summary>
        /// Initializes all services that implement IServiceInitializer
        /// </summary>
        void InitializeAllServices();
    }
}