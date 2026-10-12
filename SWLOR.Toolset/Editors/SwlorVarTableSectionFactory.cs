using Nwn.Authoring.Documents.Native;
using SWLOR.Toolset.Domain.GameData.GameCode;

namespace SWLOR.Toolset.Editors;

/// <summary>Builds the shared local-variable editor with SWLOR's key suggestions and NPC-group validation.</summary>
internal sealed class SwlorVarTableSectionFactory : IVarTableSectionFactory
{
    private readonly IGameCodeIndex? _gameCodeIndex;

    public SwlorVarTableSectionFactory(IGameCodeIndex? gameCodeIndex)
    {
        _gameCodeIndex = gameCodeIndex;
    }

    public VarTableSectionViewModel Create(Func<string, Action, bool> runEdit, VarTable table) =>
        SwlorVarTablePolicy.Create(runEdit, table, _gameCodeIndex);
}
