﻿using FluentValidation;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Data.Contracts
{
    public interface IDataProcessor<in T>
    {
        IValidator Validator { get; }
        DatabaseAction Process(JObject target);
    }
}
