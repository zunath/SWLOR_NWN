using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Validation
{
    /// <summary>
    /// Checks local-variable ("VarTable") conventions used to wire creatures into NPC groups and
    /// areas into random-creature spawn tables:
    /// <list type="bullet">
    /// <item>Creature instances (placed in an area's git file) and .utc blueprints: an int local
    /// named QUEST_NPC_GROUP_ID must match a known <c>NPCGroupType</c> value.</item>
    /// <item>Areas: a string local named CREATURE_SPAWN_TABLE_ID (verified against the corpus to
    /// live on the .git file's own root VarTable, not the area's AreaProperties struct or the
    /// .are file) must match a known spawn table ID.</item>
    /// <item>Areas: an int local named CREATURE_SPAWN_COUNT must be a positive integer.</item>
    /// </list>
    /// The NPCGroupType/spawn-table checks are skipped silently (no issue at all) when no
    /// <see cref="IGameCodeIndex"/> is available, or when the source scan half of the index could
    /// not run - there is nothing to validate against. The CREATURE_SPAWN_COUNT shape check does
    /// not depend on the game-code index and always runs.
    /// </summary>
    public sealed class VarTableEnumRule : IValidationRule
    {
        private const string QuestNpcGroupIdVar = "QUEST_NPC_GROUP_ID";
        private const string CreatureSpawnTableIdVar = "CREATURE_SPAWN_TABLE_ID";
        private const string CreatureSpawnCountVar = "CREATURE_SPAWN_COUNT";

        public string RuleId => "VarTableEnum";

        public IEnumerable<ValidationIssue> Validate(ValidationContext context)
        {
            var issues = new List<ValidationIssue>();
            var gameCodeIndex = context.GameCodeIndex;
            var npcGroupIndexAvailable = gameCodeIndex != null;
            var spawnTableIndexAvailable = gameCodeIndex is { IsSourceScanAvailable: true };

            foreach (var resRef in context.ResRefsFor(ResourceType.Utc))
            {
                var (document, error) = context.LoadBlueprint(ResourceType.Utc, resRef);
                if (error != null || document is not UtcDocument utc)
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        RuleId,
                        $"Failed to parse creature blueprint '{resRef}': {error?.Message}",
                        context.Workspace.GetResourcePath(ResourceType.Utc, resRef),
                        resRef));
                    continue;
                }

                if (npcGroupIndexAvailable)
                {
                    CheckNpcGroup(utc.VarTable, gameCodeIndex!, issues,
                        $"Creature blueprint '{resRef}'",
                        context.Workspace.GetResourcePath(ResourceType.Utc, resRef), resRef);
                }
            }

            foreach (var areaResRef in context.AreaResRefs)
            {
                var (git, error) = context.LoadGit(areaResRef);
                if (error != null || git == null)
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        RuleId,
                        $"Failed to parse area '{areaResRef}' git file: {error?.Message}",
                        context.GetGitPath(areaResRef),
                        areaResRef));
                    continue;
                }

                if (npcGroupIndexAvailable)
                {
                    foreach (var creature in git.Creatures)
                    {
                        var tag = creature.GetStringOrNull("Tag");
                        CheckNpcGroup(new VarTable(creature), gameCodeIndex!, issues,
                            $"Area '{areaResRef}' placed creature (Tag='{tag}')",
                            context.GetGitPath(areaResRef), tag);
                    }
                }

                if (spawnTableIndexAvailable)
                {
                    var spawnTableEntry = git.VarTable.FirstOrDefault(e => e.Name == CreatureSpawnTableIdVar);
                    if (spawnTableEntry != null)
                    {
                        if (spawnTableEntry.Type != VarTable.TypeString || string.IsNullOrEmpty(spawnTableEntry.StringValue))
                        {
                            issues.Add(new ValidationIssue(
                                ValidationSeverity.Error,
                                RuleId,
                                $"Area '{areaResRef}' local {CreatureSpawnTableIdVar} must be a non-empty string.",
                                context.GetGitPath(areaResRef),
                                areaResRef));
                        }
                        else if (!gameCodeIndex!.IsValidSpawnTableId(spawnTableEntry.StringValue!))
                        {
                            issues.Add(new ValidationIssue(
                                ValidationSeverity.Error,
                                RuleId,
                                $"Area '{areaResRef}' local {CreatureSpawnTableIdVar}='{spawnTableEntry.StringValue}' does not match any known spawn table.",
                                context.GetGitPath(areaResRef),
                                areaResRef));
                        }
                    }
                }

                var spawnCountEntry = git.VarTable.FirstOrDefault(e => e.Name == CreatureSpawnCountVar);
                if (spawnCountEntry != null)
                {
                    if (spawnCountEntry.Type != VarTable.TypeInt ||
                        spawnCountEntry.IntValue is not { } count || count <= 0)
                    {
                        issues.Add(new ValidationIssue(
                            ValidationSeverity.Error,
                            RuleId,
                            $"Area '{areaResRef}' local {CreatureSpawnCountVar} must be a positive integer.",
                            context.GetGitPath(areaResRef),
                            areaResRef));
                    }
                }
            }

            return issues;
        }

        private void CheckNpcGroup(
            VarTable varTable,
            IGameCodeIndex gameCodeIndex,
            List<ValidationIssue> issues,
            string contextLabel,
            string? filePath,
            string? resRef)
        {
            var entry = varTable.FirstOrDefault(e => e.Name == QuestNpcGroupIdVar);
            if (entry == null)
                return;

            if (entry.Type != VarTable.TypeInt || entry.IntValue == null)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    RuleId,
                    $"{contextLabel} has {QuestNpcGroupIdVar} with a non-integer value.",
                    filePath,
                    resRef));
                return;
            }

            if (!gameCodeIndex.IsValidNpcGroup(entry.IntValue.Value))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    RuleId,
                    $"{contextLabel} has {QuestNpcGroupIdVar}={entry.IntValue.Value}, which is not a known NPCGroupType value.",
                    filePath,
                    resRef));
            }
        }
    }
}
