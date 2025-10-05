using System.Reflection;
using SWLOR.Shared.Abstractions.Delegates;

namespace SWLOR.Game.Server.Server
{
    /// <summary>
    /// Represents a conditional script that returns a boolean result.
    /// </summary>
    public class ConditionalScript
    {
        public ConditionalScriptDelegate Action { get; set; }
        public string Name { get; set; }
        public MethodInfo MethodInfo { get; set; }
        public bool IsStatic { get; set; }
    }
}
