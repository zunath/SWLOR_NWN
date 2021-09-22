using System;
using System.Collections.Generic;
using System.Reflection;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Service.GuiService
{
    public interface IGuiWidget
    {
        string Id { get; }
        public List<IGuiWidget> Elements { get; }
        public Dictionary<string, MethodInfo> Events { get; }
        Json ToJson();
    }
}
