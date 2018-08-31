using System;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IObjectProcessingService
    {
        void OnModuleLoad();

        float ProcessingTickInterval { get; }
        string RegisterProcessingEvent(Action action);
        void UnregisterProcessingEvent(string globalID);
    }
}