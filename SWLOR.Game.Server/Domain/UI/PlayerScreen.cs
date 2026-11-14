using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.UI
{
    /// <summary>What one player had on screen when a hot reload quiesced the old build (<see cref="PlayerScreenCarry"/>).</summary>
    public sealed class PlayerScreen
    {
        /// <summary>The windows the old build closed that the new build reopens.</summary>
        public List<GuiReloadWindow> Windows { get; set; } = new();

        /// <summary>The conversation the new build resumes, or null.</summary>
        public ConversationReloadState Conversation { get; set; }
    }
}
