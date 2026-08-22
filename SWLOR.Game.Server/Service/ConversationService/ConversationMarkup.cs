using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.ConversationService
{
    /// <summary>Managed conversion of legacy NWN color tokens into NUI-native styled blocks.</summary>
    public static class ConversationMarkup
    {
        public static List<ConversationTextBlock> ParseLegacyColors(
            string text,
            ConversationTextStyle defaultStyle)
        {
            text ??= string.Empty;
            var blocks = new List<ConversationTextBlock>();
            ConversationColor color = null;
            var segmentStart = 0;
            var index = 0;

            while (index < text.Length)
            {
                if (ColorToken.TryDecodeStartToken(text, index, out var red, out var green, out var blue))
                {
                    AddSegment(blocks, text, segmentStart, index - segmentStart, defaultStyle, color);
                    color = new ConversationColor { Red = red, Green = green, Blue = blue };
                    index += 6;
                    segmentStart = index;
                    continue;
                }

                if (index + 3 < text.Length &&
                    string.Compare(text, index, "</c>", 0, 4, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    AddSegment(blocks, text, segmentStart, index - segmentStart, defaultStyle, color);
                    color = null;
                    index += 4;
                    segmentStart = index;
                    continue;
                }

                index++;
            }

            AddSegment(blocks, text, segmentStart, text.Length - segmentStart, defaultStyle, color);
            if (blocks.Count == 0)
                blocks.Add(new ConversationTextBlock { Style = defaultStyle });
            return blocks;
        }

        public static ConversationTextBlock CollapseForChoice(string text)
        {
            var blocks = ParseLegacyColors(text, ConversationTextStyle.PlayerReply);
            var firstColored = blocks.FirstOrDefault(block => block.Style == ConversationTextStyle.Custom);
            return new ConversationTextBlock
            {
                Text = string.Concat(blocks.Select(block => block.Text)),
                Style = firstColored == null
                    ? ConversationTextStyle.PlayerReply
                    : ConversationTextStyle.Custom,
                Color = firstColored?.Color
            };
        }

        private static void AddSegment(
            ICollection<ConversationTextBlock> blocks,
            string source,
            int start,
            int length,
            ConversationTextStyle defaultStyle,
            ConversationColor color)
        {
            if (length <= 0)
                return;

            blocks.Add(new ConversationTextBlock
            {
                Text = source.Substring(start, length),
                Style = color == null ? defaultStyle : ConversationTextStyle.Custom,
                Color = color
            });
        }
    }
}
