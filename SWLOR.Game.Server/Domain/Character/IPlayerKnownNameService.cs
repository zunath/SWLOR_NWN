using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>The names a player gives other characters: validation, the writes that set and forget one, and the search.</summary>
    public interface IPlayerKnownNameService
    {
        List<string> SearchKnownPlayerIdsByName(uint observer, string searchText, int maxResults);

        string ValidateKnownName(string name);

        string ValidateKnownNameInput(string name);

        string ValidateKnownNameAssignment(uint observer, uint target, string name);

        void SetKnownName(uint observer, uint target, string name);

        void ForgetKnownName(uint observer, uint target);

        int DeleteKnownNameReferences(string targetIdentityKey);
    }
}
