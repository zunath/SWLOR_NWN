using Nwn.Authoring.Documents.Native;
using Nwn.Toolset.Avalonia.Localization;
using Nwn.Toolset.Avalonia.Variables;
using SWLOR.Toolset.Domain.GameData.GameCode;

namespace SWLOR.Toolset.Editors;

/// <summary>Applies SWLOR's game-code key suggestions and validation to the shared local editor.</summary>
internal static class SwlorVarTablePolicy
{
    private static readonly string[] KnownKeys = ["QUEST_NPC_GROUP_ID", "CREATURE_SPAWN_TABLE_ID", "CREATURE_SPAWN_COUNT"];

    public static VarTableSectionViewModel Create(Func<string, Action, bool> runEdit, VarTable table,
        IGameCodeIndex? gameCodeIndex, Func<string, bool>? include = null) =>
        new(runEdit, table, KnownKeys, name =>
        {
            if (name == "QUEST_NPC_GROUP_ID" && gameCodeIndex is not null && table.GetInt(name) is { } id &&
                !gameCodeIndex.IsValidNpcGroup(id))
                return $"Warning: {id} is not a known NPCGroupType value.";
            return null;
        }, include, VarTableTexts.English);
}
