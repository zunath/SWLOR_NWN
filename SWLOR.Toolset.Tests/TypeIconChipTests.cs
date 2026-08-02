using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Render.Icons;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The same fallback symbols at the size the palette's type selector draws them - around 18-22px.
    /// A symbol that survives a palette tile can still fail here: a dash, a coin edge or a speaker wave is
    /// finer than a pixel at button size, and the failure is silent, because the pixels are all still there
    /// in the image and only the meaning is gone. So these tests check the parts that carry the meaning:
    /// that the chip versions are still eight different pictures, that their marks are solid ink rather
    /// than anti-aliased ghosts, and that the tile versions did not move while the chip ones were added.
    /// </summary>
    public class TypeIconChipTests
    {
        private const int ChipSize = 18;
        private const int TileSize = 128;

        /// <summary>
        /// Alpha at which a mark is genuinely visible rather than a hint of anti-aliasing. Held below full
        /// opacity because a diagonal chip stroke is a couple of pixels wide and never lands squarely on
        /// the grid: the mark is continuous to the eye while individual pixels dip into the low 200s.
        /// </summary>
        private const byte SolidAlpha = 200;

        private static readonly ResourceType[] PaletteTypes =
        {
            ResourceType.Utc, ResourceType.Uti, ResourceType.Utp, ResourceType.Utd,
            ResourceType.Utm, ResourceType.Utt, ResourceType.Uts, ResourceType.Utw
        };

        private static readonly int[] ChipSizes = { 18, 20, 22 };

        /// <summary>Coverage of each tile symbol before the chip variants existed, in opaque pixels.</summary>
        /// <remarks>
        /// Pinned as counts rather than as an image digest: a count moves loudly when a shape changes but
        /// tolerates the last bit of a trigonometric result differing between runtimes, which an exact
        /// digest of the speaker's arcs would not. Refresh these only when a tile symbol is meant to change.
        /// </remarks>
        private static readonly (ResourceType Type, int Coverage)[] TileCoverage =
        {
            (ResourceType.Utp, 5984), (ResourceType.Utc, 4006), (ResourceType.Utd, 4901),
            (ResourceType.Uti, 4430), (ResourceType.Utm, 6098), (ResourceType.Utt, 6382),
            (ResourceType.Uts, 2382), (ResourceType.Utw, 1886)
        };

        private static byte AlphaAt(IconImage image, int x, int y) =>
            image.Bgra[y * image.Stride + x * IconImage.BytesPerPixel + 3];

        private static int CountPixels(IconImage image, byte minimumAlpha)
        {
            var count = 0;
            for (var i = 3; i < image.Bgra.Length; i += IconImage.BytesPerPixel)
            {
                if (image.Bgra[i] >= minimumAlpha)
                    count++;
            }

            return count;
        }

        /// <summary>Total coverage, in pixels, of the marks crossing row <paramref name="y"/>.</summary>
        private static float InkWeightInRow(IconImage image, int y, int fromX, int toX)
        {
            var weight = 0f;
            for (var x = fromX; x <= toX; x++)
                weight += AlphaAt(image, x, y) / 255f;

            return weight;
        }

        /// <summary>Runs of solid pixels along row <paramref name="y"/> - the marks a horizontal cut hits.</summary>
        private static int SolidRunsInRow(IconImage image, int y)
        {
            var runs = 0;
            var inRun = false;
            for (var x = 0; x < image.Width; x++)
            {
                var solid = AlphaAt(image, x, y) >= SolidAlpha;
                if (solid && !inRun)
                    runs++;

                inRun = solid;
            }

            return runs;
        }

        /// <summary>Runs of pixels down column <paramref name="x"/> painted in <paramref name="color"/>'s tone.</summary>
        private static int ToneBandsInColumn(IconImage image, int x, uint color)
        {
            var bands = 0;
            var inBand = false;
            for (var y = 0; y < image.Height; y++)
            {
                var offset = y * image.Stride + x * IconImage.BytesPerPixel;
                var matches =
                    image.Bgra[offset] == (byte)(color & 0xFF) &&
                    image.Bgra[offset + 1] == (byte)((color >> 8) & 0xFF) &&
                    image.Bgra[offset + 2] == (byte)((color >> 16) & 0xFF) &&
                    image.Bgra[offset + 3] == 255;

                if (matches && !inBand)
                    bands++;

                inBand = matches;
            }

            return bands;
        }

        /// <summary>Separate connected marks of solid ink - 16 dashes read as 16, a closed outline as one.</summary>
        private static int SolidMarks(IconImage image)
        {
            var seen = new bool[image.Width * image.Height];
            var marks = 0;

            bool Solid(int x, int y) => AlphaAt(image, x, y) >= SolidAlpha;

            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    if (!Solid(x, y) || seen[y * image.Width + x])
                        continue;

                    marks++;
                    var pending = new Stack<(int X, int Y)>();
                    pending.Push((x, y));
                    while (pending.Count > 0)
                    {
                        var (cx, cy) = pending.Pop();
                        if (cx < 0 || cy < 0 || cx >= image.Width || cy >= image.Height)
                            continue;
                        if (seen[cy * image.Width + cx] || !Solid(cx, cy))
                            continue;

                        seen[cy * image.Width + cx] = true;
                        pending.Push((cx + 1, cy));
                        pending.Push((cx - 1, cy));
                        pending.Push((cx, cy + 1));
                        pending.Push((cx, cy - 1));
                    }
                }
            }

            return marks;
        }

        [Test]
        public void Chip_Sizes_Ask_For_The_Compact_Detail_Level_Without_The_Caller_Saying_So()
        {
            TypeIconRenderer.DetailFor(18).Should().Be(TypeIconDetail.Compact);
            TypeIconRenderer.DetailFor(22).Should().Be(TypeIconDetail.Compact);
            TypeIconRenderer.DetailFor(TypeIconRenderer.CompactSizeThreshold - 1).Should().Be(TypeIconDetail.Compact);
            TypeIconRenderer.DetailFor(TypeIconRenderer.CompactSizeThreshold).Should().Be(TypeIconDetail.Full);
            TypeIconRenderer.DetailFor(TileSize).Should().Be(TypeIconDetail.Full);
        }

        [Test]
        [TestCaseSource(nameof(PaletteTypes))]
        public void Every_Palette_Type_Draws_A_Symbol_At_Every_Chip_Size(ResourceType type)
        {
            foreach (var size in ChipSizes)
            {
                var image = TypeIconRenderer.Render(type, size);

                image.Width.Should().Be(size);
                image.Height.Should().Be(size);
                CountPixels(image, minimumAlpha: 1).Should().BeGreaterThan(size * size / 8,
                    because: $"the {type} chip at {size}px should be a symbol, not a few stray pixels");
            }
        }

        [Test]
        [TestCaseSource(nameof(PaletteTypes))]
        public void Chip_Symbols_Leave_The_Button_Surface_Showing(ResourceType type)
        {
            var image = TypeIconRenderer.Render(type, ChipSize);

            CountPixels(image, minimumAlpha: 1).Should().BeLessThan(ChipSize * ChipSize,
                because: "a chip that fills its box is a block, not an icon");
        }

        [Test]
        public void Every_Type_Is_Still_Distinguishable_At_Chip_Size()
        {
            var rendered = PaletteTypes
                .Select(type => Convert.ToHexString(TypeIconRenderer.Render(type, ChipSize).Bgra))
                .ToList();

            rendered.Distinct().Should().HaveCount(PaletteTypes.Length,
                because: "simplifying a symbol must not simplify it into one of its neighbours");
        }

        [Test]
        [TestCaseSource(nameof(PaletteTypes))]
        public void Chip_Symbols_Are_Drawn_In_Solid_Ink_Rather_Than_Ghost_Pixels(ResourceType type)
        {
            foreach (var size in ChipSizes)
            {
                var image = TypeIconRenderer.Render(type, size);

                CountPixels(image, SolidAlpha).Should().BeGreaterThan(size,
                    because: $"the {type} chip at {size}px is only visible where its marks are opaque");
            }
        }

        [Test]
        public void A_Chip_Stroke_Stays_Wide_Enough_To_See()
        {
            // The waypoint's pole is the thinnest mark in the set and the only thing holding its pennant up,
            // so it is the honest place to check that the compact stroke width survived the size drop.
            foreach (var size in ChipSizes)
            {
                var image = TypeIconRenderer.Render(ResourceType.Utw, size);
                var row = (int)(size * 0.8f);

                InkWeightInRow(image, row, 0, size - 1).Should().BeGreaterThan(1.4f,
                    because: $"the pole should still be about {size * 0.095f:0.0}px of ink at {size}px");
            }
        }

        [Test]
        public void The_Trigger_Boundary_Turns_Solid_At_Chip_Size()
        {
            var tile = TypeIconRenderer.Render(ResourceType.Utt, TileSize);
            var chip = TypeIconRenderer.Render(ResourceType.Utt, ChipSize);

            SolidMarks(tile).Should().BeGreaterThan(8,
                because: "the tile keeps the dashes that say a trigger is not a solid object");
            SolidMarks(chip).Should().Be(1,
                because: "sub-pixel dashes average out to a grey haze, so the chip draws one closed boundary");
        }

        [Test]
        public void The_Coin_Stack_Sheds_A_Coin_At_Chip_Size()
        {
            var centre = TileSize / 2;
            var tile = TypeIconRenderer.Render(ResourceType.Utm, TileSize);
            var chip = TypeIconRenderer.Render(ResourceType.Utm, ChipSize);

            ToneBandsInColumn(tile, centre, TypeIconPalette.Default.Stroke).Should().Be(3);
            ToneBandsInColumn(chip, ChipSize / 2, TypeIconPalette.Default.Stroke).Should().Be(2,
                because: "a third coin face would have under two pixels of bright ellipse left to show");
        }

        [Test]
        public void The_Speaker_Keeps_One_Wave_At_Chip_Size()
        {
            // A cut through the middle crosses the cone and then one mark per wave.
            SolidRunsInRow(TypeIconRenderer.Render(ResourceType.Uts, TileSize), TileSize / 2).Should().Be(3);
            SolidRunsInRow(TypeIconRenderer.Render(ResourceType.Uts, ChipSize), ChipSize / 2).Should().Be(2,
                because: "two waves a pixel apart merge into a blob, so the chip draws one");
        }

        [Test]
        [TestCaseSource(nameof(TileCoverage))]
        public void Tile_Symbols_Are_Untouched_By_The_Chip_Variants((ResourceType Type, int Coverage) expected)
        {
            var image = TypeIconRenderer.Render(expected.Type, TileSize);

            CountPixels(image, minimumAlpha: 1).Should().BeCloseTo(expected.Coverage, delta: 2,
                because: $"the {expected.Type} palette tile should look exactly as it did before");
        }
    }
}
