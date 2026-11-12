using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// The NUI windows and conversations of the players online during a hot reload (MaintainabilityRefactorHotReload.md, P4-15,
    /// owner decision Q3: keep them where reasonable). While the old build quiesces, each player's open conversation is taken out
    /// as data when it can be (<see cref="IConversationService.CarryForReload"/>) and every open window is closed the way the player
    /// closes it (<see cref="IGuiService.CloseWindowsForReload"/>), because the new build's element ids and view models differ;
    /// the list is left on the module (<see cref="ReloadCarry"/>, keyed by player id). When the new build activates, after the
    /// players' restore steps have made their view models, the same windows open again and the conversation resumes at its line.
    /// </summary>
    public sealed class PlayerScreenCarry
    {
        /// <summary>The key of the carried screens on the module (<see cref="ReloadCarry"/>).</summary>
        public const string Key = "player-screens";

        private static readonly JsonSerializerSettings Settings = new()
        {
            Converters = { new StringEnumConverter() },
            DateParseHandling = DateParseHandling.None,
        };

        private readonly IGuiService _gui;
        private readonly IConversationService _conversation;

        public PlayerScreenCarry(IGuiService gui, IConversationService conversation)
        {
            _gui = gui ?? throw new ArgumentNullException(nameof(gui));
            _conversation = conversation ?? throw new ArgumentNullException(nameof(conversation));
        }

        /// <summary>
        /// Takes each player's conversation out and closes their windows, returning the screens as text keyed by player id. A player
        /// whose screen fails is logged and the next player's runs; their windows the step reached are closed.
        /// </summary>
        public string Close(IReadOnlyList<uint> players)
        {
            if (players == null)
                throw new ArgumentNullException(nameof(players));

            var screens = new Dictionary<string, PlayerScreen>(StringComparer.Ordinal);
            try
            {
                PlayerReloadPlan.RunForEach(players, player =>
                {
                    var screen = new PlayerScreen();
                    screens[GetObjectUUID(player)] = screen;
                    screen.Conversation = _conversation.CarryForReload(player);
                    screen.Windows = _gui.CloseWindowsForReload(player).ToList();
                }, nameof(PlayerScreenCarry) + "." + nameof(Close));
            }
            catch (InvalidOperationException)
            {
                // RunForEach logged each player that failed; the other players' screens are still carried.
            }

            return JsonConvert.SerializeObject(screens, Settings);
        }

        /// <summary>
        /// Opens again, for each player online now, the windows and the conversation <see cref="Close"/> recorded under their id.
        /// A player the old build did not record (none should be: no frame runs in between) gets nothing.
        /// </summary>
        public void Reopen(IReadOnlyList<uint> players, string text)
        {
            if (players == null)
                throw new ArgumentNullException(nameof(players));
            if (string.IsNullOrEmpty(text))
                return;

            var screens = JsonConvert.DeserializeObject<Dictionary<string, PlayerScreen>>(text, Settings)
                          ?? new Dictionary<string, PlayerScreen>();
            PlayerReloadPlan.RunForEach(players, player =>
            {
                if (!screens.TryGetValue(GetObjectUUID(player), out var screen) || screen == null)
                    return;

                _gui.ReopenWindowsForReload(player, screen.Windows ?? new List<GuiReloadWindow>());
                if (screen.Conversation != null)
                    _conversation.ResumeAfterReload(player, screen.Conversation);
            }, nameof(PlayerScreenCarry) + "." + nameof(Reopen));
        }
    }
}
