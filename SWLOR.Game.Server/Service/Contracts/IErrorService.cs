﻿using System;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IErrorService
    {
        void LogError(Exception ex, string @event = "");
        void Trace(string component, string log);
    }
}
