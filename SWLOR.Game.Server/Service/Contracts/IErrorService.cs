using System;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IErrorService
    {
        void LogError(Exception ex, string @event = "");
        void Trace(TraceComponent component, string log);
    }
}
