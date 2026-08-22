using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.ConversationService
{
    /// <summary>
    /// A data-only conversation graph. Links use stable node IDs so conversations can contain
    /// shared branches and loops without duplicating their content.
    /// </summary>
    public sealed class ConversationGraph
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = "Conversation";
        public List<ConversationLink> EntryPoints { get; set; } = new();
        public Dictionary<string, ConversationNode> Nodes { get; set; } = new();
        public Dictionary<string, ConversationChoice> Choices { get; set; } = new();
        public uint DefaultNpcDelay { get; set; }
        public uint DefaultPlayerDelay { get; set; }
        public bool PreventZoomIn { get; set; }
        public List<ConversationAction> OnStartActions { get; set; } = new();
        public List<ConversationAction> OnEndActions { get; set; } = new();
        public List<ConversationAction> OnAbortActions { get; set; } = new();
    }

    public sealed class ConversationNode
    {
        public string Id { get; set; } = string.Empty;
        public string SpeakerName { get; set; } = string.Empty;
        public string SpeakerTag { get; set; } = string.Empty;
        public string PortraitResref { get; set; } = string.Empty;
        public string SoundResref { get; set; } = string.Empty;
        public uint Animation { get; set; }
        public bool AnimationLoops { get; set; } = true;
        public uint Delay { get; set; } = uint.MaxValue;
        public string Comment { get; set; } = string.Empty;
        public string JournalQuest { get; set; } = string.Empty;
        public List<ConversationTextBlock> Text { get; set; } = new();
        public List<ConversationAction> OnEnterActions { get; set; } = new();
        public List<ConversationChoiceLink> Choices { get; set; } = new();
    }

    public sealed class ConversationChoice
    {
        public string Id { get; set; } = string.Empty;
        public ConversationTextBlock Text { get; set; } = new();
        public string SoundResref { get; set; } = string.Empty;
        public uint Animation { get; set; }
        public bool AnimationLoops { get; set; } = true;
        public uint Delay { get; set; } = uint.MaxValue;
        public string Comment { get; set; } = string.Empty;
        public string JournalQuest { get; set; } = string.Empty;
        public List<ConversationAction> Actions { get; set; } = new();
        public List<ConversationLink> Next { get; set; } = new();
        public bool EndsConversation { get; set; }
        public bool IsAutomatic { get; set; }
    }

    /// <summary>
    /// An ordered route from an NPC line to a shared player choice. Conditions belong to this
    /// route, matching NWN's DLG semantics, rather than to the choice it points at.
    /// </summary>
    public sealed class ConversationChoiceLink
    {
        public string ChoiceId { get; set; } = string.Empty;
        public List<ConversationCondition> Conditions { get; set; } = new();
    }

    /// <summary>
    /// An ordered link to another NPC line. The first link whose conditions pass is selected.
    /// </summary>
    public sealed class ConversationLink
    {
        public string TargetNodeId { get; set; } = string.Empty;
        public List<ConversationCondition> Conditions { get; set; } = new();
    }

    public sealed class ConversationCondition
    {
        public string Key { get; set; } = string.Empty;
        public List<string> Arguments { get; set; } = new();
        public bool IsNegated { get; set; }
    }

    public sealed class ConversationAction
    {
        public string Key { get; set; } = string.Empty;
        public List<string> Arguments { get; set; } = new();
    }

    /// <summary>
    /// A separately styled block of dialogue. NUI does not support NWN inline color tokens,
    /// so semantic runs such as stage directions and highlights are represented explicitly.
    /// </summary>
    public sealed class ConversationTextBlock
    {
        public string Text { get; set; } = string.Empty;
        public ConversationTextStyle Style { get; set; } = ConversationTextStyle.Normal;
        public ConversationColor Color { get; set; }
    }

    public sealed class ConversationColor
    {
        public byte Red { get; set; } = 255;
        public byte Green { get; set; } = 255;
        public byte Blue { get; set; } = 255;
        public byte Alpha { get; set; } = 255;
    }

    public enum ConversationTextStyle
    {
        Normal = 0,
        Action = 1,
        Highlight = 2,
        Check = 3,
        PlayerReply = 4,
        Muted = 5,
        Custom = 6
    }
}
