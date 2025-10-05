using System;
using System.Reflection;

namespace SWLOR.App.Server.Server
{
    /// <summary>
    /// Represents an action script that can be executed.
    /// </summary>
    public class ActionScript
    {
        public Action Action { get; set; }
        public string Name { get; set; }
        public MethodInfo MethodInfo { get; set; }
        public bool IsStatic { get; set; }
    }
}
