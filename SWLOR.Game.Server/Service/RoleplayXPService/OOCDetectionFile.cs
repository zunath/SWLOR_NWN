using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.RoleplayXPService
{
    /// <summary>
    /// JSON layout of the external OOC detection file. Every field is optional.
    /// </summary>
    public class OOCDetectionFile
    {
        public double? Threshold { get; set; }
        public List<string> Terms { get; set; }
        public Dictionary<string, double> Weights { get; set; }
    }
}
