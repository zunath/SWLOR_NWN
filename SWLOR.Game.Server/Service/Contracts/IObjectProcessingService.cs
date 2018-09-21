using System;
using SWLOR.Game.Server.Processor.Contracts;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IObjectProcessingService
    {
        void OnModuleLoad();
        float ProcessingTickInterval { get; }
        string RegisterProcessingEvent<T>(params object[] args) 
            where T : IEventProcessor;
        void UnregisterProcessingEvent(string globalID);
    }
}