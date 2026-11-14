using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Foundation
{
    public interface ISnippetListDefinition
    {
        public Dictionary<string, SnippetDetail> BuildSnippets();
    }
}
