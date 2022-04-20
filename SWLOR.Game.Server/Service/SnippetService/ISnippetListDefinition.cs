using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SnippetService
{
    public interface ISnippetListDefinition
    {
        public Dictionary<string, SnippetDetail> BuildSnippets();
    }
}
