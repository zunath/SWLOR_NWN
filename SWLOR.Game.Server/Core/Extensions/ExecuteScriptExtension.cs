using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Make oTarget run sScript and then return execution to the calling script.
        ///   If sScript does not specify a compiled script, nothing happens.
        /// </summary>
        public static void ExecuteScript(string sScript, uint oTarget)
        {
            // Note: Bypass the NWScript round-trip and directly call the script execution.
            Internal.DirectRunScript(sScript, oTarget);
        }

    }
}
