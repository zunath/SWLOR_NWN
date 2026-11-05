using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    /// <summary>
    /// Test-server shortcuts for incubation: end a running stage now, and pick the weekday mutations check.
    /// They exist only on a server whose SWLOR_ENVIRONMENT is explicitly test (every player) or dev (Admin).
    /// Production never registers them, and neither does an unset or mistyped environment.
    /// </summary>
    public class IncubationTestChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            var settings = ApplicationSettings.Get();
            return BuildChatCommands(settings.ServerEnvironment, settings.ServerEnvironmentIsExplicit);
        }

        public Dictionary<string, ChatCommandDetail> BuildChatCommands(ServerEnvironmentType environment, bool environmentIsExplicit)
        {
            if (!IsAvailable(environment, environmentIsExplicit))
                return new Dictionary<string, ChatCommandDetail>();

            FinishStage();
            SetDay();

            return _builder.Build();
        }

        /// <summary>Only an explicitly named test or development server gets these commands.</summary>
        public static bool IsAvailable(ServerEnvironmentType environment, bool environmentIsExplicit)
        {
            return environmentIsExplicit &&
                   environment is ServerEnvironmentType.Test or ServerEnvironmentType.Development;
        }

        private void FinishStage()
        {
            _builder.Create("incubationfinish", "incfinish")
                .Description("Test server: ends the running stage of each of your incubation jobs now.")
                .Permissions(AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .Action((user, target, location, args) =>
                {
                    var playerId = GetObjectUUID(user);
                    var query = new DBQuery<IncubationJob>()
                        .AddFieldSearch(nameof(IncubationJob.PlayerId), playerId, false);
                    var finished = IncubationTestTools.FinishRunningStages(DB.Search(query).ToList(), DateTime.UtcNow);

                    if (finished.Count == 0)
                    {
                        SendMessageToPC(user, "You have no incubation stage running.");
                        return;
                    }

                    foreach (var job in finished)
                    {
                        DB.Set(job);
                        Log.Write(LogGroup.Incubation, $"Test command: '{GetName(user)}' ({playerId}) finished stage {job.CurrentStage} of incubation job '{job.Id}'.");
                    }

                    // The open window would keep showing the old countdown.
                    Gui.CloseWindow(user, GuiWindowType.Incubator, user);
                    var stages = string.Join(", ", finished.Select(job => $"{BeastMastery.GetBeastDetail(job.BeastDNAType).Name} stage {job.CurrentStage}"));
                    SendMessageToPC(user, $"Finished: {stages}. Use the incubator to start the next stage or complete the job.");
                });
        }

        private void SetDay()
        {
            _builder.Create("incubationday", "incday")
                .Description("Test server: sets the weekday your incubation mutations check, such as /incubationday Monday. /incubationday clear goes back to the server day.")
                .Permissions(AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .Validate((user, args) =>
                {
                    if (args.Length < 1)
                        return "Name a day, such as /incubationday Monday, or /incubationday clear.";

                    return IsClear(args[0]) || TryParseDay(args[0], out _)
                        ? string.Empty
                        : $"{args[0]} is not a day. Use Monday through Sunday, or clear.";
                })
                .Action((user, target, location, args) =>
                {
                    var playerId = GetObjectUUID(user);
                    if (IsClear(args[0]))
                    {
                        IncubationTestTools.ClearDayOverride(playerId);
                        SendMessageToPC(user, $"Incubation uses the server day again ({DateTime.UtcNow.DayOfWeek} UTC).");
                    }
                    else
                    {
                        TryParseDay(args[0], out var day);
                        IncubationTestTools.SetDayOverride(playerId, day);
                        SendMessageToPC(user, $"Your incubation mutations now check {day}. This resets when the server restarts.");
                    }

                    Gui.CloseWindow(user, GuiWindowType.Incubator, user);
                });
        }

        private static bool IsClear(string arg)
        {
            return string.Equals(arg, "clear", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Accepts one whole day name only. Enum.TryParse would also take numbers ("7") and comma lists
        /// ("Monday,Tuesday" combines to Wednesday), so the name is matched exactly.
        /// </summary>
        public static bool TryParseDay(string arg, out DayOfWeek day)
        {
            foreach (var candidate in Enum.GetValues<DayOfWeek>())
            {
                if (string.Equals(candidate.ToString(), arg?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    day = candidate;
                    return true;
                }
            }

            day = default;
            return false;
        }
    }
}
