using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the chat commands. The logic and state live in <see cref="IChatCommandService"/>; every member
    /// here forwards to the container's instance, and the event handlers stay here so their names in the handler order
    /// manifest do not change. The command definitions in <c>Feature/ChatCommandDefinition</c> keep calling this class.
    /// </summary>
    public static class ChatCommand
    {
        private static IChatCommandService Instance => FacadeAccessor.Resolve<IChatCommandService>();

        public static string HelpTextPlayer => Instance.HelpTextPlayer;
        public static string HelpTextEmote => Instance.HelpTextEmote;
        public static string HelpTextDM => Instance.HelpTextDM;
        public static string HelpTextAdmin => Instance.HelpTextAdmin;

        public static GuiBindingList<string> EmoteNames => Instance.EmoteNames;
        public static GuiBindingList<string> EmoteDescriptions => Instance.EmoteDescriptions;
        public static List<Animation> EmoteAnimations => Instance.EmoteAnimations;
        public static GuiBindingList<bool> EmoteIsLooping => Instance.EmoteIsLooping;

        /// <summary>
        /// Loads all chat commands into cache and builds the related help text.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void OnModuleLoad()
        {
            Instance.OnModuleLoad();
        }

        /// <summary>
        /// Handles validating and processing chat commands sent by players and DMs.
        /// </summary>
        [NWNEventHandler(ScriptName.OnNWNXChat)]
        public static void HandleChatMessage()
        {
            Instance.HandleChatMessage();
        }
    }
}
