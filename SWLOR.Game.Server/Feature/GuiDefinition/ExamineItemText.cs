using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>Renders legacy item colors explicitly because NUI text does not interpret color tokens.</summary>
    public static class ExamineItemText
    {
        // Leave room for the fixed inspection panel's group padding and vertical scrollbar.
        public const float TextWidth = 300f;
        public const float LineHeight = 20f;

        // Stock NUI fnt_maintext ASCII advances, in the font's units. nui_skin.tml uses a
        // 17px font height, 806/-274 ascent/descent, and 0.5px horizontal spacing.
        private static readonly int[] GlyphAdvances =
        {
            451, 210, 245, 522, 409, 669, 720, 124, 307, 307, 392, 424, 185, 334, 185, 320,
            480, 296, 457, 430, 530, 415, 495, 440, 500, 495, 185, 185, 424, 424, 424, 400,
            659, 659, 504, 713, 708, 498, 490, 744, 660, 243, 250, 559, 485, 850, 696, 780,
            474, 780, 531, 486, 586, 677, 622, 861, 591, 518, 573, 290, 320, 290, 424, 500,
            199, 416, 492, 452, 511, 449, 343, 409, 468, 194, 195, 423, 188, 706, 476, 498,
            519, 512, 308, 332, 407, 482, 436, 703, 435, 418, 448, 293, 168, 293, 424
        };

        public static List<ExamineItemTextRun> Parse(string text)
        {
            var runs = new List<ExamineItemTextRun>();
            text ??= string.Empty;
            var color = GuiColor.White;
            var start = 0;
            var index = 0;
            while (index < text.Length)
            {
                var isColor = TryDecodeStartToken(text, index, out var red, out var green, out var blue);
                var isEnd = text.AsSpan(index).StartsWith("</c>", StringComparison.OrdinalIgnoreCase);
                if (!isColor && !isEnd)
                {
                    index++;
                    continue;
                }

                if (index > start)
                    runs.Add(new ExamineItemTextRun(text[start..index], color));
                color = isColor ? new GuiColor(red, green, blue) : GuiColor.White;
                index += isColor ? 6 : 4;
                start = index;
            }

            if (start < text.Length)
                runs.Add(new ExamineItemTextRun(text[start..], color));
            return runs;
        }

        public static string PlainText(string text) => string.Concat(Parse(text).Select(run => run.Text));

        public static GuiColor TitleColor(string text) => Parse(text).FirstOrDefault()?.Color ?? GuiColor.White;

        private static bool TryDecodeStartToken(string text, int index, out byte red, out byte green, out byte blue)
        {
            if (ColorToken.TryDecodeStartToken(text, index, out red, out green, out blue))
                return true;

            // Authored item tokens can contain raw CP1252 bytes (including 0xFF/ÿ), whereas
            // ColorToken's historical channel table normalizes some of those characters.
            return index + 5 < text.Length && text[index] == '<' && text[index + 1] == 'c' && text[index + 5] == '>' &&
                   TryDecodeChannel(text[index + 2], out red) && TryDecodeChannel(text[index + 3], out green) &&
                   TryDecodeChannel(text[index + 4], out blue);
        }

        private static bool TryDecodeChannel(char value, out byte channel)
        {
            if (value <= byte.MaxValue)
            {
                channel = (byte)value;
                return true;
            }

            const string highChannels = "€\u0081‚ƒ„…†‡ˆ‰Š‹Œ\u008dŽ\u008f\u0090‘’“”•–—˜™š›œ\u009džŸ";
            var index = highChannels.IndexOf(value);
            channel = index < 0 ? (byte)0 : (byte)(128 + index);
            return index >= 0;
        }

        public static List<ExamineItemTextRun> Wrap(string text, float width = TextWidth)
        {
            if (width <= 0 || !float.IsFinite(width))
                throw new ArgumentOutOfRangeException(nameof(width));

            // Keep colors attached to characters so a word split by a color change still wraps as a word.
            var characters = Parse(text).SelectMany(run => run.Text
                .Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Replace("\t", "    ")
                .EnumerateRunes().Select(rune => (Text: rune.ToString(), Color: run.Color))).ToList();
            var result = new List<ExamineItemTextRun>();
            var x = 0f;
            var y = 0f;
            for (var index = 0; index < characters.Count; index++)
            {
                var character = characters[index];
                if (character.Text == "\n")
                {
                    x = 0;
                    y += LineHeight;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(character.Text) &&
                    (index == 0 || string.IsNullOrWhiteSpace(characters[index - 1].Text)))
                {
                    var wordWidth = 0f;
                    for (var next = index; next < characters.Count && !string.IsNullOrWhiteSpace(characters[next].Text); next++)
                        wordWidth += Advance(characters[next].Text);
                    if (x > 0 && x + wordWidth > width)
                    {
                        x = 0;
                        y += LineHeight;
                    }
                }

                var advance = Advance(character.Text);
                if (x > 0 && x + advance > width)
                {
                    x = 0;
                    y += LineHeight;
                    // A space that forces a wrap would otherwise indent the next line.
                    if (string.IsNullOrWhiteSpace(character.Text))
                        continue;
                }

                var last = result.LastOrDefault();
                if (last != null && last.Y == y &&
                    last.Color.R == character.Color.R && last.Color.G == character.Color.G && last.Color.B == character.Color.B)
                    result[^1] = last with { Text = last.Text + character.Text };
                else
                    result.Add(new ExamineItemTextRun(character.Text, character.Color, x, y));
                x += advance;
            }
            return result;
        }

        private static float Advance(string character)
        {
            var value = character[0];
            return value is >= ' ' and <= '~' ? GlyphAdvances[value - ' '] * (17f / 1080f) + 0.5f : 17f;
        }

        public static Json BuildLayout<TViewModel>(string text, float height, float width = TextWidth)
            where TViewModel : IGuiViewModel
        {
            var host = new GuiGroup<TViewModel>();
            host.SetShowBorder(false)
                .SetScrollbars(NuiScrollbars.None);
            host.SetHeight(height);
            host.AddColumn(column => column
                .AddRow(row =>
                {
                    if (!Parse(text).Any(run => run.Color.R != 255 || run.Color.G != 255 || run.Color.B != 255))
                    {
                        row.AddText()
                            .SetText(PlainText(text))
                            .SetHeight(height);
                        return;
                    }

                    var runs = Wrap(text, width);
                    row.AddGroup(scroll =>
                    {
                        scroll.SetScrollbars(NuiScrollbars.Auto);
                        scroll.AddColumn(content => content
                            .AddRow(line => line
                                .AddLabel()
                                .SetText(string.Empty)
                                .SetWidth(width)
                                .SetMargin(0f)
                                .SetHeight(Math.Max(LineHeight, runs.LastOrDefault()?.Y + LineHeight ?? LineHeight))
                                .AddDrawList(draw =>
                                {
                                    draw.SetIsConstrainedToTargetBounds(true);
                                    foreach (var run in runs)
                                        draw.AddText(item => item
                                            .SetText(run.Text)
                                            .SetColor(run.Color)
                                            .SetBounds(run.X, run.Y, width - run.X, LineHeight));
                                })));
                    })
                        .SetHeight(height);
                }));
            return host.ToJson();
        }
    }
}
