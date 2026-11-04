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
    /// Every player can use them on the test server; they never run on production.
    /// </summary>
    public class IncubationTestChatCommand: IChatCommandListDefinition
    {
        public const string NotOnProductionMessage = "Incubation test commands are disabled on the live server.";

        private readonly ChatCommandBuilder _builder = new();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            FinishStage();
            SetDay();

            return _builder.Build();
        }

        private static string RejectOnProduction(uint user, params string[] args)
        {
            return ApplicationSettings.Get().ServerEnvironment == ServerEnvironmentType.Production
                ? NotOnProductionMessage
                : string.Empty;
        }

        private void FinishStage()
        {
            _builder.Create("incubationfinish", "incfinish")
                .Description("Test server: ends the running stage of each of your incubation jobs now.")
                .Permissions(AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .Validate(RejectOnProduction)
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
                    var error = RejectOnProduction(user, args);
                    if (!string.IsNullOrEmpty(error))
                        return error;

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

        /// <summary>Accepts a day name only, never a number, so "7" or "-1" are rejected.</summary>
        public static bool TryParseDay(string arg, out DayOfWeek day)
        {
            day = default;
            return !string.IsNullOrWhiteSpace(arg) &&
                   !int.TryParse(arg, out _) &&
                   Enum.TryParse(arg, true, out day) &&
                   Enum.IsDefined(day);
        }
    }
}
