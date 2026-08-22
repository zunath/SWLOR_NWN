using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SnippetService;

namespace SWLOR.Game.Server.Feature.SnippetDefinition
{
    public class SkillSnippetDefinition: ISnippetListDefinition
    {
        private readonly SnippetBuilder _builder = new SnippetBuilder();
        public Dictionary<string, SnippetDetail> BuildSnippets()
        {
            // Conditions
            ConditionHasAnySkill();
            ConditionHasAllSkills();

            // Actions

            return _builder.Build();
        }

        private void ConditionHasAnySkill()
        {
            _builder.Create("condition-any-skill")
                .Description("Checks whether a player has any skill at a minimum rank.")
                .Phrase("the player has {skillId} at rank {rank} or better")
                .NegatedPhrase("the player has no skill in {skillId} at rank {rank}")
                .Argument("skillId", SnippetArgumentType.SkillId)
                .Argument("rank", SnippetArgumentType.SkillRank)
                .Repeats(2)
                .AppearsWhenAction((player, args) =>
                {
                    // Missing at least one pair of arguments.
                    if (args.Length < 2)
                    {
                        const string Error = "'condition-has-any-skill' requires at least two arguments: the first should be the skillId and the second should be the minimum rank required.";
                        SendMessageToPC(player, Error);
                        Log.Write(LogGroup.Error, Error);
                        return false;
                    }

                    // At least one pair of arguments is valid, but one or more are invalid.
                    if (args.Length % 2 != 0)
                    {
                        const string Error = "'condition-has-any-skill' requires a pair of two arguments for each skill: the first should be the skillId and the second should be the minimum rank required.";
                        SendMessageToPC(player, Error);
                        Log.Write(LogGroup.Error, Error);
                        return false;
                    }

                    var playerId = GetObjectUUID(player);
                    var dbPlayer = DB.Get<Player>(playerId);

                    for (var index = 0; index < args.Length; index += 2)
                    {
                        var skillId = args[index];

                        // Attempt to parse the skill Id.
                        if (!Enum.TryParse(typeof(SkillType), skillId, true, out var skillObj))
                        {
                            string error = $"'condition-has-any-skill' could not parse the skill at index {index}";
                            SendMessageToPC(player, error);
                            Log.Write(LogGroup.Error, error);
                            return false;
                        }

                        if (skillObj == null) return false;

                        var skill = (SkillType)skillObj;
                        var requiredRankStr = args[index + 1];

                        // Attempt to parse the required rank.
                        if (!int.TryParse(requiredRankStr, out var requiredRank))
                        {
                            string error = $"'condition-has-any-skill' could not parse the required rank at index {index + 1}";
                            SendMessageToPC(player, error);
                            Log.Write(LogGroup.Error, error);
                            return false;
                        }

                        // Player doesn't have the skill somehow.
                        if (!dbPlayer.Skills.ContainsKey(skill))
                            continue;

                        // This one is met - return true.
                        if (dbPlayer.Skills[skill].Rank >= requiredRank)
                            return true;

                    }

                    return false;
                });
        }

        private void ConditionHasAllSkills()
        {
            _builder.Create("condition-all-skills")
                .Description("Checks whether a player has all skills at a minimum rank.")
                .Phrase("the player has every one of {skillId} at rank {rank} or better")
                .NegatedPhrase("the player is short of rank {rank} in at least one of {skillId}")
                .Argument("skillId", SnippetArgumentType.SkillId)
                .Argument("rank", SnippetArgumentType.SkillRank)
                .Repeats(2)
                .AppearsWhenAction((player, args) =>
                {
                    // Missing at least one pair of arguments.
                    if (args.Length < 2)
                    {
                        const string Error = "'condition-has-all-skills' requires at least two arguments: the first should be the skillId and the second should be the minimum rank required.";
                        SendMessageToPC(player, Error);
                        Log.Write(LogGroup.Error, Error);
                        return false;
                    }

                    // At least one pair of arguments is valid, but one or more are invalid.
                    if (args.Length % 2 != 0)
                    {
                        const string Error = "'condition-has-all-skills' requires a pair of two arguments for each skill: the first should be the skillId and the second should be the minimum rank required.";
                        SendMessageToPC(player, Error);
                        Log.Write(LogGroup.Error, Error);
                        return false;
                    }

                    var playerId = GetObjectUUID(player);
                    var dbPlayer = DB.Get<Player>(playerId);

                    for (var index = 0; index < args.Length; index += 2)
                    {
                        var skillId = args[index];

                        // Attempt to parse the skill Id.
                        if (!Enum.TryParse(typeof(SkillType), skillId, true, out var skillObj))
                        {
                            string error = $"'condition-has-all-skills' could not parse the skill at index {index}";
                            SendMessageToPC(player, error);
                            Log.Write(LogGroup.Error, error);
                            return false;
                        }

                        if (skillObj == null) return false;

                        var skill = (SkillType)skillObj;
                        var requiredRankStr = args[index + 1];

                        // Attempt to parse the required rank.
                        if (!int.TryParse(requiredRankStr, out var requiredRank))
                        {
                            string error = $"'condition-has-all-skills' could not parse the required rank at index {index + 1}";
                            SendMessageToPC(player, error);
                            Log.Write(LogGroup.Error, error);
                            return false;
                        }

                        // Player doesn't have the skill somehow.
                        if (!dbPlayer.Skills.ContainsKey(skill))
                            return false;

                        // The player doesn't meet this requirement. Return false.
                        if (dbPlayer.Skills[skill].Rank < requiredRank)
                            return false;

                    }

                    return true;
                });
        }
    }
}
